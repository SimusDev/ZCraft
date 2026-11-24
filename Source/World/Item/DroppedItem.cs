using System;
using System.Collections.Generic;
using Godot;
using ZCraft.Content.Base.Items.Components;
using ZCraft.Source.Entity;
using ZCraft.Source.Networking;
using ZCraft.Source.Serialization;

namespace ZCraft.Source.World.Item;

public partial class DroppedItem : RigidBody3D, ILevelObject, ITickable
{
	private ItemStack _serverItemStack = null;
	public ItemStack ServerItemStack => _serverItemStack;
	public Item _clientItem;
	public const string DroppedItemScenePath = "res://Resources/Scenes/World/Items/DroppedItem.tscn";

	public bool _isServerSide = true;
	public double _liveTime = 0;

	private Node3D _clientRoot;

    public override void _EnterTree()
	{
		_isServerSide = NetworkManager.IsServer;
		Ticker.Registry.Add(this);

	}

    public override void _ExitTree()
	{
		if (NetworkManager.IsServer)
			return;

		Ticker.Registry.Remove(this);
		
	}

    public void OnTick(double delta)
	{
		if (_liveTime == -1)
			return;
		
		if (_isServerSide)
		{
			_liveTime += delta;

			if (_liveTime >= 60 * 5)
			{
				_liveTime = -1;
				QueueFree();
			}
		}



	}

	public void SetItemStackRef(ItemStack stack)
	{
		_serverItemStack = stack;
	}

	public void LevelObjectCreate(Level level, NetworkId id, NetworkRegion network)
	{
		
	}

	public void LevelObjectDestroy(Level level)
	{
		
	}

	public void SnapshotNetworkWrite(BytePacker packer)
	{
		ServerItemStack.ResolveItem().Serialize(packer);
	}

	public void SnapshotNetworkRead(BytePacker packer)
	{
		_clientItem = Item.Deserialize(packer);

		if (NetworkManager.IsServer)
			return;
		
		_clientRoot = new();
		_clientRoot.Position = new Vector3(0, -0.026f, 0);
		AddChild(_clientRoot);

		List<IUIIconLayerItemComponent> comps = _clientItem.GetComponents<IUIIconLayerItemComponent>();
		if (comps == null)
			return;
		
		foreach(var comp in comps)
		{
			Sprite3D sprite = new();
			sprite.Texture = comp.Texture;
			sprite.Modulate = comp.Color;
			sprite.Scale = new Vector3(3, 3, 3);
			sprite.RotationDegrees = new Vector3(-90f, 0, 0);
			sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
			_clientRoot.AddChild(sprite);
		}

	}

	public void SnapshotSave(BytePacker packer) {}
	public void SnapshotLoad(BytePacker packer) {}


}
