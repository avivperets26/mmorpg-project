using UnityEngine;
using Game.Items; // CharacterClass, ItemCategory, ItemSubtype, ItemTier, ItemRequirements, DamageProfile, SocketSlotType

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Per-item tuning used ONLY for the inventory/UI 3D preview.
/// Keeps your world prefab untouched.
/// </summary>
[System.Serializable]
public class ItemPreviewOptions
{
    [Tooltip("Optional child path inside the prefab to render (e.g. 'Model'). Leave empty to use root.")]
    public string modelRootPath = "Model";

    [Tooltip("Extra Euler rotation applied only in the preview (deg).")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Tooltip("Extra local position offset for preview framing (world units).")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Uniform scale multiplier for preview only.")]
    public float scale = 1f;

    [Tooltip("How much space to leave around the model when framing the camera.")]
    [Min(1.0f)] public float padding = 1.12f;

    [Tooltip("Final 2D nudge (pixels) applied to the UI image after placement. +X right, +Y up.")]
    public Vector2 uiOffsetPx = Vector2.zero;
}

[CreateAssetMenu(menuName = "MMO/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    // ---------------------- Identity & Presentation ----------------------

    [Header("Identity")]
    public string itemId;
    public string displayName;

    [TextArea] public string description = "";

    [Header("Inventory Size (grid cells)")]
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    [Header("Prefabs")]
    [Tooltip("Prefab placed in the world for pickups.")]
    public GameObject worldPrefab;

    [Tooltip("Clean prefab for UI preview only (no labels/roots). Falls back to worldPrefab if null.")]
    public GameObject inventoryPreviewPrefab;

    [Header("2D Icon (optional)")]
    public Sprite icon;

    [Header("3D Preview Tuning (UI only)")]
    public ItemPreviewOptions preview = new ItemPreviewOptions();

    // ---------------------- Classification ----------------------

    [Header("Classification")]
    public ItemCategory category = ItemCategory.Weapon;
    public ItemSubtype subtype = ItemSubtype.Sword;

    // ---------------------- Requirements ----------------------

    [Header("Requirements")]
    [Tooltip("Minimum level, allowed classes, and min stats to equip.")]
    public ItemRequirements requirements = new ItemRequirements
    {
        level = 1,
        usableBy = CharacterClass.All,
        minStrength = 0,
        minDexterity = 0,
        minEnergy = 0
    };

    // ---------------------- Base Stats ----------------------

    [Header("Base Stats")]
    [Tooltip("Physical (min/max) or Wizardry for magic weapons. Crit chance 0..1, multiplier e.g. 1.5 = +50%. AttackSpeed in attacks/sec or normalized.")]
    public DamageProfile baseDamage;

    [Tooltip("Armor/Shield physical defense. Ignored for non-armor items.")]
    public int baseDefense = 0;

    [Tooltip("Armor/Shield magic resistance. Ignored for non-armor items.")]
    public int baseMagicResist = 0;

    [Tooltip("HP restored after killing a monster (armor/shields options).")]
    public float hpOnKill = 0f;

    [Tooltip("Mana restored after killing a monster (armor/shields options).")]
    public float manaOnKill = 0f;

    // ---------------------- Durability & Value ----------------------

    [Header("Durability & Value")]
    [Tooltip("Max durability when new.")]
    public int baseDurability = 50;

    [Tooltip("Base NPC store value (gold).")]
    public int baseValue = 10;

    // ---------------------- Blessing & Sockets ----------------------

    [Header("Blessing & Sockets")]
    [Tooltip("Whether this item type can roll as Blessed.")]
    public bool canBeBlessed = true;

    [Tooltip("Socket family this item accepts (weapon/armor/jewelry).")]
    public SocketSlotType socketSlotType = SocketSlotType.Weapon;

    [Tooltip("Max sockets this item can have (Rings/Amulets usually 1; weapons 0-4).")]
    [Range(0, 4)] public int socketsMax = 0;

    // ---------------------- Tier / Rarity ----------------------

    [Header("Tier / Rarity")]
    [Tooltip("Primary rarity system used by tooltips, labels and multipliers.")]
    public ItemTier defaultTier = ItemTier.Common;

    [Header("Legacy (compatibility)")]
    [Tooltip("Your original 3-tier rarity. Optional: only used by old systems. You can ignore this once migrated.")]
    public ItemRarity legacyRarity = ItemRarity.Common;

    // ---------------------- Convenience helpers ----------------------

    /// <summary>
    /// Color used for labels/tooltips based on current tier.
    /// </summary>
    public Color TierLabelColor => RarityTierBridge.TierColor(defaultTier);

    /// <summary>
    /// Backward-compat color if some UI still reads legacy rarity.
    /// </summary>
    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => Color.white,
        ItemRarity.Rare => new Color(0.45f, 0.70f, 1f),
        ItemRarity.Legendary => new Color(1f, 0.70f, 0.20f),
        _ => Color.white
    };

#if UNITY_EDITOR
    // Optional editor niceties (safe to remove if you prefer a clean runtime-only file)

    private void OnValidate()
    {
        // Auto-derive category from subtype if user edits subtype directly.
        category = EquipmentMapping.GetCategoryForSubtype(subtype);

        // Gentle clamping
        width  = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        baseDurability = Mathf.Max(1, baseDurability);
        baseValue = Mathf.Max(0, baseValue);
        socketsMax = Mathf.Clamp(socketsMax, 0, 4);

        // Ensure preview defaults are sane
        if (preview == null) preview = new ItemPreviewOptions();
        preview.padding = Mathf.Max(1.0f, preview.padding);
        preview.scale = Mathf.Max(0.001f, preview.scale);
    }

    [ContextMenu("Sync Legacy Rarity -> Tier (one-time)")]
    private void SyncLegacyRarityToTier()
    {
        defaultTier = legacyRarity.ToTier();
        EditorUtility.SetDirty(this);
        Debug.Log($"[ItemDefinition] Synced legacyRarity={legacyRarity} to defaultTier={defaultTier} on {name}");
    }
#endif
}
