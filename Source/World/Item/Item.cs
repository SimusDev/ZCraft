using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Source.Content;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Item;

[GlobalClass]
public sealed partial class Item : Resource
{
    [Export] public Godot.Collections.Array<ItemBehavior> Behaviors = null;
    [Export] public Godot.Collections.Array<ItemComponent> Components = null;
    [Export] public Godot.Collections.Array<ClientItemComponent> ClientComponents = null;

    private Dictionary<Type, object> _componentsCache = null;
    private Dictionary<Type, object> _clientComponentsCache = null;

    public void AddComponent(ItemComponent component)
    {
        if (_componentsCache != null)
            throw new Exception("Can't add component in runtime! Components already cached!");

        if (Components == null)
            Components = [];

        Components.Add(component);
    }

    public void AddClientComponent(ClientItemComponent component)
    {
        if (_clientComponentsCache != null)
            throw new Exception("Can't add component in runtime! Components already cached!");
        
        if (ClientComponents == null)
            ClientComponents = [];
        
        ClientComponents.Add(component);
    }

    #nullable enable
    public List<T>? GetComponents<T>() where T : ItemComponent
    {
        if (Components == null)
            return null;
        
        _componentsCache ??= BuildComponentCache();
        return _componentsCache.TryGetValue(typeof(T), out var obj) ? (List<T>)obj : null;
    }
    #nullable disable

    #nullable enable
    public List<T>? GetClientComponents<T>() where T : ClientItemComponent
    {
        if (NetworkManager.IsServer)
            throw new ArgumentException("GetClientComponents only for client!");
        
        if (ClientComponents == null)
            return null;
        
        _clientComponentsCache ??= BuildClientComponentCache();
        return _clientComponentsCache.TryGetValue(typeof(T), out var obj) ? (List<T>)obj : null;
    }
    #nullable disable


    private Dictionary<Type, object> BuildComponentCache()
    {
        var cache = new Dictionary<Type, object>();
        var temp = new Dictionary<Type, List<ItemComponent>>();

        foreach (var component in Components)
        {
            if (component == null)
                continue;
            
            Type type = component.GetType();
            while (type != null && typeof(ItemComponent).IsAssignableFrom(type))
            {
                if (!temp.TryGetValue(type, out var list))
                {
                    list = new List<ItemComponent>();
                    temp[type] = list;
                }
                list.Add(component);
                type = type.BaseType;
            }
        }

        foreach (var kvp in temp)
        {
            var typedList = (System.Collections.IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(kvp.Key));
            foreach (var c in kvp.Value)
                typedList.Add(c);
            cache[kvp.Key] = typedList;
        }

        return cache;
    }

    private Dictionary<Type, object> BuildClientComponentCache()
    {
        var cache = new Dictionary<Type, object>();
        var temp = new Dictionary<Type, List<ClientItemComponent>>();

        foreach (var component in ClientComponents)
        {
            if (component == null)
                continue;
            
            Type type = component.GetType();
            while (type != null && typeof(ClientItemComponent).IsAssignableFrom(type))
            {
                if (!temp.TryGetValue(type, out var list))
                {
                    list = new List<ClientItemComponent>();
                    temp[type] = list;
                }
                list.Add(component);
                type = type.BaseType;
            }
        }

        foreach (var kvp in temp)
        {
            var typedList = (System.Collections.IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(kvp.Key));
            foreach (var c in kvp.Value)
                typedList.Add(c);
            cache[kvp.Key] = typedList;
        }

        return cache;
    }

    public static Item ByName(string name) => Registry.Items.GetByName(name);

    public void Serialize(BytePacker packer)
        => packer.WriteVarInt(Registry.Items.GetId(this));

    public static Item Deserialize(BytePacker packer)
        => Registry.Items.GetById(packer.ReadVarInt());
    

}