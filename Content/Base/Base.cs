using Godot;
using System;
using ZCraft.Source.Networking;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content.Base;
public partial class Base : Content
{
    public override void OnRegistryStep()
    {
        PackedScene serverPlayer = LoadGDResource<PackedScene>("ServerPlayer.tscn");
        PackedScene clientPlayer = LoadGDResource<PackedScene>("ClientPlayer.tscn");

        NetworkReplicator.AddClientPackedScene(serverPlayer, clientPlayer);

        RegisterOreAndIngot("Copper");
        RegisterOreAndIngot("Iron");
        RegisterOreAndIngot("Tin");
        RegisterOreAndIngot("Gold");
        RegisterOreAndIngot("Lead");
    }

    public void RegisterOreAndIngot(string id)
    {
        RegisterOre(id);
        RegisterIngot(id);
    }

    public void RegisterOre(string id)
    {
        Item oreItem = new();
        Registry.Items.Add(oreItem, id + ":Ore");
    }

    public void RegisterIngot(string id)
    {
        Item ingotItem = new();
        Registry.Items.Add(ingotItem, id + ":Ingot");
    }
}
