// Assets\Scripts\Gameplay\Equipment\EquipmentController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;            // only used in RefreshUI() log-safe repaint
using UnityEngine;
using UnityEngine.UI;
using Game.Items;
using Game.Equipment;
using Game.Items.Definitions;

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

    [Header("Visuals")]
    [SerializeField] private EquipmentVisualsController visuals;

    [Header("Policies")]
    [Tooltip("When both hands are occupied and hovering a 1H item, prefer comparing/replacing Right Hand.")]
    [SerializeField] private bool preferRightHandOnTie = true;

    // Current equipped state (Definition-per-slot storage for now)
    private readonly Dictionary<EquipmentSlot, ItemDefinition> _equipped = new();

    // Track potion coroutines so new ones replace active effects cleanly
    private Coroutine healthPotionRoutine;
    private Coroutine manaPotionRoutine;

    public event Action EquippedChanged;
    private void RaiseEquippedChanged() => EquippedChanged?.Invoke();
    public event Action<bool> HasShieldChanged;

    private bool _hasShield; // cached left-hand shield state

    private void UpdateHasShieldFlag()
    {
        bool newHasShield = IsShield(GetEquipped(EquipmentSlot.LeftHand));
        if (newHasShield == _hasShield)
            return;

        _hasShield = newHasShield;
        HasShieldChanged?.Invoke(_hasShield);
    }
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

        if (preview) preview.Bind(this);
    }

    private void OnEnable() => RefreshUI();

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

        Debug.Log($"[Equip] TryEquip def='{def.displayName}', subtype={def.subtype}, fromInventory={fromInventory}, frame={Time.frameCount}");

        string reason;

        // ------------------------------------------------------------------
        // POTIONS: handle as consumables, not equippable gear
        // ------------------------------------------------------------------
        if (IsPotion(def))
        {
            Debug.Log($"[Equip] {def.displayName} detected as POTION ({def.subtype}) – routing to TryUsePotion.");
            return TryUsePotion(def, fromInventory);
        }

        // ------------------------------------------------------------------
        // SHIELDS: special handling
        // ------------------------------------------------------------------
        if (IsShield(def))
        {
            // Use "out reason" (no var)
            if (!MeetsRequirements(def, out reason))
            {
                Debug.Log($"[Equip] FAIL requirements for shield {def.displayName}: {reason}");
                HighlightInvalidSlot(def);
                return false;
            }

            var right = GetEquipped(EquipmentSlot.RightHand) as EquipmentItemDefinition;
            if (right != null && EquipmentSlotMapper.IsTwoHanded(right.grip))
            {
                Debug.Log("[Equip] Cannot equip shield while a two-handed weapon is equipped in RightHand.");
                return false;
            }

            return EquipIntoSlot(EquipmentSlot.LeftHand, def);
        }

        // ------------------------------------------------------------------
        // NORMAL EQUIP LOGIC
        // ------------------------------------------------------------------
        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out var secondary))
        {
            Debug.Log($"[Equip] No slot mapping for {def?.displayName} ({def?.subtype})");
            return false;
        }

        // Reuse same "reason" variable here too 👇
        if (!MeetsRequirements(def, out reason))
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

        // One-handed weapon → prefer a free hand, else suggested
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

        if (IsShield(def) && targetSlot != EquipmentSlot.LeftHand)
        {
            Debug.Log($"[Equip] Cannot equip shield '{def.displayName}' into {targetSlot}. Shields are LeftHand only.");
            return false;
        }

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

        if (def == null)
        {
            reason = "No item";
            return false;
        }

        if (IsShield(def))
        {
            if (targetSlot != EquipmentSlot.LeftHand)
            {
                reason = "Shield can only be equipped in Left Hand.";
                return false;
            }
            // If it's LeftHand, we still want to check level requirements:
            if (!MeetsRequirements(def, out reason))
                return false;

            return true; // slot + requirements OK
        }

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
        GetSlotUI(slot)?.ShowItem(null);

        visuals?.OnUnequipped(slot);

        Debug.Log($"[Equip] Unequipped {def.displayName} from {slot}.");

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        UpdateHasShieldFlag();
        RaiseEquippedChanged();
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
        GetSlotUI(slot)?.ShowItem(def);

        visuals?.OnEquipped(slot, def as IHasItemVisual);

        ApplyStatsOnEquip(def);

        Debug.Log($"[Equip] Equipped {def.displayName} into {slot} (frame={Time.frameCount}).");

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        UpdateHasShieldFlag();
        RaiseEquippedChanged();
        RefreshUI();
        DumpEquipped();
        return true;
    }

    /// <summary>Re-applies current equipped data to all slot UIs (safe after any UI rebuild).</summary>
    public void RefreshUI()
    {
        foreach (var kv in _equipped.ToArray())
        {
            var ui = GetSlotUI(kv.Key);
            if (ui) ui.ShowItem(kv.Value);
        }
        // #if UNITY_EDITOR
        // Debug.Log($"[Equip] RefreshUI repaint complete (frame={Time.frameCount}).");
        // #endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DumpEquipped()
    {
        foreach (var kv in _equipped)
            Debug.Log($"[Equip] SLOT {kv.Key} = {(kv.Value != null ? kv.Value.displayName : "NULL")}");
    }

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

        if (playerStats.level < def.requirements.level)
        {
            reason = $"Level {def.requirements.level} required";
            return false;
        }
        return true;
    }

    private void HighlightInvalidSlot(ItemDefinition def)
    {
        if (!EquipmentSlotMapper.TrySuggestSlot(def, out var primary, out _)) return;
        var ui = GetSlotUI(primary);
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

    // -------- Stat aggregation for PlayerStats (existing logic) --------
    private void ApplyStatsOnEquip(ItemDefinition def)
    {
        if (!playerStats) return;

        if (def.category == ItemCategory.Weapon) playerStats.AddWeapon(def.baseDamage);

        if (def.category == ItemCategory.Armor || def.subtype == ItemSubtype.Shield)
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

        if (def.category == ItemCategory.Armor || def.subtype == ItemSubtype.Shield)
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
        => _equipped.TryGetValue(slot, out var d) ? d : null;

    public ItemDefinition GetEquippedDefinition(EquipmentSlot slot) => GetEquipped(slot);

    public Game.Items.ItemInstance GetEquippedInstance(EquipmentSlot slot) => null;

    public EquipmentSlotUI GetSlotUI(EquipmentSlot slot) => slot switch
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

    public bool MeetsRequirementsPublic(ItemDefinition def) => MeetsRequirements(def, out _);

    // =====================================================================
    // --------- COMPARISON HELPERS (used by tooltip) ----------------------
    // =====================================================================

    // Read from the controller state only (no EquipmentSlotUI.CurrentDef needed).
    public ItemDefinition GetRightHandDef() => GetEquipped(EquipmentSlot.RightHand);
    public ItemDefinition GetLeftHandDef() => GetEquipped(EquipmentSlot.LeftHand);
    public ItemDefinition GetRing1Def() => GetEquipped(EquipmentSlot.Ring1);
    public ItemDefinition GetRing2Def() => GetEquipped(EquipmentSlot.Ring2);

    private static readonly ItemDefinition[] EMPTY_DEFS = new ItemDefinition[0];
    private readonly ItemDefinition[] _one = new ItemDefinition[1];
    private readonly ItemDefinition[] _two = new ItemDefinition[2];
    private readonly ItemDefinition[] _tmpTwoNonNull = new ItemDefinition[2];

    /// <summary>
    /// Resolves which slot(s) the hovered item would occupy and returns the currently
    /// equipped definition(s) that form the baseline to compare against.
    /// </summary>
    public bool GetEquippedForComparison(
        ItemDefinition hovered,
        out EquipmentSlot resolvedSlot,
        out ItemDefinition[] baselineDefs,
        out string replacementNote
    )
    {
        replacementNote = string.Empty;
        baselineDefs = EMPTY_DEFS;

        if (hovered == null)
        {
            resolvedSlot = EquipmentSlot.RightHand; // arbitrary safe default
            return false;
        }

        // Shields live only in LeftHand and should only compare against other shields.
        if (IsShield(hovered))
        {
            resolvedSlot = EquipmentSlot.LeftHand;
            var left = GetLeftHandDef();

            if (IsShield(left))
            {
                replacementNote = "Replacing: Left Hand Shield";
                _one[0] = left;
                baselineDefs = _one;
            }
            else
            {
                replacementNote = "Left Hand has no shield equipped.";
                baselineDefs = EMPTY_DEFS; // suppress compare against weapons
            }

            return true;
        }

        // Resolve mapping first so we always have a valid primary for resolvedSlot.
        if (!EquipmentSlotMapper.TrySuggestSlot(hovered, out var primary, out var secondary))
        {
            resolvedSlot = EquipmentSlot.RightHand; // safe fallback
            return false;
        }

        resolvedSlot = primary;

        bool isWeapon =
            primary == EquipmentSlot.RightHand || primary == EquipmentSlot.LeftHand ||
            secondary == EquipmentSlot.RightHand || secondary == EquipmentSlot.LeftHand;

        bool isRing = primary == EquipmentSlot.Ring1 || primary == EquipmentSlot.Ring2 || hovered.subtype == ItemSubtype.Ring;

        // --- 2H weapon ---
        if (hovered.grip == WeaponGrip.TwoHanded)
        {
            resolvedSlot = EquipmentSlot.RightHand; // install will occupy both hands
            var r = GetRightHandDef();
            var l = GetLeftHandDef();

            // Shields should not be used as weapon baselines.
            if (IsShield(r)) r = null;
            if (IsShield(l)) l = null;

            if (r == null && l == null)
            {
                replacementNote = "Replacing: Empty";
                baselineDefs = EMPTY_DEFS;
            }
            else
            {
                replacementNote = "Will occupy both hands.";
                _two[0] = r;
                _two[1] = l;
                baselineDefs = CopyNonNull(_two, 2, _tmpTwoNonNull);
            }
            return true;
        }

        // --- 1H weapon ---
        if (isWeapon && hovered.grip == WeaponGrip.OneHanded)
        {
            var right = GetRightHandDef();
            var left = GetLeftHandDef();

            // Only compare weapons vs. weapons; ignore shields.
            if (IsShield(right)) right = null;
            if (IsShield(left)) left = null;

            if (right != null && left != null)
            {
                if (preferRightHandOnTie)
                {
                    resolvedSlot = EquipmentSlot.RightHand;
                    replacementNote = "Replacing: Right Hand";
                    _one[0] = right;
                }
                else
                {
                    resolvedSlot = EquipmentSlot.LeftHand;
                    replacementNote = "Replacing: Left Hand";
                    _one[0] = left;
                }
                baselineDefs = _one;
            }
            else if (right != null)
            {
                resolvedSlot = EquipmentSlot.RightHand;
                replacementNote = "Replacing: Right Hand";
                _one[0] = right;
                baselineDefs = _one;
            }
            else if (left != null)
            {
                resolvedSlot = EquipmentSlot.LeftHand;
                replacementNote = "Replacing: Left Hand";
                _one[0] = left;
                baselineDefs = _one;
            }
            else
            {
                resolvedSlot = preferRightHandOnTie ? EquipmentSlot.RightHand : EquipmentSlot.LeftHand;
                replacementNote = "Replacing: Empty";
                baselineDefs = EMPTY_DEFS;
            }
            return true;
        }

        // --- Rings (MVP: compare to Ring1; if Ring1 empty → Empty) ---
        if (isRing)
        {
            var r1 = GetRing1Def();
            resolvedSlot = EquipmentSlot.Ring1;
            if (r1 == null)
            {
                replacementNote = "Replacing: Empty";
                baselineDefs = EMPTY_DEFS;
            }
            else
            {
                replacementNote = "Replacing: Ring 1";
                _one[0] = r1;
                baselineDefs = _one;
            }
            return true;
        }

        // --- Generic single-slot armor/etc. ---
        resolvedSlot = primary;
        var current = GetEquipped(primary);
        if (current == null)
        {
            replacementNote = "Replacing: Empty";
            baselineDefs = EMPTY_DEFS;
        }
        else
        {
            replacementNote = $"Replacing: {primary}";
            _one[0] = current;
            baselineDefs = _one;
        }
        return true;
    }

    /// <summary>Aggregate minimal stats for comparison over multiple defs (no allocs on hot path).</summary>
    public ItemStatsSnapshot GetCombinedStats(ItemDefinition[] defs)
    {
        var acc = ItemStatsSnapshot.Zero;
        if (defs == null) return acc;
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            if (d == null) continue;
            acc = ItemStatsSnapshot.Sum(acc, ItemStatsSnapshot.From(d));
        }
        return acc;
    }

    private static ItemDefinition[] CopyNonNull(ItemDefinition[] src, int count, ItemDefinition[] dst)
    {
        int j = 0;
        for (int i = 0; i < count; i++)
        {
            var d = src[i];
            if (d != null) dst[j++] = d;
        }
        if (j == 0) return new ItemDefinition[0];
        if (j == 1) { dst[1] = null; } // ensure clean
        return dst;
    }

    // ------------------------------------------------------------------
    // MoveEquip: needed by InventoryDragController (you referenced it)
    // ------------------------------------------------------------------
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

            // model state
            _equipped[EquipmentSlot.LeftHand] = null;
            _equipped[EquipmentSlot.RightHand] = def;

            // UI
            GetSlotUI(EquipmentSlot.LeftHand)?.ShowItem(null);
            GetSlotUI(EquipmentSlot.RightHand)?.ShowItem(def);

            // VISUALS
            visuals?.OnUnequipped(EquipmentSlot.LeftHand);
            visuals?.OnEquipped(EquipmentSlot.RightHand, def as IHasItemVisual);
        }
        else
        {
            // If target occupied, unequip it to inventory (policy); this will also clear its visual
            if (_equipped.TryGetValue(to, out var existing) && existing != null)
                TryUnequip(to); // assumes TryUnequip calls visuals?.OnUnequipped(to)

            // model state
            _equipped[from] = null;
            _equipped[to] = def;

            // UI
            GetSlotUI(from)?.ShowItem(null);
            GetSlotUI(to)?.ShowItem(def);

            // VISUALS
            visuals?.OnUnequipped(from);
            visuals?.OnEquipped(to, def as IHasItemVisual);
        }

        if (preview) preview.SendMessage("RefreshNow", SendMessageOptions.DontRequireReceiver);

        UpdateHasShieldFlag();
        RaiseEquippedChanged();
        RefreshUI();
        DumpEquipped();
        return true;
    }

    // =====================================================================
    // SHIELD HELPERS
    // =====================================================================

    private bool IsShield(ItemDefinition def)
    {
        if (def == null) return false;
        return def.category == ItemCategory.Shield || def.subtype == ItemSubtype.Shield;
    }

    // =====================================================================
    // POTION HELPERS
    // =====================================================================
    private bool IsPotion(ItemDefinition def)
    {
        bool result =
            def is PotionItemDefinition ||
            def.subtype == ItemSubtype.HealthPotion ||
            def.subtype == ItemSubtype.ManaPotion ||
            def.category == ItemCategory.Consumable;

        Debug.Log($"[Potion] IsPotion? def='{def.displayName}', category={def.category}, subtype={def.subtype} -> {result}");
        return result;
    }

    private bool TryUsePotion(ItemDefinition def, bool fromInventory)
    {
        if (!playerStats)
        {
            Debug.LogWarning("[Potion] No PlayerStats assigned on EquipmentController.", this);
            return false;
        }

        switch (def.subtype)
        {
            case ItemSubtype.HealthPotion:
                ApplyHealthPotion(def);
                break;

            case ItemSubtype.ManaPotion:
                ApplyManaPotion(def);
                break;

            default:
                Debug.LogWarning($"[Potion] Unknown potion subtype {def.subtype}", this);
                return false;
        }

        Debug.Log($"[Potion] Used {def.displayName} (fromInventory={fromInventory})", this);

        // NOTE: actual inventory removal is done by InventoryItemView
        // (after TryEquip/TryUsePotion returns true).

        return true;
    }

    private void ApplyHealthPotion(ItemDefinition def)
    {
        if (def is not PotionItemDefinition potion)
        {
            Debug.LogWarning($"[Potion] ApplyHealthPotion called with non-potion def '{def?.displayName}'");
            return;
        }

        if (potion.instantAmount > 0)
        {
            playerStats.Heal(potion.instantAmount);
            Debug.Log($"[Potion] Restored {potion.instantAmount} HP instantly from {def.displayName}.");
        }

        HandlePotionOverTime(potion, amount => playerStats.Heal(amount), isHealthPotion: true, "HP");
    }

    private void ApplyManaPotion(ItemDefinition def)
    {
        if (def is not PotionItemDefinition potion)
        {
            Debug.LogWarning($"[Potion] ApplyManaPotion called with non-potion def '{def?.displayName}'");
            return;
        }

        if (potion.instantAmount > 0)
        {
            playerStats.RestoreMana(potion.instantAmount);
            Debug.Log($"[Potion] Restored {potion.instantAmount} MP instantly from {def.displayName}.");
        }

        HandlePotionOverTime(potion, amount => playerStats.RestoreMana(amount), isHealthPotion: false, "MP");
    }

    private void HandlePotionOverTime(
        PotionItemDefinition potion,
        Action<int> applyAction,
        bool isHealthPotion,
        string resourceLabel)
    {
        if (potion.overTimeAmount <= 0)
            return;

        if (potion.overTimeDurationSeconds <= 0f)
        {
            applyAction?.Invoke(potion.overTimeAmount);
            Debug.Log($"[Potion] Applied {potion.overTimeAmount} {resourceLabel} instantly (no duration).");
            return;
        }

        var currentRoutine = isHealthPotion ? healthPotionRoutine : manaPotionRoutine;
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        var routineHandle = StartCoroutine(PotionOverTimeRoutine(
            potion.overTimeAmount,
            potion.overTimeDurationSeconds,
            potion.tickIntervalSeconds,
            applyAction,
            resourceLabel,
            () =>
            {
                if (isHealthPotion)
                    healthPotionRoutine = null;
                else
                    manaPotionRoutine = null;
            }));

        if (isHealthPotion)
            healthPotionRoutine = routineHandle;
        else
            manaPotionRoutine = routineHandle;
    }

    private IEnumerator PotionOverTimeRoutine(
        int totalAmount,
        float durationSeconds,
        float tickIntervalSeconds,
        Action<int> applyTick,
        string label,
        Action onComplete)
    {
        if (totalAmount <= 0 || applyTick == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float interval = Mathf.Max(0.05f, tickIntervalSeconds);
        float duration = Mathf.Max(interval, durationSeconds);
        int ticks = Mathf.Max(1, Mathf.CeilToInt(duration / interval));
        int applied = 0;

        for (int tick = 0; tick < ticks && applied < totalAmount; tick++)
        {
            yield return new WaitForSeconds(interval);

            int remainingTicks = ticks - tick;
            int remainingAmount = totalAmount - applied;
            if (remainingAmount <= 0)
                break;

            int toApply = Mathf.Max(1, Mathf.RoundToInt(remainingAmount / (float)remainingTicks));
            toApply = Mathf.Min(toApply, remainingAmount);

            applied += toApply;
            applyTick(toApply);
            Debug.Log($"[Potion] Over-time {label} tick {tick + 1}/{ticks} -> +{toApply}");
        }

        if (applied < totalAmount)
        {
            int leftover = totalAmount - applied;
            applyTick(leftover);
            Debug.Log($"[Potion] Applied leftover {label} amount {leftover}");
        }

        onComplete?.Invoke();
    }
}
