using UnityEngine; // ScriptableObject, Color, TextAreaAttribute

public enum ItemType { Sword, Shield, Potion, Misc }        // <— added
public enum ItemRarity { Common, Rare, Legendary }

[CreateAssetMenu(menuName = "MMO/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;

    [Header("Grid Size")]
    public int width = 1;   // 1×3 for your sword
    public int height = 3;

    [Header("Visuals")]
    public GameObject worldPrefab; // SwordWorld prefab

    [Header("Stats")]
    public ItemType type = ItemType.Sword;
    public float damage = 12f;
    [TextArea(2, 4)] public string description = "A basic steel sword.";

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    public static Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => new Color(0.70f, 0.70f, 0.70f), // grey
        ItemRarity.Rare => new Color(1.00f, 0.90f, 0.30f), // yellow
        ItemRarity.Legendary => new Color(0.30f, 1.00f, 0.40f), // green
        _ => Color.white
    };
}
