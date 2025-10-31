namespace Game.Items
{
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

    // ---- Helpers (keep in the same namespace) ----
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
}
