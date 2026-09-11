using Godot;
  
namespace SFS
{
    [GlobalClass]
    public partial class DodgeMissile : State
    {
        private bool m_NoMissilesToDodge = false;

        [Export] private string m_dogFightState;
        [Export] private float m_perpendicularityThreshold;
        private float m_currentSwitchCooldown = 0;
        [Export] private float m_switchCooldown = 2;
        public override void _Ready()
        {
            base._Ready();
        }

        public override string CheckForTransition()
        {
            if (m_NoMissilesToDodge && m_currentSwitchCooldown <= 0)
            {
                return m_dogFightState;
            }
            return null;
        }

        public override void OnUpdate(double deltaTime)
        {
            m_currentSwitchCooldown -= (float)deltaTime;

            m_NoMissilesToDodge = true;
            Vector3 incomingVector = new();
            foreach (Missile missile in m_plane.GetTrackingMissiles())
            {
                if (m_plane.GlobalPosition.DistanceTo(missile.GlobalPosition) > m_owner.m_owner.m_missileCareDistance)
                    continue;
                Vector3 direction = m_plane.GlobalPosition - missile.GlobalPosition;
                incomingVector += direction.Normalized();
                m_NoMissilesToDodge = false;
                m_currentSwitchCooldown = m_switchCooldown;
            }

            float input = m_owner.m_owner.m_maximumInput;

            if (Mathf.Abs(incomingVector.Dot(m_plane.GlobalTransform.Basis.Z)) <= m_perpendicularityThreshold)
            {
                m_plane.m_controlInput = new(0, 0, 0);
                m_plane.m_throttleInput = input;
            }
            else
            {
                m_plane.m_controlInput = new(-input, 0, 0);
                m_plane.m_throttleInput = input;
            }
        }
    }
}
