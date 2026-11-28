using Godot;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content
{
    public static class Registry
    {
        public static readonly AssetRegistry<Item> Items = new(); 

        public static void PrintDebugInfo()
        {
            foreach(var kvp in Items.GetDictionaryByNames())
            {
                GD.Print($"Item, {kvp.Value}");
            }

            GD.Print("Game Registry Status:");
            GD.Print($"Item assets: {Items.Count}");
        }

    }
}