using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class PullUp : State
    {
        [Export] private string m_approachPlayerState;
        [Export] private uint m_targetAdditionalHeight = 1000;

        public override void _Ready()
        {
            base._Ready();
        }

        public override string CheckForTransition()
        {
            if (m_plane.GetAltitude() > m_owner.m_owner.m_minimumSafeHeight)            
                return m_approachPlayerState;            
            return null;
        }

        public override void OnUpdate(double deltaTime)
        {
            //var roll = m_plane.Basis.GetEuler().Z;
            //if (roll > Mathf.DegToRad(180))
            //    roll -= Mathf.DegToRad(360);
            //m_plane.m_controlInput =  new(-1, 0, Mathf.Clamp(-roll, -1, 1));
            //m_plane.m_throttleInput = 1;
            Vector3 target = m_plane.GlobalPosition + new Vector3(0, m_targetAdditionalHeight, 0);
            
            Vector3 targetRotation = new(Mathf.RadToDeg(-90),
                m_plane.GlobalRotation.Y, m_plane.GlobalRotation.Z);
            Utilities.TurnTowards(ref m_plane, targetRotation, target);
            
            m_plane.m_throttleInput = 1;
        }
    }
}
