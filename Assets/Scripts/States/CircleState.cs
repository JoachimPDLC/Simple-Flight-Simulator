using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class CircleState : State
    {
        [Export] float m_yawMultiplier = -1;

        public override string CheckForTransition()
        {
            return null;
        }

        public override void OnUpdate(double deltaTime)
        {
            m_plane.m_controlInput.Y = 1 * m_yawMultiplier;
            m_plane.m_throttleInput = 0;
        }
    }
}
