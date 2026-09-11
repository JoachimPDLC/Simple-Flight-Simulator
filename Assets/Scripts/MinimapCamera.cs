using Godot;

namespace SFS
{
    public partial class MinimapCamera : Camera3D
    {
        [Export] private Node3D m_player;
        [Export] private Vector3 m_offset;

        public override void _Process(double delta)
        {
            GlobalPosition = m_player.GlobalPosition + m_offset;
            Vector3 rot = GlobalRotation;
            GlobalRotation = new(rot.X, m_player.GlobalRotation.Y, rot.Z);
        }
    }
}
