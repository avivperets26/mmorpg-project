// Assets\Scripts\UI\Widgets\Tooltips\ItemTooltipUI.cs
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Items;

[DisallowMultipleComponent]
public class ItemTooltipUI : MonoBehaviour
{
    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false;

    [Header("Wiring")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitleRarity;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private TMP_Text description;          // optional
    [SerializeField] private GameObject linePrefab;         // optional (TMP_Text)
    [SerializeField] private TooltipAnchorBeside anchor;

    [Header("Badge")]
    [SerializeField] private TMP_Text statusBadge;          // assign the top-right TMP here

    [Header("Style")]
    [SerializeField] private Color blessedColor = new(1f, 0.9f, 0.3f);
    [SerializeField] private Color labelGrey = new(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color badgeEquipped = new(0.95f, 0.85f, 0.25f);
    [SerializeField] private Color badgeInventory = new(0.65f, 0.9f, 1f);

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeCurve = null; // set in Inspector (defaults to linear if null)


    private string _tag => "ItemTooltip";
    private CanvasGroup _cg;
    private InventorySlotTooltip _owner;
    private Coroutine _fadeCo;
    private PlayerStats _playerStatsForTooltip;
    private EquipmentController _equipment; // cached for future needs

    // Inline-comparison state (only used when context == Inventory)
    private bool _inlineCompareEnabled = false;
    private ItemStatsSnapshot _baseline; // valid only when _inlineCompareEnabled

    // Colors for inline deltas (HTML, used via TMP rich text)
    private const string COL_POS = "#6EEB83";
    private const string COL_NEG = "#FF6B6B";
    private const string COL_NEU = "#A0A0A0";

    private TooltipContext _context = TooltipContext.Inventory;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        _cg.alpha = 0f;               // hidden by default
        _cg.interactable = false;     // tooltip never blocks input
        _cg.blocksRaycasts = false;

        // Fallback: if statusBadge not wired, try find by name
        if (!statusBadge)
        {
            var t = transform.Find("StatusBadge");
            if (t) statusBadge = t.GetComponent<TMP_Text>();
        }
    }

    void OnDisable()
    {
        anchor?.Detach();
        _owner = null;
        _inlineCompareEnabled = false;
        if (statusBadge) statusBadge.gameObject.SetActive(false);

        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }

        UITooltipDebug.Log(enableLogs, this, _tag, "OnDisable() detached + cleared");
    }


    // ---------------- Context & inline comparison API ----------------
    public void SetContext(TooltipContext ctx)
    {
        _context = ctx;
        if (statusBadge)
        {
            statusBadge.gameObject.SetActive(true);
            statusBadge.fontStyle = FontStyles.SmallCaps;
            statusBadge.fontWeight = FontWeight.Medium;
            statusBadge.fontSize = 12;
        }

        if (ctx == TooltipContext.Equipped)
        {
            // ensure no deltas on equipped tooltips
            _inlineCompareEnabled = false;
            _baseline = ItemStatsSnapshot.Zero;

            if (statusBadge)
            {
                statusBadge.text = "EQUIPPED";
                statusBadge.color = badgeEquipped;
            }
        }
        else
        {
            if (statusBadge)
            {
                statusBadge.text = "INVENTORY";
                statusBadge.color = badgeInventory;
            }
        }

        UITooltipDebug.Log(enableLogs, this, _tag, "SetContext(" + ctx + ")");
    }

    public void ClearContext()
    {
        _context = TooltipContext.Inventory;
        _inlineCompareEnabled = false;
        if (statusBadge) statusBadge.gameObject.SetActive(false);
    }

    /// <summary>Enable inline per-stat deltas using a baseline snapshot.</summary>
    public void SetInlineComparisonBaseline(ItemStatsSnapshot baseline)
    {
        _inlineCompareEnabled = true;
        _baseline = baseline;
    }

    // -------- Owner-guarded API (prevents random hides from other slots) --------
    public void ShowFrom(InventorySlotTooltip owner, ItemInstance inst, RectTransform target)
    {
        _owner = owner;
        Show(inst, target);
    }

    public void HideOwner(InventorySlotTooltip owner)
    {
        if (_owner != null && _owner != owner) return; // ignore hides from others
        _owner = null;
        Hide();
    }

