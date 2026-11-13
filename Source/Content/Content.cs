
using Godot;

namespace ZCraft.Source.Content
{
    [Godot.GlobalClass]
    public partial class Content : Godot.Resource
    {
        public string ID = "";
        public string FolderPath;

        public void Initialize(string folderPath, string id)
        {
            ID = id;
            FolderPath = folderPath;
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

        public T LoadGDResource<T>(string path) where T: Godot.Resource
        {
            return GD.Load<T>(FolderPath.PathJoin(path));
        }

    }
}
