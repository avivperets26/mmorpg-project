// Assets\Scripts\UI\Widgets\Tooltips\StatAllocationUI.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StatAllocationUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private GameObject rootPanel; // CharacterStatPanel

    [Header("Available Points")]
    [SerializeField] private TMP_Text availablePointsText;

    [System.Serializable]
    public class StatRow
    {
        public string statName;              // "Strength", "Vitality", ...
        public TMP_Text nameAndValueLabel;   // merged: "Strength: 12"
        public Button plusBtn;               // [+] only
        public TMP_Text detailsText;         // multi-line details under the row
    }

    [Header("Rows (order = UI order)")]
    [SerializeField] private StatRow strengthRow;
    [SerializeField] private StatRow vitalityRow;
    [SerializeField] private StatRow dexterityRow;
    [SerializeField] private StatRow energyRow;

    [Header("Polish / Fade")]
    [Tooltip("Seconds for open/close fade.")]
    [SerializeField] private float fadeDuration = 0.12f;

    // ---- Local tunables (no hard dependency on PlayerStats internals) ----
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

    // Fade helpers
    private CanvasGroup _cg;
    private Coroutine _fadeCo;

    private const string DimOpen = "<color=#FFFFFF99>"; // ~60% opacity
    private const string DimClose = "</color>";
    private static string Dim(string t) => $"{DimOpen}{t}{DimClose}";

    private const string PosOpen = "<color=#6CFF6C>";
    private const string PosClose = "</color>";
    private static string Pos(string t) => $"{PosOpen}{t}{PosClose}";

    private const string NegOpen = "<color=#FF6C6C>";
    private static string Neg(string t) => $"{NegOpen}{t}{PosClose}";

    /// <summary>
    /// Unified range formatter: [12.0 min - 14.5 max]
    /// Always 1 decimal, spaces around '-'.
    /// </summary>
    private static string FormatRange(float min, float max)
    {
        return $"[{min:0.0} min - {max:0.0} max]";
    }

    // ---------- Unity ----------

    private void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        if (!playerStats) playerStats = FindFirstObjectByType<PlayerStats>();
#else
        if (!playerStats) playerStats = FindObjectOfType<PlayerStats>();
