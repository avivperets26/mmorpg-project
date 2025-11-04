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
    [SerializeField] private GameObject rootPanel; // CharacterStatPanel

    [Header("Available Points")]
    [SerializeField] private TMP_Text availablePointsText;

    [System.Serializable]
    public class StatRow
    {
        public string statName;     // "Strength", "Vitality", ...
        public TMP_Text nameAndValueLabel; // merged: "Strength: 12"
        public Button plusBtn;      // [+] only
        public TMP_Text detailsText;// multi-line details under the row
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

        // Ensure details TMPs render rich text (for <alpha=...>)
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
        t.richText = true;                     // fixes </alpha> showing
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
    }

    private void OnEnable()
    {
        RefreshAll();
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

    public void CancelAndCloseUI() => Close(); // points are final now
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

    private void OnPlusStrength() { TrySpendAndAdd(ref playerStats.strength); }
    private void OnPlusDexterity() { TrySpendAndAdd(ref playerStats.dexterity); }
    private void OnPlusVitality() { TrySpendAndAdd(ref playerStats.vitality); }
    private void OnPlusEnergy() { TrySpendAndAdd(ref playerStats.energy); }

    private void TrySpendAndAdd(ref int statField)
    {
        if (!playerStats) return;
        if (playerStats.availableStatPoints <= 0) return;

        if (playerStats.TrySpendPoint())
        {
            statField++;         // apply immediately (final)
            RefreshAll();        // rebuild numbers/details
        }
    }

    // ---------- UI refresh ----------

    private void RefreshAll()
    {
        if (!playerStats) return;

        if (availablePointsText)
            availablePointsText.text = $"Points Available: {playerStats.availableStatPoints}";

        // Order: Strength, Vitality, Dexterity, Energy (as requested)
        RefreshRow(strengthRow, playerStats.strength, BuildDetailsForStrength());
        RefreshRow(vitalityRow, playerStats.vitality, BuildDetailsForVitality());
        RefreshRow(dexterityRow, playerStats.dexterity, BuildDetailsForDexterity());
        RefreshRow(energyRow, playerStats.energy, BuildDetailsForEnergy());

        // Hide [+] entirely when you can't add
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
        row.plusBtn.gameObject.SetActive(canAdd); // ← HIDE when no points
    }

    // ---------- Calculations for details (your terminology) ----------

    private int Level => playerStats != null ? playerStats.level : 1;

    // Local fallbacks (keep UI free of hard PlayerStats dependencies)
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
    // Physical Attack [min - max]
    // Attack Success Rate [min - max]
    // Critical Chance [min - max]
    private string BuildDetailsForStrength()
    {
        int physMin = WeaponBaseMin + 2 * STR;
        int physMax = WeaponBaseMax + 2 * STR;
        int hitMin = 80 + 1 * Level + 3 * STR;
        int hitMax = 120 + 3 * Level + 6 * STR;
        float critMin = 2f + 0.10f * STR;
        float critMax = 5f + 0.25f * STR;

        return
            $"{Dim("Physical Attack")}: {physMin}–{physMax}\n" +
            $"{Dim("Attack Success Rate")}: {hitMin}–{hitMax}\n" +
            $"{Dim("Critical Chance")}: {critMin:0.0}%–{critMax:0.0}%";
    }
    // --- Vitality ---
    // Max Health Point, Max Health Regeneration, Immunity rate (non-magic)
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
    // Attack Speed, Armor, Evasion rate
    private string BuildDetailsForDexterity()
    {
        float atkSpd = 100f + 0.8f * DEX;
        int armor = EquipDefense + Mathf.FloorToInt(0.4f * DEX);
        float evasion = 0.20f * DEX;

        return
            $"{Dim("Attack Speed")}: {atkSpd:0}\n" +
            $"{Dim("Armor")}: {armor}\n" +
            $"{Dim("Evasion Rate")}: {evasion:0.0}%";
    }

    // --- Energy ---
    // Max Mana Point, Magic Attack [min-max], Magic critical [min-max], Mana Regeneration, Magic Resistance
    private string BuildDetailsForEnergy()
    {
        int maxMP = 50 + 5 * Level + 15 * ENG;
        int mMin = StaffBaseMin + 3 * ENG;
        int mMax = StaffBaseMax + 3 * ENG;
        const float critMult = 1.5f;
        int mcMin = Mathf.RoundToInt(mMin * critMult);
        int mcMax = Mathf.RoundToInt(mMax * critMult);
        float mp5 = 1f + 0.4f * ENG;
        int mRes = EquipMRes + ENG;

        return
            $"{Dim("Max MP")}: {maxMP}\n" +
            $"{Dim("Magic Attack")}: {mMin}–{mMax}\n" +
            $"{Dim("Magic Critical")}: {mcMin}–{mcMax}\n" +
            $"{Dim("MP Regen/5s")}: {mp5:0.0}\n" +
            $"{Dim("Magic Resist")}: {mRes}";
    }
}
