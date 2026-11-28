
using System.Collections.Generic;
using System.Net.Mime;
using Godot;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content
{
    [Godot.GlobalClass]
    public partial class Content : Godot.Resource
    {
        public string ID = "";
        public string FolderPath;
        public static List<Content> All = new();

        [Export] public Godot.Collections.Array<Item> AutoloadItems = new();

        public void Initialize(string folderPath, string id)
        {
            ID = id;
            FolderPath = folderPath;

            foreach(Item item in AutoloadItems)
            {
                if (item == null)
                    continue;
                
                string path = item.ResourcePath.GetBaseName().GetFile();
                Registry.Items.Add(item, path);
            }
        }

        #nullable enable
        public static T? FindContentInstance<T>() where T : Content
        {
            foreach(Content content in All)
            {
                if (content is T t)
                    return t;
            }

            return null;
        }
        #nullable disable

        public void Enable()
        {
            OnEnable();
        }

        public void Disable()
        {
            OnDisable();
        }

        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void OnRegistryStep() {}
        public virtual void OnRegistryModifyStep() {}

        public T LoadGDResource<T>(string path) where T: Godot.Resource
        {
            return GD.Load<T>(FolderPath.PathJoin(path));
        }

    }
}
