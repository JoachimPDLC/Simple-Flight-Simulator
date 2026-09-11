using Godot;
using Godot.Collections;
using System.Runtime.CompilerServices;

namespace SFS
{
    [GlobalClass]
    public partial class GameManager : Node
    {
        [ExportGroup("EnemyWaves")]
        [Export] private Array<EnemyWave> m_waves;
        [Export] private bool m_doNotSpawn = false;
        [Export] private PlayerController m_playerController;
        private double m_currentEnemySpawnTime = 0;
        private int m_currentWave = 0;
        private int m_currentEnemy = 0;
        private RandomNumberGenerator m_rng = new();
        private Plane m_player = null;
        private bool m_allEnemiesSpawned = false;
        private int m_numTimesCurrentWaveSpawned = 0;
        private float m_delayTimer = 0;
        private int m_areaSpawnIndex = 0;

        [ExportGroup("MissileGain")]
        [Export] public int m_minAmmoGain { get; private set; } = 3;
        [Export] public int m_maxAmmoGain { get; private set; } = 5;

        [ExportGroup("WorldBorders")]
        [Export] private Vector3I m_origin = Vector3I.Zero;
        [Export] private Vector3I m_borders;
        [Export] private int m_notifyDistance;
        [Export] private PlayerUI m_playerUI;
        [Export] private string m_horizontalMessage;
        [Export] private string m_verticalMessage;
        [Export] private int m_enemyBorderAddedRange = 3000;
        [Export] private int m_enemyKillDistance = 25000;

        public override void _Ready()
        {
            m_player = GetTree().GetFirstNodeInGroup("Player") as Plane;
            Debug.Assert(m_player != null, "GameManager could not find Player!");
            foreach (EnemyWave wave in m_waves)
            {
                foreach (CollisionShape3D area in wave.m_spawnAreas)
                {
                    Debug.Assert(area.Shape is BoxShape3D,
                    "Enemy wave created without a BoxShape3D as its area!");
                }
            }
        }

        public override void _Process(double delta)
        {
            UpdateBorders();

            UpdateSpawning(delta);
        }

        private void UpdateBorders()
        {
            Vector3 pos = m_player.GlobalPosition;
            int absX = (int)Mathf.Abs(pos.X);
            int absZ = (int)Mathf.Abs(pos.Z);

            if (absX > m_borders.X - m_notifyDistance ||
                absZ > m_borders.Z - m_notifyDistance)
            {
                m_playerUI.OnBorderApproach(m_horizontalMessage);
                m_player.PlayAlertBeep();
            }
            else if (pos.Y > m_borders.Y - m_notifyDistance)
            {
                m_playerUI.OnBorderApproach(m_verticalMessage);
                m_player.PlayAlertBeep();
            }

            if (absX > m_borders.X ||
                pos.Y > m_borders.Y ||
                pos.Z > m_borders.Z)
            {
                m_playerController.OnDeath(null);
            }

            float xBorder = m_borders.X + m_enemyBorderAddedRange;
            float zBorder = m_borders.Z + m_enemyBorderAddedRange;
            float yBorder = m_borders.Y + m_enemyBorderAddedRange;
            var enemies = GetTree().GetNodesInGroup("Enemies");
            foreach (var enemy in enemies)
            {
                if (enemy is Plane)
                {
                    var planeEnemy = enemy as Plane;

                    pos = (Vector3I)planeEnemy.GlobalPosition;
                    absX = (int)Mathf.Abs(pos.X);
                    absZ = (int)Mathf.Abs(pos.Z);
                    if (absX > xBorder ||
                        absZ > zBorder ||
                        pos.Y > yBorder)
                    {
                        planeEnemy.GetGameEntityComponent().Kill();
                    }
                    if (pos.DistanceTo(m_player.GlobalPosition) > m_enemyKillDistance)
                    {
                        planeEnemy.GetGameEntityComponent().Kill();
                    }
                }
            }
        }

