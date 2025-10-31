// Assets/Scripts/UI/ItemTooltipUI.cs
using System.Text;
using UnityEngine;
using TMPro;
using Game.Items;

public class ItemTooltipUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text subtitleRarity;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private TMP_Text description;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private TooltipAnchorBeside anchor;

    [Header("Style")]
    [SerializeField] private Color blessedColor = new(1f, 0.9f, 0.3f); // warm gold
    [SerializeField] private Color labelGrey = new(0.75f, 0.75f, 0.75f);

    // --- Call this from slots with the target RectTransform ---
    public void Show(ItemInstance inst, RectTransform target)
    {
        Show(inst); // build + activate
        if (anchor && target) anchor.PlaceBeside(target);
    }

    public void Show(ItemInstance inst)
    {
        var def = inst.def;
        var labelColor = RarityRules.GetLabelColor(inst.tier);

        title.text = def.displayName;
        title.color = labelColor;

        if (subtitleRarity)
        {
            subtitleRarity.gameObject.SetActive(true);
            subtitleRarity.text = inst.tier.ToString();
            subtitleRarity.color = labelColor;
        }

        // clear previous lines
        foreach (Transform t in lineContainer) Destroy(t.gameObject);

        // Construct sections
        AddHeader("Level / Class");
        AddLine($"Level {def.requirements.level}");
        AddLine($"Class: {ClassesToText(def.requirements.usableBy)}");

        AddHeader("Required Stats");
        AddLine($"STR {def.requirements.minStrength} | DEX {def.requirements.minDexterity} | ENG {def.requirements.minEnergy}");

        // Damage or Wizardry
        if (def.category == ItemCategory.Weapon)
        {
            if (def.baseDamage.wizardry > 0)
                AddHeader("Wizardry");
            else
                AddHeader("Damage");

            if (def.baseDamage.wizardry > 0)
                AddLine($"{inst.EffectiveWizardry} Wizardry");
            else
                AddLine($"{inst.EffectiveMinDamage}–{inst.EffectiveMaxDamage} Attack");

            AddLine($"Attack Speed: {inst.EffectiveAttackSpeed:0.##}/s");
            AddLine($"Critical: {inst.EffectiveCritChance * 100f:0.#}% ×{inst.EffectiveCritMult:0.##}");
        }

        if (def.category == ItemCategory.Armor || def.subtype == ItemSubtype.Shield)
        {
            AddHeader("Defense");
            if (def.baseDefense > 0) AddLine($"Physical Defense: {inst.EffectiveDefense}");
            if (def.baseMagicResist > 0) AddLine($"Magic Resist: {inst.EffectiveMagicResist}");
            if (def.hpOnKill > 0) AddLine($"HP on Kill: +{inst.EffectiveHpOnKill:0.#}");
            if (def.manaOnKill > 0) AddLine($"Mana on Kill: +{inst.EffectiveManaOnKill:0.#}");
        }

        // Legendary/Mythical/etc extra note placeholder
        if (inst.tier >= ItemTier.Legendary && inst.tier <= ItemTier.Godlike)
        {
            AddHeader($"{inst.tier} Bonus");
            AddLine("Additional legendary affix placeholder");
        }

        // Blessed
        if (inst.isBlessed)
        {
            AddHeader("Blessed");
            foreach (var s in inst.BlessedLines())
            {
                var line = AddLine(s);
                line.color = blessedColor;
            }
        }

        // Sockets
        if (def.socketsMax > 0)
        {
            AddHeader("Sockets");
            foreach (var s in inst.SocketLines()) AddLine(s);
        }

        // Durability & Value
        AddHeader("Durability / Value");
        AddLine($"Durability: {inst.currentDurability}/{def.baseDurability}");
        AddLine($"Value: {inst.EffectiveValue} gold");

        // Optional description
        if (description)
        {
            if (!string.IsNullOrEmpty(def.description))
            {
                description.gameObject.SetActive(true);
                description.text = def.description;
                description.color = labelGrey;
            }
            else description.gameObject.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    TMP_Text AddHeader(string text)
    {
        var go = Instantiate(linePrefab, lineContainer);
        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = $"<b>{text}</b>";
        return tmp;
    }

    TMP_Text AddLine(string text)
    {
        var go = Instantiate(linePrefab, lineContainer);
        var tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        return tmp;
    }

    static string ClassesToText(CharacterClass flags)
    {
        if (flags == CharacterClass.All) return "All";
        if (flags == CharacterClass.None) return "-";
        StringBuilder sb = new();
        foreach (CharacterClass c in new[] { CharacterClass.Knight, CharacterClass.Elf, CharacterClass.Wizard })
            if ((flags & c) != 0) { if (sb.Length > 0) sb.Append(", "); sb.Append(c); }
        return sb.ToString();
    }
}
