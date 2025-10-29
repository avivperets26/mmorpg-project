using UnityEngine;

public enum ItemType { Sword, Shield, Potion, Misc }

[CreateAssetMenu(menuName = "MMO/Item Definition", fileName = "ItemDefinition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemId;         // e.g., "sword.simple.01"
    public string displayName;    // e.g., "Simple Sword"

    [Header("Grid Size")]
    public int width = 1;         // 1×3 per your plan
    public int height = 3;

    [Header("Visuals")]
    public GameObject worldPrefab; // set after we make the prefab

    [Header("Stats")]
    public ItemType type = ItemType.Sword;
    public float damage = 12f;
    [TextArea] public string description = "A basic steel sword.";
}
