using Godot;
using System;

public partial class ZEntity : Node
{
    [Export] Godot.Collections.Array<ZEntityComponent> _components = new();

#nullable enable
    public T FindComponent<T>() where T: ZEntityComponent
    {
        PhysicsServer3D.BodyCreate();
        return null;
    }
#nullable disable

}
