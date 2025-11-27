using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    [CreateAssetMenu(
        fileName = "EnemyDefinition",
        menuName = "Game/Enemies/Enemy Definition",
        order = 0)]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public EnemyTier tier = EnemyTier.Normal;
        public EnemyBehaviorType behaviorType = EnemyBehaviorType.Aggressive;

        [Header("Level")]
        public int minLevel = 1;
        public int maxLevel = 1;

        [Header("Stats")]
        public float baseHealth = 50f;
        public float baseDamage = 5f;
        public float baseMoveSpeed = 2.0f;
        public float baseAttackSpeed = 1.5f; // seconds per attack

        [Header("Combat Features")]
        public SpecialDamageType specialDamage = SpecialDamageType.None;

        [Header("Experience")]
        public int baseXpReward = 10;
        public float xpMultiplierByTier = 1.0f;

        [Header("Drops")]
        public EnemyDropTable dropTable;
    }
}
