// Assets/Scripts/Gameplay/Items/Definitions/EquipmentItemDefinition.cs
using UnityEngine;
using Game.Items.Definitions;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "MMO/Items/Equipment Item", fileName = "EquipmentItem")]
    public class EquipmentItemDefinition : ItemDefinition, IHasItemVisual
    {
        [Header("Visuals (optional)")]
        [SerializeField] private ItemVisualDefinition visual;
        public ItemVisualDefinition Visual => visual;

        [Header("Equipment")]
        public EquipmentSlot slot = EquipmentSlot.Helm;

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
            if (EquipmentSlotMapper.TrySuggestSlot(this, out var primary, out var _))
                slot = primary;

            // base.OnValidate();  <-- remove this (base method is not accessible)
        }
    }
}
