using System;
using System.Collections.Generic;
using Godot;

namespace ZCraft.Source;

public static class NodeComposition
{
    #nullable enable
    public static T? FindNodeInChildrenRecursive<T>(Node? from) where T : class
    {
        if (from == null || !GodotObject.IsInstanceValid(from))
            return null;

        if (from is T t)
            return t;

        for (int i = 0; i < from.GetChildCount(); i++)
        {
            Node child = from.GetChild(i);
            T? found = FindNodeInChildrenRecursive<T>(child);
            if (found != null)
                return found;
        }

        return null;
    }
    #nullable disable

    #nullable enable
    public static void FindNodesInChildrenRecursive<T>(Node? from, List<T> addTo) where T : class
    {
        if (from == null || !GodotObject.IsInstanceValid(from))
            return;

        if (from is T t)
            addTo.Add(t);

        for (int i = 0; i < from.GetChildCount(); i++)
        {
            Node child = from.GetChild(i);
            FindNodesInChildrenRecursive<T>(child, addTo);
        }
    }
    #nullable disable

}
