using Godot;
using Godot.Collections;

namespace SFS
{
    [GlobalClass]
    public partial class MissileSystem : Node
    {
        [Export] private Array<Node3D> m_hardpoints = [];
        private int m_availableMissiles;
        [Export] private int m_totalAmmo;
        [Export] private bool m_infiniteAmmo;
        [Export] private Plane m_owner;
        [Export] private float m_missileReloadTime;
        [Export] private float m_currentMissileReloadTime = 0;

        [Export] private PackedScene m_missileScene;
        [Export] private uint m_damage;
        [Export] private float m_thrust;
        [Export] private float m_turnSpeed;
        [Export] private float m_triggerRange;
        [Export] private float m_flightTime;
        [Export] private int m_detectionAngle;

        [Export] private AudioStreamPlayer3D m_launchAudio;
        public int GetAvailableMissiles() { return m_availableMissiles; }

        public int GetAmmo() { return m_totalAmmo; }

        public void AddAmmo(int amount) { m_totalAmmo += amount; }

        public override void _Ready()
        {
            if (m_owner == null)
            {
                GD.PrintErr("Missile system is missing owner!");
                return;
            }
            if (m_hardpoints.Count == 0)
            {
                GD.Print("Missile system spawned without hardpoints!");
            }
            m_availableMissiles = m_hardpoints.Count;
        }

        public override void _Process(double delta)
        {
            if (m_currentMissileReloadTime > 0 &&
                m_availableMissiles < m_hardpoints.Count &&
                m_totalAmmo > 0)
            {
                m_currentMissileReloadTime -= (float)delta;
                if (m_currentMissileReloadTime <= 0 &&
                    m_availableMissiles < m_hardpoints.Count &&
                    m_totalAmmo > 0)
                {
                    ++m_availableMissiles;
                    if (!m_infiniteAmmo)
                        --m_totalAmmo;
                    m_currentMissileReloadTime = m_missileReloadTime;
                }
            }
        }

        public void FireMissile(Plane target)
        {
            if (!m_infiniteAmmo && m_totalAmmo <= 0)
                return;

            if (m_availableMissiles <= 0)
                return;
            --m_availableMissiles;

            m_launchAudio.Play();
            Missile newMissile = m_missileScene.Instantiate() as Missile;
            Utilities.GetFirstNode3D(this).AddChild(newMissile);
            newMissile.Init(new(m_damage, m_thrust, m_turnSpeed,
                    m_triggerRange, m_flightTime,
                    m_detectionAngle), target, m_owner);            
            newMissile.GlobalPosition = m_hardpoints[m_availableMissiles].GlobalPosition;
            newMissile.GlobalRotation = -m_owner.GlobalRotation;
            newMissile.LinearVelocity = m_owner.LinearVelocity + (-newMissile.GlobalTransform.Basis.Z * m_thrust);
            m_currentMissileReloadTime = m_missileReloadTime;
        }
    }
}