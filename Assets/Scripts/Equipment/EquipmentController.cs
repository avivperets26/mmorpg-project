using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Items;

public class EquipmentController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;   // to return items on unequip
    [SerializeField] private PlayerStats playerStats;     // apply/remove stats
    [SerializeField] private CharacterPreviewController characterPreview; // optional, if you later want to swap meshes

    [Header("UI Slots")]
    [SerializeField] private EquipmentSlotUI helm;
    [SerializeField] private EquipmentSlotUI gloves;
    [SerializeField] private EquipmentSlotUI armor;
    [SerializeField] private EquipmentSlotUI pants;
    [SerializeField] private EquipmentSlotUI boots;
    [SerializeField] private EquipmentSlotUI amulet;
    [SerializeField] private EquipmentSlotUI ring1;
    [SerializeField] private EquipmentSlotUI ring2;
    [SerializeField] private EquipmentSlotUI pet;
    [SerializeField] private EquipmentSlotUI orb;
    [SerializeField] private EquipmentSlotUI wings;
    [SerializeField] private EquipmentSlotUI rightHand;
    [SerializeField] private EquipmentSlotUI leftHand;

    public event Action<EquipmentSlot, ItemDefinition> Equipped;
    public event Action<EquipmentSlot, ItemDefinition> Unequipped;

    private readonly Dictionary<EquipmentSlot, ItemDefinition> _equipped = new();

    private void Awake()
    {
        // Link back so slots can call us on click
        Map(EquipmentSlot.Helm, helm);
        Map(EquipmentSlot.Gloves, gloves);
        Map(EquipmentSlot.Armor, armor);
        Map(EquipmentSlot.Pants, pants);
        Map(EquipmentSlot.Boots, boots);
        Map(EquipmentSlot.Amulet, amulet);
        Map(EquipmentSlot.Ring1, ring1);
        Map(EquipmentSlot.Ring2, ring2);
        Map(EquipmentSlot.Pet, pet);
        Map(EquipmentSlot.Orb, orb);
        Map(EquipmentSlot.Wings, wings);
        Map(EquipmentSlot.RightHand, rightHand);
        Map(EquipmentSlot.LeftHand, leftHand);
    }

    private void Map(EquipmentSlot slot, EquipmentSlotUI ui)
    {
        if (!ui) return;
        ui.Init(slot, this);
        ui.SetPlaceholder(slot.ToString());
    }

    public ItemDefinition Get(EquipmentSlot slot) =>
        _equipped.TryGetValue(slot, out var def) ? def : null;

    /// <summary>Right-click from inventory: try equip this def to the best slot.</summary>
    public bool TryEquip(ItemDefinition def)
    {
        if (!def) return false;

        // Choose slot(s)
        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var slot, out var alt))
        {
            // fallback: weapons default to hands, armor/accessories to their obvious places handled above
        }

        // Two-handed: occupy RightHand (main) and clear LeftHand
        bool twoHanded = EquipmentSlotMapper.IsTwoHanded(def.grip);
        if (twoHanded)
        {
            if (!CanReplace(EquipmentSlot.RightHand)) return false;
            // ok to unequip LeftHand implicitly
            InternalEquipTo(EquipmentSlot.RightHand, def, replace: true);
            ClearIfExists(EquipmentSlot.LeftHand); // free off-hand
            return true;
        }

        // One-hand / accessories: try primary, else alt (for rings/hand swap)
        if (CanReplace(slot)) { InternalEquipTo(slot, def, replace: true); return true; }
        if (!Equals(alt, default(EquipmentSlot)) && CanReplace(alt)) { InternalEquipTo(alt, def, replace: true); return true; }

        // If both occupied and both are same “family” (e.g., Ring1+Ring2), allow replacing primary
        if (IsPair(slot) && InternalReplaceIfConfirmed(slot, def)) return true;

        return false;
    }

    /// <summary>Click slot → try unequip to inventory.</summary>
    public bool TryUnequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var def) || def == null) return false;

        // Try to return to inventory
        bool placed = inventory != null && inventory.TryAdd(def);
        if (!placed)
        {
            Debug.LogWarning($"[Equipment] No inventory space to unequip {def.displayName}");
            return false;
        }

        // Remove stats, clear slot
        ApplyRemove(def, apply: false);
        _equipped.Remove(slot);
        if (SlotUI(slot)) SlotUI(slot).ShowItem(null);

        Unequipped?.Invoke(slot, def);
        return true;
    }

    // ---------- Internals ----------

    private bool CanReplace(EquipmentSlot slot) => true; // later: add requirements checks

    private bool InternalReplaceIfConfirmed(EquipmentSlot slot, ItemDefinition def)
    {
        // MVP: always replace
        InternalEquipTo(slot, def, replace: true);
        return true;
    }

    private void InternalEquipTo(EquipmentSlot slot, ItemDefinition def, bool replace)
    {
        // If occupied → move old item back to inventory (if space), else drop replace
        if (_equipped.TryGetValue(slot, out var old) && old != null)
        {
            if (inventory != null && !inventory.TryAdd(old))
                Debug.LogWarning($"[Equipment] Replacing {old.displayName} but inventory is full.");

            ApplyRemove(old, apply: false);
        }

        _equipped[slot] = def;
        ApplyRemove(def, apply: true);

        var ui = SlotUI(slot);
        if (ui) ui.ShowItem(def);

        Equipped?.Invoke(slot, def);

        // (Optional) update visual character mesh/attachments here via CharacterPreviewController
    }

    private void ClearIfExists(EquipmentSlot slot)
    {
        if (_equipped.TryGetValue(slot, out var old) && old != null)
        {
            if (inventory != null && !inventory.TryAdd(old))
                Debug.LogWarning($"[Equipment] Clearing {slot} returned {old.displayName} but inventory full.");
            ApplyRemove(old, apply: false);
            _equipped.Remove(slot);
            if (SlotUI(slot)) SlotUI(slot).ShowItem(null);
            Unequipped?.Invoke(slot, old);
        }
    }

    private EquipmentSlotUI SlotUI(EquipmentSlot s) => s switch
    {
        EquipmentSlot.Helm => helm,
        EquipmentSlot.Gloves => gloves,
        EquipmentSlot.Armor => armor,
        EquipmentSlot.Pants => pants,
        EquipmentSlot.Boots => boots,
        EquipmentSlot.Amulet => amulet,
        EquipmentSlot.Ring1 => ring1,
        EquipmentSlot.Ring2 => ring2,
        EquipmentSlot.Pet => pet,
        EquipmentSlot.Orb => orb,
        EquipmentSlot.Wings => wings,
        EquipmentSlot.RightHand => rightHand,
        EquipmentSlot.LeftHand => leftHand,
        _ => null
    };

    private static bool IsPair(EquipmentSlot s) =>
        s == EquipmentSlot.Ring1 || s == EquipmentSlot.Ring2 || s == EquipmentSlot.RightHand || s == EquipmentSlot.LeftHand;

    private void ApplyRemove(ItemDefinition def, bool apply)
    {
        if (!playerStats || !def) return;

        // Weapons → damage; Armor → defense/resist; Jewelry → on-kill; Boots → speed, etc.
        switch (def.category)
        {
            case ItemCategory.Weapon:
                if (apply) playerStats.AddWeapon(def.baseDamage);
                else playerStats.RemoveWeapon(def.baseDamage);
                break;

            case ItemCategory.Armor:
                if (apply) playerStats.AddArmor(def.baseDefense, def.baseMagicResist);
                else playerStats.RemoveArmor(def.baseDefense, def.baseMagicResist);

                // Boots speed (example kept from your prior logic)
                if (def.subtype == ItemSubtype.Boots)
                {
                    if (apply) playerStats.EquipBoots(def.preview != null ? Mathf.Max(1f, def.preview.scale) : 1.2f);
                    else playerStats.UnequipBoots();
                }
                break;

            case ItemCategory.Accessory:
                if (apply) playerStats.AddOnKill(def.hpOnKill, def.manaOnKill);
                else playerStats.RemoveOnKill(def.hpOnKill, def.manaOnKill);
                break;

            default:
                // Materials/Consumables should not be equipable; ignore.
                break;
        }
    }
}
