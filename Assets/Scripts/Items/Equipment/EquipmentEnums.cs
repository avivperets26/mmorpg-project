using UnityEngine;

namespace Game.Items
{
    // --- Existing (kept) ------------------------------------------------------
    public enum ItemRarity
    {
        Common,
        Rare,
        Legendary
        // add Uncommon/Epic if you want later
    }

    public enum ItemCategory
    {
        Weapon,
        Armor,
        Accessory,
        Consumable,
        Material
    }

    // Specific item kind (used for filtering, naming, etc.)
    public enum ItemSubtype
    {
        // Weapons
        Sword, Axe, Bow, Dagger, Staff, Mace, Shield,

        // Armor
        Helmet, Chest, Gloves, Boots, Pants,

        // Accessories
        Ring, Amulet
    }

    // Slot on the character model where this item is equipped
    public enum EquipmentSlot
    {
        Head,       // Helmet
        Chest,      // Chest armor
        Hands,      // Gloves
        Legs,       // Pants
        Feet,       // Boots
        MainHand,   // Weapon
        OffHand,    // Shield or secondary
        Ring1,      // Accessory
        Ring2,      // Accessory
        Amulet      // Accessory
    }

    // ---- Helpers (kept) ----
    public static class EquipmentMapping
    {
        public static EquipmentSlot GetSlotForSubtype(ItemSubtype subtype) => subtype switch
        {
            ItemSubtype.Helmet => EquipmentSlot.Head,
            ItemSubtype.Chest => EquipmentSlot.Chest,
            ItemSubtype.Gloves => EquipmentSlot.Hands,
            ItemSubtype.Boots => EquipmentSlot.Feet,
            ItemSubtype.Pants => EquipmentSlot.Legs,
            ItemSubtype.Shield => EquipmentSlot.OffHand,
            _ => EquipmentSlot.MainHand
        };

        public static ItemCategory GetCategoryForSubtype(ItemSubtype subtype) => subtype switch
        {
            ItemSubtype.Helmet or ItemSubtype.Chest or ItemSubtype.Gloves or ItemSubtype.Boots or ItemSubtype.Pants
                => ItemCategory.Armor,

            ItemSubtype.Ring or ItemSubtype.Amulet
                => ItemCategory.Accessory,

            _ => ItemCategory.Weapon
        };
    }

    // --- New additions (from the spec) ---------------------------------------

    // Who can equip (flags so you can combine: Knight|Elf, etc.)
    [System.Flags]
    public enum CharacterClass
    {
        None = 0,
        Knight = 1 << 0,
        Elf = 1 << 1,
        Wizard = 1 << 2,
        All = ~0
    }

    // What kind of socket the item supports (weapon/armor/jewelry)
    public enum SocketSlotType
    {
        Weapon,
        Armor,
        Jewelry
    }

    // Full 9-tier rarity (your existing ItemRarity stays for compat)
    public enum ItemTier
    {
        Common,       // Grey
        Magical,      // Blue
        Rare,         // Yellow
        UltraRare,    // Orange
        Epic,         // Purple
        Legendary,    // Green
        Mythical,     // Turquoise
        Godlike,      // Special FX
        EventItem     // White (no scaling)
    }

    // Requirements block for items
    [System.Serializable]
    public struct ItemRequirements
    {
        public int level;
        public CharacterClass usableBy; // flags
        public int minStrength;
        public int minDexterity;
        public int minEnergy;
    }

    // Damage / Combat block (covers physical and wizardry)
    [System.Serializable]
    public struct DamageProfile
    {
        public int min;
        public int max;
        public int wizardry;         // >0 for magic weapons; 0 otherwise
        public float critChance;     // 0..1
        public float critMultiplier; // e.g. 1.5 = +50%
        public float attackSpeed;    // attacks per second or normalized
    }

    // --- Small helpers to bridge old -> new ----------------------------------

    public static class RarityTierBridge
    {
        // Use while migrating: map your old ItemRarity to the richer ItemTier
        public static ItemTier ToTier(this ItemRarity r) => r switch
        {
            ItemRarity.Common => ItemTier.Common,
            ItemRarity.Rare => ItemTier.Rare,
            ItemRarity.Legendary => ItemTier.Legendary,
            _ => ItemTier.Common
        };

        // Palette suggestion for ItemTier labels (ui convenience)
        public static Color TierColor(ItemTier tier) => tier switch
        {
            ItemTier.Common => Color.grey,
            ItemTier.Magical => new Color(0.35f, 0.55f, 1f),      // Blue
            ItemTier.Rare => new Color(1f, 0.9f, 0.3f),         // Yellow
            ItemTier.UltraRare => new Color(1f, 0.55f, 0.15f),       // Orange
            ItemTier.Epic => new Color(0.7f, 0.35f, 0.9f),      // Purple
            ItemTier.Legendary => new Color(0.35f, 1f, 0.35f),       // Green
            ItemTier.Mythical => new Color(0.2f, 0.95f, 0.9f),      // Turquoise
            ItemTier.Godlike => Color.white,                       // animate later
            ItemTier.EventItem => Color.white,
            _ => Color.white
        };
    }
}
