using Godot;

namespace Components
{
    [GlobalClass]
    public partial class CStateMachine : Node
    {
        [Export] private Godot.Collections.Array<CStateMachineState> _initialStates = new();

        private Godot.Collections.Array<CStateMachineState> _states = new();
        private CStateMachineState _currentState;

        public CStateMachineState CurrentState => _currentState;

        private GDNetRpc _rpc = new();

        [Signal] public delegate void OnStateEnterEventHandler(CStateMachineState state);
        [Signal] public delegate void OnStateExitEventHandler(CStateMachineState state);

        public override void _Ready()
        {
            _rpc.BindOwnerAsNode(this);
            _rpc.BindAll(this);
            _rpc.Authority = GetMultiplayerAuthority();

            InitInitalStates();
        }

        private void InitInitalStates()
        {
            foreach (var state in _initialStates)
            {
                var initialized = (CStateMachineState)state.Duplicate();
                initialized.Init(this);
                _states.Add(initialized);
            }

            if (_states.Count > 0)
                SwitchStateLocal(_states[0]);
        }

        public override void _Process(double delta)
        {
            if (_currentState == null)
                return;

            _currentState._Process(delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_currentState == null)
                return;

            _currentState._PhysicsProcess(delta);
        }

        public override void _Input(InputEvent @event)
        {
            if (_currentState == null)
                return;

            _currentState._Input(@event);
        }

        public void SwitchStateTo(CStateMachineState state)
        {
            if (!_states.Contains(state))
            {
                return;
            }

            if (!_rpc.IsAuthority() || CurrentState == state)
                return;

            var idx = _states.IndexOf(state);
            SwitchStateToRpc(idx);
            _rpc.Invoke(SwitchStateToRpc, idx);
        }

        private void SwitchStateLocal(CStateMachineState state)
        {
            if (!_states.Contains(state))
                return;

            if (_currentState != null)
                EmitSignal(SignalName.OnStateExit, _currentState);
            
            _currentState = state;

            EmitSignal(SignalName.OnStateEnter, _currentState);
        }

        [GDNetRpc(permission: Permission.ServerOrAuth, Channel = (int)GameServer.Channel.States)]
        private void SwitchStateToRpc(int idx)
        {
            SwitchStateLocal(_states[idx]);
        }

        public CStateMachineState GetStateByName(string name)
        {
            foreach (var state in _states)
            {
                if (state.Name == name)
                {
                    return state;
                }
            }

            return null;
        }

        public void SwitchStateByName(string name)
        {
            var state = GetStateByName(name);
            if (state != null)
                SwitchStateTo(state);
        }
    }

}
