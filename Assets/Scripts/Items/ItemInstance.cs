using System.Collections.Generic;
using UnityEngine;

namespace Game.Items
{
    [System.Serializable]
    public class ItemInstance
    {
        public ItemDefinition def;

        public ItemTier tier;
        public bool isBlessed;
        [Range(0, 10)] public int upgradeLevel;  // +0 .. +10
        public int currentDurability;

        // sockets: up to def.socketsMax
        public List<GemDefinition> sockets = new();

        public ItemInstance(ItemDefinition def, ItemTier? forceTier = null)
        {
            this.def = def;
            tier = forceTier ?? def.defaultTier;
            isBlessed = false;
            upgradeLevel = 0;
            currentDurability = def.baseDurability;
        }

        public float TierMultiplier =>
                    tier == ItemTier.EventItem
                        ? 1f
                        : RarityRules.GetStatMultiplier(tier);

        public float UpgradeMultiplier =>
            1f + (0.02f * upgradeLevel); // example: +2% per level (edit to taste)

        public int EffectiveMinDamage =>
            Mathf.RoundToInt(def.baseDamage.min * TierMultiplier * UpgradeMultiplier);

        public int EffectiveMaxDamage =>
            Mathf.RoundToInt(def.baseDamage.max * TierMultiplier * UpgradeMultiplier);

        public int EffectiveWizardry =>
            Mathf.RoundToInt(def.baseDamage.wizardry * TierMultiplier * UpgradeMultiplier);

        public float EffectiveAttackSpeed => def.baseDamage.attackSpeed; // could scale if desired
        public float EffectiveCritChance => def.baseDamage.critChance;
        public float EffectiveCritMult => def.baseDamage.critMultiplier;

        public int EffectiveDefense =>
            Mathf.RoundToInt(def.baseDefense * TierMultiplier * UpgradeMultiplier);

        public int EffectiveMagicResist =>
            Mathf.RoundToInt(def.baseMagicResist * TierMultiplier * UpgradeMultiplier);

        public float EffectiveHpOnKill => def.hpOnKill;
        public float EffectiveManaOnKill => def.manaOnKill;

        public int EffectiveValue =>
            Mathf.Max(1, Mathf.RoundToInt(def.baseValue * TierMultiplier * UpgradeMultiplier * (isBlessed ? 1.15f : 1f)));

        public IEnumerable<string> BlessedLines()
        {
            if (!isBlessed) yield break;
            // Example placeholder: later you can branch by def.category/subtype.
            yield return "<b>Blessed</b>: +5% all stats, +10% durability loss resistance";
        }

        public IEnumerable<string> SocketLines()
        {
            for (int i = 0; i < def.socketsMax; i++)
            {
                if (i < sockets.Count && sockets[i] != null)
                {
                    yield return $"Socket {i + 1}: {sockets[i].displayName} — {sockets[i].GetSummary()}";
                }
                else
                {
                    yield return $"Socket {i + 1}: (empty)";
                }
            }
        }
    }
}
