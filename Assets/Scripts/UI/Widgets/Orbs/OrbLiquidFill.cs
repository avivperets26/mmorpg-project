using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OrbLiquidFill : MonoBehaviour
{
    public enum Vital { HP, MP }

    [Header("Wiring")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Vital vital;
    [Tooltip("Rect inside the orb that holds the liquid (RawImage).")]
    [SerializeField] private RectTransform liquidBody;
    [Tooltip("Wave image that sits at the liquid surface.")]
    [SerializeField] private RectTransform wave;
    [Tooltip("Optional: center label like '120/150'.")]
    [SerializeField] private TMP_Text centerText;
    [Tooltip("RawImage so we can scroll the UV for a subtle motion.")]
    [SerializeField] private RawImage liquidRaw;

    [Header("Behaviour")]
    [Tooltip("How fast the fill height follows stat changes.")]
    [SerializeField] private float fillLerpSpeed = 6f;
    [Tooltip("Horizontal UV scroll speed for the liquid tiling.")]
    [SerializeField] private float uvScrollSpeed = 0.025f;
    [Tooltip("Idle bob amplitude (px) of the wave.")]
    [SerializeField] private float waveBobAmplitude = 3f;
    [Tooltip("Idle bob frequency (Hz) of the wave.")]
    [SerializeField] private float waveBobFrequency = 1.3f;
    [Tooltip("Small horizontal sway of the wave (px).")]
    [SerializeField] private float waveSwayAmplitude = 6f;

    private RectTransform _maskRect;
    private float _targetFill01;
    private float _currentFill01;
    private float _baseWaveY;
    private Vector2 _liquidUv;

    private void Awake()
    {
        if (!stats)
        {
#if UNITY_2023_1_OR_NEWER
            stats = FindFirstObjectByType<PlayerStats>();
#else
            stats = FindObjectOfType<PlayerStats>();
#endif
        }

        _maskRect = transform as RectTransform; // this script should sit on the Mask object
        if (wave) _baseWaveY = wave.anchoredPosition.y;
    }

    private void OnEnable()
    {
        if (!stats) return;
        stats.OnVitalsChanged += HandleVitalsChanged;
        HandleVitalsChanged(); // paint initial
        _currentFill01 = _targetFill01;
        ApplyFillInstant(_currentFill01);
    }

    private void OnDisable()
    {
        if (!stats) return;
        stats.OnVitalsChanged -= HandleVitalsChanged;
    }

    private void Update()
    {
        // Lerp fill smoothly
        _currentFill01 = Mathf.MoveTowards(_currentFill01, _targetFill01, fillLerpSpeed * Time.unscaledDeltaTime);
        ApplyFillInstant(_currentFill01);

        // Subtle UV scroll for motion (only works if we have a RawImage with a Repeat texture)
        if (liquidRaw)
        {
            _liquidUv.x += uvScrollSpeed * Time.unscaledDeltaTime;
            if (_liquidUv.x > 1f) _liquidUv.x -= 1f;
            liquidRaw.uvRect = new Rect(_liquidUv, Vector2.one);
        }

        // Idle wave bob/sway
        if (wave)
        {
            float t = Time.unscaledTime;
            var p = wave.anchoredPosition;
            p.y = _baseWaveY + Mathf.Sin(t * (Mathf.PI * 2f) * waveBobFrequency) * waveBobAmplitude;
            p.x = Mathf.Sin(t * 1.5f) * waveSwayAmplitude;
            wave.anchoredPosition = p;
        }
    }

    private void HandleVitalsChanged()
    {
        int cur, max;
        if (vital == Vital.HP) { cur = stats.CurrentHp; max = stats.MaxHp; }
        else { cur = stats.CurrentMp; max = stats.MaxMp; }

        _targetFill01 = max <= 0 ? 0f : Mathf.Clamp01(cur / (float)max);

        if (centerText) centerText.text = $"{cur}/{max}";
    }

    private void ApplyFillInstant(float f01)
    {
        if (!liquidBody || _maskRect == null) return;

        float maskH = _maskRect.rect.height;

        // Make the liquid body height proportional to fill (pivot at bottom)
        var size = liquidBody.sizeDelta;
        size.y = f01 * maskH;
        liquidBody.sizeDelta = size;

        // Glue wave to the liquid surface
        if (wave)
        {
            var wpos = wave.anchoredPosition;
            wpos.y = f01 * maskH;
            wave.anchoredPosition = wpos;
        }
    }
}
