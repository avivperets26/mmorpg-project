// Assets/Scripts/Game/Items/ItemStatsSnapshot.cs
using UnityEngine;

namespace Game.Items
{
    public struct ItemStatsSnapshot
    {
        public int dmgMin;
        public int dmgMax;
        public int wizardry;      // NEW
        public float atkSpeed;
        public float critChance;    // NEW (0..1)
        public float critMult;      // NEW (x)
        public int defense;
        public int magicResist;
        public float hpOnKill;
        public float mpOnKill;

        public static readonly ItemStatsSnapshot Zero = default;

        public static ItemStatsSnapshot From(ItemDefinition d)
        {
            if (d == null) return Zero;

            var tierMult = RarityRules.GetStatMultiplier(d.defaultTier);

            ItemStatsSnapshot s;
            s.dmgMin = Mathf.RoundToInt(d.baseDamage.min * tierMult);
            s.dmgMax = Mathf.RoundToInt(d.baseDamage.max * tierMult);
            s.wizardry = Mathf.RoundToInt(d.baseDamage.wizardry * tierMult);

            // These usually aren't tier-scaled; keep as-is unless you want that design:
            s.atkSpeed = d.baseDamage.attackSpeed;
            s.critChance = d.baseDamage.critChance;
            s.critMult = d.baseDamage.critMultiplier;

            s.defense = Mathf.RoundToInt(d.baseDefense * tierMult);
            s.magicResist = Mathf.RoundToInt(d.baseMagicResist * tierMult);
            s.hpOnKill = d.hpOnKill;
            s.mpOnKill = d.manaOnKill;

            return s;
        }


        public static ItemStatsSnapshot Sum(ItemStatsSnapshot a, ItemStatsSnapshot b)
        {
            ItemStatsSnapshot s;
            s.dmgMin = a.dmgMin + b.dmgMin;
            s.dmgMax = a.dmgMax + b.dmgMax;
            s.wizardry = a.wizardry + b.wizardry;
            s.atkSpeed = a.atkSpeed + b.atkSpeed;
            s.critChance = a.critChance + b.critChance;
            s.critMult = a.critMult + b.critMult;
            s.defense = a.defense + b.defense;
            s.magicResist = a.magicResist + b.magicResist;
            s.hpOnKill = a.hpOnKill + b.hpOnKill;
            s.mpOnKill = a.mpOnKill + b.mpOnKill;
            return s;
        }

        // other - this
        public ItemStatsSnapshot DiffTo(ItemStatsSnapshot other)
        {
            ItemStatsSnapshot s;
            s.dmgMin = other.dmgMin - dmgMin;
            s.dmgMax = other.dmgMax - dmgMax;
            s.wizardry = other.wizardry - wizardry;
            s.atkSpeed = other.atkSpeed - atkSpeed;
            s.critChance = other.critChance - critChance;
            s.critMult = other.critMult - critMult;
            s.defense = other.defense - defense;
            s.magicResist = other.magicResist - magicResist;
            s.hpOnKill = other.hpOnKill - hpOnKill;
            s.mpOnKill = other.mpOnKill - mpOnKill;
            return s;
        }

        public bool IsAllZero()
        {
            return dmgMin == 0 && dmgMax == 0 && wizardry == 0 &&
                   Mathf.Approximately(atkSpeed, 0f) &&
                   Mathf.Approximately(critChance, 0f) &&
                   Mathf.Approximately(critMult, 0f) &&
                   defense == 0 && magicResist == 0 &&
                   Mathf.Approximately(hpOnKill, 0f) &&
                   Mathf.Approximately(mpOnKill, 0f);
        }
    }
}
