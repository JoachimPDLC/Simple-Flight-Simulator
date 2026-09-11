using Godot;

namespace SFS
{
    public partial class Controller : Node
    {
        [Export] public SFS.Plane m_plane { get; private set; } = null;
        [Export] public bool kControlDisabledOverride;
        public bool m_controlDisabled = true;

        virtual public void OnDamaged(Node _damager) {}
        virtual public void OnDeath(Node _damager) {}
        virtual public void OnKill() {}
    }
}
