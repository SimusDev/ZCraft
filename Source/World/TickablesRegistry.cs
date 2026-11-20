using System;
using System.Collections.Generic;

namespace ZCraft.Source.World;

public sealed class TickablesRegistry
{
    private readonly List<ITickable> _dense = new(1024);
    private readonly Dictionary<ITickable, int> _index =
        new(ReferenceEqualityComparer.Instance);

    public int Count => _dense.Count;

    public void Add(ITickable t)
    {
        if (t is null) return;
        if (!_index.TryAdd(t, _dense.Count)) return;
        _dense.Add(t);
        ZCraftWorldDebug.TickableObjects++;
    }

    public void Remove(ITickable t)
    {
        if (t is null) return;
        if (!_index.Remove(t, out int idx)) return; 

        int last = _dense.Count - 1;
        if (idx != last)
        {
            var moved = _dense[last];
            _dense[idx] = moved;
            _index[moved] = idx;     
        }
        _dense.RemoveAt(last);
        ZCraftWorldDebug.TickableObjects--;
    }

    public void Tick(double dt)
    {
        var d = _dense;
        for (int i = d.Count - 1; i >= 0; i--)
        {
            if (i >= d.Count) continue;
            d[i].OnTick(dt);
        }
    }
}