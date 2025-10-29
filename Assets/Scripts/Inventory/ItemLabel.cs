using UnityEngine;
using TMPro;

public class ItemLabel : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    [SerializeField] ItemDefinition item;

    void Reset() => text = GetComponentInChildren<TMP_Text>();

    void OnValidate()
    {
        if (text && item) Apply();
    }

    void Start()
    {
        if (text && item) Apply();
    }

    void Apply()
    {
        text.text = item.displayName;
        text.color = ItemDefinition.RarityColor(item.rarity);
    }
}
