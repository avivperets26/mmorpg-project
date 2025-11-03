using System.Collections.Generic;
using UnityEngine;
using Game.Items;

public class EquipmentController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;       // optional (for remove/add)
    [SerializeField] private PlayerStats playerStats;          // for requirements
    [SerializeField] private CharacterPreviewController preview;// optional (to refresh look)

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

    private readonly Dictionary<EquipmentSlot, ItemDefinition> _equipped = new();

    private void Awake()
    {
        // Init slot UIs → so they know their logical slot + controller
        InitSlot(EquipmentSlot.Helm, helm, "Helm");
        InitSlot(EquipmentSlot.Gloves, gloves, "Gloves");
        InitSlot(EquipmentSlot.Armor, armor, "Armor");
        InitSlot(EquipmentSlot.Pants, pants, "Pants");
        InitSlot(EquipmentSlot.Boots, boots, "Boots");
        InitSlot(EquipmentSlot.Amulet, amulet, "Amulet");
        InitSlot(EquipmentSlot.Ring1, ring1, "Ring1");
        InitSlot(EquipmentSlot.Ring2, ring2, "Ring2");
        InitSlot(EquipmentSlot.Pet, pet, "Pet");
        InitSlot(EquipmentSlot.Orb, orb, "Orb");
        InitSlot(EquipmentSlot.Wings, wings, "Wings");
        InitSlot(EquipmentSlot.RightHand, rightHand, "RightHand");
        InitSlot(EquipmentSlot.LeftHand, leftHand, "LeftHand");
    }

    private void InitSlot(EquipmentSlot slot, EquipmentSlotUI ui, string label)
    {
        if (!ui) return;
        ui.Init(slot, this);
        ui.SetPlaceholder(label);
        _equipped[slot] = null;
    }

    // ---------- Public API ----------
    public bool TryEquip(ItemDefinition def)
    {
        if (def == null) return false;

        // Map item → primary slot (e.g., Sword → RightHand/LeftHand, Helmet → Helm)
        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out var secondary))
        {
            Debug.Log($"[Equip] No slot mapping for {def.displayName} ({def.subtype})");
            return false;
        }

        // Requirements check
        if (!MeetsRequirements(def, out var reason))
        {
            Debug.Log($"[Equip] FAIL requirements for {def.displayName}: {reason}");
            return false;
        }

        // If two-handed, clear both hands first
        if (def.grip == WeaponGrip.TwoHanded)
        {
            TryUnequip(EquipmentSlot.RightHand);
            TryUnequip(EquipmentSlot.LeftHand);
        }

        // Choose a free hand for 1H weapons
        if (primary is EquipmentSlot.RightHand or EquipmentSlot.LeftHand)
        {
            var target = ChooseHandForOneHander(primary);
            return EquipIntoSlot(target, def);
        }

        // Armor / accessories into their exact slot
        return EquipIntoSlot(primary, def);
    }

    public bool TryUnequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var def) || def == null) return false;

        // Return to inventory if possible
        if (inventory && !inventory.TryAdd(def))
        {
            Debug.Log($"[Equip] Could not return {def.displayName} to inventory (no space).");
            return false;
        }

        ApplyStatsOnUnequip(def);
        _equipped[slot] = null;
        GetUI(slot)?.ShowItem(null);
        Debug.Log($"[Equip] Unequipped {def.displayName} from {slot}.");

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);
        return true;
    }

    // ---------- Internals ----------
    private bool EquipIntoSlot(EquipmentSlot slot, ItemDefinition def)
    {
        var current = _equipped.TryGetValue(slot, out var c) ? c : null;
        if (current != null) TryUnequip(slot);

        _equipped[slot] = def;
        GetUI(slot)?.ShowItem(def);
        ApplyStatsOnEquip(def);

        Debug.Log($"[Equip] Equipped {def.displayName} into {slot}.");
        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);
        return true;
    }

    private EquipmentSlotUI GetUI(EquipmentSlot slot) => slot switch
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

    private EquipmentSlot ChooseHandForOneHander(EquipmentSlot suggested)
    {
        // prefer RightHand unless occupied
        bool rightFree = !_equipped.TryGetValue(EquipmentSlot.RightHand, out var r) || r == null;
        bool leftFree = !_equipped.TryGetValue(EquipmentSlot.LeftHand, out var l) || l == null;

        if (rightFree) return EquipmentSlot.RightHand;
        if (leftFree) return EquipmentSlot.LeftHand;

        // both busy → replace suggested
        return suggested;
    }

    private bool MeetsRequirements(ItemDefinition def, out string reason)
    {
        reason = "";
        if (!playerStats) return true; // no stats component → allow

        // Level
        int playerLevel = playerStats.level;               // add ‘level’ to PlayerStats (see C below)
        if (playerLevel < def.requirements.level)
        {
            reason = $"Level {def.requirements.level} required";
            return false;
        }

        // Class flags (optional – enable if you use classes)
        // if ((def.requirements.usableBy & playerStats.classFlags) == 0) { ... }

        // Basic stats (if you’re tracking them)
        // if (playerStats.str < def.requirements.minStrength) { ... }

        return true;
    }

    private void ApplyStatsOnEquip(ItemDefinition def)
    {
        if (!playerStats) return;

        // weapons
        if (def.category == ItemCategory.Weapon) playerStats.AddWeapon(def.baseDamage);

        // armor/helm/boots etc.
        if (def.category == ItemCategory.Armor)
        {
            playerStats.AddArmor(def.baseDefense, def.baseMagicResist);
            playerStats.AddOnKill(def.hpOnKill, def.manaOnKill);
        }

        // boots movement (optional)
        if (def.subtype == ItemSubtype.Boots) playerStats.EquipBoots(1.2f);
    }

    private void ApplyStatsOnUnequip(ItemDefinition def)
    {
        if (!playerStats) return;

        if (def.category == ItemCategory.Weapon) playerStats.RemoveWeapon(def.baseDamage);

        if (def.category == ItemCategory.Armor)
        {
            playerStats.RemoveArmor(def.baseDefense, def.baseMagicResist);
            playerStats.RemoveOnKill(def.hpOnKill, def.manaOnKill);
        }

        if (def.subtype == ItemSubtype.Boots) playerStats.UnequipBoots();
    }
}
