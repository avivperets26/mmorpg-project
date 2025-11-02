// Assets/Scripts/Items/Equipment/EquipmentItemDefinition.cs
using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "MMO/Items/Equipment Item", fileName = "EquipmentItem")]
    public class EquipmentItemDefinition : ItemDefinition
    {
        [Header("Equipment")]
        public EquipmentSlot slot = EquipmentSlot.Helm;   // ← was Head

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
            // Prefer our new mapper; if it can suggest a slot, take it.
            if (EquipmentSlotMapper.TrySuggestSlot(this, out var primary, out var _))
                slot = primary;
            // 'category' continues to be derived in ItemDefinition.OnValidate()
        }
    }
}
