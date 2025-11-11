using UnityEngine;

namespace Game.Items
{
    [System.Serializable]
    public struct EquipmentStats
    {
        public int itemLevel;        // scaling / drops
        public int armor;            // for armor pieces
        public float strength;
        public float agility;
        public float intellect;
        public float stamina;

        [Header("Gameplay")]
        public float critChance;
        public float critDamage;
        public float attackSpeed;
        public float moveSpeed;

        // add more later as needed
    }
}
