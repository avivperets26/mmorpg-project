// Assets/Scripts/Enemies/Runtime/EnemySpawnZone.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Spawns enemies inside a rectangular area and respawns them after death.
    /// Designed to be data-only in the inspector: you plug prefabs + level ranges.
    /// </summary>
    public class EnemySpawnZone : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnEntry
        {
            [Header("Prefab & Level")]
            public EnemyStats enemyPrefab;       // root of the enemy prefab
            [Min(1)] public int minLevel = 1;
            [Min(1)] public int maxLevel = 1;

            [Header("Weight")]
            [Range(0f, 1f)]
            public float weight = 1f;           // relative probability
        }

        [Header("Spawn List")]
        public List<SpawnEntry> enemies = new List<SpawnEntry>();

        [Header("Limits")]
        [Min(1)] public int maxAlive = 3;
        [Min(0f)] public float respawnDelay = 5f;

        [Header("Area")]
        public Transform areaCenter;
        public Vector3 areaSize = new Vector3(8f, 0f, 8f);

        [Header("Spawn Height")]
        [Tooltip("How far above the ground to spawn enemies so they can settle on terrain.")]
        public float spawnHeightOffset = 0.8f;

        [Header("Wiring")]
        public Transform parentForSpawns;      // optional; if null → this.transform
        [SerializeField] private LayerMask groundMask = ~0;

        private int _aliveCount;
        private readonly List<EnemyHealth> _tracked = new List<EnemyHealth>();

        private void Awake()
        {
            if (!areaCenter)
                areaCenter = transform;

            if (!parentForSpawns)
                parentForSpawns = transform;
        }

        private void Start()
        {
            FillToMax();
        }

        // --------------------------------------------------------------------
        // Spawning
        // --------------------------------------------------------------------

        private void FillToMax()
        {
            while (_aliveCount < maxAlive)
            {
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            var entry = ChooseRandomEntry();
            if (entry == null || entry.enemyPrefab == null)
                return;

            Vector3 spawnPos = GetRandomPointOnGround();
            Quaternion rotation = Quaternion.identity;

            var stats = Instantiate(entry.enemyPrefab, spawnPos, rotation, parentForSpawns);
            var health = stats.GetComponent<EnemyHealth>();

            // Randomize level within the configured range
            if (entry.minLevel <= entry.maxLevel)
            {
                stats.level = Random.Range(entry.minLevel, entry.maxLevel + 1);
                stats.RecalculateStats();

                if (health != null)
                    health.ResetHealth();
            }

            if (health != null)
            {
                _aliveCount++;
                _tracked.Add(health);
                health.OnDeath += HandleEnemyDeath;
            }
        }

        private SpawnEntry ChooseRandomEntry()
        {
            if (enemies == null || enemies.Count == 0)
                return null;

            float total = 0f;
            foreach (var e in enemies)
                total += Mathf.Max(0f, e.weight);

            if (total <= 0f)
                return enemies[0];

            float r = Random.value * total;
            float accum = 0f;

            foreach (var e in enemies)
            {
                accum += Mathf.Max(0f, e.weight);
                if (r <= accum)
                    return e;
            }

            return enemies[enemies.Count - 1];
        }

        private Vector3 GetRandomPointOnGround()
        {
            Vector3 center = areaCenter ? areaCenter.position : transform.position;

            float halfX = areaSize.x * 0.5f;
            float halfZ = areaSize.z * 0.5f;

            Vector3 pos = new Vector3(
                center.x + Random.Range(-halfX, halfX),
                center.y + 50f, // ray origin
                center.z + Random.Range(-halfZ, halfZ)
            );

            // Raycast down to find terrain/ground
            if (Physics.Raycast(pos, Vector3.down, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            {
                // spawn a bit above the hit point
                return hit.point + Vector3.up * spawnHeightOffset;
            }

            // If downward misses (e.g. wrong layer), try from below
            Vector3 fromBelow = new Vector3(pos.x, center.y - 50f, pos.z);
            if (Physics.Raycast(fromBelow, Vector3.up, out hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * spawnHeightOffset;
            }

            // Try terrain height as a last resort
            if (Terrain.activeTerrain)
            {
                float terrainY = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
                return new Vector3(pos.x, terrainY + spawnHeightOffset, pos.z);
            }

            // Fallback: place just above center if nothing was found
            pos.y = center.y + 1f;
            return pos;
        }



        // --------------------------------------------------------------------
        // Death / respawn
        // --------------------------------------------------------------------

        private void HandleEnemyDeath(EnemyHealth health)
        {
            health.OnDeath -= HandleEnemyDeath;
            _tracked.Remove(health);
            _aliveCount = Mathf.Max(0, _aliveCount - 1);

            if (gameObject.activeInHierarchy)
                StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            if (respawnDelay > 0f)
                yield return new WaitForSeconds(respawnDelay);

            if (_aliveCount < maxAlive)
                SpawnOne();
        }

        // --------------------------------------------------------------------
        // Gizmos
        // --------------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            Transform center = areaCenter ? areaCenter : transform;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center.position, new Vector3(areaSize.x, 0.1f, areaSize.z));
        }
    }
}
