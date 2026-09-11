using Godot;
using Godot.Collections;

namespace SFS
{
    [GlobalClass] public partial class EnemyWave : Node
    {
        /// <summary>
        /// SHOULD BE A BOXSHAPE3D
        /// </summary>
        [Export] public Array<CollisionShape3D> m_spawnAreas { get; private set; }
        [Export] public Array<PackedScene> m_enemies { get; private set; }
        [Export] public bool m_spawnAtOnce { get; private set; } = true;
        [Export] public double m_timeBetweenSpawns { get; private set; }
        [Export] public bool m_spawnLookingAtPlayer { get; private set; } = true;
        [Export] public Vector3 m_spawnRotation { get; private set; }

        /// <summary>
        /// Set to -1 to make infinite.
        /// </summary>
        [Export] public int m_numTimesSpawned { get; private set; } = 1;
        [Export] public float m_delay { get; private set; } = 1.0f;
    }
}
