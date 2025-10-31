using UnityEngine;
using TMPro;
using Game.Items;

[RequireComponent(typeof(TMP_Text))]
public class WorldItemLabel : MonoBehaviour
{
    public ItemInstance item;
    private TMP_Text _text;

    void Awake() => _text = GetComponent<TMP_Text>();

    void Start() => Refresh();

    public void Refresh()
    {
        if (item == null || _text == null) return;

        var color = RarityRules.GetLabelColor(item.tier);
        _text.color = color;

        string bless = item.isBlessed ? "Blessed " : "";
        string upg = item.upgradeLevel > 0 ? $" +{item.upgradeLevel}" : "";
        string sockets = item.def.socketsMax > 0 ? $" + {item.sockets.Count} Socket" : "";

        _text.text = $"{bless}{item.def.displayName}{upg}{sockets}";
    }
}
