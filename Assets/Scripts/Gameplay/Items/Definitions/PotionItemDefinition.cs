using UnityEngine;

namespace Game.Items
{
    [CreateAssetMenu(menuName = "MMO/Items/Potion Item", fileName = "PotionItem")]
    public class PotionItemDefinition : ItemDefinition
    {
        [Header("Potion")]
        public PotionType potionType;
        public PotionSize potionSize;

        [Header("Healing")]
        public int instantAmount;   // heals immediately on use
        public int overTimeAmount;  // total amount healed over time
        public float overTimeDurationSeconds = 0f; // 0 = no HoT
        public float tickIntervalSeconds = 1f;     // interval between ticks

        [Header("Stacking")]
        [Min(1)]
        public int maxStack = 99;
    }
}
