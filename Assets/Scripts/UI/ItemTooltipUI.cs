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

    public void Show(ItemInstance inst, RectTransform target)
    {
        if (inst == null || inst.def == null) return;

        Build(inst);

        if (anchor && target)
        {
            anchor.Attach(target);
            anchor.RepositionNow();
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        anchor?.Detach();
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------

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

        // ----- Clear previous stat lines -----
        for (int i = lineContainer.childCount - 1; i >= 0; i--)
            Destroy(lineContainer.GetChild(i).gameObject);

        // First divider
        AddSeparator();

        // ================= CATEGORY-SPECIFIC STATS =================
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
            // place for accessory lines (ring/amulet bonuses)
        }

        // Render the stats block
        if (sb.Length > 0)
        {
            var lines = sb.ToString().TrimEnd('\n').Split('\n');
            foreach (var line in lines) AddLine(line);
            AddSeparator();
        }

        // ================= REQUIREMENTS + TYPE =================
        AddLine(StatLine("Required Level", $"{def.requirements.level}"));
        AddLine(StatLine("Type", EquipTypeLabel(def))); // <- replaces Slot/Main/Off Hand
        AddSeparator(height: 10f, alpha: 0f); // spacer

        // ================= VALUE =================
        var valueLine = AddLine(StatLine("Value", $"{inst.EffectiveValue} Gold"));
        valueLine.alignment = TextAlignmentOptions.Right;
        valueLine.fontStyle = FontStyles.Italic;

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
    }

    // ----------------------------------------------------------------------

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

        // Normalize rect so each line stretches full width of the LineContainer
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
        rt.sizeDelta = new Vector2(0, 1f);

        var spacerBottom = new GameObject("Sep_SpaceBottom", typeof(RectTransform), typeof(LayoutElement));
        spacerBottom.transform.SetParent(lineContainer, false);
        spacerBottom.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(0, (height - 1f) * 0.5f);
    }

    private string StatLine(string label, string value)
    {
        var hex = ColorUtility.ToHtmlStringRGB(labelGrey);
        return $"<color=#{hex}>{label}:</color> {value}";
    }

    // New: human-friendly type label for weapons/armor/off-hand items
    private static string EquipTypeLabel(ItemDefinition def)
    {
        // Off-hand only items first
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

        // Weapons
        if (def.category == ItemCategory.Weapon)
        {
            string hand = def.grip switch
            {
                WeaponGrip.TwoHanded => "Two-Handed",
                WeaponGrip.OneHanded => "One-Handed",
                _ => null
            };

            // Sword, Axe, Bow, Dagger, Staff, Mace, Spear, etc.
            string kind = def.subtype.ToString();
            return hand != null ? $"{hand} {kind}" : kind;
        }

        // Armor / Accessories – rename Legs -> Pants (already in enum)
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
