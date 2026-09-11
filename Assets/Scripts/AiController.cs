using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class AiController : Controller
    {
        [Export] private int m_score = 100;
        [Export] public int m_missileCareDistance { get; private set; } = 100;
        [Export] public int m_minimumDangerHeight { get; private set; } = 1100;
        [Export] public int m_minimumSafeHeight { get; private set; } = 1600;
        [Export] public int m_stallSpeedSafety { get; private set; } = 100;

        [Export] public float m_maximumInput { get; private set; } = 0.5f;

        [Export] private StateMachine m_stateMachine;
        private SFS.Plane m_target = null;

        public override void _Ready()
        {
            m_plane.AddToGroup("Enemies");
            m_plane.SetSimpleControls(false);
            m_controlDisabled = false;
        }
        public override void _Process(double delta)
        {
            if (!m_plane.m_dummy && !m_controlDisabled)
                m_stateMachine.Update(delta);
        }

        public override void OnDeath(Node _damager)
        {
            m_controlDisabled = true;

            Plane playerPlane = GetTree().GetFirstNodeInGroup("Player") as Plane;

            if (_damager == playerPlane)
            {
                if (!IsInstanceValid(playerPlane) || playerPlane == null)
                    return;

                PlayerController playerController = playerPlane.m_controller as PlayerController;
                Debug.Assert(playerController != null, "AiController could not find player controller!");

                playerController.AddScore(m_score);
                playerController.OnKill();
            }
        }
    }
}
