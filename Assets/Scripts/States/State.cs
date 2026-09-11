using Godot;

namespace SFS
{
    [GlobalClass]
    public abstract partial class State : Node
    {
        protected Plane m_playerPlane = null;
        protected Plane m_plane = null;
        [Export] protected StateMachine m_owner;

        [Export] protected string kId = "null";
        public string Id
        {
            get { return kId; }
        }
        public override void _Ready()
        {
            m_playerPlane = GetTree().GetFirstNodeInGroup("Player") as Plane;
            Debug.Assert(m_playerPlane != null,
                "Player plane in ApproachPlayer State is null!");
            m_plane = m_owner.m_owner.m_plane;
            Debug.Assert(m_playerPlane != null,
                "Owner plane in ApproachPlayer State is null!");
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public abstract void OnUpdate(double deltaTime);
        public abstract string CheckForTransition();
    }
}
