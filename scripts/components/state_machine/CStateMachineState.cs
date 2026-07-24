using Godot;

namespace Components
{
    [GlobalClass]
    public partial class CStateMachineState : Resource
    {
        [Export] private string _name = "";

        public string Name => _name;

        private CStateMachine _stateMachine;

        public CStateMachine StateMachine { get { return _stateMachine; } }

        public void Init(CStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _Ready();
        }

        public virtual void _Ready() { }
        public virtual void _Process(double delta) { }
        public virtual void _PhysicsProcess(double delta) { }
        public virtual void _Input(InputEvent @event) { }

    }

}
