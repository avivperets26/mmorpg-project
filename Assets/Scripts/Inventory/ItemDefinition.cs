// Assets/Scripts/Inventory/ItemDefinition.cs
using UnityEngine;

public enum ItemRarity { Common, Rare, Legendary }

[System.Serializable]
public class ItemPreviewOptions
{
    [Tooltip("Optional child path inside the prefab to render (e.g. 'Model'). Leave empty to use root.")]
    public string modelRootPath = "Model";

    [Tooltip("Extra Euler rotation applied only in the preview (deg).")]
    public Vector3 rotationOffsetEuler = Vector3.zero;

    [Tooltip("Extra local position offset for preview framing (units).")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Uniform scale multiplier for preview only.")]
    public float scale = 1f;

    [Tooltip("How much space to leave around the model when framing.")]
    [Min(1.0f)] public float padding = 1.12f;
}

[CreateAssetMenu(menuName = "MMO/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;

    public int width = 1;
    public int height = 1;

    [Header("Prefabs")]
    public GameObject worldPrefab;
    public GameObject previewPrefab;  // optional prettier model for UI

    public Sprite icon;

    [Header("UI Preview")]
    public ItemPreviewOptions preview = new ItemPreviewOptions();

    public ItemRarity rarity = ItemRarity.Common;

    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => Color.white,
        ItemRarity.Rare => new Color(0.45f, 0.7f, 1f),
        ItemRarity.Legendary => new Color(1f, 0.7f, 0.2f),
        _ => Color.white
    };
}
