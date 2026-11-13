
using System.Collections.Generic;

namespace ZCraft.Source.Content
{
    public static class Registry
    {
        private static Dictionary<string, World.Item.Item> _items = new();
        private static Dictionary<string, World.LevelData> _levels = new();
        public static void AddItem(Content content, string itemId, World.Item.Item item)
        {
            string actualId = content + ":" + itemId;
            _items[actualId] = item;
            item.Id = actualId;
        }

        public static void AddLevel(Content content, string levelId, World.LevelData level)
        {
             string actualId = content + ":" + levelId;
             _levels[actualId] = level;
             level.Id = actualId;
        }


    }
}