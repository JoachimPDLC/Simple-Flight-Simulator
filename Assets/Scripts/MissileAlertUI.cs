using Godot;

namespace SFS
{
    public partial class MissileAlertUI : Node3D
    {
        public Missile m_target;

        [Export] private MeshInstance3D m_mesh;
        [Export] private int m_dangerDistance;
        [Export] private int m_minDistance;
        [Export] private Color m_baseColor;
        [Export] private Vector3 m_baseSize;
        [Export] private Color m_dangerColor;
        [Export] private Vector3 m_dangerSize;
        public bool m_enabled { get; private set; } = false;

        public void TurnOff()
        {
            Hide();
            ProcessMode = ProcessModeEnum.Disabled;
            m_enabled = false;
        }

        public void TurnOn()
        {
            Show();
            ProcessMode = ProcessModeEnum.Inherit;
            m_enabled = true;
        }

        public override void _Process(double delta)
        {
            if (m_target == null || !IsInstanceValid(m_target) || !m_target.HasValidTarget())
            {
                TurnOff();
                return;
            }

            LookAt(m_target.GlobalPosition);

            float distance = GlobalPosition.DistanceTo(m_target.GlobalPosition);

            Material material = m_mesh.GetActiveMaterial(0);
            StandardMaterial3D standardMaterial = material as StandardMaterial3D;
            Debug.Assert(standardMaterial != null, "Standard material is null!");

            if (distance >= m_minDistance)
            {
                standardMaterial.AlbedoColor = m_baseColor;
                Scale = m_baseSize;
            }
            else if (distance <= m_dangerDistance)
            {
                standardMaterial.AlbedoColor = m_dangerColor;
                Scale = m_dangerSize;
            }
            else
            {
                float progession = (m_dangerDistance - distance) / m_dangerDistance;

                standardMaterial.AlbedoColor = m_baseColor.Lerp(m_dangerColor, progession);
                Scale = m_baseSize.Lerp(m_dangerSize, progession);
            }
        }
    }
}