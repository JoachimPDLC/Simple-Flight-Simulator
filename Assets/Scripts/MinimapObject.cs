using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class MinimapObject : Sprite3D
    {
        // I can't make interfaces global classes, but this should be an ITeam!
        [Export] private Node3D m_object;
        [Export] private Teams kTeamsResource;
        [Export] private bool m_showOnStart = true;

        public override void _Ready()
        {
            Debug.Assert(m_object is ITeam, "Owning object should be an ITeam!");
            Debug.Assert(kTeamsResource.kColorsDic.Count > 0, "Colors dictionary is empty!");
            RotationDegrees = new(90, 0, 0);
            TopLevel = true;
            Visible = m_showOnStart;

            ITeam teamOwner = m_object as ITeam;
            Modulate = kTeamsResource.kColorsDic[teamOwner.GetTeam()];
        }

        public void SetColor(Teams.Team newTeam)
        {
            Modulate = kTeamsResource.kColorsDic[newTeam];
        }

        public override void _Process(double delta)
        {
            GlobalPosition = m_object.GlobalPosition;
            Vector3 rot = GlobalRotation;
            GlobalRotation = new(rot.X, m_object.GlobalRotation.Y, rot.Z);
        }
    }
}
