// Assets/Scripts/UI/Widgets/Labels/ItemLabel.cs
using UnityEngine;
using TMPro;
using Game.Items;   // ItemDefinition, RarityRules, ItemWorldPickup

/// <summary>
/// World-space label for a pickup:
/// - Lives on the Text (TMP) object
/// - Auto-reads the ItemDefinition from the closest ItemWorldPickup parent
/// - Colors the text by item rarity
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ItemLabel : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("If left empty, this will be taken from the parent ItemWorldPickup at runtime.")]
    [SerializeField] private ItemDefinition def;

    [Tooltip("If true, use the legacy 3-tier rarity color instead of the new 9-tier tier color.")]
    [SerializeField] private bool useLegacyIfPresent = false;

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();

        // Auto-wire from parent pickup if not assigned in Inspector
        if (def == null)
        {
            var pickup = GetComponentInParent<ItemWorldPickup>();
            if (pickup != null && pickup.def != null)
            {
                def = pickup.def;   // NOTE: field name is 'def', not 'Def'
            }
        }

        Refresh();
    }

    /// <summary>
    /// Allows setting the definition from code (e.g. for spawned items).
    /// </summary>
    public void SetItem(ItemDefinition definition)
    {
        def = definition;
        Refresh();
    }

    /// <summary>
    /// Rebuilds the label text + color from the current definition.
    /// </summary>
    public void Refresh()
    {
        if (_text == null || def == null)
            return;

        // Choose color
        var color = RarityRules.GetLabelColor(def.defaultTier);
        if (useLegacyIfPresent)
        {
            color = ItemDefinition.RarityColor(def.legacyRarity);
        }

        _text.color = color;
        _text.text = string.IsNullOrEmpty(def.displayName)
            ? def.name           // fallback to asset name
            : def.displayName;
    }
}
