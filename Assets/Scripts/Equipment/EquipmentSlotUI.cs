using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Items;

[DisallowMultipleComponent]
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private Image frame;       // square/rect frame
    [SerializeField] private TMP_Text label;    // placeholder name text
    [SerializeField] private Image icon;        // optional item icon preview

    // ← missing field (needed by Init/Show/OnClick)
    private EquipmentSlot _slot;

    public EquipmentSlot Slot => _slot;

    private EquipmentController _ctrl;

    public void Init(EquipmentSlot slot, EquipmentController controller)
    {
        _slot = slot;
        _ctrl = controller;
    }

    public void SetPlaceholder(string text)
    {
        if (label) label.text = text;
        if (icon) icon.enabled = false;
    }

    public void ShowItem(ItemDefinition def)
    {
        if (!icon) return;

        if (def != null && def.icon != null)
        {
            icon.sprite = def.icon;
            icon.enabled = true;
            if (label) label.text = def.displayName;
        }
        else
        {
            icon.enabled = false;
            if (label) label.text = _slot.ToString();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Left click → unequip
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _ctrl?.TryUnequip(_slot);
        }
    }
}
