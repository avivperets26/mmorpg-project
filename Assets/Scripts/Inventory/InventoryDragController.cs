// Assets/Scripts/Inventory/InventoryDragController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class InventoryDragController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;   // auto-found if null
    [SerializeField] private InventoryUI inventoryUI;     // to call Refresh() after place
    [SerializeField] private RectTransform gridRoot;      // same gridRoot used by InventoryUI
    [SerializeField] private Canvas canvas;               // root canvas (for ScreenPoint->LocalPoint)

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
    private InventoryItemView _pickedView;
    private InventoryItem _pickedItem;
    private int _origX, _origY;

    // Ghost image that follows the cursor
    private RectTransform _ghostRect;
    private RawImage _ghostRaw;

    // Footprint overlay under the cursor
    private RectTransform _footprintRect;
    private Image _footprintImg;

    private void Awake()
    {
        if (!inventory)
        {
#if UNITY_2023_1_OR_NEWER
            inventory = Object.FindFirstObjectByType<PlayerInventory>();
#else
            inventory = Object.FindObjectOfType<PlayerInventory>();
#endif
        }

        if (!inventoryUI)
            inventoryUI = GetComponent<InventoryUI>();

        if (!gridRoot && inventoryUI != null)
        {
            var f = typeof(InventoryUI).GetField("gridRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            gridRoot = (RectTransform)f?.GetValue(inventoryUI);
        }

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

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
            CancelDrag();
            return;
        }

        var mouse = MousePos();

        // Move ghost to cursor (canvas space)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
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

        // Update footprint overlay
        var fit = PreviewFootprintAt(cellX, cellY, _pickedItem);
        _footprintImg.color = fit ? fitColor : badColor;

        // Dim the grid cells underneath to visualize occupied area
        inventoryUI.ClearHighlights();
        Color dim = fit ? new Color(0f, 0f, 0f, 0.5f) : new Color(0.5f, 0f, 0f, 0.5f);
        inventoryUI.HighlightCells(cellX, cellY, _pickedItem.Width, _pickedItem.Height, dim);

        // Place on left-click
        if (LeftClickDown())
        {
            TryPlace(cellX, cellY);
        }
    }


    public void OnItemClicked(InventoryItemView view)
    {
        if (!_dragging)
        {
            BeginDrag(view);
        }
        else
        {
            // If already dragging something, clicking another item does nothing (or could swap).
            // Keep it simple for now.
        }
    }

    private void BeginDrag(InventoryItemView view)
    {
        if (view == null || view.item == null) return;
        _pickedView = view;
        _pickedItem = view.item;
        inventoryUI.dragHiddenItem = _pickedItem;

        _origX = _pickedItem.x;
        _origY = _pickedItem.y;

        // Free the cells while dragging so we can preview placement correctly
        inventory.Data.Remove(_pickedItem);

        // Build the ghost under the Canvas so it freely follows the cursor
        var tex = view.previewTexture != null ? view.previewTexture : view.raw.texture;
        _ghostRect = CreateGhost(tex, view.raw.rectTransform.rect.size);
        _ghostRect.localScale = Vector3.one * dragScale;

        // Build footprint overlay under gridRoot
        _footprintRect = CreateFootprint();
        _footprintImg = _footprintRect.GetComponent<Image>();

        _dragging = true;

        // Re-render UI without this item occupying cells (dims disappear etc.)
        inventoryUI.Refresh();

    }

    private void TryPlace(int cellX, int cellY)
    {
        _pickedItem.x = cellX;
        _pickedItem.y = cellY;
        inventoryUI.dragHiddenItem = _pickedItem;


        if (inventory.Data.Place(_pickedItem))
        {
            inventoryUI.ClearHighlights();
            EndDrag(commit: true);
        }
        else
        {
            // Thump feedback (brief red flash already shown via overlay)
            // keep dragging
        }
        inventoryUI.Refresh();

    }

    private void CancelDrag()
    {
        // return item to original coords
        _pickedItem.x = _origX;
        _pickedItem.y = _origY;
        inventory.Data.Place(_pickedItem);
        inventoryUI.ClearHighlights();
        EndDrag(commit: false);
    }

    private void EndDrag(bool commit)
    {
        _dragging = false;

        if (_ghostRect) Destroy(_ghostRect.gameObject);
        if (_footprintRect) Destroy(_footprintRect.gameObject);

        _ghostRect = null;
        _ghostRaw = null;
        _footprintRect = null;
        _footprintImg = null;

        _pickedItem = null;
        _pickedView = null;
        inventoryUI.dragHiddenItem = null;

        // Force UI refresh to rebuild proper views at new/old location
        inventoryUI.Refresh();
    }

    // -------- Helpers --------

    private RectTransform CreateGhost(Texture tex, Vector2 size)
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        var rt = go.GetComponent<RectTransform>();
        go.transform.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        _ghostRaw = go.GetComponent<RawImage>();
        _ghostRaw.texture = tex;
        _ghostRaw.raycastTarget = false; // don't block clicks
        _ghostRaw.color = new Color(1f, 1f, 1f, 0.9f);

        return rt;
    }

    private RectTransform CreateFootprint()
    {
        var go = new GameObject("FootprintPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        go.transform.SetParent(gridRoot, false);

        rt.anchorMin = Vector2.up;   // top-left (0,1)
        rt.anchorMax = Vector2.up;
        rt.pivot = new Vector2(0f, 1f);

        var img = go.GetComponent<Image>();
        img.type = Image.Type.Sliced;
        img.sprite = footprintSprite; // optional
        img.color = fitColor;

        return rt;
    }

    private bool PreviewFootprintAt(int cellX, int cellY, InventoryItem it)
    {
        var cs = _grid.cellSize;
        var sp = _grid.spacing;

        float pitchX = cs.x + sp.x;
        float pitchY = cs.y + sp.y;

        // Top-left pixel within the grid for this cell
        float px = cellX * pitchX;
        float py = cellY * pitchY;

        // Size in pixels across footprint
        float wPx = it.Width * cs.x + (it.Width - 1) * sp.x;
        float hPx = it.Height * cs.y + (it.Height - 1) * sp.y;

        // Position & size footprint rect (anchored to grid top-left)
        _footprintRect.anchoredPosition = new Vector2(px, -py);
        _footprintRect.sizeDelta = new Vector2(wPx, hPx);

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

        // Convert local (pivoted) coords to "top-left origin" space
        float left = -rect.width * pivot.x;
        float top = rect.height * (1f - pivot.y);

        Vector2 tl = new Vector2(left, top);
        Vector2 fromTL = localInGrid - tl;

        var cs = _grid.cellSize;
        var sp = _grid.spacing;
        float pitchX = cs.x + sp.x;
        float pitchY = cs.y + sp.y;

        int cx = Mathf.FloorToInt(fromTL.x / pitchX);
        int cy = Mathf.FloorToInt(-fromTL.y / pitchY); // Y grows downward

        return (cx, cy);
    }

    private void ClampTopLeftForItem(ref int cx, ref int cy, InventoryItem it)
    {
        cx = Mathf.Clamp(cx, 0, inventory.Data.width - it.Width);
        cy = Mathf.Clamp(cy, 0, inventory.Data.height - it.Height);
    }
    // --- Input System helpers ---
    private static Vector2 MousePos() =>
        Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : Vector2.zero;

    private static bool LeftClickDown() =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

    private static bool RightClickDown() =>
        Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

    private static bool EscapeDown() =>
        Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

}
