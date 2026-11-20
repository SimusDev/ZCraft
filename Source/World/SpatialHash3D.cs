using System;
using System.Collections.Generic;
using Godot;

namespace ZCraft.Source.World;

public sealed class SpatialHash3D<T> where T : class
{
    private struct Entry
    {
        public T Item;
        public Vector3 Position;
        public Vector3I Cell;
    }

    private sealed class Bucket
    {
        public readonly List<int> Indices = new(8);
        // slot -> индекс в Indices, для O(1) удаления
        public readonly Dictionary<int, int> SlotToIndex = new(8);
    }

    private readonly float _cellSize;
    private readonly float _invCellSize;
    private readonly Func<T, Vector3> _getPosition;

    private Entry[] _entries;
    private int _liveCount;       // сколько живых
    private int _highWaterMark;   // до какого индекса entries реально занято
    private readonly Stack<int> _freeSlots = new();

    private readonly Dictionary<T, int> _itemToSlot;
    private readonly Dictionary<Vector3I, Bucket> _buckets;
    private readonly Stack<Bucket> _bucketPool = new();
    private readonly List<T> _resultBuffer;

    public int Count => _liveCount;

    public SpatialHash3D(
        float cellSize,
        Func<T, Vector3> getPosition,
        int initialCapacity = 256,
        int initialBucketCapacity = 256)
    {
        if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
        _cellSize = cellSize;
        _invCellSize = 1f / cellSize;
        _getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));

        int cap = Math.Max(16, initialCapacity);
        _entries = new Entry[cap];
        _itemToSlot = new Dictionary<T, int>(cap);
        _buckets = new Dictionary<Vector3I, Bucket>(Math.Max(16, initialBucketCapacity));
        _resultBuffer = new List<T>(cap);
    }

    // ---------------------------------------------------------------
    //  Insert / Update / Remove / Clear
    // ---------------------------------------------------------------

    public void Insert(T item, Vector3 position)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (_itemToSlot.ContainsKey(item))
        {
            Update(item, position);
            return;
        }

        Vector3 pos = _getPosition(item);
        Vector3I cell = GetCell(pos);

        int slot = AcquireSlot();
        _entries[slot] = new Entry { Item = item, Position = pos, Cell = cell };
        _itemToSlot[item] = slot;
        AddToBucket(cell, slot);
    }

    public void Update(T item, Vector3 pos)
    {
        if (item is null) return;
        if (!_itemToSlot.TryGetValue(item, out int slot))
        {
            Insert(item, pos);
            return;
        }

        Vector3I newCell = GetCell(pos);
        ref Entry e = ref _entries[slot];

        Vector3I oldCell = e.Cell;
        e.Position = pos;

        if (oldCell.Equals(newCell)) return;

        RemoveFromBucket(oldCell, slot);
        e.Cell = newCell;
        AddToBucket(newCell, slot);
    }

    public bool Remove(T item)
    {
        if (item is null) return false;
        if (!_itemToSlot.TryGetValue(item, out int slot)) return false;

        ref Entry e = ref _entries[slot];
        RemoveFromBucket(e.Cell, slot);

        _itemToSlot.Remove(item);
        e = default;                    // Item = null, Position = 0, Cell = 0
        _freeSlots.Push(slot);
        _liveCount--;

        // Опционально: сжать при сильном разрежении
        // if (_entries.Length > 256 && _liveCount < _entries.Length / 4) Compact();
        return true;
    }

    public bool Contains(T item) => item is not null && _itemToSlot.ContainsKey(item);

    public void Clear()
    {
        foreach (var kv in _buckets)
        {
            kv.Value.Indices.Clear();
            kv.Value.SlotToIndex.Clear();
            _bucketPool.Push(kv.Value);
        }
        _buckets.Clear();
        _itemToSlot.Clear();
        _freeSlots.Clear();
        Array.Clear(_entries, 0, _entries.Length);
        _liveCount = 0;
        _highWaterMark = 0;
    }

    // ---------------------------------------------------------------
    //  Query
    // ---------------------------------------------------------------

    /// <summary>
    /// Возвращает внутренний переиспользуемый буфер. Не сохраняй ссылку!
    /// </summary>
    public List<T> QueryRadius(Vector3 center, float radius)
    {
        _resultBuffer.Clear();
        QueryRadius(center, radius, _resultBuffer);
        return _resultBuffer;
    }

    public void QueryRadius(Vector3 center, float radius, List<T> results)
    {
        if (results is null) throw new ArgumentNullException(nameof(results));
        results.Clear();

        float radiusSq = radius * radius;
        int minX = Mathf.FloorToInt((center.X - radius) * _invCellSize);
        int maxX = Mathf.FloorToInt((center.X + radius) * _invCellSize);
        int minY = Mathf.FloorToInt((center.Y - radius) * _invCellSize);
        int maxY = Mathf.FloorToInt((center.Y + radius) * _invCellSize);
        int minZ = Mathf.FloorToInt((center.Z - radius) * _invCellSize);
        int maxZ = Mathf.FloorToInt((center.Z + radius) * _invCellSize);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            if (!_buckets.TryGetValue(new Vector3I(x, y, z), out var bucket)) continue;
            var indices = bucket.Indices;
            for (int i = 0; i < indices.Count; i++)
            {
                ref Entry e = ref _entries[indices[i]];
                if (e.Item is null) continue;
                if ((e.Position - center).LengthSquared() <= radiusSq)
                    results.Add(e.Item);
            }
        }
    }

    // ---------------------------------------------------------------
    //  Internals
    // ---------------------------------------------------------------

    private Vector3I GetCell(in Vector3 pos) => new Vector3I(
        Mathf.FloorToInt(pos.X * _invCellSize),
        Mathf.FloorToInt(pos.Y * _invCellSize),
        Mathf.FloorToInt(pos.Z * _invCellSize));

    private int AcquireSlot()
    {
        int slot;
        if (_freeSlots.Count > 0)
        {
            slot = _freeSlots.Pop();
        }
        else
        {
            if (_highWaterMark == _entries.Length) Grow();
            slot = _highWaterMark++;
        }
        _liveCount++;
        return slot;
    }

    private void Grow()
    {
        int newCap = _entries.Length * 2;
        Array.Resize(ref _entries, newCap);
    }

    private void AddToBucket(Vector3I cell, int slot)
    {
        if (!_buckets.TryGetValue(cell, out var b))
        {
            b = _bucketPool.Count > 0 ? _bucketPool.Pop() : new Bucket();
            _buckets[cell] = b;
        }
        b.SlotToIndex[slot] = b.Indices.Count;
        b.Indices.Add(slot);
    }

    private void RemoveFromBucket(Vector3I cell, int slot)
    {
        if (!_buckets.TryGetValue(cell, out var b)) return;
        if (!b.SlotToIndex.TryGetValue(slot, out int idx)) return;

        int lastIdx = b.Indices.Count - 1;
        int lastSlot = b.Indices[lastIdx];
        b.Indices[idx] = lastSlot;
        b.SlotToIndex[lastSlot] = idx;
        b.Indices.RemoveAt(lastIdx);
        b.SlotToIndex.Remove(slot);

        if (b.Indices.Count == 0)
        {
            _buckets.Remove(cell);
            _bucketPool.Push(b);
        }
    }
}