    // ----------------------------- Public API ----------------------------------
    /// <summary>Show & place the tooltip. Safe even when called right after creating slots.</summary>
    public void Show(ItemInstance inst, RectTransform target)
    {
        ApplyBadgeFromContext();
        if (inst == null || inst.def == null)
        {
            UITooltipDebug.Warn(enableLogs, this, _tag, "Show() with null inst/def");
            return;
        }

        // Cache PlayerStats (requirements coloring, etc.)
#if UNITY_2023_1_OR_NEWER
        _playerStatsForTooltip ??= FindFirstObjectByType<PlayerStats>();
        _equipment            ??= FindFirstObjectByType<EquipmentController>(FindObjectsInactive.Include);
#else
        _playerStatsForTooltip = _playerStatsForTooltip ?? FindObjectOfType<PlayerStats>();
        _equipment = _equipment ?? FindObjectOfType<EquipmentController>(true);
#endif

        // Bring tooltip above everything
        transform.SetAsLastSibling();

        // Build visual content
        Build(inst);

        // Follow the provided target (slot / item rect)
        if (anchor && target) anchor.Attach(target);

        // First-pass layout before placement
        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // Place the tooltip now (uses current rect sizes)
        anchor?.RepositionNow();

        // Fade in
        StartFade(1f);

        // Second placement after TMP/layout settle
        var eof = PlaceAfterLayout();
        if (isActiveAndEnabled) StartCoroutine(eof);
        else UiCoroutineRunner.Run(eof);

        UITooltipDebug.Log(enableLogs, this, _tag, $"Show('{inst.def.displayName}') target='{target?.name}'");

    }

    public void Hide()
    {
        anchor?.Detach();
        StartFade(0f);

        // Keep the current context so the badge stays configured.
        // We only clear inline comparison, not the badge.
        _inlineCompareEnabled = false;

        UITooltipDebug.Log(enableLogs, this, _tag, "Hide()");
    }

