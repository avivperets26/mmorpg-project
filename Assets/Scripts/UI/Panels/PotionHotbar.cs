using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Items;

/// <summary>
/// Manages 3 potion slots on the Bottom HUD (Q / W / E).
/// - Aggregates inventory stacks per potion type
/// - Chooses which definition to show per type based on pickup order
/// - Handles hotkeys and global cooldown (logic only; overlay driven via PotionSlotUI)
/// </summary>
[DisallowMultipleComponent]
public class PotionHotbar : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private EquipmentController equipment;
    [SerializeField] private PlayerStats playerStats;

    [Header("Slots")]
    [SerializeField] private PotionSlotUI slotQ;
    [SerializeField] private PotionSlotUI slotW;
    [SerializeField] private PotionSlotUI slotE;

    [Header("Cooldown")]
    [Tooltip("Global potion cooldown in seconds (applies to all slots).")]
    [SerializeField] private float globalCooldownSeconds = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float _cooldownEndTime = 0f;

    // For each potion type, remember the order in which we first saw each definition.
    // This lets us respect "first picked up Medium HP, then Small HP decides later".
    private readonly Dictionary<PotionType, List<PotionItemDefinition>> _orderPerType = new();

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        inventory ??= FindFirstObjectByType<PlayerInventory>();
        equipment ??= FindFirstObjectByType<EquipmentController>();
        playerStats ??= FindFirstObjectByType<PlayerStats>();