        private void UpdateSpawning(double delta)
        {
            if (m_doNotSpawn)
                return;

            if (!GlobalSettings.sSpawnEnemies)
                return;

            if (m_currentEnemySpawnTime > 0)
                m_currentEnemySpawnTime -= delta;

            if (m_allEnemiesSpawned)
            {
                if (CheckToProgressWave())
                {
                    ++m_currentWave;
                    m_allEnemiesSpawned = false;
                    m_currentEnemy = 0;
                    m_delayTimer = m_waves[m_currentWave].m_delay;
                    m_areaSpawnIndex = m_rng.RandiRange(0, m_waves[m_currentWave].m_spawnAreas.Count - 1);
                    m_numTimesCurrentWaveSpawned = 0;
                }
                return;
            }

            if (m_delayTimer >= 0)
            {
                m_delayTimer -= (float)delta;
                if (m_delayTimer < 0)
                    m_playerUI.OnEnemyWaveSpawned();
                else
                    return;                
            }

            if (m_waves[m_currentWave].m_spawnAtOnce)
            {
                foreach (PackedScene enemy in m_waves[m_currentWave].m_enemies)
                {
                    SpawnEnemy(enemy);
                }
                m_allEnemiesSpawned = true;
                ++m_numTimesCurrentWaveSpawned;
            }
            else if (m_currentEnemySpawnTime <= 0)
            {
                SpawnEnemy(m_waves[m_currentWave].m_enemies[m_currentEnemy]);                
                ++m_currentEnemy;

                m_currentEnemySpawnTime = m_waves[m_currentWave].m_timeBetweenSpawns;

                if (m_currentEnemy >= m_waves[m_currentWave].m_enemies.Count)
                {
                    m_allEnemiesSpawned = true;
                    ++m_numTimesCurrentWaveSpawned;
                }
            }
        }

        private void SpawnEnemy(PackedScene enemy)
        {
            Node3D instantiatedNode = enemy.Instantiate() as Node3D;
            Utilities.GetFirstNode3D(this).AddChild(instantiatedNode);
            instantiatedNode.GlobalPosition = GenerateSpawnPoint();
            if (m_waves[m_currentWave].m_spawnLookingAtPlayer)
                instantiatedNode.LookAt(m_player.GlobalPosition, null, true);
            else
                instantiatedNode.GlobalRotationDegrees = m_waves[m_currentWave].m_spawnRotation;
        }

        private bool CheckToProgressWave()
        {
            int numEnemies = GetTree().GetNodesInGroup("Enemies").Count;

            if (m_allEnemiesSpawned && numEnemies == 0)
            {
                if (m_waves[m_currentWave].m_numTimesSpawned == -1 ||
                    m_numTimesCurrentWaveSpawned < 
                    m_waves[m_currentWave].m_numTimesSpawned)
                {
                    m_allEnemiesSpawned = false;
                    m_currentEnemy = 0;
                    m_delayTimer = m_waves[m_currentWave].m_delay;
                    m_areaSpawnIndex = m_rng.RandiRange(0, m_waves[m_currentWave].m_spawnAreas.Count - 1);
                    return false;
                }
                return true;
            }
            return false;
        }

        private Vector3 GenerateSpawnPoint()
        {
            Vector3 spawn = Vector3.Zero;
            BoxShape3D box = m_waves[m_currentWave].m_spawnAreas[m_areaSpawnIndex].Shape as BoxShape3D;
            Vector3 boxHalfSize = box.Size / 2;

            spawn.X = m_rng.RandfRange(-boxHalfSize.X, boxHalfSize.X);
            spawn.Y = m_rng.RandfRange(-boxHalfSize.Y, boxHalfSize.Y);
            spawn.Z = m_rng.RandfRange(-boxHalfSize.Z, boxHalfSize.Z);

            spawn += m_waves[m_currentWave].m_spawnAreas[m_areaSpawnIndex].GlobalPosition;

            return spawn;
        }
    }
}
