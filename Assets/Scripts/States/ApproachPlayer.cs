using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class ApproachPlayer : State
    {
        [Export] private int m_slowdownDistance;
        [Export] private int m_brakeDistance;
        [Export] private int m_transitionRange;

        [Export] private string m_dogFightState;
        [Export] private string m_dodgeMissileState;
        [Export] private string m_PullUpState;

        public override void _Ready()
        {
            base._Ready();
        }

        public override string CheckForTransition()
        {
            if (m_plane.GetAltitude() < m_owner.m_owner.m_minimumDangerHeight)
                return m_PullUpState;

            foreach (Missile missile in m_plane.GetTrackingMissiles())
            {
                if (m_plane.GlobalPosition.DistanceTo(missile.GlobalPosition) < m_owner.m_owner.m_missileCareDistance)
                    return m_dodgeMissileState;
            }
                
            if (m_plane.GlobalPosition.DistanceTo(m_playerPlane.GlobalPosition) <= m_transitionRange)
            {
                return m_dogFightState;
            }
            return null;
        }

        public override void OnUpdate(double deltaTime)
        {
            Vector3 targetRotation = Utilities.GetRotationTo(m_plane, m_playerPlane.GlobalPosition);

            Utilities.TurnTowards(ref m_plane, targetRotation, m_playerPlane.GlobalPosition);

            float distance = m_plane.GlobalPosition.DistanceTo(m_playerPlane.GlobalPosition);
            if (distance > m_slowdownDistance)
                m_plane.m_throttleInput = 1;
            else if (distance < m_brakeDistance)
                m_plane.m_throttleInput = -1;
            else
                m_plane.m_throttleInput = 0;
        }
    }
}
