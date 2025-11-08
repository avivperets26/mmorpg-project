// Assets/Scripts/Inventory/InventoryDragController.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.Items;

public class InventoryDragController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;           // auto-found if null
    [SerializeField] private InventoryUI inventoryUI;             // to call Refresh() after place
    [SerializeField] private RectTransform gridRoot;              // same gridRoot used by InventoryUI
    [SerializeField] private Canvas canvas;                       // root canvas (for ScreenPoint->LocalPoint)
    [SerializeField] private EquipmentController equipmentController; // auto-filled if missing
    [SerializeField] private Canvas dragCanvas;  // assign DragCanvas in Inspector


    [Header("Drag Visuals")]
    [Tooltip("Scale multiplier while dragging.")]
    [SerializeField] private float dragScale = 1.15f;
    [Tooltip("Footprint color when valid.")]
    [SerializeField] private Color fitColor = new Color(0, 1, 0, 0.25f);
    [Tooltip("Footprint color when invalid.")]
    [SerializeField] private Color badColor = new Color(1, 0, 0, 0.25f);
    [Tooltip("Optional outline image for the footprint.")]
    [SerializeField] private Sprite footprintSprite;

    private GridLayoutGroup _grid;
    private bool _dragging;

    // IMPORTANT: after BeginDrag(), DO NOT rely on _pickedView (its GO is rebuilt by Refresh()).
    private InventoryItemView _pickedView;    // only used to start drag
    private InventoryItem _pickedItem;        // survives Refresh()
    private ItemDefinition _pickedDef;        // survives Refresh()
    private int _origX, _origY;

    // Ghost image that follows the cursor
    private RectTransform _ghostRect;
    private RawImage _ghostRaw;

    // Footprint overlay under the cursor
    private RectTransform _footprintRect;
    private Image _footprintImg;

    // Hover feedback over equipment slots
    private EquipmentSlotUI _hoverSlotUI;
    private Image _hoverSlotImage;
    private Color _hoverSlotOriginal;
    private bool _hoverCanEquip;

    // Debounce: ignore mouse release until after this frame
    private int _suppressReleaseUntilFrame = -1;

    public static int LastEquipDropFrame = -100000;
    public static bool IsDragging { get; private set; } = false;
    private EquipmentSlot? _equipSourceSlot = null;


    private void Awake()
    {
        if (!inventory)
        {
#if UNITY_2023_1_OR_NEWER
            inventory = Object.FindFirstObjectByType<PlayerInventory>();
#else
            inventory = Object.FindObjectOfType<PlayerInventory>();
#endif
            if (!inventory) Debug.LogWarning("[Drag] Awake: PlayerInventory not found via auto-find.");
        }

        if (!inventoryUI) inventoryUI = GetComponent<InventoryUI>();

        if (!gridRoot && inventoryUI != null)
        {
            // reflect InventoryUI.gridRoot so we don't need duplicate wiring
            var f = typeof(InventoryUI).GetField("gridRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            gridRoot = (RectTransform)f?.GetValue(inventoryUI);
            if (!gridRoot) Debug.LogWarning("[Drag] Awake: gridRoot not wired and not found on InventoryUI.");
        }

        if (!canvas) canvas = GetComponentInParent<Canvas>();

        if (!equipmentController && inventoryUI != null)
        {
            var ef = typeof(InventoryUI).GetField("equipmentController", BindingFlags.NonPublic | BindingFlags.Instance);
            equipmentController = (EquipmentController)ef?.GetValue(inventoryUI);
            if (!equipmentController) Debug.LogWarning("[Drag] Awake: equipmentController not wired and not found on InventoryUI.");
        }

        _grid = gridRoot ? gridRoot.GetComponent<GridLayoutGroup>() : null;
        if (!_grid) Debug.LogError("[InventoryDragController] gridRoot must have GridLayoutGroup.");
        if (!canvas) Debug.LogError("[InventoryDragController] Please assign the root Canvas.");
    }

    private void Update()
    {
        if (!_dragging) return;

        // Cancel with Esc or Right-Click
        if (EscapeDown() || RightClickDown())
        {
            Debug.Log("[Drag] Cancel requested (Esc/RightClick).");
            CancelDrag();
            return;
        }

        var mouse = MousePos();

        // Move ghost to cursor (canvas space)
        if (_ghostRect && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform, mouse, canvas.worldCamera, out var localCanvas))
        {
            const float dragYOffset = -40f; // tweak to taste
            _ghostRect.anchoredPosition = localCanvas + new Vector2(0f, dragYOffset);
        }

        // Candidate cell (grid space)
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRoot, mouse, canvas.worldCamera, out var localGrid))
            return;

        var (cellX, cellY) = LocalToCell(localGrid);
        ClampTopLeftForItem(ref cellX, ref cellY, _pickedItem);

        // Update footprint overlay and cell dimming (footprint rect itself hidden)
        var fit = PreviewFootprintAt(cellX, cellY, _pickedItem);
        if (_footprintImg) _footprintImg.enabled = false;

        inventoryUI.ClearHighlights();
        Color dim = fit ? new Color(0f, 0f, 0f, 0.5f) : new Color(0.5f, 0f, 0f, 0.5f);
        inventoryUI.HighlightCells(cellX, cellY, _pickedItem.Width, _pickedItem.Height, dim);

        // Equipment hover (turns slot green/red while hovering)
        UpdateEquipmentHover(_pickedDef);

        if (_hoverSlotUI)
        {
            // Hide the grid footprint / highlights while hovering equipment
            if (_footprintRect && _footprintRect.gameObject.activeSelf)
                _footprintRect.gameObject.SetActive(false);

            inventoryUI.ClearHighlights();
        }
        else
        {
            // Ensure the footprint can show again when we are back over the grid
            if (_footprintRect && !_footprintRect.gameObject.activeSelf)
                _footprintRect.gameObject.SetActive(true);
        }

        // Place/equip on left-click release (after debounce)
        if (LeftClickReleased() && Time.frameCount >= _suppressReleaseUntilFrame)
        {
            Debug.Log("[Drag] Left click released while dragging (passed debounce).");

            // 1) Try to drop onto an equipment slot under the cursor
            if (TryEquipViaDropAtCursor())
            {
                inventoryUI.ClearHighlights();
                EndDrag(commit: true);
                inventoryUI.Refresh();
                return;
            }
            Debug.Log("[Drag] Equip failed (returned false), falling back to TryPlace on grid.");

            // 2) Otherwise, try to place back into the grid
            TryPlace(cellX, cellY);
        }
    }

    public void OnItemClicked(InventoryItemView view)
    {
        if (!_dragging) BeginDrag(view);
        else Debug.Log("[Drag] OnItemClicked ignored: already dragging.");
    }

    private void OnDisable()
    {
        if (_dragging)
        {
            Debug.Log("[Drag] OnDisable while dragging → CancelDrag()");
            CancelDrag();
            return;
        }

        // Clean stragglers
        if (_footprintRect) Destroy(_footprintRect.gameObject);
        _footprintRect = null; _footprintImg = null;
        if (_ghostRect) Destroy(_ghostRect.gameObject);
        _ghostRect = null; _ghostRaw = null;
    }

    private void BeginDrag(InventoryItemView view)
    {
        if (view == null || view.item == null) return;

        Debug.Log($"[Drag] BeginDrag → {view.item.def?.displayName ?? "NULL"} @ ({view.item.x},{view.item.y})");

        // Clear any stale visuals
        if (_footprintRect) { Destroy(_footprintRect.gameObject); _footprintRect = null; _footprintImg = null; }
        if (_ghostRect) { Destroy(_ghostRect.gameObject); _ghostRect = null; _ghostRaw = null; }

        _pickedView = view;               // transient
        _pickedItem = view.item;          // persist
        _pickedDef = view.item.def;      // persist
        inventoryUI.dragHiddenItem = _pickedItem;

        _origX = _pickedItem.x;
        _origY = _pickedItem.y;

        if (inventory?.Data != null)
        {
            inventory.Data.Remove(_pickedItem); // void
            Debug.Log("[Drag] BeginDrag: freed old footprint (Remove called).");
        }
        else
        {
            Debug.LogWarning("[Drag] BeginDrag: inventory or inventory.Data is NULL.");
        }

        // Prevent immediate drop on the same frame we start the drag
        _suppressReleaseUntilFrame = Time.frameCount + 1;
        Debug.Log($"[Drag] Debounce set: ignore releases until frame >= {_suppressReleaseUntilFrame}");

        // Build the ghost under the Canvas so it freely follows the cursor
        var tex = view.previewTexture != null ? view.previewTexture : view.raw.texture;
        _ghostRect = CreateGhost(tex, view.raw.rectTransform.rect.size);
        _ghostRect.localScale = Vector3.one * dragScale;

        // Build footprint overlay under gridRoot
        _footprintRect = CreateFootprint();
        _footprintImg = _footprintRect.GetComponent<Image>();
        if (_footprintImg) _footprintImg.enabled = false;

        _dragging = true;
        IsDragging = true;

        // Re-render UI without this item occupying cells (this destroys view's GO)
        inventoryUI.Refresh();
    }

    private void TryPlace(int cellX, int cellY)
    {
        _pickedItem.x = cellX;
        _pickedItem.y = cellY;
        inventoryUI.dragHiddenItem = _pickedItem;

        bool placed = inventory.Data.Place(_pickedItem);
        Debug.Log($"[Drag] TryPlace @ ({cellX},{cellY}) => {placed}");

        if (placed)
        {
            inventoryUI.ClearHighlights();
            EndDrag(commit: true);
        }
        else
        {
            Debug.Log("[Drag] TryPlace failed (spot invalid/blocked) — continue dragging.");
        }

        inventoryUI.Refresh();
    }

    private void CancelDrag()
    {
        Debug.Log("[Drag] CancelDrag → returning item to original coords.");
        _pickedItem.x = _origX;
        _pickedItem.y = _origY;
        inventory.Data.Place(_pickedItem);
        inventoryUI.ClearHighlights();
        EndDrag(commit: false);
    }

    private void EndDrag(bool commit)
    {
        Debug.Log($"[Drag] EndDrag commit={commit}");

        _dragging = false;
        IsDragging = false;

        if (_ghostRect) Destroy(_ghostRect.gameObject);
        if (_footprintRect) Destroy(_footprintRect.gameObject);
        if (_hoverSlotImage) _hoverSlotImage.color = _hoverSlotOriginal;

        _hoverSlotUI = null;
        _hoverSlotImage = null;
        _hoverCanEquip = false;

        _ghostRect = null;
        _ghostRaw = null;
        _footprintRect = null;
        _footprintImg = null;

        // ✅ Restore BEFORE clearing _equipSourceSlot
        var sourceSlot = _equipSourceSlot;
        if (sourceSlot.HasValue && equipmentController != null)
        {
            var ui = equipmentController.GetSlotUI(sourceSlot.Value);
            if (ui != null) ui.SetPreviewVisible(true);
        }
        _equipSourceSlot = null;

        _pickedView = null;
        _pickedDef = null;
        _pickedItem = null;
        inventoryUI.dragHiddenItem = null;

        inventoryUI.Refresh();
    }

    // ---------- Drop-to-equip ----------
    // Replace TryEquipViaDropAtCursor() with the version below (or edit the body accordingly)
    private bool TryEquipViaDropAtCursor()
    {
        if (equipmentController == null || _pickedDef == null) return false;

        bool equipped = false;

        if (_hoverSlotUI)
        {
            if (_hoverCanEquip)
            {
                // If dragging FROM another equipment slot, move instead of duplicating
                if (_equipSourceSlot.HasValue)
                {
                    equipped = equipmentController.MoveEquip(_equipSourceSlot.Value, _hoverSlotUI.Slot);
                }
                else
                {
                    equipped = equipmentController.TryEquipInto(_hoverSlotUI.Slot, _pickedDef, fromInventory: true);
                }
            }
            else
            {
                equipmentController.PreviewCanEquip(_pickedDef, _hoverSlotUI.Slot, out var reason);
                Debug.Log($"[Drag] Cannot equip into slot={_hoverSlotUI.Slot}: {reason}");
            }
        }
        else
        {
            // Drop on grid: either equip (auto) when dragging from grid,
            // or UNEQUIP to inventory when dragging from equipment.
            if (_equipSourceSlot.HasValue)
            {
                // Unequip from slot → add to inventory
                equipped = equipmentController.TryUnequip(_equipSourceSlot.Value);
            }
            else
            {
                equipped = equipmentController.TryEquip(_pickedDef, fromInventory: true);
            }
        }

        if (equipped)
        {
            // If we dragged from GRID → remove that instance (already done in BeginDrag)
            if (!_equipSourceSlot.HasValue && inventory != null && _pickedItem != null)
            {
                inventory.Remove(_pickedItem);
                Debug.Log($"[Drag] Post-equip: removed INVENTORY INSTANCE of dragged item (frame={Time.frameCount}).");
            }

            LastEquipDropFrame = Time.frameCount;
            inventoryUI.ClearHighlights();
            equipmentController.RefreshUI();

            EndDrag(commit: true);
            inventoryUI.Refresh();
            _equipSourceSlot = null;
            return true;
        }

        return false;
    }


    // -------- Helpers --------
    private RectTransform CreateGhost(Texture tex, Vector2 size)
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rt = go.GetComponent<RectTransform>();
        go.transform.SetParent((dragCanvas ? dragCanvas.transform : canvas.transform), false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        _ghostRaw = go.GetComponent<RawImage>();
        _ghostRaw.texture = tex;
        _ghostRaw.raycastTarget = false;
        _ghostRaw.color = new Color(1f, 1f, 1f, 0.9f);
        return rt;
    }
    private RectTransform CreateFootprint()
    {
        var go = new GameObject("FootprintPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        go.transform.SetParent(gridRoot, false);

        var le = go.AddComponent<LayoutElement>(); // keep it out of GridLayoutGroup
        le.ignoreLayout = true;

        rt.anchorMin = Vector2.up;   // (0,1) top-left
        rt.anchorMax = Vector2.up;
        rt.pivot = new Vector2(0f, 1f);

        var img = go.GetComponent<Image>();
        img.type = Image.Type.Sliced;
        img.sprite = footprintSprite;
        img.raycastTarget = false;
        img.enabled = false;
        go.SetActive(false);

        return rt;
    }

    private bool PreviewFootprintAt(int cellX, int cellY, InventoryItem it)
    {
        var cs = _grid.cellSize;
        var sp = _grid.spacing;
        var pad = _grid.padding;

        float pitchX = cs.x + sp.x;
        float pitchY = cs.y + sp.y;

        // Top-left pixel within the grid for this cell (include padding)
        float px = pad.left + cellX * pitchX;
        float py = pad.top + cellY * pitchY;

        // Size in pixels across footprint
        float wPx = it.Width * cs.x + (it.Width - 1) * sp.x;
        float hPx = it.Height * cs.y + (it.Height - 1) * sp.y;

        // Position & size footprint rect (anchored to grid top-left)
        if (_footprintRect)
        {
            _footprintRect.anchoredPosition = new Vector2(px, -py); // anchorMin/Max = (0,1), pivot=(0,1)
            _footprintRect.sizeDelta = new Vector2(wPx, hPx);

            if (!_footprintRect.gameObject.activeSelf)
                _footprintRect.gameObject.SetActive(true);
        }

        // Test validity using InventoryData.CanPlace with the candidate coords
        int prevX = it.x; int prevY = it.y;
        it.x = cellX; it.y = cellY;
        bool fit = inventory.Data.CanPlace(it);
        it.x = prevX; it.y = prevY;

        return fit;
    }

    private (int x, int y) LocalToCell(Vector2 localInGrid)
    {
        var rect = gridRoot.rect;
        var pivot = gridRoot.pivot;

        float left = -rect.width * pivot.x;
        float top = rect.height * (1f - pivot.y);

        Vector2 tl = new Vector2(left, top);     // grid rect top-left in local space
        Vector2 fromTL = localInGrid - tl;           // vector from grid rect top-left

        var cs = _grid.cellSize;
        var sp = _grid.spacing;
        var pad = _grid.padding;

        float pitchX = cs.x + sp.x;
        float pitchY = cs.y + sp.y;

        // Remove padding so (0,0) corresponds to the first inner cell
        float adjX = fromTL.x - pad.left;       // rightwards positive
        float adjY = -fromTL.y - pad.top;       // downwards positive (note the minus)

        int cx = Mathf.FloorToInt(adjX / pitchX);
        int cy = Mathf.FloorToInt(adjY / pitchY);

        return (cx, cy);
    }

    private void ClampTopLeftForItem(ref int cx, ref int cy, InventoryItem it)
    {
        cx = Mathf.Clamp(cx, 0, inventory.Data.width - it.Width);
        cy = Mathf.Clamp(cy, 0, inventory.Data.height - it.Height);
    }

    // ---------- Hover feedback over equipment slots ----------
    private void UpdateEquipmentHover(ItemDefinition def)
    {
        // clear previous highlight
        if (_hoverSlotImage)
        {
            _hoverSlotImage.color = _hoverSlotOriginal;
            _hoverSlotUI = null;
            _hoverSlotImage = null;
            _hoverCanEquip = false;
        }

        var es = EventSystem.current;
        if (es == null || def == null) return;

        var ped = new PointerEventData(es) { position = MousePos() };
        var hits = new List<RaycastResult>();
        es.RaycastAll(ped, hits);

        for (int i = 0; i < hits.Count; i++)
        {
            var slotUI = hits[i].gameObject.GetComponentInParent<EquipmentSlotUI>();
            if (!slotUI) continue;

            var img = slotUI.GetComponent<Image>();
            if (!img) break;

            _hoverSlotUI = slotUI;
            _hoverSlotImage = img;
            _hoverSlotOriginal = img.color;

            string reason;
            _hoverCanEquip = equipmentController.PreviewCanEquip(def, slotUI.Slot, out reason);
            img.color = _hoverCanEquip
                ? new Color(0f, 1f, 0f, 0.35f)   // green
                : new Color(1f, 0f, 0f, 0.35f);  // red

            Debug.Log($"[Drag] Hover slot={slotUI.Slot}, canEquip={_hoverCanEquip}, reason='{reason}'");
            break; // first hit is enough
        }
    }

    public void BeginDragFromEquipment(EquipmentSlot sourceSlot, ItemDefinition def, RenderTexture optionalPreview = null)
    {
        if (_dragging) return;

        _equipSourceSlot = sourceSlot;
        _pickedDef = def;

        // Build a temporary InventoryItem so grid footprint preview works on grid drops
        _pickedItem = new InventoryItem { def = def, x = 0, y = 0, rotated = false };
        inventoryUI.dragHiddenItem = null;

        Debug.Log($"[Drag] BeginDragFromEquipment → {_pickedDef.displayName} from {sourceSlot}");

        // Hide slot preview while dragging (avoid duplicate)
        if (equipmentController != null)
        {
            var ui = equipmentController.GetSlotUI(sourceSlot);
            if (ui != null) ui.SetPreviewVisible(false);
        }

        // 🔧 Always render a crisp, dedicated ghost RT (ignore slot’s tiny RT)
        const int ghostRT = 768; // 512–1024 is fine; 768 balances quality/VRAM
        var tex = ItemPreviewRenderer.Instance.Render(def, ghostRT, ghostRT);
        if (tex) ((RenderTexture)tex).filterMode = FilterMode.Bilinear;

        // The on-screen ghost can be ~256px; the 768 RT keeps it sharp
        _ghostRect = CreateGhost(tex, new Vector2(256, 256));
        _ghostRect.localScale = Vector3.one * dragScale;

        // Footprint overlay (for grid drops)
        _footprintRect = CreateFootprint();
        _footprintImg = _footprintRect.GetComponent<Image>();
        if (_footprintImg) _footprintImg.enabled = false;

        _suppressReleaseUntilFrame = Time.frameCount + 1;
        _dragging = true;
        IsDragging = true;
    }


    // --- Input System helpers ---
    private static Vector2 MousePos() =>
        Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : Vector2.zero;

    private static bool LeftClickReleased() =>
        Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;

    private static bool RightClickDown() =>
        Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

    private static bool EscapeDown() =>
        Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

    // Forward access to the EquipmentController
    public EquipmentSlotUI GetSlotUI(EquipmentSlot slot) => equipmentController?.GetSlotUI(slot);

}
