// Assets\Scripts\Gameplay\Items\Definitions\ItemDefinition.cs
using UnityEngine;
using Game.Items;

[System.Serializable]
public class ItemPreviewOptions
{
    public string modelRootPath = "Model";
    public Vector3 rotationOffsetEuler = Vector3.zero;
    public Vector3 positionOffset = Vector3.zero;
    public float scale = 1f;
    [Min(1.0f)] public float padding = 1.12f;
    public Vector2 uiOffsetPx = Vector2.zero;
}

[CreateAssetMenu(menuName = "MMO/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    // Identity & Presentation
    public string itemId;
    public string displayName;
    [TextArea] public string description = "";

    // Inventory Size (grid cells)
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    // Prefabs
    [Tooltip("Prefab placed in the world for pickups.")]
    public GameObject worldPrefab;
    [Tooltip("Clean prefab for UI preview only (no labels/roots). Falls back to worldPrefab if null.")]
    public GameObject inventoryPreviewPrefab;

    // 2D Icon (optional)
    public Sprite icon;

    // 3D Preview Tuning (UI only)
    public ItemPreviewOptions preview = new ItemPreviewOptions();

    // Classification
    public ItemCategory category = ItemCategory.Weapon;
    public ItemSubtype subtype = ItemSubtype.Sword;

    // Weapon Handling
    [Tooltip("How this weapon is held. For non-weapons leave as None.")]
    public WeaponGrip grip = WeaponGrip.None;

    // Requirements
    public ItemRequirements requirements = new ItemRequirements
    {
        level = 1,
        usableBy = CharacterClass.All,
        minStrength = 0,
        minDexterity = 0,
        minEnergy = 0
    };

    // Variant Stats (drawn conditionally by custom editor)
    [Tooltip("Physical (min/max) or Wizardry for magic weapons. Crit chance 0..1 or 0..100%, crit multiplier e.g. 1.5, attack speed in APS.")]
    public DamageProfile baseDamage = new DamageProfile(); // used when category == Weapon

    [Tooltip("Physical defense (armor/shields).")]
    public int baseDefense = 0;      // used when category == Armor || subtype == Shield
    [Tooltip("Magic resistance (armor/shields).")]
    public int baseMagicResist = 0;  // used when category == Armor || subtype == Shield

    // Generic bonuses
    public float hpOnKill = 0f;
    public float manaOnKill = 0f;

    // Durability & Value
    public int baseDurability = 50;
    public int baseValue = 10;

    // Blessing & Sockets
    public bool canBeBlessed = true;
    public SocketSlotType socketSlotType = SocketSlotType.Weapon;
    [Min(0)] public int socketsMax = 0; // editor-clamped by footprint

    // Tier / Rarity
    public ItemTier defaultTier = ItemTier.Common;

    [HideInInspector]
    public ItemRarity legacyRarity;

    public Color TierLabelColor => RarityRules.GetLabelColor(defaultTier);
    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => Color.white,
        ItemRarity.Rare => new Color(0.45f, 0.70f, 1f),
        ItemRarity.Legendary => new Color(1f, 0.70f, 0.20f),
        _ => Color.white
    };

#if UNITY_EDITOR
    private void OnValidate()
    {
        category = EquipmentMapping.GetCategoryForSubtype(subtype);
        AutoInferGripIfUnset();
        NormalizeCombatFields();

        width          = Mathf.Max(1, width);
        height         = Mathf.Max(1, height);
        baseDurability = Mathf.Max(1, baseDurability);
        baseValue      = Mathf.Max(0, baseValue);

        socketsMax = Mathf.Clamp(socketsMax, 0, Mathf.Max(1, width * height));

        if (preview == null) preview = new ItemPreviewOptions();
        preview.padding = Mathf.Max(1.0f, preview.padding);
        preview.scale   = Mathf.Max(0.001f, preview.scale);

        socketSlotType = DeriveSocketSlotType(category, subtype, socketSlotType);
    }

    private static SocketSlotType DeriveSocketSlotType(ItemCategory cat, ItemSubtype sub, SocketSlotType current)
    {
        if (sub == ItemSubtype.Ring || sub == ItemSubtype.Amulet) return SocketSlotType.Jewelry;
        if (sub == ItemSubtype.Shield)                             return SocketSlotType.Armor;
        if (cat == ItemCategory.Weapon)                            return SocketSlotType.Weapon;
        if (cat == ItemCategory.Armor)                             return SocketSlotType.Armor;
        return current;
    }

    private void AutoInferGripIfUnset()
    {
        if (category != ItemCategory.Weapon || grip != WeaponGrip.None) return;
        switch (subtype)
        {
            case ItemSubtype.Shield:
            case ItemSubtype.Orb:
            case ItemSubtype.Book:
            case ItemSubtype.Arrows: grip = WeaponGrip.OffHandOnly; break;
            case ItemSubtype.Bow:
            case ItemSubtype.Staff:  grip = WeaponGrip.TwoHanded;   break;
            case ItemSubtype.Sword:
            case ItemSubtype.Dagger:
            case ItemSubtype.Axe:
            case ItemSubtype.Mace:
            case ItemSubtype.Spear:  grip = WeaponGrip.OneHanded;   break;
            default:                 grip = WeaponGrip.None;         break;
        }
    }

    private void NormalizeCombatFields()
    {
        if (category != ItemCategory.Weapon) return;

        if (baseDamage.critChance > 1f && baseDamage.critChance <= 100f)
            baseDamage.critChance /= 100f;
        baseDamage.critChance = Mathf.Clamp01(baseDamage.critChance);

        if (baseDamage.critMultiplier >= 10f)
            baseDamage.critMultiplier /= 100f;
        baseDamage.critMultiplier = Mathf.Clamp(baseDamage.critMultiplier, 1.0f, 5.0f);

        if (baseDamage.attackSpeed <= 0f) baseDamage.attackSpeed = 1f;
        else baseDamage.attackSpeed = Mathf.Clamp(baseDamage.attackSpeed, 0.05f, 50f);

        baseDamage.min      = Mathf.Max(0, baseDamage.min);
        baseDamage.max      = Mathf.Max(baseDamage.min, baseDamage.max);
        baseDamage.wizardry = Mathf.Max(0, baseDamage.wizardry);
    }

    [ContextMenu("Sync Legacy Rarity -> Tier (one-time)")]
    private void SyncLegacyRarityToTier()
    {
        defaultTier = legacyRarity.ToTier();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ItemDefinition] Synced legacyRarity={legacyRarity} to defaultTier={defaultTier} on {name}");
    }
#endif
}
