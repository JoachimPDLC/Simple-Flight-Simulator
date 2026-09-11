using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class MachineGun : Node3D
    {
        [Export] private bool m_disabled = false;
        [Export] private RigidBody3D m_owner;

        [Export] private PackedScene m_bulletScene;
        // Bullets per second
        [Export] private int m_bulletRateOfFire;

        [Export] private float m_bulletSpawnVelocity;
        public float GetBulletVelocity() { return m_bulletSpawnVelocity; }
        [Export] private uint m_bulletDmg;
        [Export] private Teams.Team m_bulletTeam;

        [ExportGroup("Variance")]
        [Export] private float m_angleVariance;
        [Export] private float m_forwardVariance = 0;
        [Export] private int m_bulletTimeVariance;

        private double m_currentBulletSpawnTime;

        private RandomNumberGenerator m_rng = new();


        public override void _Process(double delta)
        {
            if (m_currentBulletSpawnTime >= 0)
                m_currentBulletSpawnTime -= delta;
        }

        public void Shoot()
        {
            if (m_disabled)
                return;

            if (m_currentBulletSpawnTime <= 0)
            {
                Bullet newBullet = m_bulletScene.Instantiate() as Bullet;
                Utilities.GetFirstNode3D(this).AddChild(newBullet);
                newBullet.Init(m_bulletDmg, m_bulletTeam, m_owner);
                newBullet.LinearVelocity = m_owner.LinearVelocity;
                newBullet.GlobalPosition = GlobalPosition;

                float forwardVariance = m_rng.RandfRange(0, m_forwardVariance);
                Vector3 forward = m_owner.GlobalTransform.Basis.Z;
                newBullet.GlobalPosition += forward * forwardVariance;

                newBullet.GlobalRotation = m_owner.GlobalRotation;
                
                Vector3 impulse = m_owner.Transform.Basis.Z * m_bulletSpawnVelocity;
                impulse.X += m_rng.RandfRange(-m_angleVariance, m_angleVariance);
                impulse.Y += m_rng.RandfRange(-m_angleVariance, m_angleVariance);
                newBullet.ApplyCentralImpulse(impulse);

                int rof = m_bulletRateOfFire + m_rng.RandiRange(-m_bulletTimeVariance, m_bulletTimeVariance);
                m_currentBulletSpawnTime = 60.0 / rof;
            }
        }
    }
}
