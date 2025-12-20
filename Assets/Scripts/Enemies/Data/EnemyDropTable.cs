// Assets\Scripts\Enemies\Data\EnemyDropTable.cs
using System.Collections.Generic;
using UnityEngine;
using Game.Items;

namespace Game.Enemies
{
    /// <summary>
    /// Single source of truth for enemy loot.
    ///
    /// Best-practice model:
    /// 1) Roll a GOLD AMOUNT from goldBudget (per enemy / per level range).
    /// 2) Choose a VISUAL COIN TIER that can represent that amount (9/50/300/1k/10k piles).
    /// 3) Spawn the tier prefab and force the pickup to that exact amount.
    ///
    /// Items stay independent: dropChance + quantityRange.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDropTable", menuName = "Game/Enemies/Drop Table", order = 1)]
    public class EnemyDropTable : ScriptableObject
    {
        // -------------------- Coins --------------------

        [System.Serializable]
        public class CoinTier
        {
            [Tooltip("Just a label for designers (Small/Medium/Large/1k/10k...).")]
            public string name;

            [Tooltip("Prefab to spawn (pf_CoinPickup_9 / 50 / 300 / 1k / 10k...). Must contain CoinWorldPickup on root.")]
            public GameObject prefab;

            [Tooltip("Gold amounts that this prefab visually represents well. Example: 1–9 for pf_CoinPickup_9.")]
            public Vector2Int goldRange = new Vector2Int(1, 9);

            [Tooltip("Optional extra bias when multiple tiers can represent the same amount. Usually keep at 1.")]
            [Min(0)] public int weight = 1;
        }

        [Header("Coins")]
        [Tooltip("Roll a gold amount from this budget. For lvl 1–3 mushroom set to 1–9.")]
        public Vector2Int goldBudget = new Vector2Int(1, 9);

        [Tooltip("Available visual coin piles to represent the rolled amount.")]
        public List<CoinTier> coinTiers = new List<CoinTier>();

        // -------------------- Items --------------------

        [System.Serializable]
        public class DropEntry
        {
            public ItemDefinition item;

            [Range(0f, 1f)]
            public float dropChance = 0.2f;

            [Tooltip("Quantity range for stackable items (e.g. arrows 1–10). For equipment usually 1–1.")]
            public Vector2Int quantityRange = new Vector2Int(1, 1);
        }

        [Header("Item Drops")]
        public List<DropEntry> drops = new List<DropEntry>();

        // -------------------- Helpers --------------------

        /// <summary>
        /// Rolls the gold amount for this enemy.
        /// </summary>
        public int RollGoldAmount()
        {
            int min = Mathf.Max(0, goldBudget.x);
            int max = Mathf.Max(min, goldBudget.y);
            return Random.Range(min, max + 1);
        }

        /// <summary>
        /// Picks the best visual tier for a specific gold amount.
        /// Strategy:
        /// - Prefer tiers whose goldRange contains the amount.
        /// - Prefer the tightest matching tier (smallest range) to keep visuals honest.
        /// - If multiple tiers have the same tightness, use weight as a tie-breaker.
        /// - Fallback: if no tier contains the amount, use the "largest" tier (highest max range).
        /// </summary>
        public CoinTier PickTierForGold(int goldAmount)
        {
            if (coinTiers == null || coinTiers.Count == 0) return null;

            CoinTier best = null;
            int bestRangeSize = int.MaxValue;

            // 1) Find tiers that contain the gold amount.
            for (int i = 0; i < coinTiers.Count; i++)
            {
                var t = coinTiers[i];
                if (t == null || t.prefab == null) continue;

                int min = Mathf.Max(0, t.goldRange.x);
                int max = Mathf.Max(min, t.goldRange.y);

                if (goldAmount < min || goldAmount > max) continue;

                int rangeSize = max - min;
                if (rangeSize < bestRangeSize)
                {
                    best = t;
                    bestRangeSize = rangeSize;
                }
                else if (rangeSize == bestRangeSize && best != null)
                {
                    // Tie-breaker using weight (optional).
                    int a = Mathf.Max(0, t.weight);
                    int b = Mathf.Max(0, best.weight);

                    // If both are 0, keep current best.
                    if (a > 0 && (b == 0 || Random.Range(0, a + b) < a))
                        best = t;
                }
            }

            if (best != null) return best;

            // 2) Fallback: choose tier with the highest max range (largest pile).
            CoinTier largest = null;
            int largestMax = -1;

            for (int i = 0; i < coinTiers.Count; i++)
            {
                var t = coinTiers[i];
                if (t == null || t.prefab == null) continue;

                int max = Mathf.Max(Mathf.Max(0, t.goldRange.x), t.goldRange.y);
                if (max > largestMax)
                {
                    largestMax = max;
                    largest = t;
                }
            }

            return largest;
        }
    }
}
