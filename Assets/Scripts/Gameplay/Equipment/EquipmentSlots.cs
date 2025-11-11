// Assets\Scripts\UI\Widgets\InventorySlots\EquipmentSlots.cs
using UnityEngine;

namespace Game.Items
{
    /// <summary>Logical equipment slots.</summary>
    public enum EquipmentSlot
    {
        Helm,
        Gloves,
        Armor,
        Pants,
        Boots,
        Amulet,
        Ring1,
        Ring2,
        Pet,
        Orb,        // off-hand focus/booster for casters
        Wings,
        RightHand,
        LeftHand
    }

    /// <summary>Simplified mapping from ItemDefinition -> preferred slot(s).</summary>
    public static class EquipmentSlotMapper
    {
        public static bool TrySuggestSlot(ItemDefinition def, out EquipmentSlot slot, out EquipmentSlot alt)
        {
            alt = default;

            switch (def.subtype)
            {
                // Armor
                case ItemSubtype.Helmet: slot = EquipmentSlot.Helm; return true;
                case ItemSubtype.Gloves: slot = EquipmentSlot.Gloves; return true;
                case ItemSubtype.Boots: slot = EquipmentSlot.Boots; return true;
                case ItemSubtype.Pants: slot = EquipmentSlot.Pants; return true;
                case ItemSubtype.Chest: slot = EquipmentSlot.Armor; return true;

                // Accessories
                case ItemSubtype.Amulet: slot = EquipmentSlot.Amulet; return true;
                case ItemSubtype.Ring: slot = EquipmentSlot.Ring1; alt = EquipmentSlot.Ring2; return true;

                // Pets / Wings / Orbs
                case ItemSubtype.Pet: slot = EquipmentSlot.Pet; return true;
                case ItemSubtype.Wings: slot = EquipmentSlot.Wings; return true;
                case ItemSubtype.Orb: slot = EquipmentSlot.Orb; return true;

                // Off-hand shields
                case ItemSubtype.Shield: slot = EquipmentSlot.LeftHand; alt = EquipmentSlot.RightHand; return true;

                // Weapons (by grip)
                case ItemSubtype.Bow:
                case ItemSubtype.Staff:
                    // two-hand → use RightHand as the logical “owner”
                    slot = EquipmentSlot.RightHand; return true;

                case ItemSubtype.Sword:
                case ItemSubtype.Dagger:
                case ItemSubtype.Axe:
                case ItemSubtype.Mace:
                case ItemSubtype.Spear:
                    // one-hand default RightHand, alt LeftHand
                    slot = EquipmentSlot.RightHand; alt = EquipmentSlot.LeftHand; return true;

                default:
                    break;
            }

            // Fallback by category
            if (def.category == ItemCategory.Armor) { slot = EquipmentSlot.Armor; return true; }

            slot = EquipmentSlot.RightHand;
            return false;
        }

        public static bool IsTwoHanded(WeaponGrip grip) => grip == WeaponGrip.TwoHanded;
    }
}
