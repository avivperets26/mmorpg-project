using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Enemies.UI
{
    /// <summary>
    /// Binds EnemyStats + EnemyHealth to a world-space nameplate:
    /// shows name, tier, level & a small health bar.
    /// </summary>
    public class EnemyNameplateUI : MonoBehaviour
    {
        [Header("Wiring")]
        public EnemyStats stats;
        public EnemyHealth health;

        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image healthFill;

        [Header("Visibility")]
        [SerializeField] private float maxVisibleDistance = 25f;
        [SerializeField] private Canvas canvas; // world-space canvas

        private Camera _cam;

        private void Reset()
        {
            if (!canvas) canvas = GetComponentInChildren<Canvas>();
            if (!stats) stats = GetComponentInParent<EnemyStats>();
            if (!health) health = GetComponentInParent<EnemyHealth>();
        }

        private void Awake()
        {
            if (!canvas) canvas = GetComponentInChildren<Canvas>();
            if (!stats) stats = GetComponentInParent<EnemyStats>();
            if (!health) health = GetComponentInParent<EnemyHealth>();

            _cam = Camera.main;

            InitStaticInfo();
            UpdateHealthUI();
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

        private void InitStaticInfo()
        {
            if (stats == null) return;
            if (nameText) nameText.text = stats.definition ? stats.definition.enemyName : gameObject.name;
            if (tierText) tierText.text = stats.Tier.ToString();
            if (levelText) levelText.text = $"Lv. {stats.level}";

            // Optional: tier color coding
            if (tierText)
            {
                switch (stats.Tier)
                {
                    case EnemyTier.Normal: tierText.color = Color.white; break;
                    case EnemyTier.Advanced: tierText.color = new Color(0.3f, 0.8f, 1f); break; // cyan-ish
                    case EnemyTier.Elite: tierText.color = new Color(0.8f, 0.6f, 0.1f); break; // gold-ish
                    case EnemyTier.Boss: tierText.color = Color.red; break;
                }
            }
        }

        private void OnHealthChanged(EnemyHealth _, float __)
        {
            UpdateHealthUI();
        }

        private void OnDeath(EnemyHealth _)
        {
            UpdateHealthUI();
            // optional: hide nameplate on death
            if (canvas) canvas.enabled = false;
        }

        private void UpdateHealthUI()
        {
            if (healthFill == null || health == null) return;
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
    }
}
