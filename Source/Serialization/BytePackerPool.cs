using System;
using System.Collections.Generic;

namespace ZCraft.Source.Serialization;

public static class BytePackerPool
{
    private const int DefaultInitialSize = 1024;

    [ThreadStatic] private static Stack<BytePacker> _pool;

    public static BytePacker Rent()
    {
        Stack<BytePacker> pool = _pool ??= new Stack<BytePacker>();
        if (pool.Count > 0) return pool.Pop();

        BytePacker p = new BytePacker(DefaultInitialSize);
        p.IsFromPool = true;
        return p;
    }

    public static BytePacker Rent(ReadOnlySpan<byte> readData)
    {
        BytePacker packer = Rent();
        packer.Wrap(readData);
        return packer;
    }

    public static void Return(BytePacker p)
    {
        p.Reset();
        Stack<BytePacker> pool = _pool ??= new Stack<BytePacker>();
        pool.Push(p);
    }

    public static void Clear()
    {
        if (_pool == null) return;
        while (_pool.Count > 0) _pool.Pop().Dispose();
        _pool = null;
    }
}