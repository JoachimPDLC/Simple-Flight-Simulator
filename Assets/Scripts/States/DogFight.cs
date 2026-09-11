using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class DogFight : State
    {

        [Export] private string m_dodgeMissileState;
        [Export] private string m_PullUpState;
        [Export] private string m_approachPlayerState;
        [Export] private float m_neutralDistance;
        [Export] private float m_brakeDistance;
        [Export] private float m_transitionRange;

        [Export] private float m_targetedInputMultiplier = 0.5f;

        private RandomNumberGenerator m_RNG = new();
        [Export] private float m_interceptPositionRadius;
        private float m_stallSpeedCheck;

        [ExportGroup("Attack")]
        private float m_currentMissileWaitTime = 0;
        [Export] private float m_missileWaitTime;
        [Export] private float m_missileWaitTimeVariance;

        [ExportGroup("Offset")]
        [Export] private MinMaxF kOffsetDistance;
        [Export] private MinMaxF kOffsetHeight;
        [Export] private MinMaxF kRefeshOffsetTime;
        [Export] private Timer m_refreshOffsetTimer;
        private Vector3 m_offset;

        [ExportGroup("Attack Opportunity")]
        [Export] private MinMaxF kOppLength;
        [Export] private MinMaxF kOppCheckGrace;
        [Export] private MinMaxF kOppCheckInterval;
        [Export] private float kOppChance;
        [Export] private Timer m_oppTimer;
        [Export] private Timer m_oppCheckGraceTimer;
        [Export] private Timer m_oppCheckTimer;


        public override void _Ready()
        {
            base._Ready();
            m_refreshOffsetTimer.Timeout += RefreshOffset;
            m_oppTimer.Timeout += OpportunityFinished;
            m_oppCheckGraceTimer.Timeout += OppCheckGraceFinished;
            m_oppCheckTimer.Timeout += CheckForOpportunity;
            m_stallSpeedCheck = m_plane.GetStallSpeed() + 
                m_owner.m_owner.m_stallSpeedSafety;
        }

        public override void OnEnter()
        {
            m_refreshOffsetTimer.Paused = false;
            RefreshOffset();
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
            if (m_plane.GlobalPosition.DistanceTo(
                m_playerPlane.GlobalPosition) > m_transitionRange)
            {
                return m_approachPlayerState;
            }
            return null;
        }

        public override void OnUpdate(double deltaTime)
        {
            if (m_oppTimer.TimeLeft > 0 && !m_oppTimer.Paused)
            {
                GD.Print(m_oppTimer.TimeLeft);
                if (m_plane.GetSpeed() <= m_stallSpeedCheck)
                    m_plane.m_throttleInput = 1.0f;
                m_plane.m_controlInput = Vector3.Zero;

                HandleAttack((float)deltaTime);
                return;
            }

            float input = m_owner.m_owner.m_maximumInput;
            if (m_playerPlane.GetTarget() == m_owner.m_owner.m_plane &&
                m_playerPlane.IsLockedOn())
            {
                input *= m_targetedInputMultiplier;
                m_oppCheckTimer.Paused = false;
                m_oppCheckTimer.Start(kOppCheckInterval.Rand());
            }
            else
            {
                m_oppCheckTimer.Stop();
                m_oppCheckTimer.Paused = true;
            }

            Vector3 target = m_playerPlane.GlobalPosition + (m_offset * -m_playerPlane.GlobalTransform.Basis.Z);
            Vector3 targetRotation = Utilities.GetRotationTo(m_plane, target);

            Utilities.TurnTowards(ref m_plane, targetRotation, target, input);

            float distance = m_plane.GlobalPosition.DistanceTo(target);
            if (Utilities.IsFacing(m_plane, m_playerPlane.GlobalPosition))
            {
              if (distance < m_brakeDistance)
                  m_plane.m_throttleInput = -input;
              else if (distance > m_neutralDistance)
                  m_plane.m_throttleInput = input;
              else
                  m_plane.m_throttleInput = 0;
            }
            else
                m_plane.m_throttleInput = 0;

            if (m_plane.GetSpeed() <= m_stallSpeedCheck)
                m_plane.m_throttleInput = 1;

            HandleAttack((float)deltaTime);            
        }

        public void RefreshOffset()
        {
            float distance = kOffsetDistance.Rand();
            float angle = m_RNG.RandfRange(0, Mathf.Tau);
            
            float x = Mathf.Cos(angle) * distance;
            float y = kOffsetHeight.Rand();
            float z = Mathf.Sin(angle) * distance;
            m_offset = new(x, y, z);
            m_refreshOffsetTimer.WaitTime = kRefeshOffsetTime.Rand();
        }
        private void OpportunityFinished()
        {
            // Start opportunity check grace period
            GD.Print("Opportunity Finished!");
            m_oppCheckGraceTimer.Start(kOppCheckGrace.Rand());
            m_oppCheckGraceTimer.Paused = false;
        }

        private void CheckForOpportunity()
        {
            if (m_RNG.RandfRange(0, 100) < kOppChance)
            {
                // Start opportunity
                m_oppCheckTimer.Paused = true;
                m_oppTimer.Start(kOppLength.Rand());
                m_oppTimer.Paused = false;
                GD.Print("Opportunity Started!");
            }
            m_oppCheckTimer.Start(kOppCheckInterval.Rand());
        }

        private void OppCheckGraceFinished()
        {
            // The m_oppCheckTimer is set in CheckForOpportunity()
            m_oppCheckTimer.Paused = false;
        }

        private void HandleAttack(float delta)
        {
            Vector3 bulletInterceptPosition =
                Utilities.FirstOrderIntercept(m_plane.GlobalPosition,
                m_plane.LinearVelocity, m_plane.GetBulletVelocity(),
                m_playerPlane.GlobalPosition,
                m_playerPlane.LinearVelocity);
            if (bulletInterceptPosition.DistanceTo(m_plane.GlobalPosition) <= m_interceptPositionRadius)
                m_plane.m_shootingGun = true;
            else
                m_plane.m_shootingGun = false;

            if (m_currentMissileWaitTime <= 0)
            {
                if (m_plane.IsLockedOn())
                {
                    m_plane.TryShootMissile();
                    m_currentMissileWaitTime = m_missileWaitTime + m_RNG.RandfRange(-m_missileWaitTimeVariance, m_missileWaitTimeVariance);
                }
            }
            else
                m_currentMissileWaitTime -= delta;
        }

        public override void OnExit()
        {
            m_refreshOffsetTimer.Paused = true;
            m_oppCheckTimer.Paused = true;
            m_oppTimer.Paused = true;
        }
    }
}
