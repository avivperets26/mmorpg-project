// Assets\Scripts\Enemies\Runtime\EnemyLootOnDeath.cs
using UnityEngine;
using Game.VFX;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyLootOnDeath : MonoBehaviour
    {
        [Header("Loot Profile")]
        public EnemyDropTable dropTable;

        [Header("VFX")]
        [Tooltip("Optional one-shot VFX played when coins drop (e.g. VFX_CoinBurst_Base).")]
        public GameObject coinBurstVfxPrefab;

        [Tooltip("Failsafe destroy time for the VFX in case Stop Action isn't set to Destroy.")]
        [Min(0.1f)]
        public float coinBurstVfxDestroyAfter = 2.0f;

        [Tooltip("If true, the VFX will align to the ground normal under the spawn point.")]
        public bool alignCoinBurstToGround = true;

        [Tooltip("Optional coin splash VFX spawner. If set, it controls splash + delayed pile spawn.")]
        [SerializeField] private CoinSplashVFXSpawner coinSplashVfxSpawner;

        [Header("Spawn")]
        public float spawnHeight = 0.25f;
        public float scatterRadius = 0.4f;

        [Header("Ground Snap")]
        public LayerMask groundMask = ~0;
        public float groundSnapDistance = 10f;
        public float groundSnapRaycastHeight = 1.5f;
        public bool groundSnapUseTriggers = false;

        [Header("Debug")]
        public bool enableLogs = false;

        private EnemyHealth _health;

        void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.OnDeath += HandleDeath;

            if (!coinSplashVfxSpawner)
                coinSplashVfxSpawner = GetComponent<CoinSplashVFXSpawner>();

            if (!coinSplashVfxSpawner)
                coinSplashVfxSpawner = GetComponentInChildren<CoinSplashVFXSpawner>(true);
        }

        void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath(EnemyHealth _)
        {
            if (!dropTable) return;

            DropCoins();
            DropItems();
        }

        private void DropCoins()
        {
            if (dropTable.coinTiers == null || dropTable.coinTiers.Count == 0) return;

            int gold = dropTable.RollGoldAmount();

            var tier = dropTable.PickTierForGold(gold);
            if (tier == null || tier.prefab == null)
            {
                if (enableLogs) Debug.LogWarning("[Loot] No valid coin tier/prefab found.", this);
                return;
            }

            Vector3 pos = GetSpawnPos();

            if (coinSplashVfxSpawner != null)
            {
                coinSplashVfxSpawner.PlaySplashAndSpawnPile(
                    pos,
                    tier.prefab,
                    null,
                    go =>
                    {
                        var coinPickup = go.GetComponent<CoinWorldPickup>();
                        if (coinPickup != null)
                            coinPickup.SetRange(gold, gold);
                        else if (enableLogs)
                            Debug.LogWarning($"[Loot] Coin prefab '{tier.prefab.name}' missing CoinWorldPickup on root.", go);

                        if (enableLogs) Debug.Log($"[Loot] Dropped {gold} gold at {pos}", this);
                    });
            }
            else
            {
                // Fallback to legacy behavior if no spawner is assigned.
                if (coinBurstVfxPrefab != null)
                    SpawnCoinBurstVfx(pos);

                StartCoroutine(SpawnCoinAfterDelay(tier.prefab, pos, gold, 0.12f));
            }
        }

        private System.Collections.IEnumerator SpawnCoinAfterDelay(GameObject coinPrefab, Vector3 pos, int gold, float delay)
        {
            yield return new WaitForSeconds(delay);

            var go = Instantiate(coinPrefab, pos, Quaternion.identity);

            var coinPickup = go.GetComponent<CoinWorldPickup>();
            if (coinPickup != null)
                coinPickup.SetRange(gold, gold);
            else if (enableLogs)
                Debug.LogWarning($"[Loot] Coin prefab '{coinPrefab.name}' missing CoinWorldPickup on root.", go);

            if (enableLogs) Debug.Log($"[Loot] Dropped {gold} gold at {pos}", this);
        }

        private void SpawnCoinBurstVfx(Vector3 pos)
        {
            if (!coinBurstVfxPrefab) return;

            Quaternion rot = Quaternion.identity;

            // Optional: align VFX to ground so it "sits" on slopes nicely.
            if (alignCoinBurstToGround)
            {
                // Raycast a bit above the spawn pos downward
                Vector3 origin = pos + Vector3.up * 0.5f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // Align "up" to ground normal
                    rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }

            var vfx = Instantiate(coinBurstVfxPrefab, pos, rot);

            // Failsafe cleanup (in case the prefab doesn't Destroy itself via Stop Action)
            if (coinBurstVfxDestroyAfter > 0f)
            {
                Destroy(vfx, coinBurstVfxDestroyAfter);
            }
        }

        private void DropItems()
        {
            if (dropTable.drops == null || dropTable.drops.Count == 0) return;

            foreach (var entry in dropTable.drops)
            {
                if (entry == null || entry.item == null) continue;

                if (Random.value <= entry.dropChance)
                {
                    int qty = Random.Range(entry.quantityRange.x, entry.quantityRange.y + 1);

                    // TODO: spawn your existing item pickup prefab logic here
                    // Example: ItemDropper.Spawn(entry.item, qty, GetSpawnPos());

                    if (enableLogs) Debug.Log($"[Loot] Dropped item: {entry.item.name} x{qty}", this);
                }
            }
        }

        private Vector3 GetSpawnPos()
        {
            Vector3 pos = transform.position + Vector3.up * spawnHeight;

            if (scatterRadius > 0f)
            {
                Vector2 r = Random.insideUnitCircle * scatterRadius;
                pos += new Vector3(r.x, 0f, r.y);
            }

            Vector3 origin = pos + Vector3.up * groundSnapRaycastHeight;
            var triggerMode = groundSnapUseTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            float maxDistance = Mathf.Max(groundSnapDistance, 0.1f);
            if (TryGetGroundHit(origin, maxDistance, groundMask, triggerMode, out RaycastHit hit))
                pos = hit.point;

            return pos;
        }

        private bool TryGetGroundHit(
            Vector3 origin,
            float maxDistance,
            LayerMask mask,
            QueryTriggerInteraction triggerMode,
            out RaycastHit bestHit)
        {
            bestHit = default;
            bool found = false;
            var hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, mask, triggerMode);

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                if (h.collider.transform.IsChildOf(transform)) continue;

                if (!found || h.distance < bestHit.distance)
                {
                    bestHit = h;
                    found = true;
                }
            }

            return found;
        }
    }
}

