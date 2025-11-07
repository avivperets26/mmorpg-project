using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OrbLiquidMaterialDriver : MonoBehaviour
{
    public enum Vital { HP, MP }

    [Header("Wiring")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Vital vital;
    [SerializeField] private Image liquidImage; // the Image using MAT_Liquid_*

    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 4f;   // how fast value catches up
    [SerializeField] private float speedMultiplier = 1f; // extra per-orb wave speed

    private Material _runtimeMat;   // per-instance so both orbs can differ
    private float _displayFill;     // smoothed value

    private static readonly int FillProp = Shader.PropertyToID("_Fill");
    private static readonly int SpeedProp = Shader.PropertyToID("_Speed");

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (!stats) stats = FindFirstObjectByType<PlayerStats>();
#else
        if (!stats) stats = FindObjectOfType<PlayerStats>();
#endif
        if (!liquidImage) liquidImage = GetComponent<Image>();

        // Instantiate the material so this orb has its own copy
        if (liquidImage && liquidImage.material)
            _runtimeMat = new Material(liquidImage.material);
        if (liquidImage) liquidImage.material = _runtimeMat;

        _displayFill = GetTargetFill();
        SetFill(_displayFill);
        SetSpeed(GetBaseSpeed() * speedMultiplier);
    }

    private void OnEnable()
    {
        if (!stats) return;
        stats.OnVitalsChanged += OnVitals;
    }

    private void OnDisable()
    {
        if (!stats) return;
        stats.OnVitalsChanged -= OnVitals;
    }

    private void Update()
    {
        // Smoothly animate towards target
        float target = GetTargetFill();
        _displayFill = Mathf.MoveTowards(_displayFill, target, lerpSpeed * Time.unscaledDeltaTime);
        SetFill(_displayFill);
    }

    private void OnVitals()
    {
        // nothing else; Update will lerp
    }

    private float GetTargetFill()
    {
        if (!stats) return 0f;
        if (vital == Vital.HP) return Mathf.Clamp01(stats.CurrentHp / (float)stats.MaxHp);
        else return Mathf.Clamp01(stats.CurrentMp / (float)stats.MaxMp);
    }

    private float GetBaseSpeed() => 1.2f;

    private void SetFill(float v)
    {
        if (_runtimeMat) _runtimeMat.SetFloat(FillProp, v);
    }

    private void SetSpeed(float v)
    {
        if (_runtimeMat) _runtimeMat.SetFloat(SpeedProp, v);
    }
}
