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
    [SerializeField] private Image frame;            // optional decorative frame
    [SerializeField] private TMP_Text label;         // fallback text (Helm, RightHand, ...)
    [SerializeField] private Image icon;             // optional 2D sprite (rarely used now)
    [SerializeField] private RawImage previewRaw;    // NEW: 3D preview (fill the slot rect)
    [SerializeField] private InventoryDragController dragCtrl; // NEW: to start drags from equipment

    private EquipmentSlot _slot;
    public EquipmentSlot Slot => _slot;

    private EquipmentController _ctrl;
    private ItemDefinition _current;                 // cache what we’re showing
    private bool _hiddenForDrag = false; // <— NEW

    public void Init(EquipmentSlot slot, EquipmentController controller)
    {
        _slot = slot;
        _ctrl = controller;

        // Safety: create a RawImage if not wired
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
            previewRaw.raycastTarget = true; // needs to receive clicks
            previewRaw.texture = null;
            previewRaw.color = new Color(1, 1, 1, 0);   // hidden when empty
        }
    }

    public void SetPlaceholder(string text)
    {
        if (label) label.text = text;
        if (icon) icon.enabled = false;
        if (previewRaw) { previewRaw.texture = null; previewRaw.color = new Color(1, 1, 1, 0); }
        _current = null;
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
            // keep enabled as-is (empty state is fine)
            return;
        }

        var rect = (transform as RectTransform)?.rect ?? new Rect(0, 0, 192, 192);
        int rtW = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(96f, rect.width)), 96, 1024);
        int rtH = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(96f, rect.height)), 96, 1024);

        var rt = ItemPreviewRenderer.Instance.Render(def, rtW, rtH);
        previewRaw.texture = rt;
        previewRaw.color = Color.white;

        // ✅ Ensure visible in case it was hidden during drag
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
