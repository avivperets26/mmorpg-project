using UnityEngine;
using UnityEngine.UI;
using Game.Enemies;

namespace Game.Enemies.UI
{
    [DisallowMultipleComponent]
    public class EnemyHealthBarWorldUI : MonoBehaviour
    {
        [Header("Wiring")]
        public EnemyHealth health;
        public Image healthFill;
        public Canvas canvas;

        [Header("Visibility")]
        [Min(0f)]
        public float maxVisibleDistance = 25f;

        [Header("Animation")]
        [Tooltip("How fast the bar interpolates towards the target value.")]
        [Min(0.1f)]
        public float fillLerpSpeed = 8f;

        private Transform _camTransform;

        // current & target fill values (0–1)
        private float _currentFill;
        private float _targetFill;

        private void Reset()
        {
            if (!health) health = GetComponentInParent<EnemyHealth>();
            if (!healthFill) healthFill = GetComponentInChildren<Image>();
            if (!canvas) canvas = GetComponentInChildren<Canvas>();
        }

        private void Awake()
        {
            if (!health) health = GetComponentInParent<EnemyHealth>();
            if (!healthFill) healthFill = GetComponentInChildren<Image>();
            if (!canvas) canvas = GetComponentInChildren<Canvas>();

            if (Camera.main)
                _camTransform = Camera.main.transform;

            SetFillInstant();
        }

        private void Start()
        {
            // Ensure we pick up the final HP after all Awakes ran.
            SetFillInstant();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged += OnHealthChanged;
                health.OnHealed += OnHealthChanged;
                health.OnDeath += OnDeath;
            }

            SetFillInstant();
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

        private void LateUpdate()
        {
            if (!canvas) return;

            // Grab camera lazily in case it spawns after Awake
            if (_camTransform == null && Camera.main)
                _camTransform = Camera.main.transform;

            if (_camTransform != null)
            {
                // Billboard towards camera
                transform.forward = _camTransform.forward;

                // Distance-based visibility
                float dist = Vector3.Distance(_camTransform.position, transform.position);
                canvas.enabled = !health.IsDead && dist <= maxVisibleDistance;
            }

            // ----- Smooth fill animation -----
            if (healthFill)
            {
                _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * fillLerpSpeed);
                healthFill.fillAmount = _currentFill;
            }
        }

        private void OnHealthChanged(EnemyHealth _, float __)
        {
            UpdateTargetFill();
        }

        private void OnDeath(EnemyHealth _)
        {
            UpdateTargetFill(); // will go to 0
        }

        // Set both current & target to the exact health value (no animation)
        private void SetFillInstant()
        {
            if (!healthFill || health == null) return;

            float normalized = health.GetHealthNormalized();
            _currentFill = _targetFill = Mathf.Clamp01(normalized);
            healthFill.fillAmount = _currentFill;
        }

        // Only change the target; LateUpdate will smoothly lerp current→target
        private void UpdateTargetFill()
        {
            if (!health || !healthFill) return;

            _targetFill = Mathf.Clamp01(health.GetHealthNormalized());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!health) health = GetComponentInParent<EnemyHealth>();
            if (!healthFill) healthFill = GetComponentInChildren<Image>();
            if (!canvas) canvas = GetComponentInChildren<Canvas>();

            if (!Application.isPlaying)
            {
                // In editor we still want instant update.
                SetFillInstant();
            }
        }
#endif
    }
}
