// Assets\Scripts\Gameplay\Items\Definitions\EquipmentEnums.cs
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
        Weapon = 0,
        Armor = 1,
        Accessory = 2,
        Consumable = 3,
        Material = 4,
        Shield = 5
    }

    // NOTE: Adding new enum values in the MIDDLE will reindex existing assets.
    // To stay safe, we APPEND new values at the END of the list.
    public enum ItemSubtype
    {
        // Weapons
        Sword, Axe, Bow, Dagger, Staff, Mace, Shield,

        // Armor
        Helmet, Chest, Gloves, Boots, Pants,

        // Accessories
        Ring, Amulet,

        // --- APPENDED (safe for existing serialized assets) -------------------
        Spear,     // weapon
        Orb,       // off-hand focus
        Book,      // off-hand tome
        Arrows,    // quiver
        Wings,     // back slot
        Pet,        // companion
        HealthPotion, // consumable
        ManaPotion   // consumable
    }

    // ---- Helpers (kept) ----
    public static class EquipmentMapping
    {
        // Category inference used by ItemDefinition.OnValidate
        public static ItemCategory GetCategoryForSubtype(ItemSubtype subtype) => subtype switch
        {
            // Armor
            ItemSubtype.Helmet or ItemSubtype.Chest or ItemSubtype.Gloves or ItemSubtype.Boots or ItemSubtype.Pants
                => ItemCategory.Armor,

            // Accessories
            ItemSubtype.Ring or ItemSubtype.Amulet or ItemSubtype.Wings or ItemSubtype.Pet or ItemSubtype.Orb or ItemSubtype.Book
                => ItemCategory.Accessory,

            // Consumables
            ItemSubtype.HealthPotion or ItemSubtype.ManaPotion
                => ItemCategory.Consumable,

            // NEW: Shields
            ItemSubtype.Shield
                => ItemCategory.Shield,

            // Everything else is a weapon
            _ => ItemCategory.Weapon
        };


        // category → subtype lists (used by the editor) -----------------
        private static readonly ItemSubtype[] WeaponSubtypes =
        {
        ItemSubtype.Sword,
        ItemSubtype.Axe,
        ItemSubtype.Bow,
        ItemSubtype.Dagger,
        ItemSubtype.Staff,
        ItemSubtype.Mace,
        ItemSubtype.Spear
    };

        private static readonly ItemSubtype[] ArmorSubtypes =
        {
        ItemSubtype.Helmet,
        ItemSubtype.Chest,
        ItemSubtype.Gloves,
        ItemSubtype.Boots,
        ItemSubtype.Pants
    };

        private static readonly ItemSubtype[] AccessorySubtypes =
        {
        ItemSubtype.Ring,
        ItemSubtype.Amulet,
        ItemSubtype.Orb,
        ItemSubtype.Book,
        ItemSubtype.Arrows,
        ItemSubtype.Wings,
        ItemSubtype.Pet
    };

        private static readonly ItemSubtype[] ConsumableSubtypes =
        {
        ItemSubtype.HealthPotion,
        ItemSubtype.ManaPotion
    };

        // For shields we keep an internal subtype, but we **won’t** show a dropdown.
        private static readonly ItemSubtype[] ShieldSubtypes =
        {
        ItemSubtype.Shield
    };

        public static bool CategoryHasSubtype(ItemCategory category) => category switch
        {
            ItemCategory.Weapon => true,
            ItemCategory.Armor => true,
            ItemCategory.Accessory => true,
            ItemCategory.Consumable => true,
            // Shield + Material currently have **no UI** subtype
            _ => false
        };

        public static ItemSubtype[] GetSubtypesForCategory(ItemCategory category) => category switch
        {
            ItemCategory.Weapon => WeaponSubtypes,
            ItemCategory.Armor => ArmorSubtypes,
            ItemCategory.Accessory => AccessorySubtypes,
            ItemCategory.Consumable => ConsumableSubtypes,
            ItemCategory.Shield => ShieldSubtypes, // internal, UI can hide it
            _ => System.Array.Empty<ItemSubtype>()
        };

        // Handy default if we ever need to set it automatically
        public static ItemSubtype GetDefaultSubtypeForCategory(ItemCategory category) => category switch
        {
            ItemCategory.Shield => ItemSubtype.Shield,
            ItemCategory.Weapon => WeaponSubtypes.Length > 0 ? WeaponSubtypes[0] : default,
            ItemCategory.Armor => ArmorSubtypes.Length > 0 ? ArmorSubtypes[0] : default,
            _ => default
        };
    }

    // Weapon handling for UI/labeling (doesn't replace slots)
    public enum WeaponGrip
    {
        None,        // non-weapon or unspecified
        OneHanded,
        TwoHanded,
        OffHandOnly  // arrows, shield, orb, book, etc.
    }

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
        public static ItemTier ToTier(this ItemRarity r) => r switch
        {
            ItemRarity.Common => ItemTier.Common,
            ItemRarity.Rare => ItemTier.Rare,
            ItemRarity.Legendary => ItemTier.Legendary,
            _ => ItemTier.Common
        };

        public static Color TierColor(ItemTier tier) => tier switch
        {
            ItemTier.Common => Color.grey,
            ItemTier.Magical => new Color(0.35f, 0.55f, 1f),  // Blue
            ItemTier.Rare => new Color(1f, 0.9f, 0.3f),       // Yellow
            ItemTier.UltraRare => new Color(1f, 0.55f, 0.15f),// Orange
            ItemTier.Epic => new Color(0.7f, 0.35f, 0.9f),    // Purple
            ItemTier.Legendary => new Color(0.35f, 1f, 0.35f),// Green
            ItemTier.Mythical => new Color(0.2f, 0.95f, 0.9f),// Turquoise
            ItemTier.Godlike => Color.white,
            ItemTier.EventItem => Color.white,
            _ => Color.white
        };
    }
}
