// Assets/Scripts/UI/BottomHUD.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BottomHUD : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerStats stats;

    [Header("XP Bar")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expCenterText;

    [Header("Orbs Text")]
    [SerializeField] private TMP_Text hpCenterText;
    [SerializeField] private TMP_Text mpCenterText;

    [Header("Stamina")]
    [SerializeField] private TMP_Text staminaCenterText;   // "75/100"
    [SerializeField] private Image staminaFillImage;       // thin bar Image (Filled Horizontal)

    [Header("Animation")]
    [SerializeField] private float expLerpDuration = 0.35f;

    private Coroutine _expCo;

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (!stats) stats = FindFirstObjectByType<PlayerStats>();
#else
        if (!stats) stats = FindObjectOfType<PlayerStats>();
#endif
    }

    private void OnEnable()
    {
        if (!stats) return;
        stats.OnXpChanged += HandleXpChanged;
        stats.OnLevelChanged += HandleLevelChanged;
        stats.OnVitalsChanged += HandleVitalsChanged;

        // initial paint
        HandleLevelChanged();
        HandleXpChanged();
        HandleVitalsChanged();
    }

    private void OnDisable()
    {
        if (!stats) return;
        stats.OnXpChanged -= HandleXpChanged;
        stats.OnLevelChanged -= HandleLevelChanged;
        stats.OnVitalsChanged -= HandleVitalsChanged;
    }

    // 🔹 NEW: hard-refresh vitals every frame as a safety net
    private void Update()
    {
        if (!stats) return;
        HandleVitalsChanged();
    }

    // --- Handlers ---
    private void HandleLevelChanged()
    {
        if (levelText) levelText.text = stats.level.ToString();
        if (expCenterText) expCenterText.text = $"{stats.CurrentXp} / {stats.XpToNext}";
    }

    private void HandleXpChanged()
    {
        if (expCenterText)
            expCenterText.text = $"{stats.CurrentXp} / {stats.XpToNext}";

        if (!expFillImage) return;

        float target = stats.XpNormalized;
        if (_expCo != null) StopCoroutine(_expCo);
        _expCo = StartCoroutine(LerpExpFill(expFillImage.fillAmount, target, expLerpDuration));
    }

    private IEnumerator LerpExpFill(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            expFillImage.fillAmount = Mathf.Lerp(from, to, p);
            yield return null;
        }
        expFillImage.fillAmount = to;
        _expCo = null;
    }

    private void HandleVitalsChanged()
    {
        if (!stats) return;

        if (hpCenterText)
            hpCenterText.text = $"{stats.CurrentHp}/{stats.MaxHp}";

        if (mpCenterText)
            mpCenterText.text = $"{stats.CurrentMp}/{stats.MaxMp}";

        // Stamina text + bar
        if (staminaCenterText)
            staminaCenterText.text =
                $"{Mathf.CeilToInt(stats.CurrentStamina)}/{Mathf.CeilToInt(stats.MaxStamina)}";

        if (staminaFillImage)
            staminaFillImage.fillAmount = stats.StaminaNormalized;
    }
}
