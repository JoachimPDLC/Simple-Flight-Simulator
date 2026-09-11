using Godot;

namespace SFS
{
    public abstract partial class SelectableWeapon : Node3D
    {
        abstract public void OnWeaponSelected();
        abstract public void OnWeaponDeselected();
        abstract public void Fire();
    }
}
