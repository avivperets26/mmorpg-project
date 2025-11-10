// Assets/Scripts/Equipment/EquipmentSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Items;

[DisallowMultipleComponent]
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Wiring")]
    [SerializeField] private Image frame;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private RawImage previewRaw;
    [SerializeField] private InventoryDragController dragCtrl;

    // NEW: the hover script on the Preview object (or whatever child handles equipped hover)
    [SerializeField] private EquipmentSlotTooltip equippedTooltip;   // <—

    private EquipmentSlot _slot;
    public EquipmentSlot Slot => _slot;

    private EquipmentController _ctrl;
    private ItemDefinition _current;
    private bool _hiddenForDrag = false;

    public void Init(EquipmentSlot slot, EquipmentController controller)
    {
        _slot = slot;
        _ctrl = controller;

        if (!previewRaw)
        {
            var riGo = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
            var rt = riGo.GetComponent<RectTransform>();
            riGo.transform.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            previewRaw = riGo.GetComponent<RawImage>();
            previewRaw.raycastTarget = true;
            previewRaw.texture = null;
            previewRaw.color = new Color(1, 1, 1, 0);
        }

        // Auto-find the equipped tooltip on the Preview child if not wired
        if (!equippedTooltip)
            equippedTooltip = previewRaw.GetComponent<EquipmentSlotTooltip>();
    }

    public void SetPlaceholder(string text)
    {
        if (label) label.text = text;
        if (icon) icon.enabled = false;
        if (previewRaw) { previewRaw.texture = null; previewRaw.color = new Color(1, 1, 1, 0); }
        _current = null;

        // clear equipped hover item
        if (equippedTooltip) equippedTooltip.SetItem(null);   // <—
    }

    public void ShowItem(ItemDefinition def)
    {
        _current = def;

        if (label) label.text = def ? def.displayName : _slot.ToString();
        if (icon) icon.enabled = false;

        if (!previewRaw) return;

        if (def == null)
        {
            previewRaw.texture = null;
            previewRaw.color = new Color(1, 1, 1, 0);
            if (equippedTooltip) equippedTooltip.SetItem(null);    // <—
#if UNITY_EDITOR
            Debug.Log($"[SlotUI] ShowItem slot={_slot} → label='{label?.text}', RT=(0x0)");
#endif
            return;
        }

        var rect = (transform as RectTransform)?.rect ?? new Rect(0, 0, 192, 192);
        int rtW = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(96f, rect.width)), 96, 1024);
        int rtH = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(96f, rect.height)), 96, 1024);

        var rt = ItemPreviewRenderer.Instance.Render(def, rtW, rtH);
        previewRaw.texture = rt;
        previewRaw.color = Color.white;

        // keep hover script in sync with the actual equipped item
        if (equippedTooltip) equippedTooltip.SetItem(ItemInstance.FromDefinition(def));  // <—

        previewRaw.enabled = !_hiddenForDrag;

#if UNITY_EDITOR
        Debug.Log($"[SlotUI] ShowItem slot={_slot} → label='{label?.text}', RT=({rtW}x{rtH})");
#endif
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        // suppress spurious clicks right after a drop or during grid drag
        if (InventoryDragController.LastEquipDropFrame == Time.frameCount) return;
        if (InventoryDragController.IsDragging) return;

        // Right–click = quick unequip (to inventory), keep this handy
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _ctrl?.TryUnequip(_slot);
            return;
        }

        // Left–click = begin drag FROM equipment if there is an item
        if (eventData.button == PointerEventData.InputButton.Left && _current != null && dragCtrl != null)
        {
            dragCtrl.BeginDragFromEquipment(_slot, _current, previewRaw.texture as RenderTexture);
        }
    }

    // Public helper so other scripts can hide the slot's preview while dragging
    public void SetPreviewVisible(bool visible)
    {
        _hiddenForDrag = !visible;
        if (previewRaw) previewRaw.enabled = visible;
    }


}
