// Assets/Scripts/Inventory/ItemLabel.cs
using UnityEngine;
using TMPro;
using Game.Items;

[RequireComponent(typeof(TMP_Text))]
public class ItemLabel : MonoBehaviour
{
    [SerializeField] private ItemDefinition def;
    [SerializeField] private bool useLegacyIfPresent = false;

    private TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        Refresh();
    }

    public void SetItem(ItemDefinition definition)
    {
        def = definition;
        Refresh();
    }

    public void Refresh()
    {
        if (!_text || !def) return;

        // Prefer the new 9-tier system
        var color = RarityRules.GetLabelColor(def.defaultTier);

        // Optional: if you want old 3-tier coloring sometimes
        if (useLegacyIfPresent)
            color = ItemDefinition.RarityColor(def.legacyRarity);

        _text.color = color;
        _text.text = def.displayName;
    }
}
