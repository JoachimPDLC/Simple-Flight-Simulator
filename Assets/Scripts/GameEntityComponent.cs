using Godot;

namespace SFS
{
    [GlobalClass]
    public partial class GameEntityComponent : Node
    {
        [Export] private bool m_invincible = false;
        [Export] public uint m_maxHealth { get; private set; }
        public int m_health { get; private set; }
        public bool m_dead { get; private set; } = false;

        private IDamageable m_owner;

        public override void _Ready()
        {
            Debug.Assert(m_maxHealth > 0, "Maximum health cannot be less or equal to 0!");
            m_health = (int)m_maxHealth;
            Node parent = GetParent();
            Debug.Assert(parent is IDamageable, "Owner of GameEntity is not IDamageable!");
            m_owner = parent as IDamageable;
        }

        public void Damage(uint damage, Node damager,
            bool bypassInvincible = false)
        {
            if (m_dead)
                return;

            if (m_invincible && !bypassInvincible)
                return;
            
            m_health -= (int)damage;
            if (m_health <= 0)
            {
                m_dead = true;
                m_owner.OnDeath(damager);
            }
            else
                m_owner.OnDamaged(damager);
        }

        /// <summary>
        /// Kills the unit, bypassing invinicle. 
        /// </summary>
        public void Kill()
        {
            Damage(m_maxHealth, null, true);
        }
    }
}
