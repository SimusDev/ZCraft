using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using ZCraft.Source.Networking;

static class Program
{
    static readonly PacketBatchPipeline.ReceiveHandler IgnorePacket = (_, _) => { };
    static readonly PacketBatchPipeline.SendHandler IgnoreSend = (_, _, _, _) => { };
    static readonly Action<int, Exception> Fail = (_, e) => throw new Exception("Unexpected transport fault", e);

    static void Main()
    {
        BatchBoundaries();
        CompressionAndOversized();
        RouteSpecificAccumulation();
        RouteOrderAndOwnership();
        ControlledRouteScheduling();
        IncomingRouteScheduling();
        RouteDisconnectAndLimits();
        ParallelProducers();
        MalformedInput();
        LifetimeAndBackpressure();
        MeasureProducerAllocations();
        Console.WriteLine("All transport checks passed.");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Until(Func<bool> done, Action pump)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            pump();
            if (clock.Elapsed.TotalSeconds > 15) throw new Exception("Timed out waiting for workers.");
            Thread.Yield();
        }
    }

    static List<byte[]> Encode(PacketBatchPipeline pipeline)
    {
        var result = new List<byte[]>();
        pipeline.Flush();
        PacketBatchPipeline.SendHandler capture = (_, data, _, _) => result.Add(data.ToArray());
        Until(() => pipeline.QueuedBatches == 0, () => pipeline.Pump(capture, IgnorePacket, Fail));
        Check(pipeline.BufferedBytes == 0, "Send buffers were not released.");
        return result;
    }

    static List<byte[]> Decode(List<byte[]> frames)
    {
        using var receiver = new PacketBatchPipeline(512);
        var packets = new List<byte[]>();
        int ownerThread = Environment.CurrentManagedThreadId;
        foreach (byte[] frame in frames) receiver.Receive(7, frame);
        PacketBatchPipeline.ReceiveHandler capture = (peer, data) =>
        {
            Check(Environment.CurrentManagedThreadId == ownerThread, "Callback ran on a worker.");
            Check(peer == 7, "Lost peer id.");
            packets.Add(data.ToArray());
        };
        Until(() => receiver.QueuedBatches == 0, () => receiver.Pump(IgnoreSend, capture, Fail));
        Check(receiver.BufferedBytes == 0, "Receive buffers were not released.");
        return packets;
    }

    static void BatchBoundaries()
    {
        using var sender = new PacketBatchPipeline(int.MaxValue);
        int sent = 0;
        sender.Send(7, new byte[10], 2, 0, false);
        sender.Pump((_, _, _, _) => sent++, IgnorePacket, Fail);
        Check(sent == 0, "Partial batch was sent before flush.");
        Check(Decode(Encode(sender)).Single().Length == 10, "Partial batch failed.");

        // No arbitrary packet-count cap: hundreds of zero-length messages fit into one batch.
        int emptyCount = (PacketBatchPipeline.Mtu - PacketBatchPipeline.HeaderSize) / PacketBatchPipeline.LengthSize;
        for (int i = 0; i < emptyCount; i++) sender.Send(7, ReadOnlySpan<byte>.Empty, 2, 0, false);
        List<byte[]> frames = Encode(sender);
        Check(frames.Count == 1 && frames[0].Length == PacketBatchPipeline.HeaderSize + emptyCount * 4,
            "Packet-count cap or incorrect framing.");
        Check(Decode(frames).Count == emptyCount, "Lost empty packets.");

        sender.Send(7, new byte[PacketBatchPipeline.Mtu - PacketBatchPipeline.HeaderSize - 4], 2, 0, false);
        Until(() => sender.QueuedBatches == 0,
            () => sender.Pump((_, data, _, _) => { Check(data.Length == 1350, "MTU mismatch."); sent++; }, IgnorePacket, Fail));
        Check(sent == 1, "Exact MTU did not seal automatically.");

        sender.Send(7, new byte[800], 2, 0, false);
        sender.Send(7, new byte[800], 2, 0, false);
        frames = Encode(sender);
        Check(frames.Count == 2 && frames.All(f => f.Length <= 1350), "Overflow mixed two MTU batches.");

        sender.Send(7, new byte[1], 2, 0, true);
        Until(() => sender.QueuedBatches == 0,
            () => sender.Pump((_, _, _, _) => sent++, IgnorePacket, Fail));
        Check(sent == 2, "Immediate packet waited for Flush.");
        Console.WriteLine("PASS batching, empty packets, exact MTU, overflow, immediate");
    }

    static void CompressionAndOversized()
    {
        byte[] repeated = Enumerable.Repeat((byte)42, 512).ToArray();
        using var sender = new PacketBatchPipeline(512);
        sender.Send(7, repeated, 2, 1, true);
        List<byte[]> frames = Encode(sender);
        Check((frames[0][3] & 1) == 1 && frames[0].Length < repeated.Length, "Threshold compression failed.");
        Check(Decode(frames)[0].SequenceEqual(repeated), "Compressed round trip failed.");

        using var belowThreshold = new PacketBatchPipeline(517);
        belowThreshold.Send(7, repeated, 2, 1, true);
        Check((Encode(belowThreshold)[0][3] & 1) == 0, "Compressed below the framed payload threshold.");

        byte[] random = new byte[PacketBatchPipeline.MaxPacketSize];
        new Random(251).NextBytes(random);
        sender.Send(7, new byte[] { 1 }, 2, 0, false);
        sender.Send(7, random, 2, 0, false);
        sender.Send(7, new byte[] { 3 }, 2, 0, false);
        frames = Encode(sender);
        Check(frames.Count == 3, "Oversized packet was not isolated.");
        Check((frames[1][3] & 1) == 0 && frames[1].Length == random.Length + PacketBatchPipeline.HeaderSize + 4,
            "Incompressible data expanded.");
        List<byte[]> decoded = Decode(frames);
        Check(decoded[0][0] == 1 && decoded[1].SequenceEqual(random) && decoded[2][0] == 3,
            "Parallel codecs reordered batches.");

        byte[] largeCompressible = new byte[128 * 1024];
        sender.Send(7, largeCompressible, 2, 0, true);
        frames = Encode(sender);
        Check((frames[0][3] & 1) == 1 && Decode(frames)[0].Length == largeCompressible.Length,
            "Oversized compression failed.");
        Console.WriteLine("PASS threshold, compression, oversized packets, incompressible fallback");
    }

    static void RouteOrderAndOwnership()
    {
        using var sender = new PacketBatchPipeline(0);
        var expected = new List<(int Peer, int Mode, int Channel, byte Value)>();
        for (int i = 0; i < 200; i++)
        {
            var route = (Peer: i % 3, Mode: i % 3, Channel: i % 4, Value: (byte)i);
            expected.Add(route);
            byte[] data = { (byte)i };
            sender.Send(route.Peer, data, route.Mode, route.Channel, i % 2 == 0);
            data[0] = 255; // Caller owns/reuses its memory immediately after Send.
        }
        sender.Flush();
        var frames = new List<(int Peer, int Mode, int Channel, byte[] Data)>();
        PacketBatchPipeline.SendHandler send = (peer, data, mode, channel) =>
        {
            Check((data[3] >> 1) == mode && BinaryPrimitives.ReadInt32LittleEndian(data.Slice(8)) == channel,
                "Wire route metadata differs from SendBytes arguments.");
            frames.Add((peer, mode, channel, data.ToArray()));
        };
        Until(() => sender.QueuedBatches == 0, () => sender.Pump(send, IgnorePacket, Fail));

        var expectedByRoute = expected
            .GroupBy(value => (value.Peer, value.Mode, value.Channel))
            .ToDictionary(group => group.Key, group => group.Select(value => value.Value).ToArray());
        var actualByRoute = frames
            .GroupBy(frame => (frame.Peer, frame.Mode, frame.Channel))
            .ToDictionary(group => group.Key, group =>
                Decode(group.Select(frame => frame.Data).ToList()).Select(packet => packet.Single()).ToArray());

        Check(expectedByRoute.Count == actualByRoute.Count, "A route disappeared or an unexpected route appeared.");
        foreach (var pair in expectedByRoute)
        {
            Check(actualByRoute.TryGetValue(pair.Key, out byte[] actual), "A route disappeared.");
            Check(actual.SequenceEqual(pair.Value), "Payload/order corrupted inside a route.");
        }
        Console.WriteLine("PASS per-route ordering, compressed metadata and caller buffer ownership");
    }

    static void RouteSpecificAccumulation()
    {
        using var sender = new PacketBatchPipeline(int.MaxValue);
        sender.Send(10, new byte[] { 1 }, 2, 0, false);
        sender.Send(20, new byte[] { 2 }, 2, 0, false);
        sender.Send(10, new byte[] { 3 }, 2, 0, false);

        var frames = new List<(int Peer, byte[] Data)>();
        sender.Flush();
        Until(() => sender.QueuedBatches == 0,
            () => sender.Pump((peer, data, _, _) => frames.Add((peer, data.ToArray())), IgnorePacket, Fail));

        Check(frames.Count == 2, "Different routes were merged or forced to flush each other.");
        var route10 = frames.Single(frame => frame.Peer == 10);
        var route20 = frames.Single(frame => frame.Peer == 20);
        Check(ReadFramePayloads(new[] { route10.Data }).SequenceEqual(new byte[] { 1, 3 }),
            "Peer 10 did not keep its own accumulator.");
        Check(ReadFramePayloads(new[] { route20.Data }).SequenceEqual(new byte[] { 2 }),
            "Peer 20 did not keep its own accumulator.");
        Console.WriteLine("PASS independent accumulators for A -> B -> A");
    }

    static byte[] ReadFramePayloads(IEnumerable<byte[]> frames)
    {
        var result = new List<byte>();
        foreach (byte[] frame in frames)
        {
            int offset = PacketBatchPipeline.HeaderSize;
            while (offset < frame.Length)
            {
                int length = BinaryPrimitives.ReadInt32LittleEndian(frame.AsSpan(offset));
                offset += 4;
                Check(length >= 0 && offset + length <= frame.Length, "Invalid test frame.");
                Check(length == 1, "Route test expected one-byte payloads.");
                result.Add(frame[offset]);
                offset += length;
            }
        }

        return result.ToArray();
    }

    static void ControlledRouteScheduling()
    {
        var work = new List<IThreadPoolWorkItem>();
        using var sender = new PacketBatchPipeline(int.MaxValue, schedule: work.Add);
        sender.Send(10, new byte[] { 1 }, 2, 1, true); // A1: deliberately stalled
        sender.Send(10, new byte[] { 2 }, 2, 1, true); // A2: completes first
        sender.Send(20, new byte[] { 3 }, 2, 1, true); // Different peer
        sender.Send(10, new byte[] { 4 }, 2, 2, true); // Different channel
        sender.Send(10, new byte[] { 5 }, 0, 1, true); // Different mode
        var delivered = new List<byte>();
        PacketBatchPipeline.SendHandler capture = (_, data, _, _) => delivered.Add(data[PacketBatchPipeline.HeaderSize + 4]);
        for (int i = 1; i < work.Count; i++) work[i].Execute();
        sender.Pump(capture, IgnorePacket, Fail);
        Check(delivered.SequenceEqual(new byte[] { 3, 4, 5 }),
            "Stalled A1 blocked another route or let A2 overtake it.");
        work[0].Execute();
        sender.Pump(capture, IgnorePacket, Fail, maxBatches: 1);
        Check(delivered[^1] == 1 && sender.QueuedBatches == 1, "Per-tick budget or A1 ordering failed.");
        sender.Pump(capture, IgnorePacket, Fail);
        Check(delivered.SequenceEqual(new byte[] { 3, 4, 5, 1, 2 }), "FIFO failed after head completion.");
        Check(sender.BufferedBytes == 0, "Route scheduler leaked buffers.");

        // With several ready heads, take one batch from each route per round.
        work.Clear();
        delivered.Clear();
        sender.Send(10, new byte[] { 1 }, 2, 1, true);
        sender.Send(10, new byte[] { 2 }, 2, 1, true);
        sender.Send(20, new byte[] { 3 }, 2, 1, true);
        sender.Send(20, new byte[] { 4 }, 2, 1, true);
        foreach (var item in work) item.Execute();
        sender.Pump(capture, IgnorePacket, Fail, maxBatches: 2);
        Check(delivered.SequenceEqual(new byte[] { 1, 3 }), "A hot route starved another ready route.");
        sender.Pump(capture, IgnorePacket, Fail);
        Check(delivered.SequenceEqual(new byte[] { 1, 3, 2, 4 }), "Round-robin scheduler lost FIFO.");

        // sendImmediate closes ONLY its route; changing mode/channel also keeps partial batches open.
        work.Clear();
        sender.Send(10, new byte[] { 1 }, 2, 1, false);
        sender.Send(10, new byte[] { 2 }, 0, 1, false);
        sender.Send(10, new byte[] { 3 }, 2, 2, false);
        sender.Send(20, new byte[] { 4 }, 2, 1, true);
        Check(work.Count == 1, "Immediate send flushed unrelated accumulators.");
        work[0].Execute();
        sender.Pump(IgnoreSend, IgnorePacket, Fail);
        Check(sender.QueuedBatches == 3, "Unrelated partial batches were consumed.");
        work.Clear();
        sender.Flush();
        Check(work.Count == 3, "Flush did not seal all partial routes.");
        foreach (var item in work) item.Execute();
        sender.Pump(IgnoreSend, IgnorePacket, Fail);
        Check(sender.QueuedBatches == 0 && sender.BufferedBytes == 0, "Flush leaked route state.");
        Console.WriteLine("PASS controlled completion: per-route FIFO, independent peers/channels/modes, fairness, immediate");
    }

    static void IncomingRouteScheduling()
    {
        var work = new List<IThreadPoolWorkItem>();
        using var receiver = new PacketBatchPipeline(512, schedule: work.Add);
        var decoded = new List<byte>();
        PacketBatchPipeline.ReceiveHandler capture = (_, data) => decoded.Add(data[0]);
        receiver.Receive(7, MakeFrame(1, mode: 2, channel: 1));
        receiver.Receive(7, MakeFrame(2, mode: 2, channel: 1));
        receiver.Receive(7, MakeFrame(3, mode: 2, channel: 2));
        receiver.Receive(7, MakeFrame(4, mode: 0, channel: 1));
        receiver.Receive(8, MakeFrame(5, mode: 2, channel: 1));
        for (int i = 1; i < work.Count; i++) work[i].Execute();
        receiver.Pump(IgnoreSend, capture, Fail);
        Check(decoded.SequenceEqual(new byte[] { 3, 4, 5 }), "Slow decoder blocked unrelated incoming routes.");
        work[0].Execute();
        receiver.Pump(IgnoreSend, capture, Fail);
        Check(decoded.SequenceEqual(new byte[] { 3, 4, 5, 1, 2 }), "Incoming route FIFO broken.");

        // A malformed head reports once, releases its buffers, and doesn't leave its follower stuck.
        work.Clear();
        byte[] broken = MakeFrame(6, 2, 1);
        BinaryPrimitives.WriteInt32LittleEndian(broken.AsSpan(PacketBatchPipeline.HeaderSize), -1);
        receiver.Receive(7, broken);
        receiver.Receive(7, MakeFrame(7, 2, 1));
        work[1].Execute();
        work[0].Execute();
        int faults = 0;
        receiver.Pump(IgnoreSend, capture, (_, _) => faults++);
        Check(faults == 1 && decoded[^1] == 7 && receiver.BufferedBytes == 0, "Fault stalled a route.");
        Console.WriteLine("PASS incoming route isolation and malformed head recovery");
    }

    static byte[] MakeFrame(byte value, int mode, int channel)
    {
        using var sender = new PacketBatchPipeline(int.MaxValue);
        sender.Send(1, new byte[] { value }, mode, channel, true);
        return Encode(sender).Single();
    }

    static void RouteDisconnectAndLimits()
    {
        var work = new List<IThreadPoolWorkItem>();
        using var pipeline = new PacketBatchPipeline(512, schedule: work.Add);
        pipeline.Send(7, new byte[] { 1 }, 2, 1, true); // in flight at disconnect
        pipeline.Send(7, new byte[] { 2 }, 2, 1, true); // ready but blocked by previous batch
        pipeline.Send(7, new byte[] { 3 }, 2, 2, false); // partial
        pipeline.Send(8, new byte[] { 4 }, 2, 1, true);
        pipeline.Receive(7, MakeFrame(5, 2, 2));
        work[1].Execute();
        work[2].Execute();
        pipeline.DropPeer(7);
        Check(pipeline.RouteCount == 1, "Disconnected peer's route metadata retained.");
        work[0].Execute(); // old jobs must return buffers exactly once
        work[3].Execute();
        var output = new List<byte>();
        pipeline.Pump((peer, data, _, _) =>
        {
            Check(peer == 8, "Disconnected peer was sent stale data.");
            output.Add(data[PacketBatchPipeline.HeaderSize + 4]);
        }, (_, _) => throw new Exception("Stale incoming packet"), Fail);
        Check(output.SequenceEqual(new byte[] { 4 }) && pipeline.BufferedBytes == 0, "Disconnect leaked work.");

        work.Clear();
        pipeline.Send(7, new byte[] { 6 }, 2, 1, true);
        work[0].Execute();
        pipeline.Pump((_, data, _, _) => output.Add(data[PacketBatchPipeline.HeaderSize + 4]), IgnorePacket, Fail);
        Check(output[^1] == 6, "Reconnect reused a cancelled route.");

        // Completed/partial/in-flight jobs across both directions, including a live callback.
        work.Clear();
        pipeline.Send(9, new byte[] { 7 }, 2, 1, true);
        pipeline.Send(10, new byte[] { 8 }, 2, 1, true);
        pipeline.Receive(11, MakeFrame(9, 2, 1));
        pipeline.Send(12, new byte[] { 10 }, 2, 1, false);
        work[0].Execute();
        pipeline.Pump((_, _, _, _) => pipeline.Dispose(), IgnorePacket, Fail);
        work[1].Execute();
        work[2].Execute();
        Check(pipeline.RouteCount == 0 && pipeline.QueuedBatches == 0 && pipeline.BufferedBytes == 0,
            "Shutdown left route metadata or in-flight buffers.");

        using var bounded = new PacketBatchPipeline(512, maxQueuedBatches: 2);
        for (int peer = 1; peer <= 2; peer++)
        {
            bounded.Send(peer, new byte[] { 1 }, 2, 1, true);
            Encode(bounded);
        }
        bool rejected = false;
        try { bounded.Send(3, new byte[] { 2 }, 2, 1, true); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected && bounded.RouteCount == 2 && bounded.BufferedBytes == 0, "Route metadata grew without a limit.");
        bounded.DropPeer(1);
        bounded.Send(3, new byte[] { 2 }, 2, 1, true);
        Encode(bounded);
        Check(bounded.RouteCount == 2, "Disconnected route capacity was not reusable.");
        Console.WriteLine("PASS disconnect/reconnect, mixed in-flight shutdown, metadata bounds");
    }

    static void ParallelProducers()
    {
        using var sender = new PacketBatchPipeline(512, maxQueuedBatches: 12000, maxBufferedBytes: 64 * 1024 * 1024);
        Parallel.For(0, 8, producer =>
        {
            Span<byte> packet = stackalloc byte[8];
            for (int sequence = 0; sequence < 1000; sequence++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(packet, producer);
                BinaryPrimitives.WriteInt32LittleEndian(packet.Slice(4), sequence);
                sender.Send(7, packet, 2, 1, sequence % 5 == 0);
                if (sequence % 11 == 0) sender.Flush();
            }
        });
        List<byte[]> packets = Decode(Encode(sender));
        int[] next = new int[8];
        foreach (byte[] packet in packets)
        {
            int producer = BinaryPrimitives.ReadInt32LittleEndian(packet);
            int sequence = BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(4));
            Check(sequence == next[producer]++, "Concurrent send lost per-producer order.");
        }
        Check(next.All(n => n == 1000), "Concurrent send lost packets.");
        Console.WriteLine("PASS 8 concurrent producers / 8000 packets");
    }

    static void MalformedInput()
    {
        using var sender = new PacketBatchPipeline(int.MaxValue);
        sender.Send(7, new byte[] { 1 }, 2, 0, false);
        sender.Send(7, new byte[] { 2 }, 2, 0, false);
        byte[] valid = Encode(sender)[0];
        byte[] malformed = (byte[])valid.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(PacketBatchPipeline.HeaderSize + 5), -1);
        Reject(malformed); // Even the valid first record must not reach the application.
        Reject(valid.AsSpan(0, valid.Length - 1).ToArray());
        malformed = (byte[])valid.Clone();
        malformed[2] = 200;
        Reject(malformed);
        malformed = (byte[])valid.Clone();
        malformed[2] = 1; // Previous wire format cannot identify the incoming route.
        Reject(malformed);
        malformed = (byte[])valid.Clone();
        malformed[3] = 6; // Unknown transfer mode.
        Reject(malformed);
        malformed = (byte[])valid.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(8), -1);
        Reject(malformed);
        malformed = (byte[])valid.Clone();
        BinaryPrimitives.WriteInt32LittleEndian(malformed.AsSpan(4), int.MaxValue);
        Reject(malformed);

        using var compressed = new PacketBatchPipeline(0);
        compressed.Send(7, new byte[4096], 2, 0, true);
        byte[] packed = Encode(compressed)[0];
        Reject(packed.AsSpan(0, packed.Length - 1).ToArray());
        Reject(packed.Concat(new byte[] { 0 }).ToArray());
        Console.WriteLine("PASS malformed lengths, versions, truncated/trailing compressed data");
    }

    static void Reject(byte[] frame)
    {
        using var receiver = new PacketBatchPipeline(512);
        int faults = 0, delivered = 0;
        try { receiver.Receive(7, frame); }
        catch (InvalidDataException) { faults++; }
        Until(() => receiver.QueuedBatches == 0,
            () => receiver.Pump(IgnoreSend, (_, _) => delivered++, (_, _) => faults++));
        Check(faults == 1 && delivered == 0 && receiver.BufferedBytes == 0, "Bad frame was delivered or leaked.");
    }

    static void LifetimeAndBackpressure()
    {
        using var limited = new PacketBatchPipeline(512, maxBufferedBytes: 4096, maxQueuedBatches: 1);
        limited.Send(7, new byte[] { 1 }, 2, 0, true);
        bool rejected = false;
        try { limited.Send(7, new byte[] { 2 }, 2, 0, true); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected, "Backpressure did not reject new input.");
        Check(Decode(Encode(limited)).Single()[0] == 1, "Backpressure lost already accepted packet.");

        using var dropped = new PacketBatchPipeline(0);
        dropped.Send(7, new byte[1000], 2, 0, true);
        dropped.Send(7, new byte[100], 2, 0, false);
        dropped.DropPeer(7);
        Check(Encode(dropped).Count == 0, "Disconnected peer still received queued packets.");

        for (int round = 0; round < 30; round++)
        {
            var pipeline = new PacketBatchPipeline(0);
            for (int i = 0; i < 32; i++) pipeline.Send(7, new byte[32768], 2, 0, true);
            pipeline.Dispose();
            pipeline.Dispose();
            Until(() => pipeline.QueuedBatches == 0, () => Thread.Yield());
            Check(pipeline.BufferedBytes == 0, "Shutdown leaked in-flight buffers.");
        }

        using var received = new PacketBatchPipeline(512);
        using var source = new PacketBatchPipeline(int.MaxValue);
        for (int i = 0; i < 10; i++) source.Send(7, new byte[] { (byte)i }, 2, 0, false);
        received.Receive(7, Encode(source)[0]);
        int count = 0;
        Until(() => received.QueuedBatches == 0,
            () => received.Pump(IgnoreSend, (_, _) => { count++; received.Dispose(); }, Fail));
        Check(count == 1 && received.BufferedBytes == 0, "Shutdown from callback did not cancel/release the batch.");

        using var disconnected = new PacketBatchPipeline(512);
        for (int i = 0; i < 10; i++) source.Send(7, new byte[] { (byte)i }, 2, 0, false);
        disconnected.Receive(7, Encode(source)[0]);
        count = 0;
        Until(() => disconnected.QueuedBatches == 0,
            () => disconnected.Pump(IgnoreSend, (_, _) => { count++; disconnected.DropPeer(7); }, Fail));
        Check(count == 1 && disconnected.BufferedBytes == 0, "Disconnect during callback delivered stale packets.");
        Console.WriteLine("PASS backlog limits, peer disconnect, shutdown races and callback shutdown");
    }

    static void MeasureProducerAllocations()
    {
        using var pipeline = new PacketBatchPipeline(int.MaxValue);
        byte[] packet = new byte[32];
        for (int i = 0; i < 2000; i++)
        {
            pipeline.Send(7, packet, 2, 0, true);
            while (pipeline.QueuedBatches != 0) { pipeline.Pump(IgnoreSend, IgnorePacket, Fail); Thread.Yield(); }
        }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        {
            pipeline.Send(7, packet, 2, 0, true);
            while (pipeline.QueuedBatches != 0) { pipeline.Pump(IgnoreSend, IgnorePacket, Fail); Thread.Yield(); }
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine($"Producer/main thread after warmup, 10000 send/pump cycles: {bytes} managed bytes (workers/native memory excluded).");
    }
}