#endif
        if (rootPanel)
        {
            _cg = rootPanel.GetComponent<CanvasGroup>();
            if (!_cg) _cg = rootPanel.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.interactable = false;
            _cg.blocksRaycasts = false;
        }

        // Wire rows (plus only) and set fallback names
        WireRow(strengthRow, OnPlusStrength, "Strength");
        WireRow(vitalityRow, OnPlusVitality, "Vitality");
        WireRow(dexterityRow, OnPlusDexterity, "Dexterity");
        WireRow(energyRow, OnPlusEnergy, "Energy");

        // Ensure details TMPs render rich text
        EnsureRichText(strengthRow?.detailsText);
        EnsureRichText(vitalityRow?.detailsText);
        EnsureRichText(dexterityRow?.detailsText);
        EnsureRichText(energyRow?.detailsText);

        // (Optional) Also allow rich text on the merged label (harmless)
        EnsureRichText(strengthRow?.nameAndValueLabel);
        EnsureRichText(vitalityRow?.nameAndValueLabel);
        EnsureRichText(dexterityRow?.nameAndValueLabel);
        EnsureRichText(energyRow?.nameAndValueLabel);
    }

    private static void EnsureRichText(TMP_Text t)
    {
        if (!t) return;
        t.richText = true;
#if UNITY_2021_3_OR_NEWER
        t.textWrappingMode = TextWrappingModes.Normal;
#else
        t.enableWordWrapping = true;
#endif
        t.overflowMode = TextOverflowModes.Overflow;
    }

    void OnEnable()
    {
        if (playerStats) playerStats.OnDerivedChanged += RefreshAll;
        RefreshAll();
    }

    void OnDisable()
    {
        if (playerStats) playerStats.OnDerivedChanged -= RefreshAll;
    }

    // ---------- Public API: Show / Hide ----------

    public void Open()
    {
        if (!rootPanel) return;

        rootPanel.transform.SetAsLastSibling(); // on top
        rootPanel.SetActive(true);

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

    public void CancelAndCloseUI() => Close();
    public void OnPressClose() => Close();

    // ---------- Internal: Fade ----------

    private void StartFade(float to, float dur, bool enableInteractionAtEnd, System.Action onDone = null)
    {
        if (!_cg)
        {
            if (to < 1f && rootPanel) rootPanel.SetActive(false);
            onDone?.Invoke();
            return;
        }

        if (!isActiveAndEnabled)
        {
            if (to >= 1f && rootPanel && !rootPanel.activeSelf)
                rootPanel.SetActive(true);

            UiCoroutineRunner.Run(FadeCo(to, dur, enableInteractionAtEnd, onDone));
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

    private void WireRow(StatRow row, UnityEngine.Events.UnityAction onPlus, string fallbackName)
    {
        if (row == null) return;

        if (string.IsNullOrWhiteSpace(row.statName))
            row.statName = fallbackName;

        if (row.plusBtn)
            row.plusBtn.onClick.AddListener(onPlus);
    }

    // ---------- [+] callbacks — immediate & permanent ----------

    private void OnPlusStrength()
    {
        if (!playerStats) return;
        if (!playerStats.TrySpendPoint()) return;
        playerStats.ApplyDelta(1, 0, 0, 0);  // +1 STR
        RefreshAll();
    }

    private void OnPlusVitality()
    {
        if (!playerStats) return;
        if (!playerStats.TrySpendPoint()) return;
        playerStats.ApplyDelta(0, 0, 1, 0);  // +1 VIT
        RefreshAll();
    }

    private void OnPlusDexterity()
    {
        if (!playerStats) return;
        if (!playerStats.TrySpendPoint()) return;
        playerStats.ApplyDelta(0, 1, 0, 0);  // +1 DEX
        RefreshAll();
    }

    private void OnPlusEnergy()
    {
        if (!playerStats) return;
        if (!playerStats.TrySpendPoint()) return;
        playerStats.ApplyDelta(0, 0, 0, 1);  // +1 ENG
        RefreshAll();
    }

    private void TrySpendAndAdd(ref int statField)
    {
        if (!playerStats) return;
        if (playerStats.availableStatPoints <= 0) return;

        if (playerStats.TrySpendPoint())
        {
            statField++;         // apply immediately (final)
            playerStats.RecalculateCapsAndClamp();
            RefreshAll();        // rebuild numbers/details
        }
    }

    // ---------- UI refresh ----------

    public void RefreshAll()
    {
        if (!playerStats) return;

        if (availablePointsText)
            availablePointsText.text = $"Points Available: {playerStats.availableStatPoints}";

        // Order: Strength, Vitality, Dexterity, Energy
        RefreshRow(strengthRow, playerStats.strength, BuildDetailsForStrength());
        RefreshRow(vitalityRow, playerStats.vitality, BuildDetailsForVitality());
        RefreshRow(dexterityRow, playerStats.dexterity, BuildDetailsForDexterity());
        RefreshRow(energyRow, playerStats.energy, BuildDetailsForEnergy());

        // Hide [+] when you can't add
        bool canAdd = playerStats.availableStatPoints > 0;
        SetPlusState(strengthRow, canAdd);
        SetPlusState(vitalityRow, canAdd);
        SetPlusState(dexterityRow, canAdd);
        SetPlusState(energyRow, canAdd);
    }

    private void RefreshRow(StatRow row, int value, string details)
    {
        if (row == null) return;

        if (row.nameAndValueLabel)
            row.nameAndValueLabel.text = $"{row.statName}: {value}";

        if (row.detailsText)
            row.detailsText.text = details;
    }

    private void SetPlusState(StatRow row, bool canAdd)
    {
        if (row?.plusBtn == null) return;
        row.plusBtn.interactable = canAdd;
        row.plusBtn.gameObject.SetActive(canAdd);
    }

    // ---------- Calculations for details (your terminology) ----------

    private int Level => playerStats != null ? playerStats.level : 1;

    // Local fallbacks (kept for future; not used when playerStats available)
    private int WeaponBaseMin => fallbackWeaponBaseMin;
    private int WeaponBaseMax => fallbackWeaponBaseMax;
    private int StaffBaseMin => fallbackStaffBaseMin;
    private int StaffBaseMax => fallbackStaffBaseMax;
    private int EquipDefense => fallbackEquipDefense;
    private int EquipMRes => fallbackEquipMagicRes;

    private int STR => playerStats ? playerStats.strength : 0;
    private int DEX => playerStats ? playerStats.dexterity : 0;
    private int VIT => playerStats ? playerStats.vitality : 0;
    private int ENG => playerStats ? playerStats.energy : 0;

    // --- Strength ---
    private string BuildDetailsForStrength()
    {
        // Base from attributes only (no gear).
        int baseMin = 2 * STR;
        int baseMax = 2 * STR;

        // Gear contribution (from PlayerStats aggregation)
        int gearMin = playerStats ? playerStats.equipDamageMin : 0;
        int gearMax = playerStats ? playerStats.equipDamageMax : 0;

        // Totals (shown as min–max)
        int totalMin = baseMin + gearMin;
        int totalMax = baseMax + gearMax;

        // Attack success & crit base
        int hitMin = 80 + 1 * Level + 3 * STR;
        int hitMax = 120 + 3 * Level + 6 * STR;
        float baseCritMin = 2f + 0.10f * STR;
        float baseCritMax = 5f + 0.25f * STR;

        // Gear crit in percentage points
        float gearCrit = playerStats ? playerStats.equipCritChance : 0f;
        float totalCritMin = baseCritMin + gearCrit;
        float totalCritMax = baseCritMax + gearCrit;

        // Deltas (green): + (...) and + (x%)
        string deltaPhys = (gearMin == 0 && gearMax == 0)
            ? ""
            : (gearMin == gearMax
                ? $" {Pos($"+ ({gearMin})")}"
                : $" {Pos($"+ ({gearMin}–{gearMax})")}");

        string deltaCrit = gearCrit > 0f ? $" {Pos($"+ ({gearCrit:0.0}%)")}" : "";

        return
            $"{Dim("Physical Attack")}: {FormatRange(totalMin, totalMax)}{deltaPhys}\n" +
            $"{Dim("Attack Success Rate")}: {FormatRange(hitMin, hitMax)}\n" +
            $"{Dim("Critical Chance")}: {FormatRange(totalCritMin, totalCritMax)}%{deltaCrit}";
    }

    // --- Vitality ---
    private string BuildDetailsForVitality()
    {
        int maxHP = 100 + 10 * Level + 20 * VIT;
        float hp5 = 1f + 0.5f * VIT;
        float immunity = Mathf.Clamp01(0.004f * VIT + 0.001f * Mathf.Max(0, EquipDefense)) * 100f;

        return
            $"{Dim("Max HP")}: {maxHP}\n" +
            $"{Dim("HP Regen/5s")}: {hp5:0.0}\n" +
            $"{Dim("Immunity Rate")}: {immunity:0.0}%";
    }

    // --- Dexterity ---
    private string BuildDetailsForDexterity()
    {
        float baseAtkSpd = 100f + 0.8f * DEX;
        float gearAtkSpd = playerStats ? playerStats.equipAttackSpeedRating : 0f;
        float totalAtkSpd = baseAtkSpd + gearAtkSpd;

        // Show +(...) or -(...) in green/red
        string deltaAtkSpd =
            Mathf.Abs(gearAtkSpd) > 0.01f
                ? (gearAtkSpd > 0 ? $" {Pos($"+ ({gearAtkSpd:0})")}" : $" {Neg($"- ({Mathf.Abs(gearAtkSpd):0})")}")
                : "";

        // Optional: show approximate APS resulting from gear delta only
        float approxAPS = 1f + (gearAtkSpd / 100f); // 0 rating => 1.00 APS, +20 => 1.20 APS
        string apsHint = $"  (~{approxAPS:0.00} APS)";

        int gearArmor = playerStats ? playerStats.equipDefense : 0;
        int baseArmor = Mathf.FloorToInt(0.4f * DEX);
        int totalArmor = baseArmor + gearArmor;

        // For now min == max, but we display a range so later we can introduce variance if we want.
        float defMin = totalArmor;
        float defMax = totalArmor;

        string deltaArmor = gearArmor > 0 ? $" {Pos($"+ ({gearArmor})")}" : "";

        float evasion = 0.20f * DEX;

        return
            $"{Dim("Attack Speed")}: {totalAtkSpd:0}{deltaAtkSpd}{apsHint}\n" +
            $"{Dim("Physical Defense")}: {FormatRange(defMin, defMax)}{deltaArmor}\n" +
            $"{Dim("Evasion Rate")}: {evasion:0.0}%";
    }

    // --- Energy ---
    private string BuildDetailsForEnergy()
    {
        int maxMP = 50 + 5 * Level + 15 * ENG;

        int baseMMin = 3 * ENG;
        int baseMMax = 3 * ENG;

        int gearWiz = playerStats ? Mathf.RoundToInt(playerStats.equipWizardry) : 0;

        int totalMMin = baseMMin + gearWiz;
        int totalMMax = baseMMax + gearWiz;

        // Wizardry adds flat to both ends; show + (X)
        string deltaMagic = gearWiz > 0 ? $" {Pos($"+ ({gearWiz})")}" : "";

        const float critMult = 1.5f;
        int mcMin = Mathf.RoundToInt(totalMMin * critMult);
        int mcMax = Mathf.RoundToInt(totalMMax * critMult);

        float mp5 = 1f + 0.4f * ENG;

        int gearMRes = playerStats ? playerStats.equipMagicResist : 0;
        int baseMRes = ENG;
        int totalMRes = baseMRes + gearMRes;
        string deltaMRes = gearMRes > 0 ? $" {Pos($"+ ({gearMRes})")}" : "";

        return
            $"{Dim("Max MP")}: {maxMP}\n" +
            $"{Dim("Magic Attack")}: {FormatRange(totalMMin, totalMMax)}{deltaMagic}\n" +
            $"{Dim("Magic Critical")}: {FormatRange(mcMin, mcMax)}\n" +
            $"{Dim("MP Regen/5s")}: {mp5:0.0}\n" +
            $"{Dim("Magic Resist")}: {totalMRes}{deltaMRes}";
    }
}
