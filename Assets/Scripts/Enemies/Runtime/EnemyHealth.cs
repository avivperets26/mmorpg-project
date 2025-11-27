using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Handles current HP, taking damage, healing and death.
    /// Works together with EnemyStats (for MaxHealth) but can also
    /// fall back to a manual Max Health if needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("Optional. If assigned, MaxHealth will come from EnemyStats.")]
        public EnemyStats stats;

        [Tooltip("Used if Stats is not assigned.")]
        public float fallbackMaxHealth = 50f;

        [Header("Debug")]
        public bool logDamage = false;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => stats ? stats.MaxHealth : fallbackMaxHealth;
        public bool IsDead { get; private set; }

        // Events
        public event Action<EnemyHealth, float> OnDamaged; // (self, damageAmount)
        public event Action<EnemyHealth, float> OnHealed;  // (self, healAmount)
        public event Action<EnemyHealth> OnDeath;

        private void Awake()
        {
            if (!stats)
                stats = GetComponent<EnemyStats>();

            ResetHealth();
        }

        public void ResetHealth()
        {
            IsDead = false;
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamaged?.Invoke(this, amount);

            if (logDamage)
            {
                Debug.Log($"{name} took {amount} damage. HP: {CurrentHealth}/{MaxHealth}", this);
            }

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
                return;

            float old = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float healed = CurrentHealth - old;

            if (healed > 0f)
            {
                OnHealed?.Invoke(this, healed);
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            if (logDamage)
            {
                Debug.Log($"{name} died.", this);
            }

            OnDeath?.Invoke(this);
        }

        /// <summary>
        /// Returns HP fraction 0-1, useful for health bars.
        /// </summary>
        public float GetHealthNormalized()
        {
            return MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        }
    }
}
