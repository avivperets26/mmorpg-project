// Assets/Scripts/Enemies/UI/EnemyDamagePopupSpawner.cs
using UnityEngine;
using Game.Enemies;

namespace Game.Enemies.UI
{
    /// <summary>
    /// Listens to EnemyHealth events and spawns damage popups when
    /// the enemy takes damage.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyDamagePopupSpawner : MonoBehaviour
    {
        [Header("Wiring")]
        public EnemyHealth health;

        [Tooltip("DamagePopup prefab to spawn when the enemy is hit.")]
        public DamagePopup popupPrefab;

        [Tooltip("Optional: if set, popups spawn around this transform instead of this GameObject.")]
        public Transform worldAnchor;

        [Header("Popup")]
        public float offsetY = 1.8f;

        private void Reset()
        {
            if (!health) health = GetComponentInParent<EnemyHealth>();
        }

        private void OnEnable()
        {
            if (!health) health = GetComponentInParent<EnemyHealth>();
            if (health != null)
            {
                health.OnDamaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= OnDamaged;
            }
        }

        private void OnDamaged(EnemyHealth _, float amount)
        {
            if (!popupPrefab) return;

            int rounded = Mathf.RoundToInt(amount);
            if (rounded <= 0) return;

            Vector3 basePos = worldAnchor ? worldAnchor.position : transform.position;
            Vector3 spawnPos = basePos + Vector3.up * offsetY;

            DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
            popup.SetEnemyDamage(rounded);
        }
    }
}
