using System;
using Godot;
using ZCraft.Source.Entity;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;
using ZCraft.Source.World.Player;

namespace ZCraft.Source.World.Interaction;

[GlobalClass]
public partial class EntityInteractor3D : RayCast3D, IFixedTickable, ILevelObject, INetworkReplicationObject
{
    [Export] private Godot.Collections.Array<CollisionObject3D> _excludeBodies = new();
    [Export] public Node Source = null;
    [Export] private PlayerComponent _player = null;

    private NetworkRegion _network;

    public const string InteractInputAction = "interact";

    public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
    {
        _network = network;
        NetworkReplicator.AddObject(this, id);
    }

    public void LevelObjectDestroy(Level level)
    {
        NetworkReplicator.RemoveObject(this);
    }

    public void SnapshotLoad(TagCompound tagCompound) {}
    public void SnapshotSave(TagCompound tagCompound) {}
    public void SnapshotNetworkRead(BytePacker packer) {}
    public void SnapshotNetworkWrite(BytePacker packer) {}

    public void OnFixedTick(double delta)
    {
        // ClientSide Tick
        
        GodotObject collider = GetCollider();
        if (collider == null)
            return;
        

    }

    public override void _EnterTree()
    {
        if (!NetworkManager.IsServer && IsMultiplayerAuthority())
        {
            Ticker.FixedRegistry.Add(this);
        }
    }

    public override void _ExitTree()
    {
        if (!NetworkManager.IsServer && IsMultiplayerAuthority())
        {
            Ticker.FixedRegistry.Remove(this);
        }
    }

    public override void _Ready()
    {
        Enabled = false;

        if (!IsMultiplayerAuthority() && !NetworkManager.IsServer)
        {
            QueueFree();
            return;
        }

		SetCollisionMaskValue((int)WorldPhysicsCollisions.Layers.Interaction, true);


        foreach (var obj in _excludeBodies)
        {
            AddException(obj);
        }

        if (NetworkManager.IsServer)
        {
            SetProcessInput(false);
            SetProcessUnhandledInput(false);
        }
        else
        {
            Enabled = IsMultiplayerAuthority();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed(InteractInputAction))
        {
            BytePacker packer = BytePackerPool.Rent();
            packer.WriteByte(0);
            _network.ClientSend(this, packer);
            BytePackerPool.Return(packer);
        }
    }

    public void OnNetworkMessage(int fromPeer, BytePacker packer)
    {
        ForceRaycastUpdate();
        GodotObject collider = GetCollider();

        if (!IsInstanceValid(collider))
            return;
        
        if (collider is IInteractable interactable)
        {
            InteractionContext context = InteractionContext.Create(Source).WithPlayer(_player);
            interactable.OnInteract(context);
        }

    }
}