#else
        if (!inventory) inventory = FindObjectOfType<PlayerInventory>();
        if (!equipment) equipment = FindObjectOfType<EquipmentController>();
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();
#endif

        if (slotQ) slotQ.SetKeyLabel("Q");
        if (slotW) slotW.SetKeyLabel("W");
        if (slotE) slotE.SetKeyLabel("E");
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.Changed += HandleInventoryChanged;

        HandleInventoryChanged(); // initial fill
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.Changed -= HandleInventoryChanged;
    }

    private void Update()
    {
        // Hotkeys using the Input System
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame)
            TryUseFromSlot(slotQ);

        if (kb.wKey.wasPressedThisFrame)
            TryUseFromSlot(slotW);

        if (kb.eKey.wasPressedThisFrame)
            TryUseFromSlot(slotE);

        UpdateCooldownVisuals();
    }

    // ------------------------------------------------------------------------
    // Inventory tracking
    // ------------------------------------------------------------------------
    private void HandleInventoryChanged()
    {
        if (inventory == null)
            return;

        // 1) Aggregate counts per potion type + definition
        var countsByDef = new Dictionary<PotionItemDefinition, int>();
        var typeByDef = new Dictionary<PotionItemDefinition, PotionType>();

        foreach (var it in inventory.Items)
        {
            if (it?.def is not PotionItemDefinition potionDef)
                continue;

            if (!countsByDef.TryGetValue(potionDef, out var count))
                count = 0;

            count += Mathf.Max(1, it.quantity);
            countsByDef[potionDef] = count;

            typeByDef[potionDef] = potionDef.potionType;

            RegisterDefOrder(potionDef);
        }

        // 2) Build summary per potion type (total count + primary def to show)
        var summaryByType = new Dictionary<PotionType, (PotionItemDefinition def, int totalCount)>();

        foreach (var kv in countsByDef)
        {
            var def = kv.Key;
            int count = kv.Value;
            var type = typeByDef[def];

            if (!summaryByType.TryGetValue(type, out var current))
            {
                // First time we see this type, seed with this def
                summaryByType[type] = (def, count);
            }
            else
            {
                // There is already some def for this type. We choose the
                // primary def later based on our discovery order.
                summaryByType[type] = (current.def, current.totalCount + count);
            }
        }

        // 3) Decide which def is "active" for each type using our order list.
        //    We must NOT modify the dictionary while iterating its Keys collection,
        //    so we iterate over a copy of the keys.
        var typesSnapshot = new List<PotionType>(summaryByType.Keys);
        foreach (var type in typesSnapshot)
        {
            var current = summaryByType[type];
            int totalCount = current.totalCount;

            PotionItemDefinition activeDef = ChooseActiveDefForType(type, countsByDef);

            summaryByType[type] = (activeDef, totalCount);
        }

        // 4) Map potion types to slots.
        // For now:
        //   Health -> Q
        //   Mana   -> W
        //   (E reserved for future types like Stamina/Poison)
        summaryByType.TryGetValue(PotionType.Health, out var healthSummary);
        summaryByType.TryGetValue(PotionType.Mana, out var manaSummary);

        if (slotQ) slotQ.SetData(healthSummary.def, healthSummary.totalCount);
        if (slotW) slotW.SetData(manaSummary.def, manaSummary.totalCount);

        // E slot empty for now
        if (slotE) slotE.SetData(null, 0);

        if (debugLogs)
        {
            Debug.Log($"[PotionHotbar] Inventory changed. HP={healthSummary.totalCount}, MP={manaSummary.totalCount}");
        }
    }

    private void RegisterDefOrder(PotionItemDefinition potionDef)
    {
        if (potionDef == null) return;

        if (!_orderPerType.TryGetValue(potionDef.potionType, out var list))
        {
            list = new List<PotionItemDefinition>();
            _orderPerType[potionDef.potionType] = list;
        }

        if (!list.Contains(potionDef))
        {
            list.Add(potionDef);
            if (debugLogs)
                Debug.Log($"[PotionHotbar] Registered {potionDef.name} for type {potionDef.potionType} (order index {list.Count - 1}).");
        }
    }

    private PotionItemDefinition ChooseActiveDefForType(
        PotionType type,
        Dictionary<PotionItemDefinition, int> countsByDef)
    {
        if (!_orderPerType.TryGetValue(type, out var list) || list.Count == 0)
            return null;

        // Find the first def in our remembered order that still has >0 count
        for (int i = 0; i < list.Count; i++)
        {
            var def = list[i];
            if (def == null) continue;

            if (countsByDef.TryGetValue(def, out var count) && count > 0)
                return def;
        }

        return null;
    }

    // ------------------------------------------------------------------------
    // Use logic
    // ------------------------------------------------------------------------
    private bool IsOnCooldown()
    {
        return Time.time < _cooldownEndTime;
    }

    private void StartCooldown()
    {
        _cooldownEndTime = Time.time + Mathf.Max(0f, globalCooldownSeconds);
    }

    private void UpdateCooldownVisuals()
    {
        if (globalCooldownSeconds <= 0f)
        {
            // No cooldown configured
            if (slotQ) slotQ.SetCooldown01(0f);
            if (slotW) slotW.SetCooldown01(0f);
            if (slotE) slotE.SetCooldown01(0f);
            return;
        }

        float remaining = Mathf.Max(0f, _cooldownEndTime - Time.time);
        float norm = remaining <= 0f ? 0f : remaining / globalCooldownSeconds;

        if (slotQ) slotQ.SetCooldown01(norm);
        if (slotW) slotW.SetCooldown01(norm);
        if (slotE) slotE.SetCooldown01(norm);
    }

    private void TryUseFromSlot(PotionSlotUI slot)
    {
        if (slot == null) return;
        if (slot.currentDef == null || slot.totalCount <= 0) return;

        if (IsOnCooldown())
        {
            if (debugLogs)
                Debug.Log("[PotionHotbar] Cannot use potion – still on global cooldown.");
            return;
        }

        if (equipment == null || inventory == null)
        {
            Debug.LogWarning("[PotionHotbar] Missing EquipmentController or PlayerInventory reference.");
            return;
        }

        var def = slot.currentDef;

        // Reuse existing potion use path in EquipmentController:
        // TryEquip() knows how to detect potions and route to TryUsePotion().
        bool used = equipment.TryEquip(def, fromInventory: true);

        if (!used)
        {
            if (debugLogs)
                Debug.Log($"[PotionHotbar] TryEquip/TryUsePotion failed for '{def.displayName}'.");
            return;
        }

        // If use succeeded, consume exactly one from inventory.
        bool consumed = inventory.ConsumeOne(def);
        if (!consumed)
        {
            Debug.LogWarning($"[PotionHotbar] Used potion '{def.displayName}' but could not find stack to consume.");
        }

        // Rebuild slots (handle count changes and possible size switch)
        HandleInventoryChanged();

        // Start shared cooldown
        if (globalCooldownSeconds > 0f)
            StartCooldown();
    }
}
