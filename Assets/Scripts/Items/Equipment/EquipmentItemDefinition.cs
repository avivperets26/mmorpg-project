using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "MMO/Items/Equipment Item", fileName = "EquipmentItem")]
    public class EquipmentItemDefinition : ItemDefinition
    {
        [Header("Classification")]
        public ItemCategory category = ItemCategory.Armor;
        public ItemSubtype subtype = ItemSubtype.Helmet;
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
            slot = EquipmentMapping.GetSlotForSubtype(subtype);
            category = EquipmentMapping.GetCategoryForSubtype(subtype);
        }
    }
}
