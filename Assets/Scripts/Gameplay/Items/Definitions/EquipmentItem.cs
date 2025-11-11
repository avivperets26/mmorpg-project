using UnityEngine;

[CreateAssetMenu(fileName = "LegacyEquipmentItem", menuName = "MMO/Legacy/Equipment Item")]
[System.Obsolete("Use Game.Items.EquipmentItemDefinition instead.")]
public class EquipmentItem : ScriptableObject
{
    public string displayName = "Boots";
    [Tooltip("Movement speed multiplier when equipped (1.2 = +20%).")]
    public float moveSpeedMultiplier = 1.2f;
}
