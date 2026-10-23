using Godot;
using Godot.Collections;

namespace ZCraft.Source.World.Entity
{
	[GlobalClass]
	public partial class EntityModules : Node
	{
		public const string MetaName = "EntityModulesRef";

		[Export] public Node Target;

		[Export] private Array<Node> _initialNodes = new();
		[Export] private Array<Resource> _initialResources = new();

		private System.Collections.Generic.List<GodotObject> _modules = new();

		private bool _initialized = false;

#nullable enable
		public static EntityModules? TryFindIn(Node target)
		{
			if (target == null)
				return null;

			if (target.HasMeta(MetaName))
			{
				return (EntityModules)target.GetMeta(MetaName);
			}

			return null;

		}
#nullable disable

		public EntityModules Initialize(Node target)
		{
			if (target == null)
			{
				_initialized = false;
				return this;
			}

			if (_initialized) return this;

			Target.SetMeta(MetaName, this);

			_initialized = true;
			return this;
		}

		public override void _Ready()
		{
			Initialize(Target);

			foreach(Node node in _initialNodes)
			{
				AddModule(node);
			}

			foreach(Resource resource in _initialResources)
			{
				AddModule(resource);
			}
		}

		public void AddModule(GodotObject module)
		{
			_modules.Add(module);
		}

		public void RemoveModule(GodotObject module)
		{
			_modules.Remove(module);
		}

		public T TryFindModule<T>(T moduleType) where T : GodotObject
		{
			for (int i = 0; i < _modules.Count; i++)
			{
				var m = _modules[i];
				if (m is T) 
					return (T)m;
			}

			return null;
		}

	}
}
