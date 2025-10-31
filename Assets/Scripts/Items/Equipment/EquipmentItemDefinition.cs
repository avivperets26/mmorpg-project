// Assets/Scripts/Items/Equipment/EquipmentItemDefinition.cs
using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "MMO/Items/Equipment Item", fileName = "EquipmentItem")]
    public class EquipmentItemDefinition : ItemDefinition
    {
        [Header("Equipment")]
        public EquipmentSlot slot = EquipmentSlot.Head;

        [Header("Stats & Set")]
        public EquipmentStats stats = new EquipmentStats();
        public EquipmentSetDefinition belongsToSet;

        [Header("Presentation")]
        [Tooltip("Prefab used when this item is spawned for pickup in the world.")]
        public GameObject pickupPrefab;

        [Tooltip("Prefab used when this item is equipped on the character.")]
        public GameObject equipPrefab;

        private void OnValidate()
        {
            // 'subtype' is inherited from ItemDefinition
            slot = EquipmentMapping.GetSlotForSubtype(subtype);
            // 'category' is auto-derived in ItemDefinition.OnValidate()
        }
    }
}
