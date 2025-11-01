// Assets/Scripts/UI/ItemTooltipUI.cs
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Items;

/// <summary>
/// Builds and displays the item tooltip beside an inventory slot.
/// Prefers right side of the slot and auto-flips if out of screen bounds.
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitleRarity;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private TMP_Text description;               // hidden for now (no description)
    [SerializeField] private GameObject linePrefab;              // a TMP_Text prefab for lines
    [SerializeField] private TooltipAnchorBeside anchor;

    [Header("Style")]
    [SerializeField] private Color blessedColor = new(1f, 0.9f, 0.3f); // warm gold
    [SerializeField] private Color labelGrey = new(0.75f, 0.75f, 0.75f);

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // stop following when hidden
        anchor?.Detach();
    }

    // ----------------------------------------------------------------------

    /// <summary>
    /// Show tooltip for given item beside the given target RectTransform.
    /// </summary>
    public void Show(ItemInstance inst, RectTransform target)
    {
        if (inst == null || inst.def == null) return;

        // 1) Build content (does not activate yet)
        Build(inst);

        // 2) Position beside target (prefer right side)
        if (anchor && target)
        {
            anchor.Attach(target);
            anchor.RepositionNow();
        }

        // 3) Now show (after positioned)
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hide tooltip and detach anchor.
    /// </summary>
    public void Hide()
    {
        anchor?.Detach();
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------

    /// <summary>
    /// Builds tooltip UI content for given item.
    /// </summary>
    private void Build(ItemInstance inst)
    {
        var def = inst.def;
        var tierColor = RarityRules.GetLabelColor(inst.tier);

        // ----- RARITY (on top) -----
        if (subtitleRarity)
        {
            subtitleRarity.gameObject.SetActive(true);
            subtitleRarity.text = inst.tier.ToString();
            subtitleRarity.color = tierColor;
            subtitleRarity.enableAutoSizing = false;
            subtitleRarity.fontStyle = FontStyles.SmallCaps;
            subtitleRarity.fontWeight = FontWeight.Medium;   // thinner than bold
            subtitleRarity.fontSize = 12;                  // fixed size keeps hierarchy tidy
        }

        // ----- NAME (thinner, not bold) -----
        if (title)
        {
            title.text = string.IsNullOrEmpty(def.displayName) ? "Item" : def.displayName;
            title.color = Color.white;            // keep neutral; rarity gets the color
            title.fontStyle = FontStyles.Normal; // not italic/bold
            title.fontWeight = FontWeight.Medium; // slimmer look
            title.enableAutoSizing = false;
            title.fontSize = 16;
        }

        // ----- Clear previous stat lines -----
        for (int i = lineContainer.childCount - 1; i >= 0; i--)
            Destroy(lineContainer.GetChild(i).gameObject);

        // First divider
        AddSeparator();

        // ================= CATEGORY-SPECIFIC STATS =================
        var sb = new StringBuilder();

        if (def.category == ItemCategory.Weapon)
        {
            // Wizardry weapons show "Wizardry Attack", others show "Attack Power"
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

            // Durability is useful on all equippable items
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
            // Hook for EquipmentStats when ready (Strength/Agility/Intellect/Stamina, etc.)
        }

        // Render the stats block
        if (sb.Length > 0)
        {
            var lines = sb.ToString().TrimEnd('\n').Split('\n');
            foreach (var line in lines) AddLine(line);
            AddSeparator();
        }

        // ================= REQUIREMENTS =================
        AddLine(StatLine("Required Level", $"{def.requirements.level}"));
        var slot = EquipmentMapping.GetSlotForSubtype(def.subtype);
        AddLine(StatLine("Slot", SlotToText(slot)));
        AddSeparator();

        // ================= VALUE =================
        AddLine(StatLine("Value", $"{inst.EffectiveValue} Gold"));
        AddSeparator();

        // ================= OPTIONALS =================
        if (description) description.gameObject.SetActive(false);

        if (inst.isBlessed)
        {
            foreach (var s in inst.BlessedLines())
            {
                var line = AddLine(s);
                line.color = blessedColor;
            }
        }
        // Sockets block can be enabled later
    }

    // ----------------------------------------------------------------------

    private TMP_Text AddLine(string text)
    {
        TMP_Text tmp = null;

        if (linePrefab != null)
        {
            var go = Instantiate(linePrefab, lineContainer);
            tmp = go.GetComponent<TMP_Text>();
        }

        // Fallback: create a TMP_Text if prefab missing/misconfigured
        if (tmp == null)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(lineContainer, false);
            tmp = go.GetComponent<TextMeshProUGUI>();

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(0, 20);
        }

        tmp.text = text;
        tmp.color = new Color(0.92f, 0.92f, 0.92f);
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
        tmp.enableAutoSizing = false;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }

    /// <summary>
    /// Inserts a subtle horizontal rule with small top/bottom spacing.
    /// </summary>
    private void AddSeparator(float height = 6f, float alpha = 0.15f)
    {
        // top space
        var spacerTop = new GameObject("Sep_SpaceTop", typeof(RectTransform), typeof(LayoutElement));
        spacerTop.transform.SetParent(lineContainer, false);
        spacerTop.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(0, (height - 1f) * 0.5f);

        // the line
        var line = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(lineContainer, false);
        var img = line.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, alpha); // subtle dark line

        var rt = (RectTransform)line.transform;
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 1f);

        // bottom space
        var spacerBottom = new GameObject("Sep_SpaceBottom", typeof(RectTransform), typeof(LayoutElement));
        spacerBottom.transform.SetParent(lineContainer, false);
        spacerBottom.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(0, (height - 1f) * 0.5f);
    }

    /// <summary>
    /// Formats a stat line with a grey label and white value (no bullet).
    /// </summary>
    private string StatLine(string label, string value)
    {
        var hex = ColorUtility.ToHtmlStringRGB(labelGrey);
        return $"<color=#{hex}>{label}:</color> {value}";
    }

    private static string SlotToText(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => "Head",
        EquipmentSlot.Chest => "Chest",
        EquipmentSlot.Hands => "Hands",
        EquipmentSlot.Legs => "Legs",
        EquipmentSlot.Feet => "Feet",
        EquipmentSlot.MainHand => "Main Hand",
        EquipmentSlot.OffHand => "Off Hand",
        EquipmentSlot.Ring1 or EquipmentSlot.Ring2 => "Ring",
        EquipmentSlot.Amulet => "Amulet",
        _ => slot.ToString()
    };

    private static string ClassesToText(CharacterClass flags)
    {
        if (flags == CharacterClass.All) return "All";
        if (flags == CharacterClass.None) return "-";

        StringBuilder sb = new();
        foreach (CharacterClass c in new[] { CharacterClass.Knight, CharacterClass.Elf, CharacterClass.Wizard })
        {
            if ((flags & c) != 0)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
