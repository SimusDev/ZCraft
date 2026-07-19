using Godot;
using System;

public partial class ClientServerNodeParser : RefCounted
{
    public const string ServerGroup = "Server";
    public const string ClientGroup = "Client";

    public static System.Collections.Generic.Dictionary<string, bool> ClientClasses = new()
    {
        {"AudioStreamPlayer3D", true},
    };

    public static bool IsClientNode(Node node)
    {
        if (node.IsInGroup(ClientGroup))
            return true;

        if (node is VisualInstance3D)
            return true;

        return ClientClasses.ContainsKey(node.GetClass());

    }

    public static void Parse(Node node)
    {
        Godot.Collections.Array<Node> children = node.GetChildren();
        for (int i = 0; i < children.Count; i++)
        {
            if (IsClientNode(children[i]) && !GDNet.isServer)
            {
                children[i].QueueFree();
                continue;
            }

            Parse(children[i]);
        }
    }
}
