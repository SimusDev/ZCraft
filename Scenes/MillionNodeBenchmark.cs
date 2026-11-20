using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class MillionNodeBenchmark : Node
{
    // 1) Теоретический минимум: пустой managed-класс.
    //    Только GC-заголовок и указатель в массиве.
    private class EmptyNode { }

    // 2) "Настоящий" лёгкий узел на чистом C#.
    //    Поля примерно соответствуют тому, что есть у Godot Node,
    //    но всё это живёт ТОЛЬКО в managed-куче. Никакого P/Invoke.
    private class LightWeightNode
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale = Vector3.One;
        public string Name = "";
        public LightWeightNode Parent;
        public List<LightWeightNode> Children;
        public bool IsActive = true;
        public int Layer = 0;
    }

    public override void _Ready()
    {
        const int count = 1_000_000;
        GD.Print($"=== Бенчмарк: {count:N0} объектов ===\n");

        Warmup();

        TestEmptyClass(count);
        TestLightWeightNode(count);
        TestGodotNode(count);
        TestGodotNodeWithParent(100_000); // AddChild тяжелее — 100k достаточно
    }

    private void Warmup()
    {
        // Прогреваем JIT, чтобы первый тест не платил за компиляцию
        for (int i = 0; i < 10_000; i++)
        {
            _ = new EmptyNode();
            _ = new LightWeightNode();
        }
        for (int i = 0; i < 100; i++)
        {
            var n = new Node();
            n.Dispose();
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private void TestEmptyClass(int count)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var arr = new EmptyNode[count];
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++) arr[i] = new EmptyNode();
        sw.Stop();
        PrintResult("Empty C# class", count, sw.Elapsed);
        arr = null;
    }

    private void TestLightWeightNode(int count)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var arr = new LightWeightNode[count];
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++) arr[i] = new LightWeightNode();
        sw.Stop();
        PrintResult("LightWeightNode (pure C#)", count, sw.Elapsed);
        arr = null;
    }

    private void TestGodotNode(int count)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var arr = new Node[count];
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++) arr[i] = new Node();
        sw.Stop();
        PrintResult("Godot Node (new only)", count, sw.Elapsed);
        for (int i = 0; i < count; i++) arr[i].Dispose();
        arr = null;
    }

    private void TestGodotNodeWithParent(int count)
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var root = new Node();
        AddChild(root);
        var arr = new Node[count];
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var n = new Node();
            root.AddChild(n);
            arr[i] = n;
        }
        sw.Stop();
        PrintResult("Godot Node + AddChild", count, sw.Elapsed);
        root.QueueFree();
        arr = null;
    }

    private void PrintResult(string name, int count, TimeSpan elapsed)
    {
        double totalMs = elapsed.TotalMilliseconds;
        double perItemNs = totalMs * 1_000_000.0 / count; // 1 мс = 1e6 нс
        double perItemUs = perItemNs / 1000.0;
        double perSec = count / elapsed.TotalSeconds;

        GD.Print($"--- {name} ---");
        GD.Print($"  Всего:     {totalMs:F3} мс");
        GD.Print($"  На объект: {perItemNs:F1} нс  ({perItemUs:F4} мкс)");
        GD.Print($"  Скорость:  {perSec:N0} объектов/сек");
        GD.Print("");
    }
}