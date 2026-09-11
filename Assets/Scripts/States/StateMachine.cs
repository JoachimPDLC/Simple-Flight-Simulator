using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class StateMachine : Node
    {
        [Export] private string m_initialState;
        [Export] public AiController m_owner { get; private set; }
        [Export] public bool kPrintDebug = false;
        private State m_currentState = null;
        private Godot.Collections.Dictionary<string, State> m_states = new();

        public override void _Ready()
        {
            // Gets all direct children. If they are a state,
            // emplace them in the m_states dictionary.
            var children = GetChildren();
            foreach (Node child in children)
            {
                State newState = child as State;
                m_states[newState.Id] = newState;
            }
            SetState(m_initialState);
        }
        public void Update(double deltaTime)
        {
            if (m_currentState != null)
            {
                m_currentState.OnUpdate(deltaTime);

                string newStateId = m_currentState.CheckForTransition();
                if (newStateId != null)
                    SetState(newStateId);
            }
        }

        public void SetState(string newStateId)
        {
            if (kPrintDebug)
                Debug.Print("Set state to: " + newStateId + " previous state: " + m_currentState?.Id);

            if (m_currentState != null)
            {
                m_currentState.OnExit();
                m_currentState = null;
            }

            if (newStateId != null)
            {
                if (m_states.ContainsKey(newStateId))
                {
                    m_currentState = m_states[newStateId];
                    m_currentState?.OnEnter();
                }
                else
                {
                    Debug.Assert(false, " State machine's state was set to a state " +
                        newStateId + " that does not exist!");
                }
            }

            Debug.Assert(m_currentState != null, "StateMahcine has no state!");
        }
    }
}