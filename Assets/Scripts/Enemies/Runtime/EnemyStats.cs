using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Runtime stats for an enemy, derived from its EnemyDefinition and level.
    /// Other systems (AI, combat, UI) should read values from here instead of
    /// touching the ScriptableObject directly.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyStats : MonoBehaviour
    {
        [Header("Source")]
        public EnemyDefinition definition;

        [Header("Level")]
        [Min(1)]
        public int level = 1;

        [Tooltip("If true, level will be clamped to definition's min/max range on Awake.")]
        public bool clampLevelToDefinition = true;

        // --------------------------------------------------------------------
        // Derived stats (read-only from outside)
        // --------------------------------------------------------------------
        public float MaxHealth { get; private set; }
        public float Damage { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackSpeed { get; private set; }

        public EnemyTier Tier => definition ? definition.tier : EnemyTier.Normal;
        public EnemyBehaviorType BehaviorType => definition ? definition.behaviorType : EnemyBehaviorType.Aggressive;
        public SpecialDamageType SpecialDamage => definition ? definition.specialDamage : SpecialDamageType.None;

        private void Awake()
        {
            RecalculateStats();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (definition && clampLevelToDefinition)
            {
                level = Mathf.Clamp(level, definition.minLevel, definition.maxLevel);
            }

            // Recalculate in-editor so you can see values in Inspector
            if (Application.isPlaying == false && definition)
            {
                RecalculateStats();
            }
        }
#endif

        /// <summary>
        /// Recalculate all derived stats based on definition and level.
        /// Call this if you ever change level at runtime.
        /// </summary>
        public void RecalculateStats()
        {
            if (!definition)
            {
                Debug.LogWarning($"{name}: EnemyStats has no EnemyDefinition assigned.", this);
                return;
            }

            if (clampLevelToDefinition)
            {
                level = Mathf.Clamp(level, definition.minLevel, definition.maxLevel);
            }

            // Simple example scaling: +15% per level above min.
            // We can tweak this formula later as we test difficulty.
            float levelFactor = 1f;
            int levelOffset = level - definition.minLevel;
            if (levelOffset > 0)
            {
                levelFactor += 0.15f * levelOffset;
            }

            MaxHealth = definition.baseHealth * levelFactor;
            Damage = definition.baseDamage * levelFactor;
            MoveSpeed = definition.baseMoveSpeed;        // can also scale later
            AttackSpeed = definition.baseAttackSpeed;      // seconds per attack for now
        }
    }
}
