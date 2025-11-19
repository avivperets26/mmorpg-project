// Assets/Scripts/UI/Widgets/Orbs/OrbLiquidMaterialDriver.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class OrbLiquidMaterialDriver : MonoBehaviour
{
    public enum Vital { HP, MP }

    [Header("Wiring")]
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Vital vital;
    [SerializeField] private Image liquidImage; // the Image using MAT_Liquid_HP/MP

    [Header("Animation")]
    [SerializeField] private float lerpSpeed = 4f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Material _runtimeMat;
    private float _displayFill;
    private bool _useImageFill; // true = use Image.fillAmount, false = use _Fill in shader

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

        // Decide which mode to use based on the MATERIAL
        bool matHasFill = liquidImage && liquidImage.material &&
                          liquidImage.material.HasProperty(FillProp);
        _useImageFill = !matHasFill; // if shader has _Fill -> use shader, else use Image.fillAmount

        // Only force Filled mode if we're using Image.fillAmount
        if (liquidImage && _useImageFill)
        {
            liquidImage.type = Image.Type.Filled;
            liquidImage.fillMethod = Image.FillMethod.Vertical;
            liquidImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        // Instantiate the material so this orb has its own copy
        if (liquidImage && liquidImage.material)
            _runtimeMat = new Material(liquidImage.material);
        if (liquidImage)
            liquidImage.material = _runtimeMat;

        _displayFill = GetTargetFill();
        SetFill(_displayFill);
        SetSpeed(GetBaseSpeed() * speedMultiplier);

        if (debugLogs && stats)
        {
            Debug.Log($"[ORB] {name} Awake. Vital={vital}, initial fill={_displayFill:F3}", this);
        }
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
        if (!stats) return;

        float target = GetTargetFill();

        if (!Mathf.Approximately(target, _displayFill))
        {
            _displayFill = Mathf.MoveTowards(
                _displayFill,
                target,
                lerpSpeed * Time.unscaledDeltaTime
            );
            SetFill(_displayFill);
        }
    }

    private void OnVitals()
    {
        if (!stats) return;

        if (debugLogs)
        {
            float target = GetTargetFill();
            if (vital == Vital.HP)
                Debug.Log($"[ORB] {name} OnVitals HP -> {stats.CurrentHp}/{stats.MaxHp}, target={target:F3}", this);
            else
                Debug.Log($"[ORB] {name} OnVitals MP -> {stats.CurrentMp}/{stats.MaxMp}, target={target:F3}", this);
        }
        // Leave _displayFill unchanged so Update can smoothly animate toward the new target.
    }

    private float GetTargetFill()
    {
        if (!stats) return 0f;

        return vital == Vital.HP
            ? Mathf.Clamp01(stats.CurrentHp / (float)stats.MaxHp)
            : Mathf.Clamp01(stats.CurrentMp / (float)stats.MaxMp);
    }

    private float GetBaseSpeed() => 1.2f;

    private void SetFill(float v)
    {
        v = Mathf.Clamp01(v);

        // 1) Image.fillAmount path (for simple materials without _Fill)
        if (_useImageFill && liquidImage)
            liquidImage.fillAmount = v;

        // 2) Shader _Fill path (your UI/LiquidFill shader)
        if (_runtimeMat != null && _runtimeMat.HasProperty(FillProp))
        {
            _runtimeMat.SetFloat(FillProp, v);
            PushToRenderedMaterial(FillProp, v);
        }

        if (debugLogs)
        {
            Debug.Log($"[ORB] {name} SetFill -> target={v:F3}, " +
                      $"useImageFill={_useImageFill}, " +
                      $"image.fillAmount={(liquidImage ? liquidImage.fillAmount : -1f):F3}", this);
        }
    }

    private void SetSpeed(float v)
    {
        if (_runtimeMat == null || !_runtimeMat.HasProperty(SpeedProp)) return;

        _runtimeMat.SetFloat(SpeedProp, v);
        PushToRenderedMaterial(SpeedProp, v);
    }

    private void PushToRenderedMaterial(int propertyId, float value)
    {
        if (!liquidImage) return;

        liquidImage.material = _runtimeMat; // keep base reference in sync
        var renderMat = liquidImage.materialForRendering;
        if (renderMat && renderMat.HasProperty(propertyId))
            renderMat.SetFloat(propertyId, value);

        liquidImage.SetMaterialDirty();
    }
}
