// Assets\Scripts\Enemies\Runtime\EnemyLootOnDeath.cs
using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyLootOnDeath : MonoBehaviour
    {
        [Header("Loot Profile")]
        public EnemyDropTable dropTable;

        [Header("Spawn")]
        public float spawnHeight = 0.25f;
        public float scatterRadius = 0.4f;

        [Header("Debug")]
        public bool enableLogs = false;

        private EnemyHealth _health;

        void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            if (_health != null) _health.OnDeath += HandleDeath;
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

            // ✅ Roll gold amount based on enemy budget (lvl 1–3 mushroom = 1–9)
            int gold = dropTable.RollGoldAmount();

            // ✅ Choose visual tier that represents this amount
            var tier = dropTable.PickTierForGold(gold);
            if (tier == null || tier.prefab == null)
            {
                if (enableLogs) Debug.LogWarning("[Loot] No valid coin tier/prefab found.", this);
                return;
            }

            Vector3 pos = GetSpawnPos();
            var go = Instantiate(tier.prefab, pos, Quaternion.identity);

            // ✅ Force exact amount so visuals match your rolled budget
            var coinPickup = go.GetComponent<CoinWorldPickup>();
            if (coinPickup != null)
            {
                coinPickup.SetRange(gold, gold);
            }
            else
            {
                if (enableLogs)
                    Debug.LogWarning($"[Loot] Coin prefab '{tier.prefab.name}' is missing CoinWorldPickup on root.", go);
            }

            if (enableLogs) Debug.Log($"[Loot] Dropped {gold} gold using tier '{tier.name}'", this);
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

            return pos;
        }
    }
}
