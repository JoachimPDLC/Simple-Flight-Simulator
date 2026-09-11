using Godot;

namespace SFS
{
    public partial class Bullet : RigidBody3D, ITeam
    {
        private Node m_shooter;

        private bool m_shooterIsPlayer = false;
        public uint m_dmg { get; private set; }
        public Teams.Team m_team { get; private set; }

        [Export] private PackedScene m_sparkScene;

        public override void _Ready()
        {
            BodyEntered += OnContact;
        }

        public void Init(uint dmg, Teams.Team team, Node shooter)
        {
            m_dmg = dmg;
            m_team = team;
            m_shooter = shooter;
            m_shooterIsPlayer = m_shooter.IsInGroup("Player");
        }

        public Teams.Team GetTeam()
        {
            return m_team;
        }

        public void OnContact(Node body)
        {
            if (body == m_shooter)
                return;

            ITeam bodyAsITeam = body as ITeam;

            if (bodyAsITeam != null)
            {
                if (bodyAsITeam.GetTeam() != m_team)
                { 
                    GameEntityComponent component = 
                        Utilities.GetFirstChildOfType<GameEntityComponent>(body);
                    if (component != null)
                    {
                        component.Damage(m_dmg, m_shooter);

                        if (m_shooterIsPlayer)
                        {
                            PlayerUI ui = GetTree().GetFirstNodeInGroup("PlayerUI") as PlayerUI;
                            ui?.OnBulletHit();
                        }
                        
                    }
                }
            }

            Spark newSpark = m_sparkScene.Instantiate() as Spark;
            Utilities.GetFirstNode3D(this).AddChild(newSpark);
            newSpark.Init(-GlobalRotation.Normalized());

            BodyEntered -= OnContact;
            QueueFree();
        }

        public override void _Process(double delta)
        {
            if (!IsInsideTree() || !IsInstanceValid(this))
            {
                QueueFree();
                return;
            }
        }
    }
}
