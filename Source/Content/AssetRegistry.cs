using System;
using System.Collections.Generic;
using System.Linq;

namespace ZCraft.Source.Content;

public class AssetRegistry<T> where T: class
{
    public int Count => _names.Count;
    private Dictionary<int, T> _byId = new();
    private Dictionary<string, T> _byName = new();
    private Dictionary<T, int> _ids = new();
    private Dictionary<T, string> _names = new();

    private static int _nextId = 0;

    public Dictionary<T, string> GetDictionaryByNames() => _names;

    public void Add(T asset, string name)
    {
        if (_byName.ContainsKey(name))
            throw new Exception($"{name} asset already exists.");
        
        _byName[name] = asset;
        _byId[_nextId] = asset;
        _ids[asset] = _nextId;
        _names[asset] = name;

        _nextId++;
    }

    public void Remove(T asset)
    {
        if (!_names.TryGetValue(asset, out string name))
        {
            throw new Exception($"{asset} asset was not found.");
        }

        _names.Remove(asset);
        
        int id = _ids[asset];
        _ids.Remove(asset);
        _byId.Remove(id);
        _byName.Remove(name);
    }

    public int GetId(T asset)
    {
        return _ids[asset];
    }

    public T GetById(int id)
    {
        return _byId[id];
    }

    public string GetName(T asset)
    {
        return _names[asset];
    }

    public T GetByName(string name)
    {
        return _byName[name];
    }

}

