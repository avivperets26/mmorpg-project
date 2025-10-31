// Assets/Scripts/Inventory/ItemDefinition.cs
using UnityEngine;

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
    [Header("Identity")]
    public string itemId;
    public string displayName;

    [Header("Inventory Size (grid cells)")]
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    [Header("Prefabs")]
    [Tooltip("Prefab placed in the world for pickups.")]
    public GameObject worldPrefab;

    [Tooltip("Clean prefab for UI preview only (no labels/roots). Falls back to worldPrefab if null.")]
    public GameObject inventoryPreviewPrefab;

    [Header("2D Icon (optional fallback)")]
    public Sprite icon;

    [Header("3D Preview Tuning")]
    public ItemPreviewOptions preview = new ItemPreviewOptions();

    [Header("Meta")]
    public Game.Items.ItemRarity rarity = Game.Items.ItemRarity.Common;

    public static Color RarityColor(Game.Items.ItemRarity r) => r switch
    {
        Game.Items.ItemRarity.Common => Color.white,
        Game.Items.ItemRarity.Rare => new Color(0.45f, 0.70f, 1f),
        Game.Items.ItemRarity.Legendary => new Color(1f, 0.70f, 0.20f),
        _ => Color.white
    };
}
