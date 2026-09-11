using Godot;

namespace SFS
{
    public interface IDamageable
    {
        void OnDamaged(Node _damager);
        void OnDeath(Node _damager);
    }
}
