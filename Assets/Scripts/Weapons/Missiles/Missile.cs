using Godot;
using static SFS.Teams;

namespace SFS
{
    [GlobalClass]
    public partial class Missile : RigidBody3D, ITeam
    {
        private float m_thrust;
        private float m_adjustedThrust;
        private float m_turnSpeed;
        private uint m_dmg;
        private float m_triggerRange;
        private float m_flightTime;
        private int m_detectionAngle;

        private Node m_shooter;
        private Node3D m_target;
        // Will be null if the target is not a plane!
        private Plane m_targetAsPlane;
        private float m_currentFlightTime;
        private Team m_team;
        private bool m_shooterIsPlayer;

        private float m_launchTime = 0;
        [Export] private float m_currentLaunchTime;
        [Export] private PackedScene m_explosionScene;
        [Export] private ParticleHandler m_particleHandler;
        [Export] private GpuParticles3D m_smokeParticle;
        [Export] private MinimapObject m_minimapObject;

        public struct MissileInitData
        {
            public float thrust;
            public float adjustedThrust;
            public float turnSpeed;
            public uint dmg;
            public float triggerRange;
            public float flightTime;
            public int detectionAngle;

            public MissileInitData(uint dmg,
                float thrust,
                float turnSpeed,
                float triggerRange,
                float flightTime, int detectionAngle) : this()
            {
                this.thrust = thrust;
                this.turnSpeed = turnSpeed;
                this.dmg = dmg;
                this.triggerRange = triggerRange;
                this.flightTime = flightTime;
                this.detectionAngle = detectionAngle;
            }
        }

        public bool HasValidTarget() { return m_target != null; }

        public override void _Ready()
        {
            base._Ready();      
            BodyEntered += OnContact;
        }

        public void Init(MissileInitData data, Node3D target, Plane shooter)
        {
            m_dmg = data.dmg;
            m_target = target;
            if (m_target != null)
            {
                m_targetAsPlane = m_target as Plane;
                m_targetAsPlane.AddTrackingMissile(this);
            }
            m_shooter = shooter;
            m_shooterIsPlayer = shooter.IsInGroup("Player");
            m_team = shooter.m_team;
            m_minimapObject.SetColor(shooter.m_team);
            float shooterForwardVelocity = shooter.GlobalTransform.Basis.Z.Dot(shooter.LinearVelocity);
            m_thrust = data.thrust;
            m_adjustedThrust = data.thrust + shooterForwardVelocity;
            m_turnSpeed = data.turnSpeed;
            m_triggerRange = data.triggerRange;
            m_flightTime = data.flightTime;
            m_currentFlightTime = m_flightTime;
            m_currentLaunchTime = m_launchTime;
            m_detectionAngle = data.detectionAngle;
        }

        public Team GetTeam() { return m_team; }

        public override void _PhysicsProcess(double delta)
        {
            if (!IsInsideTree() || !IsInstanceValid(this))
            {
                QueueFree();
                return;
            }

            base._PhysicsProcess(delta);

            // If the target was set and it is no longer valid (deletion), set it to null.
            if (m_target != null && !IsInstanceValid(m_target))
                m_target = null;

            Debug.Assert(!float.IsNaN(GlobalPosition.X), "GlobalPositionX is NaN!");
            Debug.Assert(!float.IsNaN(GlobalPosition.Y), "GlobalPositionY is NaN!");
            Debug.Assert(!float.IsNaN(GlobalPosition.Z), "GlobalPositionZ is NaN!");

            if (m_currentFlightTime <= 0)
            {
                OnFree();
                return;
            }
            m_currentFlightTime -= (float)delta;

            if (m_currentLaunchTime <= 0) 
                m_adjustedThrust = m_thrust;
            m_currentLaunchTime -= (float)delta;

            // If target is null, fly straight
            if (m_target == null)
            {
                LinearVelocity = -GlobalTransform.Basis.Z * m_adjustedThrust * (float)delta;
                AngularVelocity = Vector3.Zero;
                return;
            }

            if (m_target.GlobalPosition.DistanceTo(GlobalPosition) <= m_triggerRange)
            {
                if (m_target != null)
                {
                    GameEntityComponent component = 
                        Utilities.GetFirstChildOfType<GameEntityComponent>(m_target);

                    if (component != null)
                    {
                        component.Damage(m_dmg, m_shooter);

                        if (m_shooterIsPlayer)
                        {
                            PlayerUI ui = GetTree().GetFirstNodeInGroup("PlayerUI") as PlayerUI;
                            ui?.OnMissileHit();
                        }
                    }

                    OnExplosion();
                    return;
                }
            }

            Vector3 currentRotation = GlobalRotation;
            LookAt(m_target.GlobalPosition);
            Vector3 newRotation = currentRotation.Lerp(GlobalRotation, m_turnSpeed);
            if (Mathf.RadToDeg(currentRotation.AngleTo(newRotation)) < m_detectionAngle)
                GlobalRotation = newRotation;
            else
            {
                // The angle is too large,
                // and the missile loses tracking of the target.
                m_targetAsPlane?.RemoveTrackingMissile(this);
                m_target = null;
                if (m_shooterIsPlayer)
                {
                    PlayerUI ui = GetTree().GetFirstNodeInGroup("PlayerUI") as PlayerUI;
                    ui?.OnMissileMiss();
                }

            }
            LinearVelocity = -GlobalTransform.Basis.Z * m_adjustedThrust * (float)delta;
        }

        public void OnContact(Node body)
        {
            if (body is StaticBody3D || body is CsgCombiner3D)
            {
                OnExplosion();
                return;
            }    

            Plane plane = body as Plane;
            if (IsInstanceValid(m_shooter) && body == m_shooter)
                return;

            GameEntityComponent component =
                        Utilities.GetFirstChildOfType<GameEntityComponent>(body);

            component?.Damage(m_dmg, m_shooter);
            OnExplosion();
        }

        private void OnExplosion()
        {
            Explosion newExplosion = m_explosionScene.Instantiate() as Explosion;
            Utilities.GetFirstNode3D(this).AddChild(newExplosion);
            newExplosion.GlobalPosition = m_target.GlobalPosition;

            OnFree();
        }

        private void OnFree()
        {          
            m_targetAsPlane?.RemoveTrackingMissile(this);
            m_particleHandler.DeleteAfterTime(30);
            QueueFree();
        }
    }
}
