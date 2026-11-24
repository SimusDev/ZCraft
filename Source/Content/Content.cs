
using Godot;
using ZCraft.Source.World.Item;

namespace ZCraft.Source.Content
{
    [Godot.GlobalClass]
    public partial class Content : Godot.Resource
    {
        public string ID = "";
        public string FolderPath;

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
                GD.Print(path);
                Registry.Items.Add(item, path);
            }
        }

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