    // ---------------------------- Internals ------------------------------------
    private System.Collections.IEnumerator PlaceAfterLayout()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (!this || !gameObject.activeInHierarchy) yield break;

        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        anchor?.RepositionNow();
    }

    private void StartFade(float targetAlpha)
    {
        // Stop any running fade
        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }

        // If this GameObject is inactive or the component is disabled,
        // we can't start a coroutine. Just snap the alpha and bail.
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            if (_cg != null)
                _cg.alpha = targetAlpha;

            return;
        }

        // Normal case during gameplay: fade via coroutine
        _fadeCo = StartCoroutine(FadeTo(targetAlpha));
    }


    private System.Collections.IEnumerator FadeTo(float target)
    {
        if (_cg == null) yield break;

        float start = _cg.alpha;
        if (Mathf.Approximately(start, target)) yield break;

        float t = 0f;
        float dur = Mathf.Max(0f, fadeDuration);

        _cg.blocksRaycasts = false;

        if (dur <= 0f)
        {
            _cg.alpha = target;
            yield break;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime; // UI fades independent of timescale
            float k = Mathf.Clamp01(t / dur);
            float eased = fadeCurve != null ? fadeCurve.Evaluate(k) : k;
            _cg.alpha = Mathf.Lerp(start, target, eased);
            yield return null;
        }

        _cg.alpha = target;
    }

    private void Build(ItemInstance inst)
    {
        var def = inst.def;
        var tierColor = RarityRules.GetLabelColor(inst.tier);

        bool isPotion = def is PotionItemDefinition;

        // ----- RARITY -----
        if (subtitleRarity)
        {
            subtitleRarity.gameObject.SetActive(true);
            subtitleRarity.text = inst.tier.ToString();
            subtitleRarity.color = tierColor;
            subtitleRarity.enableAutoSizing = false;
            subtitleRarity.fontStyle = FontStyles.SmallCaps;
            subtitleRarity.fontWeight = FontWeight.Medium;
            subtitleRarity.fontSize = 14;
            subtitleRarity.alignment = TextAlignmentOptions.Center;
        }

        // ----- NAME -----
        if (title)
        {
            title.text = string.IsNullOrEmpty(def.displayName) ? "Item" : def.displayName;
            title.color = new Color(0.9f, 0.9f, 0.9f);
            title.fontStyle = FontStyles.Normal;
            title.fontWeight = FontWeight.Medium;
            title.alignment = TextAlignmentOptions.Center;
            title.enableAutoSizing = false;
            title.fontSize = 18;
        }

        // ----- CLEAR LINES -----
        for (int i = lineContainer.childCount - 1; i >= 0; i--)
            Destroy(lineContainer.GetChild(i).gameObject);

        AddSeparator();

        // ----- POTION (consumable) ------------------------------------------
        if (isPotion)
        {
            var potionDef = (PotionItemDefinition)def;
            BuildPotionBody(inst, potionDef);

            // Layout and early return (we’ve already built everything we want)
            var rtPotion = (RectTransform)transform;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rtPotion);
            return;
        }

        // ----- BASE STATS (with inline deltas when enabled) -----
        if (def.category == ItemCategory.Weapon)
        {
            bool isWizardWeapon = def.baseDamage.wizardry > 0;

            if (isWizardWeapon)
            {
                AddStatLineWithInlineDelta("Magic Attack",
                    inst.EffectiveWizardry, StatKind.Wizardry);
            }
            else
            {
                AddDamageLineWithInlineDelta(inst.EffectiveMinDamage, inst.EffectiveMaxDamage);
            }

            if (def.baseDamage.critChance > 0)
                AddStatLineWithInlineDelta("Critical Chance", inst.EffectiveCritChance * 100f, StatKind.CritChance, suffix: "%");

            if (def.baseDamage.attackSpeed > 0)
                AddStatLineWithInlineDelta("Attack Speed",
                    inst.EffectiveAttackSpeed, StatKind.AttackSpeed);

            AddStatLineSimple("Durability", $"{inst.currentDurability}/{def.baseDurability}");
        }
        else if (def.category == ItemCategory.Armor || def.subtype == ItemSubtype.Shield)
        {
            if (def.baseDefense > 0)
                AddStatLineWithInlineDelta("Defense", inst.EffectiveDefense, StatKind.Defense);

            if (def.baseMagicResist > 0)
                AddStatLineWithInlineDelta("Magic Resist", inst.EffectiveMagicResist, StatKind.MagicResist, suffix: "%");

            if (def.hpOnKill > 0)
                AddStatLineWithInlineDelta("HP on Kill", inst.EffectiveHpOnKill, StatKind.HpOnKill);

            if (def.manaOnKill > 0)
                AddStatLineWithInlineDelta("Mana on Kill", inst.EffectiveManaOnKill, StatKind.MpOnKill);

            AddStatLineSimple("Durability", $"{inst.currentDurability}/{def.baseDurability}");
        }



        // ----- REQS + TYPE -----
        bool hasStats = _playerStatsForTooltip != null;
        bool levelOk = !hasStats || _playerStatsForTooltip.level >= def.requirements.level;

        var reqLevel = AddLine(StatLine("Required Level", $"{def.requirements.level}"));
        reqLevel.color = levelOk ? new Color(0.85f, 0.85f, 0.85f) : Color.red;

        AddLine(StatLine("Type", EquipTypeLabel(def)));
        AddSeparator(height: 10f, alpha: 0f);

        // ----- VALUE -----
        var valueLine = AddLine(StatLine("Value", $"{inst.EffectiveValue} Gold"));
        valueLine.alignment = TextAlignmentOptions.Right;
        valueLine.fontStyle = FontStyles.Italic;

        // ----- OPTIONALS -----
        if (description) description.gameObject.SetActive(false);

        if (inst.isBlessed)
        {
            foreach (var s in inst.BlessedLines())
            {
                var line = AddLine(s);
                line.color = blessedColor;
            }
        }

        // (No bottom "Comparison" block — inline deltas already shown)
        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // ---------------- Inline delta helpers ----------------
    private enum StatKind { Damage, AttackSpeed, Defense, MagicResist, HpOnKill, MpOnKill, Wizardry, CritChance }

    private void AddDamageLineWithInlineDelta(int min, int max)
    {
        string value = $"{min} – {max}";
        string line = StatLine("Physical Attack", value);

        if (_inlineCompareEnabled)
        {
            int dMin = min - _baseline.dmgMin;
            int dMax = max - _baseline.dmgMax;
            if (dMin != 0 || dMax != 0)
                line += "  " + InlineDeltaRange(dMin, dMax);
        }
        AddLine(line);
    }

    private void AddStatLineWithInlineDelta(string label, float current, StatKind kind, string suffix = "")
    {
        string main = suffix == "%" ? $"{current:0.#}%" : $"{current:0.##}";
        string line = StatLine(label, main);

        if (_inlineCompareEnabled)
        {
            float diff = kind switch
            {
                StatKind.AttackSpeed => current - _baseline.atkSpeed,
                StatKind.Defense => current - _baseline.defense,
                StatKind.MagicResist => current - _baseline.magicResist,
                StatKind.HpOnKill => current - _baseline.hpOnKill,
                StatKind.MpOnKill => current - _baseline.mpOnKill,
                StatKind.Wizardry => current - _baseline.wizardry,
                StatKind.CritChance => current - (_baseline.critChance * 100f),
                _ => 0f
            };
            if (!Mathf.Approximately(diff, 0f))
                line += "  " + InlineDeltaFloat(diff, suffix);
        }

        AddLine(line);
    }

    private void AddStatLineSimple(string label, string value)
    {
        AddLine(StatLine(label, value));
    }

    private static string InlineDeltaRange(int dMin, int dMax)
    {
        // both up or both down → one sign, one arrow, parentheses
        bool bothUp = dMin > 0 && dMax > 0;
        bool bothDown = dMin < 0 && dMax < 0;

        if (bothUp || bothDown)
        {
            string sign = bothUp ? "+" : "-";
            string col = bothUp ? COL_POS : COL_NEG;
            string arrow = bothUp ? "▲" : "▼";
            int absMin = Mathf.Abs(dMin);
            int absMax = Mathf.Abs(dMax);
            // → "+ ( 7 – 3 ) ▲"
            return $"<color={col}>{sign} ( {absMin} – {absMax} ) {arrow}</color>";
        }

        // mixed → color each term separately, no parentheses
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (dMin != 0)
        {
            bool up = dMin > 0;
            string col = up ? COL_POS : COL_NEG;
            string arrow = up ? "▲" : "▼";
            sb.Append($"<color={col}>{(up ? "+" : "-")} {Mathf.Abs(dMin)} min {arrow}</color>");
        }

        if (dMax != 0)
        {
            if (sb.Length > 0) sb.Append("  "); // space between parts
            bool up = dMax > 0;
            string col = up ? COL_POS : COL_NEG;
            string arrow = up ? "▲" : "▼";
            sb.Append($"<color={col}>{(up ? "+" : "-")} {Mathf.Abs(dMax)} max {arrow}</color>");
        }

        return sb.ToString();
    }


    private static string InlineDeltaFloat(float diff, string suffix = "")
    {
        string col = diff > 0 ? COL_POS : COL_NEG;
        string arrow = diff > 0 ? "▲" : "▼";
        float abs = Mathf.Abs(diff);
        string val = suffix == "%" ? $"{abs:0.#}%" : $"{abs:0.##}";
        string sign = diff > 0 ? "+" : "-";
        // → "+ 2.0% ▲"
        return $"<color={col}>{sign} {val} {arrow}</color>";
    }

    // ---------------- Potions ----------------

    private void BuildPotionBody(ItemInstance inst, PotionItemDefinition potion)
    {
        var def = inst.def;
        // Clear lines already done before this method is called.

        // 1) Type line (e.g. "Small Health Potion")
        string typeLabel;
        string size = potion.potionSize.ToString();   // Small / Medium / Large
        string kind = potion.potionType == PotionType.Health ? "Health" :
                      potion.potionType == PotionType.Mana ? "Mana" :
                      "Potion";

        typeLabel = $"{size} {kind} Potion";
        AddLine(StatLine("Type", typeLabel));

        // 2) Effect line(s)
        string resShort = potion.potionType == PotionType.Health ? "HP" : "MP";

        bool hasInstant = potion.instantAmount > 0;
        bool hasOverTime = potion.overTimeAmount > 0 && potion.overTimeDurationSeconds > 0f;

        var sb = new StringBuilder();

        if (hasInstant)
        {
            sb.Append($"Restores {potion.instantAmount} {resShort} instantly");
        }

        if (hasInstant && hasOverTime)
        {
            sb.Append(" and ");
        }

        if (hasOverTime)
        {
            sb.Append($"restores {potion.overTimeAmount} {resShort} over {potion.overTimeDurationSeconds:0.#} sec");
        }

        if (!hasInstant && !hasOverTime)
        {
            sb.Append($"Restores {resShort}.");
        }
        else
        {
            sb.Append(".");
        }

        AddLine(sb.ToString());

        AddSeparator(height: 10f, alpha: 0f);

        // 3) Requirements (optional, like other items)
        bool hasStats = _playerStatsForTooltip != null;
        bool levelOk = !hasStats || _playerStatsForTooltip.level >= def.requirements.level;

        var reqLevel = AddLine(StatLine("Required Level", $"{def.requirements.level}"));
        reqLevel.color = levelOk ? new Color(0.85f, 0.85f, 0.85f) : Color.red;

        AddSeparator(height: 8f, alpha: 0f);

        // 4) Value line
        var valueLine = AddLine(StatLine("Value", $"{inst.EffectiveValue} Gold"));
        valueLine.alignment = TextAlignmentOptions.Right;
        valueLine.fontStyle = FontStyles.Italic;

        // 5) Flavor description (from def.description)
        if (description)
        {
            if (!string.IsNullOrWhiteSpace(def.description))
            {
                description.gameObject.SetActive(true);
                description.text = def.description;
            }
            else
            {
                description.gameObject.SetActive(false);
            }
        }

        // Potions currently don't use blessing/sockets; skip BlessedLines().
    }

    // ---------------- UI line builders ----------------
    private TMP_Text AddLine(string text)
    {
        TMP_Text tmp = null;
        RectTransform rt = null;

        if (linePrefab != null)
        {
            var go = Instantiate(linePrefab, lineContainer);
            tmp = go.GetComponent<TMP_Text>();
            rt = go.transform as RectTransform;
        }

        if (tmp == null)
        {
            var go = new GameObject("TextLine", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(lineContainer, false);
            tmp = go.GetComponent<TextMeshProUGUI>();
            rt = go.transform as RectTransform;
        }

        // stretch across container
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 20f);
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);

            var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 0f;
            le.preferredWidth = -1f;
            le.flexibleWidth = 1f;
        }

        tmp.text = text;
        tmp.color = new Color(0.85f, 0.85f, 0.85f);
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
        tmp.enableAutoSizing = false;
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Left;

        return tmp;
    }

    private void AddSeparator(float height = 6f, float alpha = 0.15f)
    {
        var spacerTop = new GameObject("Sep_SpaceTop", typeof(RectTransform), typeof(LayoutElement));
        spacerTop.transform.SetParent(lineContainer, false);
        spacerTop.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(0, (height - 1f) * 0.5f);

        var line = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(lineContainer, false);
        var img = line.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, alpha);

        var rt = (RectTransform)line.transform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 1);

        var spacerBottom = new GameObject("Sep_SpaceBottom", typeof(RectTransform), typeof(LayoutElement));
        spacerBottom.transform.SetParent(lineContainer, false);
        spacerBottom.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(0, (height - 1f) * 0.5f);
    }

    private string StatLine(string label, string value)
    {
        var hex = ColorUtility.ToHtmlStringRGB(labelGrey);
        return $"<color=#{hex}>{label}:</color> {value}";
    }

    // Human-friendly type label
    // Human-friendly type label
    private static string EquipTypeLabel(ItemDefinition def)
    {
        if (def == null) return string.Empty;

        if (def.category == ItemCategory.Shield || def.subtype == ItemSubtype.Shield)
        {
            // You can tweak wording later if you prefer just "Shield".
            return "Shield (Left Hand)";
        }

        // Off-hand-only items (non-shield)
        if (def.grip == WeaponGrip.OffHandOnly)
        {
            return def.subtype switch
            {
                ItemSubtype.Orb => "Off-Hand Orb",
                ItemSubtype.Book => "Off-Hand Book",
                ItemSubtype.Arrows => "Off-Hand Arrows",
                _ => "Off-Hand"
            };
        }

        // Main weapons
        if (def.category == ItemCategory.Weapon)
        {
            string hand = def.grip switch
            {
                WeaponGrip.TwoHanded => "Two-Handed",
                WeaponGrip.OneHanded => "One-Handed",
                _ => null
            };

            string kind = def.subtype.ToString();
            return hand != null ? $"{hand} {kind}" : kind;
        }

        // Armor / accessories etc.
        return def.subtype switch
        {
            ItemSubtype.Helmet => "Helmet",
            ItemSubtype.Chest => "Chest",
            ItemSubtype.Gloves => "Gloves",
            ItemSubtype.Boots => "Boots",
            ItemSubtype.Pants => "Pants",
            ItemSubtype.Ring => "Ring",
            ItemSubtype.Amulet => "Amulet",
            _ => def.subtype.ToString()
        };
    }


    public void ClearInlineComparison()
    {
        // OLD: only zeroed the baseline (left the flag ON)
        // SetInlineComparisonBaseline(ItemStatsSnapshot.Zero);

        // NEW: actually disable the feature
        _inlineCompareEnabled = false;
        _baseline = ItemStatsSnapshot.Zero;
    }

    private void ApplyBadgeFromContext()
    {
        if (!statusBadge) return;

        statusBadge.gameObject.SetActive(true);
        statusBadge.fontStyle = FontStyles.SmallCaps;
        statusBadge.fontWeight = FontWeight.Medium;
        statusBadge.fontSize = 12;

        if (_context == TooltipContext.Equipped)
        {
            statusBadge.text = "EQUIPPED";
            statusBadge.color = badgeEquipped;
        }
        else
        {
            statusBadge.text = "INVENTORY";
            statusBadge.color = badgeInventory;
        }
    }
}
