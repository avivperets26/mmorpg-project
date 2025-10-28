using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentItem", menuName = "MMO/Equipment Item")]
public class EquipmentItem : ScriptableObject
{
    public string displayName = "Boots";
    [Tooltip("Movement speed multiplier when equipped (1.2 = +20%).")]
    public float moveSpeedMultiplier = 1.2f;
}
