using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Handles current HP, taking damage, healing and death.
    /// Works together with EnemyStats (for MaxHealth) but can also
    /// fall back to a manual Max Health if needed.
    ///
    /// Events:
    ///   OnDamaged(this, damageAmount)
    ///   OnHealed(this, healAmount)
    ///   OnDeath(this)
    ///
    /// Used by:
    ///   - EnemyDebugDamage
    ///   - EnemyNameplateUI
    ///   - EnemyHealthBarWorldUI
    ///   - EnemyXpOnDeath
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Sources")]
        [Tooltip("Optional. If assigned, MaxHealth will come from EnemyStats.")]
        public EnemyStats stats;

        [Tooltip("Used if Stats is null, or if Stats doesn't have a valid MaxHealth yet.")]
        public float fallbackMaxHealth = 50f;

        [Header("Debug")]
        [Tooltip("Log damage / heals to the Console.")]
        public bool logDamage = false;

        /// <summary>Current HP at runtime.</summary>
        public float CurrentHealth { get; private set; }

        /// <summary>Maximum HP derived from stats or fallback.</summary>
        public float MaxHealth { get; private set; }

        /// <summary>True after Die() has been called.</summary>
        public bool IsDead { get; private set; }

        // Cache colliders so we can disable clicks/highlights on death.
        private Collider[] _colliders;
        private int _originalLayer;

        // Events --------------------------------------------------------------
        public event Action<EnemyHealth, float> OnDamaged;
        public event Action<EnemyHealth, float> OnHealed;
        public event Action<EnemyHealth> OnDeath;

        private void Awake()
        {
            _colliders = GetComponentsInChildren<Collider>(true);
            _originalLayer = gameObject.layer;

            RecalculateMaxHealth();
            ResetHealth();
        }

        private void OnValidate()
        {
            // Keep MaxHealth in sync in the editor.
            RecalculateMaxHealth();
            if (!Application.isPlaying)
            {
                CurrentHealth = MaxHealth;
            }
        }

        /// <summary>
        /// Re-evaluates MaxHealth from stats or from fallback.
        /// Does not change CurrentHealth; call ResetHealth() for that.
        /// </summary>
        public void RecalculateMaxHealth()
        {
            if (stats != null && stats.MaxHealth > 0f)
            {
                MaxHealth = stats.MaxHealth;
            }
            else
            {
                MaxHealth = Mathf.Max(1f, fallbackMaxHealth);
            }
        }

        /// <summary>
        /// Sets CurrentHealth to MaxHealth and clears IsDead.
        /// </summary>
        public void ResetHealth()
        {
            RecalculateMaxHealth();
            CurrentHealth = MaxHealth;
            IsDead = false;

            SetCollidersEnabled(true);
            RestoreLayer();
        }

        /// <summary>
        /// Apply damage. Returns the actual amount subtracted (>= 0).
        /// </summary>
        public float TakeDamage(float amount)
        {
            if (amount <= 0f) return 0f;

            if (MaxHealth <= 0f)
                RecalculateMaxHealth();

            if (IsDead)
                return 0f;

            float previous = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            float dealt = previous - CurrentHealth;

            if (logDamage)
            {
                Debug.Log($"{name} took {dealt} damage. HP: {CurrentHealth}/{MaxHealth}", this);
            }

            if (dealt > 0f)
            {
                OnDamaged?.Invoke(this, dealt);
            }

            if (CurrentHealth <= 0f && !IsDead)
            {
                Die();
            }

            return dealt;
        }

        /// <summary>
        /// Heal some HP. Returns the actual amount restored.
        /// </summary>
        public float Heal(float amount)
        {
            if (amount <= 0f) return 0f;

            if (MaxHealth <= 0f)
                RecalculateMaxHealth();

            if (IsDead)
                return 0f;

            float previous = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float healed = CurrentHealth - previous;

            if (healed > 0f)
            {
                if (logDamage)
                {
                    Debug.Log($"{name} healed {healed}. HP: {CurrentHealth}/{MaxHealth}", this);
                }

                OnHealed?.Invoke(this, healed);
            }

            return healed;
        }

        /// <summary>
        /// Implements IDamageable so player auto-attacks can target enemies via clicks/raycast.
        /// </summary>
        public void TakeHit(int amount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (IsDead) return;
            TakeDamage(amount);
        }

        /// <summary>
        /// Kill instantly (sets HP to 0 and fires OnDeath).
        /// </summary>
        public void Kill()
        {
            if (IsDead) return;

            float previous = CurrentHealth;
            CurrentHealth = 0f;

            if (previous > 0f)
            {
                OnDamaged?.Invoke(this, previous);
            }

            Die();
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

            SetCollidersEnabled(false); // prevent further raycast clicks/highlights
            SetIgnoreRaycastLayer();
        }

        /// <summary>
        /// Returns HP fraction 0-1, useful for health bars.
        /// </summary>
        public float GetHealthNormalized()
        {
            return MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (_colliders == null) return;
            foreach (var col in _colliders)
            {
                if (col) col.enabled = enabled;
            }
        }

        private void SetIgnoreRaycastLayer()
        {
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0)
            {
                gameObject.layer = ignoreLayer;
            }
        }

        private void RestoreLayer()
        {
            gameObject.layer = _originalLayer;
        }
    }
}
