// Assets\Scripts\UI\Widgets\Tooltips\EquippedSlotTooltipBinder.cs
using UnityEngine;
using UnityEngine.UI;            // <- for Graphic/Image
using Game.Items;

[RequireComponent(typeof(RectTransform))]
public class EquippedSlotTooltipBinder : MonoBehaviour
{
    [Header("Wiring")]
    public EquipmentController equipment;   // auto-found if null
    public EquipmentSlot slot;              // set per-slot in Inspector

    private InventorySlotTooltip _tip;

    void Awake()
    {
        if (!equipment) equipment = GetComponentInParent<EquipmentController>();

        // Ensure there is a raycastable Graphic on THIS object.
        // (Pointer events won't hit a bare RectTransform.)
        var g = GetComponent<Graphic>();
        if (!g)
        {
            // Add a transparent Image that only exists to receive raycasts.
            var img = gameObject.AddComponent<Image>();
            img.sprite = null;
            img.color = new Color(1, 1, 1, 0);  // fully transparent
            img.raycastTarget = true;
            g = img;
        }
        else
        {
            g.raycastTarget = true;
        }

        // Make sure any decorative children (frames, icons) don’t steal the raycast
        // unless you explicitly want them to.
        foreach (var childG in GetComponentsInChildren<Graphic>(includeInactive: true))
        {
            if (childG.gameObject != gameObject)
            {
                // Most frames/icons should NOT block hover.
                // If you have a specific clickable child, set it back to true in its own script.
                childG.raycastTarget = false;
            }
        }

        // Ensure we use the same tooltip component as inventory items
        _tip = GetComponent<InventorySlotTooltip>();
        if (!_tip) _tip = gameObject.AddComponent<InventorySlotTooltip>();

        // Anchor beside THIS slot rect
        _tip.targetOverride = (RectTransform)transform;
    }

    void OnEnable()
    {
        Subscribe(true);
        Refresh(); // populate itemInstance so tooltip can show immediately
    }

    void OnDisable()
    {
        Subscribe(false);
        if (_tip) _tip.itemInstance = null;
    }

    private void Subscribe(bool on)
    {
        if (!equipment) return;
        if (on) equipment.EquippedChanged += OnEquippedChanged;
        else equipment.EquippedChanged -= OnEquippedChanged;
    }

    private void OnEquippedChanged() => Refresh();

    public void Refresh()
    {
        if (!_tip || equipment == null) return;

        var inst = equipment.GetEquippedInstance(slot);
        if (inst != null) { _tip.itemInstance = inst; return; }

        var def = equipment.GetEquippedDefinition(slot);
        _tip.itemInstance = def ? new ItemInstance(def, def.defaultTier) : null;
    }
}
