// Assets/Scripts/Enemies/UI/TargetInfoUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Enemies;

namespace Game.Enemies.UI
{
    /// <summary>
    /// Top-of-screen HUD for the currently selected enemy.
    /// Shows name, level, tier and a large HP bar.
    /// </summary>
    public class TargetInfoUI : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("UI")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text tierText;
        [SerializeField] private Image hpFill;
        [SerializeField] private GameObject specialIconsRow; // optional, for later

        private EnemyStats _stats;
        private EnemyHealth _health;
        private EnemyTargetInteractable _currentTarget; // the one currently driving the UI
        private EnemyTargetInteractable _selectedTarget; // from click/selection
        private EnemyTargetInteractable _hoverTarget;    // from hover ray
        private CanvasGroup _canvasGroup;

        private void Reset()
        {
            if (!panelRoot) panelRoot = gameObject;
        }

        private void Awake()
        {
            if (!panelRoot) panelRoot = gameObject;
            CacheCanvasGroup();
            SetVisible(false);
        }

        private void OnEnable()
        {
            EnemyTargetInteractable.OnEnemyTargeted += HandleEnemyTargeted;
            EnemyTargetInteractable.OnEnemyHoverChanged += HandleEnemyHovered;
        }

        private void OnDisable()
        {
            EnemyTargetInteractable.OnEnemyTargeted -= HandleEnemyTargeted;
            EnemyTargetInteractable.OnEnemyHoverChanged -= HandleEnemyHovered;
            UnsubscribeHealthEvents();
        }

        // --------------------------------------------------------------------
        // Selection handling
        // --------------------------------------------------------------------

        private void HandleEnemyTargeted(EnemyTargetInteractable target)
        {
            _selectedTarget = target;
            EvaluateActiveTarget();
        }

        private void HandleEnemyHovered(EnemyTargetInteractable target)
        {
            _hoverTarget = target;
            EvaluateActiveTarget();
        }

        private void EvaluateActiveTarget()
        {
            var next = _hoverTarget != null ? _hoverTarget : _selectedTarget;
            SetActiveTarget(next);
        }

        private void SetActiveTarget(EnemyTargetInteractable target)
        {
            if (target == _currentTarget)
                return;

            ClearTarget();

            if (target == null)
                return;

            _currentTarget = target;
            _stats = target.stats;
            _health = target.health;

            if (_health != null)
            {
                _health.OnDamaged += HandleHealthChanged;
                _health.OnHealed += HandleHealthChanged;
                _health.OnDeath += HandleTargetDeath;
            }

            RepaintAll();
            SetVisible(true);
        }

        private void ClearTarget()
        {
            UnsubscribeHealthEvents();
            _currentTarget = null;
            _stats = null;
            _health = null;
            SetVisible(false);
        }

        private void UnsubscribeHealthEvents()
        {
            if (_health == null) return;

            _health.OnDamaged -= HandleHealthChanged;
            _health.OnHealed -= HandleHealthChanged;
            _health.OnDeath -= HandleTargetDeath;
        }

        // --------------------------------------------------------------------
        // Events from EnemyHealth
        // --------------------------------------------------------------------

        private void HandleHealthChanged(EnemyHealth health, float _)
        {
            if (health == _health)
            {
                UpdateHpBar();
            }
        }

        private void HandleTargetDeath(EnemyHealth health)
        {
            if (health == _health)
            {
                if (_selectedTarget != null && _selectedTarget.health == health)
                    _selectedTarget = null;
                if (_hoverTarget != null && _hoverTarget.health == health)
                    _hoverTarget = null;
                ClearTarget();
            }
        }

        // --------------------------------------------------------------------
        // UI updates
        // --------------------------------------------------------------------

        private void RepaintAll()
        {
            if (_stats == null || _stats.definition == null || _health == null)
                return;

            var def = _stats.definition;

            if (nameText) nameText.text = def.enemyName;
            if (levelText) levelText.text = $"Lv. {_stats.level}";

            if (tierText)
            {
                tierText.text = def.tier.ToString();

                // Simple tier color logic (tune to your palette)
                Color c = Color.white;
                switch (def.tier)
                {
                    case EnemyTier.Normal:
                        c = Color.white;
                        break;
                    case EnemyTier.Advanced:
                        c = new Color(0.3f, 0.8f, 1f);  // light blue
                        break;
                    case EnemyTier.Elite:
                        c = new Color(0.8f, 0.4f, 1f);  // purple
                        break;
                    case EnemyTier.Boss:
                        c = new Color(1f, 0.5f, 0.1f);  // orange
                        break;
                }
                tierText.color = c;
            }

            UpdateHpBar();

            // Later: update specialIconsRow based on _stats.SpecialDamage etc.
            if (specialIconsRow)
            {
                specialIconsRow.SetActive(_stats.SpecialDamage != SpecialDamageType.None);
            }
        }

        private void UpdateHpBar()
        {
            if (!_health || !hpFill) return;

            float max = _health.MaxHealth;
            float cur = Mathf.Clamp(_health.CurrentHealth, 0f, max);

            float t = (max > 0f) ? cur / max : 0f;
            hpFill.fillAmount = t;
        }

        private void SetVisible(bool visible)
        {
            if (!panelRoot) return;

            // If panelRoot is the same GameObject as this component, don't disable it,
            // otherwise we would unsubscribe from events and never see updates.
            if (panelRoot == gameObject)
            {
                CacheCanvasGroup();
                if (_canvasGroup)
                {
                    _canvasGroup.alpha = visible ? 1f : 0f;
                    _canvasGroup.interactable = visible;
                    _canvasGroup.blocksRaycasts = visible;
                }
                else
                {
                    // Fallback: enable/disable children only
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        transform.GetChild(i).gameObject.SetActive(visible);
                    }
                }
            }
            else
            {
                panelRoot.SetActive(visible);
            }
        }

        private void CacheCanvasGroup()
        {
            if (_canvasGroup == null && panelRoot == gameObject)
            {
                _canvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = panelRoot.AddComponent<CanvasGroup>();
                }
            }
        }
    }
}
