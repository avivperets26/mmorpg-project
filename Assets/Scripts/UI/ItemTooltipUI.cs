// Assets/Scripts/UI/ItemTooltipUI.cs
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Items;

[DisallowMultipleComponent]
public class ItemTooltipUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitleRarity;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private TMP_Text description;          // optional
    [SerializeField] private GameObject linePrefab;         // optional (TMP_Text)
    [SerializeField] private TooltipAnchorBeside anchor;

    [Header("Style")]
    [SerializeField] private Color blessedColor = new(1f, 0.9f, 0.3f);
    [SerializeField] private Color labelGrey = new(0.75f, 0.75f, 0.75f);

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private AnimationCurve fadeCurve = null; // set in Inspector (defaults to linear if null)

    private CanvasGroup _cg;
    private InventorySlotTooltip _owner;
    private Coroutine _fadeCo;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();

        // Keep GO ACTIVE at all times; visibility via alpha
        _cg.alpha = 0f;               // hidden by default
        _cg.interactable = false;     // tooltip never blocks input
        _cg.blocksRaycasts = false;
    }

    // Single OnDisable (avoid duplicates)
    void OnDisable()
    {
        anchor?.Detach();
        _owner = null;
        // leave alpha as-is; typically 0 by the time we disable the canvas/panel
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
        if (inst == null || inst.def == null) return;

        transform.SetAsLastSibling();   // render above everything

        // Build content first (GO stays active; layout has valid sizes)
        Build(inst);

        // Follow target
        if (anchor && target) anchor.Attach(target);

        // First-pass layout & placement
        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        anchor?.RepositionNow();

        // Ensure we are visible (fade to 1)
        StartFade(1f);

        // Second-pass placement (after TMP/Layout settle at end of frame)
        var eof = PlaceAfterLayout();
        if (isActiveAndEnabled) StartCoroutine(eof);
        else UiCoroutineRunner.Run(eof);
    }

    public void Hide()
    {
        anchor?.Detach();
        StartFade(0f);
    }

    // ---------------------------- Internals ------------------------------------
    private System.Collections.IEnumerator PlaceAfterLayout()
    {
        // Allow one frame so TMP preferred sizes resolve
        yield return null;
        // And end-of-frame so ContentSizeFitter/VerticalLayoutGroup settle
        yield return new WaitForEndOfFrame();

        if (!this || !gameObject.activeInHierarchy) yield break;

        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        anchor?.RepositionNow();
    }

    private void StartFade(float targetAlpha)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeTo(targetAlpha));
    }

    private System.Collections.IEnumerator FadeTo(float target)
    {
        if (_cg == null) yield break;

        float start = _cg.alpha;
        if (Mathf.Approximately(start, target)) yield break;

        float t = 0f;
        float dur = Mathf.Max(0f, fadeDuration);

        // stay non-blocking; if you ever want it to block, flip blocksRaycasts accordingly
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

        // ----- STATS -----
        var sb = new StringBuilder();

        if (def.category == ItemCategory.Weapon)
        {
            bool isWizardWeapon = def.baseDamage.wizardry > 0;
            string atkLabel = isWizardWeapon ? "Wizardry Attack" : "Attack Power";

            if (isWizardWeapon)
                sb.AppendLine(StatLine(atkLabel, $"{inst.EffectiveWizardry}"));
            else
                sb.AppendLine(StatLine(atkLabel, $"{inst.EffectiveMinDamage} – {inst.EffectiveMaxDamage}"));

            if (def.baseDamage.critChance > 0)
                sb.AppendLine(StatLine("Critical Chance", $"+{inst.EffectiveCritChance * 100f:0.#}%"));

            if (def.baseDamage.attackSpeed > 0)
                sb.AppendLine(StatLine("Attack Speed", $"{inst.EffectiveAttackSpeed:0.00}"));

            sb.AppendLine(StatLine("Durability", $"{inst.currentDurability}/{def.baseDurability}"));
        }
        else if (def.category == ItemCategory.Armor || def.subtype == ItemSubtype.Shield)
        {
            if (def.baseDefense > 0)
                sb.AppendLine(StatLine("Defense", $"+{inst.EffectiveDefense}"));
            if (def.baseMagicResist > 0)
                sb.AppendLine(StatLine("Magic Resist", $"+{inst.EffectiveMagicResist}%"));

            if (def.hpOnKill > 0)
                sb.AppendLine(StatLine("HP on Kill", $"+{inst.EffectiveHpOnKill:0.#}"));
            if (def.manaOnKill > 0)
                sb.AppendLine(StatLine("Mana on Kill", $"+{inst.EffectiveManaOnKill:0.#}"));

            sb.AppendLine(StatLine("Durability", $"{inst.currentDurability}/{def.baseDurability}"));
        }
        else if (def.category == ItemCategory.Accessory)
        {
            // reserved for accessory lines
        }

        if (sb.Length > 0)
        {
            var lines = sb.ToString().TrimEnd('\n').Split('\n');
            foreach (var line in lines) AddLine(line);
            AddSeparator();
        }

        // ----- REQS + TYPE -----
        AddLine(StatLine("Required Level", $"{def.requirements.level}"));
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

        // Ensure rect is up to date before first placement
        var rt = (RectTransform)transform;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

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
    private static string EquipTypeLabel(ItemDefinition def)
    {
        if (def.grip == WeaponGrip.OffHandOnly)
        {
            return def.subtype switch
            {
                ItemSubtype.Shield => "Off-Hand Shield",
                ItemSubtype.Orb => "Off-Hand Orb",
                ItemSubtype.Book => "Off-Hand Book",
                ItemSubtype.Arrows => "Off-Hand Arrows",
                _ => "Off-Hand"
            };
        }

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
}
