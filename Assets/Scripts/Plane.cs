using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFS
{
    [GlobalClass]
    public partial class Plane : RigidBody3D, IDamageable, ITeam
    {
        [Export] public Controller m_controller { get; private set; }

        [ExportGroup("Particles")]
        [Export] private PackedScene m_explosionScene;
        [Export] private float m_explosionParticleScale = 40;
        [Export] private Array<EngineAfterburner> m_afterBurners;

        [ExportGroup("Gameplay")]
        [Export] public bool m_dummy = false;
        [Export] private GameEntityComponent m_gameEntityComponent;
        [Export] public Teams.Team m_team { get; private set; }
        [Export] private float kDeathGravityScale = 1.0f;
        [Export] public string kName { get; private set; } = "default";

        [ExportGroup("Weapons")]
        [Export] private MachineGun m_machineGun;
        //[Export] private SelectableWeapon[] m_weapons;

        [Export] private MissileSystem m_missileSystem;
        [Export] private Area3D m_sensorCone;
        [Export] private float m_lockOnTime;
        private Node3D m_target = null;
        [Export] private uint m_targetDistanceCull = 15000;
        private List<Node3D> m_bodiesInCone = new();
        private List<Node3D> m_skippedTargets = new();
        private float m_currentSkippedTargetsResetTime = 0;
        [Export] private float m_skippedTargetsResetTime = 0.5f;
        private float m_currentLockOnTime = 0;
        private bool m_lockedOn = false;
        public bool m_countdownLockOnTime { get; private set; } = false;
        private List<Missile> m_trackingMissiles = [];

        [ExportGroup("Speed limits")]
        [Export] private float m_minSpeed;
        [Export] private float m_maxSpeed;
        [Export] private float m_cruisingSpeed;
        [Export] private float m_brakeSpeed;
        [Export] private float m_linearVelocityLerpWeight = 1.5f;

        private float m_speed;

        [ExportGroup("Throttle")]
        [Export] private float m_throttleSpeed;
        [Export] private float m_minThrottle;
        [Export] private float m_maxThrottle;
        private float m_throttle;
        public float m_throttleInput;
        [Export] private float m_ABThrottleAdjustment = -0.2f;

        [ExportGroup("Flaps")]
        [Export] private float m_flapsRetractSpeed = 0.01f;

        [ExportGroup("Stalling")]
        [Export] private int m_stallSpeed = 50;
        [Export] private float m_stallRotPower = 0.002f;
        [Export] private int m_stallForce = 50;
        [Export] private float m_stalllingInputCut = 0.2f;

        [ExportGroup("Steering")]
        private Godot.Vector3 m_usedTurnSpeed = Godot.Vector3.Zero;
        private Godot.Vector3 m_usedTurnAcceleration = Godot.Vector3.Zero;
        [Export] private Godot.Vector3 m_turnSpeed = new(270, 15, 30);
        [Export] private Godot.Vector3 m_turnAcceleration = new(540, 30, 60);
        [Export] private Godot.Vector3 m_simpleTurnSpeed = new(270, 15, 30);
        [Export] private Godot.Vector3 m_simpleTurnAcceleration = new(540, 30, 60);
        [Export] private Godot.Curve m_steeringCurve;

        [ExportGroup("Audio")]
        [Export] private AudioStreamPlayer3D m_machineGunAudio;
        [Export] private AudioStreamPlayer3D m_lockOnToneAudio;
        [Export] private AudioStreamPlayer3D m_missileAlertBeepAudio;
        [Export] private float m_baseTimeBetweenMissileBeeps;
        [Export] private float m_missileBeepAdjustmentDistance;
        private float m_MissileBeepTimer;
        [Export] private AudioStreamPlayer3D m_alertBeepAudio;
        [Export] private float m_minTimeBetweenAlertBeeps;
        private float m_alertBeepTimer;

        public bool m_airBrakesDeployed { get; private set; } = false;
        private bool m_airFlapsDeployed = false;
        public Godot.Vector3 m_controlInput;
        public bool m_shootingGun = false;
        public bool m_stalling = false;
        public float m_ABValue { get; private set; }

        public class TargetingComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                return y.CompareTo(x);
            }
        }
        public float GetSpeed() { return m_speed; }

        public float GetAltitude() { return Transform.Origin.Y; }

        public float GetNormalizedSpeed() { return Math.Max(m_speed / m_maxSpeed, 0); }

        public int GetNumMissiles() { return m_missileSystem.GetAvailableMissiles(); }

        public int GetMissilesAmmo() { return m_missileSystem.GetAmmo(); }

        public void AddMissilesAmmo(int amount) { m_missileSystem.AddAmmo(amount); }
        public bool IsLockedOn() { return m_lockedOn; }
        public Node3D GetTarget() { return m_target; }
        public float GetLockOnTime() { return m_lockOnTime; }
        public float GetCurrentLockOnTime() { return m_currentLockOnTime; }

        public bool IsBeingTracked() { return m_trackingMissiles.Count > 0; }
        public void AddTrackingMissile(Missile missile)
        {
            m_trackingMissiles.Add(missile);
        }

        public float GetBulletVelocity() { return m_machineGun.GetBulletVelocity(); }

        public bool RemoveTrackingMissile(Missile missile)
        {
            return m_trackingMissiles.Remove(missile);
        }

        public List<Missile> GetTrackingMissiles()
        {
            return m_trackingMissiles;
        }

        public float GetLockOnPercentage()
        {
            return (-100 / m_lockOnTime) * m_currentLockOnTime + 100;
        }

        public GameEntityComponent GetGameEntityComponent()
        {
            return m_gameEntityComponent;
        }

        public float GetStallSpeed() { return m_stallSpeed; }

        public void ForceStall() 
        {
            LinearVelocity = LinearVelocity.Clamp(-float.MaxValue, m_stallSpeed - 10);
        }

        public bool IsDead()
        {
            return m_gameEntityComponent.m_dead;
        }

        public override void _Ready()
        {
            base._Ready();

            BodyEntered += OnBodyEntered;

            Debug.Assert(m_maxSpeed > 0, "Maximum speed cannot be less or equal to 0!");
            
            if (m_sensorCone != null)
            {
                m_sensorCone.BodyEntered += OnSensorConeEntered;
                m_sensorCone.BodyExited += OnSensorConeExited;
            }
        }

        private void OnBodyEntered(Node body)
        {
            if (body is StaticBody3D ||
                body is CsgCombiner3D)
            {
                m_gameEntityComponent.Kill();
            }
        }

        public void OnDamaged(Node damager)
        {
            m_controller.OnDamaged(damager);
        }

        public void OnDeath(Node damager)
        {
            m_ABValue = 0;
            m_shootingGun = false;
            m_controller.OnDeath(damager);
            Explosion newExplosion = m_explosionScene.Instantiate() as Explosion;
            GetTree().Root.AddChild(newExplosion);
            newExplosion.ScaleParticle(m_explosionParticleScale, m_explosionParticleScale);
            newExplosion.GlobalPosition = GlobalPosition;
            GravityScale = kDeathGravityScale;
            if (IsInGroup("Player"))
                newExplosion.Reparent(this);
            else
                QueueFree();
        }

        public Teams.Team GetTeam() { return m_team; }

        private void UpdateThrust(float delta)
        {
            float target = m_throttleInput;                

            if (Mathf.IsEqualApprox(m_throttleInput, 0))
            {                
                if (Mathf.Abs(m_speed - m_cruisingSpeed) <= 3)
                {
                    LinearVelocity *= (m_cruisingSpeed / m_speed);                   
                    ApplyCentralForce(m_cruisingSpeed * GlobalTransform.Basis.Z);
                    LinearVelocity.Lerp(GlobalTransform.Basis.Z * 
                        (m_throttle * m_cruisingSpeed),
                        m_linearVelocityLerpWeight);
                    return;
                }

                if (m_speed < m_cruisingSpeed)
                    target = Mathf.Min((m_cruisingSpeed - m_speed) * 0.1f, 0.5f);
                if (m_speed > m_cruisingSpeed)
                    target = Mathf.Min((m_speed - m_cruisingSpeed) * -0.1f, -0.1f);
            }

            m_throttle = Mathf.Lerp(m_throttle, target, m_throttleSpeed * delta);

            m_throttle = Mathf.Clamp(m_throttle, m_minThrottle, m_maxThrottle);

            float acceleration;
            if (m_throttle < 0)
                acceleration = m_throttle * m_brakeSpeed;
            else
                acceleration = m_throttle * m_maxSpeed;
            ApplyCentralForce(acceleration * GlobalTransform.Basis.Z);
            
            m_airBrakesDeployed = m_throttle <= 0 && m_throttleInput == -1;

            Vector3 targetForwardVelocity = GlobalTransform.Basis.Z * acceleration;
            LinearVelocity.Lerp(targetForwardVelocity, m_linearVelocityLerpWeight);

            if (m_speed < m_minSpeed)
                LinearVelocity *= (m_minSpeed / m_speed);
            if (m_speed > m_maxSpeed)
                LinearVelocity *= (m_maxSpeed / m_speed);
        }

        void UpdateStalling(float delta)
        {
            m_stalling = false;
            if (Mathf.Abs(m_speed) <= m_stallSpeed + 2)
            {
                if (!IsInGroup("Player"))
                {
                    GD.Print("Stalling!");
                }

                PlayAlertBeep();
                m_stalling = true;
                // If the plane is stalling, push the nose
                // down.
                float weight = 1f - Mathf.Exp(-m_stallRotPower * delta);
                GlobalRotation = GlobalRotation.Lerp(new(90, 0, 0), weight);
                AngularVelocity = AngularVelocity.Lerp(new(0, 0, 0), weight);
                ApplyCentralForce(Vector3.Down * m_stallForce);
            }                
        }

        void UpdateSteering(float deltatime)
        {            
            float steeringPower = m_steeringCurve.Sample(GetNormalizedSpeed());

            if (m_stalling)
                steeringPower *= m_stalllingInputCut;

            Godot.Vector3 targetAngularVelocity = m_controlInput * (m_usedTurnSpeed * steeringPower);

            Godot.Vector3 localAngularVelocity = Basis * AngularVelocity;

            Godot.Vector3 correction = new(
                CalculateSteering(deltatime, localAngularVelocity.X, targetAngularVelocity.X, m_usedTurnAcceleration.X * steeringPower),
                CalculateSteering(deltatime, localAngularVelocity.Y, targetAngularVelocity.Y, m_usedTurnAcceleration.Y * steeringPower),
                CalculateSteering(deltatime, localAngularVelocity.Z, targetAngularVelocity.Z, m_usedTurnAcceleration.Z * steeringPower)
            );
            correction = GlobalBasis * correction;
            ApplyTorque(correction);
        }

        float CalculateSteering(float delta, float angularVelocity, float targetVelocity, float acceleration)
        {
            float error = targetVelocity - angularVelocity;
            float accel = acceleration * delta;
            return Mathf.Clamp(error, -accel, accel);
        }
        
        void UpdateFlaps()
        {
            if (m_speed > m_flapsRetractSpeed)
                m_airFlapsDeployed = false;    
        }

        public void TryShootMissile()
        {
            if (IsLockedOn())
                m_missileSystem.FireMissile(m_target as Plane);
            else
                m_missileSystem.FireMissile(null);
        }

        public void UpdateTarget()
        {
            if (m_team == Teams.Team.kEnemy)            
                m_target = GetTree().GetFirstNodeInGroup("Player") as Node3D;

            if (IsInGroup("Player"))
            {
                if (GetTree().GetNodeCountInGroup("Enemies") <= 0)
                {
                    m_target = null;
                    m_lockedOn = false;
                    return;
                }
            }

            if (!IsInstanceValid(m_target) ||
                m_target.GlobalPosition.DistanceTo(GlobalPosition) > 
                m_targetDistanceCull)
            {
                m_target = null;
                m_lockedOn = false;
            }

            if (m_target != null)
            {
                if (m_bodiesInCone.Contains(m_target))
                {
                    // This means the target just entered the cone.
                    if (!m_countdownLockOnTime && !m_lockedOn)
                    {
                        m_currentLockOnTime = m_lockOnTime;
                        m_countdownLockOnTime = true;
                    }
                }
                else
                {
                    m_lockedOn = false;
                    m_countdownLockOnTime = false;
                }
                return;
            }

            // Find new target!
            FindNewTarget();
        }

        public void SwitchTarget()
        {
            if (m_target == null)
                return;            
            
            // Only the player calls this function so this is not as bad.
            if (GetTree().GetNodeCountInGroup("Enemies") <= 0)            
                return;

            if (!IsInstanceValid(m_target))            
                return;

            m_skippedTargets.Add(m_target);

            // Find new target!
            FindNewTarget();
        }

        private void FindNewTarget()
        {
            // Only the player calls this function so this is not as bad.
            if (GetTree().GetNodeCountInGroup("Enemies") <= 0)
                return;

            m_lockedOn = false;

            m_target = null;

            Array<Node> enemies = GetTree().GetNodesInGroup("Enemies");

            Camera3D camera = GetTree().GetFirstNodeInGroup("Camera") as Camera3D;

            Debug.Assert(camera != null, "Camera could not be found!");

            Array<Node3D> enemiesOnScreen = new();
            Array<Godot.Vector2> screenPositions = new();

            foreach (Node node in enemies)
            {
                if (!IsInstanceValid(node))
                    continue;

                Node3D node3D = node as Node3D;

                if (node3D == null)
                    continue;

                if (node3D.GlobalPosition.DistanceTo(GlobalPosition) >
                    m_targetDistanceCull)
                {
                    continue;
                }

                if (!camera.IsPositionBehind(node3D.GlobalPosition))
                    enemiesOnScreen.Add(node3D);
            }

            if (enemiesOnScreen.Count <= 0)
                return;

            // Get position for each enemy on the screen.
            foreach (Node3D enemy in enemiesOnScreen)
                screenPositions.Add(camera.UnprojectPosition(enemy.GlobalPosition));

            Godot.Vector2 screenCenter = GetViewport().GetVisibleRect().Size / 2;

            IEnumerable<Node3D> sortedEnemies = enemiesOnScreen.OrderBy(
                enemy => screenCenter.DistanceSquaredTo(camera.UnprojectPosition(enemy.GlobalPosition)));

            foreach (Node3D enemy in sortedEnemies)
            {
                if (!m_skippedTargets.Contains(enemy))
                {
                    m_target = enemy;
                    m_currentSkippedTargetsResetTime = m_skippedTargetsResetTime;
                    break;
                }
            }

            // If no target is found, that means the skippedTargets list
            // contains all possible targets, so reset it.
            // This happens in cases where the player mashes the skip target
            // button.
            if (m_target == null)
            {
                m_skippedTargets.Clear();
                FindNewTarget();
            }
        }

        public override void _Process(double delta)
        {
            if (m_dummy || m_gameEntityComponent.m_dead)
                return;

            m_ABValue = Math.Max(
                m_throttle + m_ABThrottleAdjustment, 0);

            UpdateStalling((float)delta);

            if (m_currentLockOnTime > 0 && m_countdownLockOnTime)
            {
                m_currentLockOnTime -= (float)delta;
                if (m_currentLockOnTime <= 0 && m_countdownLockOnTime)
                {
                    m_countdownLockOnTime = false;
                    m_lockedOn = true;
                }
            }

            if (m_currentSkippedTargetsResetTime > 0)
            {
                m_currentSkippedTargetsResetTime -= (float)delta;
                if (m_currentSkippedTargetsResetTime <= 0)
                    m_skippedTargets.Clear();
            }
            UpdateTarget();
            UpdateAudio();
            if (m_MissileBeepTimer > 0)
                m_MissileBeepTimer -= (float)delta;
            if (m_alertBeepTimer > 0)
                m_alertBeepTimer -= (float)delta;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (m_dummy || m_gameEntityComponent.m_dead)
                return;
            UpdateControls((float)delta);

            if (m_shootingGun)
            {
                if (!m_machineGunAudio.Playing)
                    m_machineGunAudio.Play();
                m_machineGun.Shoot();
            }
            else
                m_machineGunAudio.Stop();
        }

        // My early iterations of the plane where base off of this 
        // tutorial: https://vazgriz.com/346/flight-simulator-in-unity3d-part-1/
        // However, a lot of the code has changed since then, with only
        // UpdateSteering() being basically still the same code.
        public void UpdateControls(float delta)
        {
            m_speed = LinearVelocity.Length();

            UpdateFlaps();                     

            UpdateThrust(delta);  
            
            UpdateSteering(delta);

            UpdateAfterburners();
        }

        public void OnSensorConeEntered(Node3D body)
        {
            if (!body.CanProcess())
                return;

            ITeam bodyAsITeam = body as ITeam;

            if (bodyAsITeam == null)            
                return;            

            if (bodyAsITeam.GetTeam() == m_team)
                return;

            m_bodiesInCone.Add(body);
        }

        public void OnSensorConeExited(Node3D body)
        {
            if (!body.CanProcess())
                return;

            ITeam bodyAsITeam = body as ITeam;

            if (bodyAsITeam == null)
                return;

            if (bodyAsITeam.GetTeam() == m_team)
                return;

            m_bodiesInCone.Remove(body);
            m_skippedTargets.Remove(body);
            if (m_target == body)
            {
                m_currentLockOnTime = 0;
                m_countdownLockOnTime = false;
                m_lockedOn = false;
            }                           
        }

        public void SetSimpleControls(bool value)
        {
            if (value)
            {
                m_usedTurnSpeed = m_simpleTurnSpeed;
                m_usedTurnAcceleration = m_simpleTurnAcceleration;
            }
            else
            {
                m_usedTurnSpeed = m_turnSpeed;
                m_usedTurnAcceleration = m_turnAcceleration;
            }
        }

        public void UpdateAudio()
        {
            if (!IsInGroup("Player"))
                return;

            if (m_lockedOn)
            {
                if (!m_lockOnToneAudio.Playing)
                    m_lockOnToneAudio.Play();
            }
            else
                m_lockOnToneAudio.Stop();

            if (m_trackingMissiles.Count <= 0)
            {
                m_MissileBeepTimer = 0;
                return;
            }

            // Find closestMissile.
            Missile closestMissile = m_trackingMissiles[0];
            foreach(Missile missile in m_trackingMissiles)
            {
                if (closestMissile.GlobalPosition.DistanceSquaredTo(GlobalPosition) >
                    missile.GlobalPosition.DistanceSquaredTo(GlobalPosition))                
                    closestMissile = missile;
            }

            float distance = closestMissile.GlobalPosition.DistanceTo(GlobalPosition);

            if (m_MissileBeepTimer <= 0)
            {
                m_missileAlertBeepAudio.Play();
                m_MissileBeepTimer =
                    (distance * m_baseTimeBetweenMissileBeeps) / m_missileBeepAdjustmentDistance;
            }
        }

        public void PlayAlertBeep()
        {
            if (!IsInGroup("Player"))
                return;

            if (m_alertBeepTimer <= 0)
            {
                m_alertBeepAudio.Play();
                m_alertBeepTimer = m_minTimeBetweenAlertBeeps;
            }
        }

        private void UpdateAfterburners()
        {
            foreach (EngineAfterburner afterBurner in m_afterBurners)
                afterBurner.SetPower(m_ABValue);
        }
    }
}