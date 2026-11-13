using System.Collections.Concurrent;
using System.Threading;

namespace ZCraft.Source.World;

public static class ChunkDataPool
{
    private const int MaxPooled = 1024;
    private static readonly ConcurrentBag<ChunkData> _pool = new();
    private static int _count;

    public static ChunkData Rent()
    {
        if (_pool.TryTake(out var data))
        {
            Interlocked.Decrement(ref _count);
            return data;
        }
        return new ChunkData();
    }

    public static void Return(ChunkData data)
    {
        data.Clear();
        if (Interlocked.Increment(ref _count) <= MaxPooled)
            _pool.Add(data);
        else
            Interlocked.Decrement(ref _count);
    }

    public static void Clear()
    {
        while (_pool.TryTake(out _)) { }
        Interlocked.Exchange(ref _count, 0);
    }
}
