using UnityEngine;
using UnityEngine.UI;
using Game.Enemies;   // ✅ add this
                      // (this is where EnemyHealth lives)

namespace Game.Enemies.UI
{
    /// <summary>
    /// World-space health bar that tracks an EnemyHealth and
    /// hides itself when the enemy is too far or dead.
    /// </summary>
    public class EnemyHealthBarWorldUI : MonoBehaviour
    {
        [Header("Wiring")]
        public EnemyHealth health;
        public Image healthFill;
        public Canvas canvas;

        [Header("Visibility")]
        public float maxVisibleDistance = 25f;

        private Camera _cam;

        private void Reset()
        {
            if (!canvas) canvas = GetComponentInChildren<Canvas>();
            if (!health) health = GetComponentInParent<EnemyHealth>();
            if (!healthFill) healthFill = GetComponentInChildren<Image>();
        }

        private void Awake()
        {
            if (!canvas) canvas = GetComponentInChildren<Canvas>();
            if (!health) health = GetComponentInParent<EnemyHealth>();
            _cam = Camera.main;

            UpdateHealth();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += OnHealthChanged;
                health.OnHealed += OnHealthChanged;
                health.OnDeath += OnDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged -= OnHealthChanged;
                health.OnHealed -= OnHealthChanged;
                health.OnDeath -= OnDeath;
            }
        }

        private void OnHealthChanged(EnemyHealth _, float __) => UpdateHealth();

        private void OnDeath(EnemyHealth _)
        {
            UpdateHealth();
            if (canvas) canvas.enabled = false;
        }

        private void UpdateHealth()
        {
            if (!healthFill || !health) return;
            healthFill.fillAmount = health.GetHealthNormalized();
        }

        private void LateUpdate()
        {
            if (!_cam)
            {
                _cam = Camera.main;
                if (!_cam) return;
            }

            if (!canvas) return;

            float dist = Vector3.Distance(_cam.transform.position, transform.position);
            canvas.enabled = dist <= maxVisibleDistance;
        }

        private void OnValidate()
        {
            UpdateHealth();
        }
    }
}
