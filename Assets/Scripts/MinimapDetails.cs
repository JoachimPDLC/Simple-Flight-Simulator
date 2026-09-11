using Godot;
using Godot.Collections;

namespace SFS
{
    public partial class MinimapDetails : Node3D
    {
        [Export] Camera3D m_minimapCamera;
        [Export] Array<Label3D> m_labels;

        public override void _Process(double delta)
        {
            float camRotY = m_minimapCamera.GlobalRotation.Y;
            foreach (Label3D label in m_labels)
            {
                Vector3 newRot = label.GlobalRotation;
                newRot.Y = camRotY;
                label.GlobalRotation = newRot;
            }
        }
    }
}
