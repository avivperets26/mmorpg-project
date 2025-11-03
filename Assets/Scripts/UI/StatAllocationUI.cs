// Assets/Scripts/UI/StatAllocationUI.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StatAllocationUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject rootPanel; // the whole dialog root (CharacterStatPanel)

    [Header("Available Points")]
    [SerializeField] private TMP_Text availablePointsText;

    [System.Serializable]
    public class StatRow
    {
        public string statName;     // e.g., "Strength"
        public TMP_Text nameLabel;    // left label
        public Button minusBtn;     // –
        public TMP_Text valueText;    // big number in the middle
        public Button plusBtn;      // +
        public TMP_Text detailsText;  // MU-like details block under the row
    }

    [Header("Rows")]
    [SerializeField] private StatRow strengthRow;
    [SerializeField] private StatRow dexterityRow;
    [SerializeField] private StatRow vitalityRow;
    [SerializeField] private StatRow energyRow;

    [Header("Footer")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("Polish / Fade")]
    [Tooltip("Seconds for open/close fade.")]
    [SerializeField] private float fadeDuration = 0.12f;

    // ---- Base numbers kept locally so we don't depend on PlayerStats members ----
    [Header("Base numbers (can be tuned in Inspector)")]
    [Tooltip("Physical weapon base min/max")]
    [SerializeField] private int fallbackWeaponBaseMin = 0;
    [SerializeField] private int fallbackWeaponBaseMax = 0;

    [Tooltip("Magic staff base min/max")]
    [SerializeField] private int fallbackStaffBaseMin = 0;
    [SerializeField] private int fallbackStaffBaseMax = 0;

    [Tooltip("Total defense from equipped armor")]
    [SerializeField] private int fallbackEquipDefense = 0;

    [Tooltip("Total magic resistance from equipped items")]
    [SerializeField] private int fallbackEquipMagicRes = 0;

    // pending deltas (not yet applied to PlayerStats)
    private int _dStr, _dDex, _dVit, _dEng;

    // fade helpers
    private CanvasGroup _cg;
    private Coroutine _fadeCo;

    // ---------- Unity ----------

    private void Awake()
    {
        // Wire rows
        WireRow(strengthRow, OnMinusStrength, OnPlusStrength, "Strength");
        WireRow(dexterityRow, OnMinusDexterity, OnPlusDexterity, "Dexterity");
        WireRow(vitalityRow, OnMinusVitality, OnPlusVitality, "Vitality");
        WireRow(energyRow, OnMinusEnergy, OnPlusEnergy, "Energy");

        // Footer buttons
        if (applyButton) applyButton.onClick.AddListener(Apply);
        if (cancelButton) cancelButton.onClick.AddListener(CancelAndCloseUI); // <-- fixed

        // PlayerStats fallback
        if (!playerStats)
        {
#if UNITY_2023_1_OR_NEWER
            playerStats = FindFirstObjectByType<PlayerStats>();
#else
            playerStats = FindObjectOfType<PlayerStats>();
#endif
        }

        // CanvasGroup for fade (on the root panel)
        if (rootPanel)
        {
            _cg = rootPanel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = rootPanel.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.interactable = false;
            _cg.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        ResetPending();
        RefreshAll();
    }

    // ---------- Public API: Show / Hide ----------

    public void Open()
    {
        if (!rootPanel) return;

        rootPanel.transform.SetAsLastSibling(); // on top
        rootPanel.SetActive(true);

        ResetPending();
        RefreshAll();
        StartFade(1f, fadeDuration, enableInteractionAtEnd: true);
    }

    public void Close()
    {
        if (!rootPanel) return;

        StartFade(0f, fadeDuration, enableInteractionAtEnd: false, onDone: () =>
        {
            rootPanel.SetActive(false);
        });
    }

    public void CancelAndCloseUI()
    {
        Cancel();   // refunds pending deltas
        Close();    // fades out
    }

    public void OnPressClose() => CancelAndCloseUI();

    // ---------- Internal: Fade ----------

    private void StartFade(float to, float dur, bool enableInteractionAtEnd, System.Action onDone = null)
    {
        if (!_cg)
        {
            if (to < 1f && rootPanel) rootPanel.SetActive(false);
            onDone?.Invoke();
            return;
        }

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeCo(to, dur, enableInteractionAtEnd, onDone));
    }

    private IEnumerator FadeCo(float to, float dur, bool enableInteractionAtEnd, System.Action onDone)
    {
        float from = _cg.alpha;
        float t = 0f;

        _cg.interactable = false;
        _cg.blocksRaycasts = to > from; // block during fade-in

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            _cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        _cg.alpha = to;
        _cg.interactable = enableInteractionAtEnd && Mathf.Approximately(to, 1f);
        _cg.blocksRaycasts = _cg.interactable;

        _fadeCo = null;
        onDone?.Invoke();
    }

    // ---------- Row wiring ----------

    private void WireRow(StatRow row, UnityEngine.Events.UnityAction onMinus, UnityEngine.Events.UnityAction onPlus, string label)
    {
        if (row == null) return;
        if (row.nameLabel) row.nameLabel.text = string.IsNullOrWhiteSpace(row.statName) ? label : row.statName;
        if (row.minusBtn) row.minusBtn.onClick.AddListener(onMinus);
        if (row.plusBtn) row.plusBtn.onClick.AddListener(onPlus);
    }

    // ---------- Button callbacks per stat ----------

    private void OnPlusStrength() { TryAdd(ref _dStr); }
    private void OnPlusDexterity() { TryAdd(ref _dDex); }
    private void OnPlusVitality() { TryAdd(ref _dVit); }
    private void OnPlusEnergy() { TryAdd(ref _dEng); }

    private void OnMinusStrength() { TryRemove(ref _dStr); }
    private void OnMinusDexterity() { TryRemove(ref _dDex); }
    private void OnMinusVitality() { TryRemove(ref _dVit); }
    private void OnMinusEnergy() { TryRemove(ref _dEng); }

    // ---------- Core add/remove logic ----------

    private void TryAdd(ref int delta)
    {
        if (!playerStats) return;
        if (playerStats.availableStatPoints <= 0) return;

        if (playerStats.TrySpendPoint())
        {
            delta++;
            RefreshAll();
        }
    }

    private void TryRemove(ref int delta)
    {
        if (!playerStats) return;
        if (delta <= 0) return;

        delta--;
        playerStats.RefundPoint();
        RefreshAll();
    }

    // ---------- Apply / Cancel ----------

    private void Apply()
    {
        if (!playerStats) return;
        if (_dStr == 0 && _dDex == 0 && _dVit == 0 && _dEng == 0) return;

        playerStats.ApplyDelta(_dStr, _dDex, _dVit, _dEng);
        ResetPending();
        RefreshAll();
    }

    private void Cancel()
    {
        if (!playerStats) return;

        int totalPending = _dStr + _dDex + _dVit + _dEng;
        playerStats.availableStatPoints += totalPending;

        ResetPending();
        RefreshAll();
    }

    private void ResetPending()
    {
        _dStr = _dDex = _dVit = _dEng = 0;
    }

    // ---------- UI refresh ----------

    private void RefreshAll()
    {
        if (!playerStats) return;

        if (availablePointsText)
            availablePointsText.text = $"Points Available: {playerStats.availableStatPoints}";

        // Update numbers + details per row
        RefreshRow(strengthRow, playerStats.strength, _dStr, BuildDetailsForStrength());
        RefreshRow(dexterityRow, playerStats.dexterity, _dDex, BuildDetailsForDexterity());
        RefreshRow(vitalityRow, playerStats.vitality, _dVit, BuildDetailsForVitality());
        RefreshRow(energyRow, playerStats.energy, _dEng, BuildDetailsForEnergy());

        // Footer buttons
        int pending = _dStr + _dDex + _dVit + _dEng;
        if (applyButton) applyButton.interactable = pending > 0;
        if (cancelButton) cancelButton.interactable = pending > 0;

        // Show/hide +/– per rules
        bool canAdd = playerStats.availableStatPoints > 0;
        SetRowAddRemoveState(strengthRow, canAdd, _dStr);
        SetRowAddRemoveState(dexterityRow, canAdd, _dDex);
        SetRowAddRemoveState(vitalityRow, canAdd, _dVit);
        SetRowAddRemoveState(energyRow, canAdd, _dEng);
    }

    private void RefreshRow(StatRow row, int baseValue, int delta, string details)
    {
        if (row == null) return;

        int shown = baseValue + delta;
        if (row.valueText) row.valueText.text = shown.ToString();
        if (row.detailsText) row.detailsText.text = details;
    }

    // Shows/hides +/– per row, and keeps interactable states in sync.
    private void SetRowAddRemoveState(StatRow row, bool canAdd, int delta)
    {
        if (row == null) return;

        if (row.plusBtn)
        {
            row.plusBtn.interactable = canAdd;
            row.plusBtn.gameObject.SetActive(canAdd);
        }

        bool canRemove = delta > 0;
        if (row.minusBtn)
        {
            row.minusBtn.interactable = canRemove;
            row.minusBtn.gameObject.SetActive(canRemove);
        }
    }

    // ---------- Calculations for details ----------

    private int Level => playerStats != null ? playerStats.level : 1;

    // Use local fallbacks so there is no dependency on PlayerStats members.
    private int WeaponBaseMin => fallbackWeaponBaseMin;
    private int WeaponBaseMax => fallbackWeaponBaseMax;
    private int StaffBaseMin => fallbackStaffBaseMin;
    private int StaffBaseMax => fallbackStaffBaseMax;
    private int EquipDefense => fallbackEquipDefense;
    private int EquipMRes => fallbackEquipMagicRes;

    private int CurrentStrength => (playerStats?.strength ?? 0) + _dStr;
    private int CurrentDex => (playerStats?.dexterity ?? 0) + _dDex;
    private int CurrentVit => (playerStats?.vitality ?? 0) + _dVit;
    private int CurrentInt => (playerStats?.energy ?? 0) + _dEng;

    private string BuildDetailsForStrength()
    {
        int str = CurrentStrength;
        int dex = CurrentDex;

        int minPhys = WeaponBaseMin + 2 * str;
        int maxPhys = WeaponBaseMax + 2 * str;
        float atkSpd = 100f + 0.5f * dex; // (optionally show only under DEX)

        return
            $"<alpha=#99>Physical Attack</alpha>: {minPhys}–{maxPhys}\n" +
            $"<alpha=#99>Attack Speed</alpha>: {atkSpd:0}";
    }

    private string BuildDetailsForDexterity()
    {
        int dex = CurrentDex;
        int hit = 100 + 2 * Level + 5 * dex;
        float crt = 5f + 0.2f * dex;
        float eva = 0.15f * dex;

        return
            $"<alpha=#99>Attack Success</alpha>: {hit}\n" +
            $"<alpha=#99>Critical Chance</alpha>: {crt:0.0}%\n" +
            $"<alpha=#99>Evasion</alpha>: {eva:0.0}%";
    }

    private string BuildDetailsForVitality()
    {
        int vit = CurrentVit;
        int dex = CurrentDex;

        int hp = 100 + 10 * Level + 20 * vit;
        int def = EquipDefense + vit + Mathf.FloorToInt(0.5f * dex);
        float h5 = 1f + 0.5f * vit;
        int sp = 100 + 5 * vit + 2 * dex;
        float s5 = 1f + 0.2f * vit;

        return
            $"<alpha=#99>Max HP</alpha>: {hp}\n" +
            $"<alpha=#99>Defense</alpha>: {def}\n" +
            $"<alpha=#99>HP Regen/5s</alpha>: {h5:0.0}\n" +
            $"<alpha=#99>Stamina</alpha>: {sp}  <alpha=#99>(+{s5:0.0}/5s)</alpha>";
    }

    private string BuildDetailsForEnergy()
    {
        int intel = CurrentInt;

        int mp = 50 + 5 * Level + 15 * intel;
        int matkMin = StaffBaseMin + 3 * intel;
        int matkMax = StaffBaseMax + 3 * intel;
        float m5 = 1f + 0.4f * intel;
        int mres = EquipMRes + intel;

        return
            $"<alpha=#99>Max MP</alpha>: {mp}\n" +
            $"<alpha=#99>Magic Attack</alpha>: {matkMin}–{matkMax}\n" +
            $"<alpha=#99>MP Regen/5s</alpha>: {m5:0.0}\n" +
            $"<alpha=#99>Magic Resist</alpha>: {mres}";
    }
}
