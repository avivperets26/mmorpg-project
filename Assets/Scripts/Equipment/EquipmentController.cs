// Assets/Scripts/Equipment/EquipmentController.cs
using System;
using System.Collections.Generic;
using System.Linq;            // for ToArray in RefreshUI()
using UnityEngine;
using UnityEngine.UI;
using Game.Items;

public class EquipmentController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private CharacterPreviewController preview;

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

    // Current equipped state (Definition-per-slot storage for now)
    private readonly Dictionary<EquipmentSlot, ItemDefinition> _equipped = new();

    // ---- Tooltip/slots can subscribe to be notified when equipment changes ----
    public event Action EquippedChanged;
    private void RaiseEquippedChanged() => EquippedChanged?.Invoke();

    private void Awake()
    {
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

    private void OnEnable()
    {
        // If UI elements were rebuilt while disabled, this re-applies the visuals.
        RefreshUI();
    }

    private void InitSlot(EquipmentSlot slot, EquipmentSlotUI ui, string label)
    {
        if (!ui) return;
        ui.Init(slot, this);
        ui.SetPlaceholder(label);
        _equipped[slot] = null;
    }

    // =====================================================================
    // Public API (generic auto-slot)
    // =====================================================================
    public bool TryEquip(ItemDefinition def, bool fromInventory = false)
    {
        if (def == null) return false;

        Debug.Log($"[Equip] TryEquip def='{def.displayName}', fromInventory={fromInventory}, frame={Time.frameCount}");

        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out var secondary))
        {
            Debug.Log($"[Equip] No slot mapping for {def.displayName} ({def.subtype})");
            return false;
        }

        if (!MeetsRequirements(def, out var reason))
        {
            Debug.Log($"[Equip] FAIL requirements for {def.displayName}: {reason}");
            HighlightInvalidSlot(def);
            return false;
        }

        // Two-handed → clear both, then use RightHand as the actual owner
        if (def.grip == WeaponGrip.TwoHanded)
        {
            TryUnequip(EquipmentSlot.RightHand);
            TryUnequip(EquipmentSlot.LeftHand);
            return EquipIntoSlot(EquipmentSlot.RightHand, def);
        }

        // One-handed weapon → prefer a free hand
        if (primary is EquipmentSlot.RightHand or EquipmentSlot.LeftHand)
        {
            var target = ChooseHandForOneHander(primary);
            return EquipIntoSlot(target, def);
        }

        return EquipIntoSlot(primary, def);
    }

    // =====================================================================
    // Public API (explicit slot – used by drag & hover)
    // =====================================================================
    public bool TryEquipInto(EquipmentSlot targetSlot, ItemDefinition def, bool fromInventory = false)
    {
        if (def == null) return false;

        Debug.Log($"[Equip] TryEquipInto slot={targetSlot}, def='{def.displayName}', fromInventory={fromInventory}, frame={Time.frameCount}");

        if (!PreviewCanEquip(def, targetSlot, out var reason))
        {
            Debug.Log($"[Equip] Cannot equip {def.displayName} into {targetSlot}: {reason}");
            return false;
        }

        // If it's a two-hander dropped on either hand, clear both and use RightHand.
        if (def.grip == WeaponGrip.TwoHanded &&
            (targetSlot == EquipmentSlot.LeftHand || targetSlot == EquipmentSlot.RightHand))
        {
            TryUnequip(EquipmentSlot.RightHand);
            TryUnequip(EquipmentSlot.LeftHand);
            return EquipIntoSlot(EquipmentSlot.RightHand, def);
        }

        return EquipIntoSlot(targetSlot, def);
    }

    /// <summary>For hover UI: tells if item would equip into a specific slot and why/why not.</summary>
    public bool PreviewCanEquip(ItemDefinition def, EquipmentSlot targetSlot, out string reason)
    {
        reason = "";

        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out var secondary))
        {
            reason = "No slot mapping";
            return false;
        }

        // slot compatibility
        bool slotOk =
            targetSlot == primary ||
            (secondary != default && targetSlot == secondary) ||
            (def.grip == WeaponGrip.TwoHanded && (targetSlot == EquipmentSlot.RightHand || targetSlot == EquipmentSlot.LeftHand));

        if (!slotOk)
        {
            reason = $"Wrong slot ({targetSlot})";
            return false;
        }

        // requirements
        if (!MeetsRequirements(def, out reason))
            return false;

        return true;
    }

    // =====================================================================
    // Unequip
    // =====================================================================
    public bool TryUnequip(EquipmentSlot slot)
    {
        Debug.Log($"[Equip] TryUnequip CALLED for {slot} (frame={Time.frameCount}). " +
                  $"LastEquipDropFrame={InventoryDragController.LastEquipDropFrame}, IsDragging={InventoryDragController.IsDragging}");

        if (!_equipped.TryGetValue(slot, out var def) || def == null)
            return false;

        if (inventory && !inventory.TryAdd(def))
        {
            Debug.Log($"[Equip] Could not return {def.displayName} to inventory (no space).");
            return false;
        }

        ApplyStatsOnUnequip(def);
        _equipped[slot] = null;

        // Update just that slot immediately
        GetUI(slot)?.ShowItem(null);

        Debug.Log($"[Equip] Unequipped {def.displayName} from {slot}.");

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        // Inform listeners (tooltips, slot binders, etc.)
        RaiseEquippedChanged();

        // Defensive repaint in case any UI rebuilt this frame
        RefreshUI();
        return true;
    }

    // =====================================================================
    // Internals
    // =====================================================================
    private bool EquipIntoSlot(EquipmentSlot slot, ItemDefinition def)
    {
        var current = _equipped.TryGetValue(slot, out var c) ? c : null;
        if (current != null) TryUnequip(slot);

        _equipped[slot] = def;

        // Update the specific slot immediately
        GetUI(slot)?.ShowItem(def);

        ApplyStatsOnEquip(def);

        Debug.Log($"[Equip] Equipped {def.displayName} into {slot} (frame={Time.frameCount}).");

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        // Inform listeners (tooltips, slot binders, etc.)
        RaiseEquippedChanged();

        // If any UI elements were rebuilt during the drag/drop, make sure all slot UIs match state.
        RefreshUI();
        DumpEquipped();
        return true;
    }

    /// <summary>Re-applies current equipped data to all slot UIs (safe after any UI rebuild).</summary>
    public void RefreshUI()
    {
        foreach (var kv in _equipped.ToArray())
        {
            var ui = GetUI(kv.Key);
            if (ui) ui.ShowItem(kv.Value);
        }
#if UNITY_EDITOR
        Debug.Log($"[Equip] RefreshUI repaint complete (frame={Time.frameCount}).");
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DumpEquipped()
    {
        foreach (var kv in _equipped)
            Debug.Log($"[Equip] SLOT {kv.Key} = {(kv.Value != null ? kv.Value.displayName : "NULL")}");
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
        bool rightFree = !_equipped.TryGetValue(EquipmentSlot.RightHand, out var r) || r == null;
        bool leftFree = !_equipped.TryGetValue(EquipmentSlot.LeftHand, out var l) || l == null;

        if (rightFree) return EquipmentSlot.RightHand;
        if (leftFree) return EquipmentSlot.LeftHand;
        return suggested; // replace suggested if both busy
    }

    private bool MeetsRequirements(ItemDefinition def, out string reason)
    {
        reason = "";
        if (!playerStats) return true;

        // Level
        if (playerStats.level < def.requirements.level)
        {
            reason = $"Level {def.requirements.level} required";
            return false;
        }

        // TODO: add STR/DEX/etc checks if/when you have them.
        return true;
    }

    // -------- Visual: brief red flash used when generic equip fails --------
    private void HighlightInvalidSlot(ItemDefinition def)
    {
        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out _)) return;
        var ui = GetUI(primary);
        if (!ui) return;

        var img = ui.GetComponent<Image>();
        if (!img) return;

        Color orig = img.color;
        img.color = new Color(1f, 0f, 0f, 0.35f);
        StartCoroutine(RestoreSlotColor(img, orig, 0.35f));
    }

    private System.Collections.IEnumerator RestoreSlotColor(Image img, Color orig, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (img) img.color = orig;
    }

    // -------- Stat aggregation --------
    private void ApplyStatsOnEquip(ItemDefinition def)
    {
        if (!playerStats) return;

        if (def.category == ItemCategory.Weapon) playerStats.AddWeapon(def.baseDamage);

        if (def.category == ItemCategory.Armor)
        {
            playerStats.AddArmor(def.baseDefense, def.baseMagicResist);
            playerStats.AddOnKill(def.hpOnKill, def.manaOnKill);
        }

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

    // ----------------------------------------------------------------------
    // Public accessors for external scripts (tooltips, drag controller, etc.)
    // ----------------------------------------------------------------------
    public ItemDefinition GetEquipped(EquipmentSlot slot)
    {
        return _equipped.TryGetValue(slot, out var d) ? d : null;
    }

    /// <summary>
    /// If in future you store real instances per slot, return them here.
    /// For now, return null so tooltip binder creates a temporary ItemInstance from definition.
    /// </summary>
    public Game.Items.ItemInstance GetEquippedInstance(EquipmentSlot slot)
    {
        return null;
    }

    /// <summary>Current equipped definition in a slot (null if empty).</summary>
    public ItemDefinition GetEquippedDefinition(EquipmentSlot slot)
    {
        return GetEquipped(slot);
    }

    /// <summary>Move the equipped item from one slot to another without passing through the inventory.</summary>
    public bool MoveEquip(EquipmentSlot from, EquipmentSlot to)
    {
        if (from == to) return true;

        var def = GetEquipped(from);
        if (def == null) return false;

        if (!PreviewCanEquip(def, to, out var reason))
        {
            Debug.Log($"[Equip] MoveEquip FAIL {from}->{to}: {reason}");
            return false;
        }

        // Two-handers always end up in RightHand; clear both hands first
        if (def.grip == WeaponGrip.TwoHanded &&
            (to == EquipmentSlot.LeftHand || to == EquipmentSlot.RightHand))
        {
            to = EquipmentSlot.RightHand;
            _equipped[EquipmentSlot.LeftHand] = null;
            _equipped[EquipmentSlot.RightHand] = def;
            GetUI(EquipmentSlot.LeftHand)?.ShowItem(null);
            GetUI(EquipmentSlot.RightHand)?.ShowItem(def);
        }
        else
        {
            // If target occupied, unequip it to inventory (optional policy)
            if (_equipped.TryGetValue(to, out var existing) && existing != null)
                TryUnequip(to);

            _equipped[from] = null;
            _equipped[to] = def;

            GetUI(from)?.ShowItem(null);
            GetUI(to)?.ShowItem(def);
        }

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        // Inform listeners
        RaiseEquippedChanged();

        RefreshUI();
        DumpEquipped();
        return true;
    }

    // ----------------------------------------------------------------------
    // Public accessor for external scripts (like InventoryDragController)
    // ----------------------------------------------------------------------
    public EquipmentSlotUI GetSlotUI(EquipmentSlot slot)
    {
        return slot switch
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
    }

    public bool MeetsRequirementsPublic(ItemDefinition def)
    {
        return MeetsRequirements(def, out _);
    }
}
