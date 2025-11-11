// Assets/Scripts/Inventory/InventoryUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // for Mouse position (safe to keep)

/// <summary>
/// Pure inventory view/controller:
/// - Builds the grid once the PlayerInventory.Data exists
/// - Renders one preview per placed item
/// - Maintains hover spin, drag view and tooltip hookup
/// - Replays tooltip hover on open/refresh so it feels seamless
///
/// NOTE: Input blocking / action map switching / cursor are owned by UIBlocker
/// attached on the InventoryPanel root. This class just opens/closes the panel
/// and handles visuals.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel; // InventoryPanel root (with UIBlocker on it)

    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;     // Player gameplay object
    [SerializeField] private RectTransform gridRoot;        // InventoryPanel/GridRoot (GridLayoutGroup)
    [SerializeField] private GameObject slotPrefab;         // Slot prefab (must have RawImage)

    [Header("Preview")]
    [SerializeField] private Texture2D emptyTexture;        // Optional bg for empty cells
    [SerializeField] private int previewSize = 256;         // Base RT size (scaled by item footprint)
    [SerializeField] private InventoryDragController dragController; // Optional drag controller
    [HideInInspector] public InventoryItem dragHiddenItem;  // Temporarily hidden while dragging
    [SerializeField] private EquipmentController equipmentController;

    [Header("Requirement Visuals")]
    [SerializeField] private Sprite requirementFailIcon;
    [SerializeField] private Color requirementFailTint = new Color(1f, 0f, 0f, 0.5f);

    // grid/cache
    private int _cols, _rows;
    private RawImage[,] _cells;
    private RectTransform[,] _cellRects;
    private GridLayoutGroup _grid;
    private bool _built;

    // active item view containers (overlays)
    private readonly List<GameObject> _itemViews = new();

    private bool IsOpen => inventoryPanel && inventoryPanel.activeSelf;

    private void Awake()
    {
        if (!dragController) dragController = GetComponent<InventoryDragController>();
    }

    private void Start()
    {
        // Build grid once inventory + data are ready
        StartCoroutine(InitWhenReady());
    }

    private System.Collections.IEnumerator InitWhenReady()
    {
        // Wait until we have an inventory component
        while (inventory == null) yield return null;

        // Wait until PlayerInventory created its Data (done in its Awake)
        while (inventory.Data == null) yield return null;

        _cols = inventory.Data.width;
        _rows = inventory.Data.height;

        _grid = gridRoot ? gridRoot.GetComponent<GridLayoutGroup>() : null;
        if (_grid == null)
        {
            Debug.LogError("[InventoryUI] GridRoot must have a GridLayoutGroup.");
            yield break;
        }

        BuildGrid();
        _built = true;

        // subscribe & draw
        inventory.Changed += Refresh;
        Refresh();
    }

    // ------- Public API (open/close handled visually; UIBlocker does the rest) -------
    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        if (inventoryPanel) inventoryPanel.SetActive(true);

        var cg = inventoryPanel.GetComponent<CanvasGroup>();
        if (cg) { cg.blocksRaycasts = true; cg.interactable = true; }

        // Tooltip replay only (UIBlocker handles guard/input/cursor)
        UiCoroutineRunner.Run(OpenAfterDelay());
    }

    public void Close()
    {
        if (!IsOpen) return;
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    private System.Collections.IEnumerator OpenAfterDelay()
    {
        // 1 frame for SetActive, then end-of-frame for layout/TMP
        yield return null;
        yield return new WaitForEndOfFrame();

        // Now safely replay pointer enter for tooltip under cursor
        yield return ReplayPointerEnterUnderCursor();
    }

    private void OnEnable()
    {
        if (_built) Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.Changed -= Refresh;
        ClearItemViews();
    }

    // ------- Grid build / refresh -------
    private void BuildGrid()
    {
        // Clear previous slots
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        _cells = new RawImage[_cols, _rows];
        _cellRects = new RectTransform[_cols, _rows];

        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                var go = Instantiate(slotPrefab, gridRoot);
                go.name = $"Slot_{x}_{y}";

                var raw = go.GetComponent<RawImage>();
                if (!raw) raw = go.AddComponent<RawImage>();

                // EMPTY = transparent or provided texture
                if (emptyTexture != null)
                {
                    raw.texture = emptyTexture;
                    raw.color = Color.white;
                }
                else
                {
                    raw.texture = null;
                    raw.color = new Color(1f, 1f, 1f, 0f);
                }
                // Cells themselves are not interactive; item overlays are.
                raw.raycastTarget = false;

                _cells[x, y] = raw;
                _cellRects[x, y] = go.GetComponent<RectTransform>();
            }
        }
    }

    public void Refresh()
    {
        if (!_built || _cells == null || inventory == null) return;

        // 1) Clear previous overlays
        ClearItemViews();

        // 2) Reset all cells to their empty look
        for (int y = 0; y < _rows; y++)
        {
            for (int x = 0; x < _cols; x++)
            {
                var raw = _cells[x, y];
                if (emptyTexture)
                {
                    raw.texture = emptyTexture;
                    raw.color = Color.white;
                }
                else
                {
                    raw.texture = null;
                    raw.color = new Color(1f, 1f, 1f, 0f);
                }
                raw.uvRect = new Rect(0, 0, 1, 1);
            }
        }

        // 3) One preview per placed item
        foreach (var it in inventory.Items)
        {
            var def = it.def;
            if (!def) continue;
            if (dragHiddenItem == it) continue;

            int w = Mathf.Max(1, it.Width);
            int h = Mathf.Max(1, it.Height);

            // grid metrics (include padding)
            var cs = _grid.cellSize;
            var sp = _grid.spacing;
            var pad = _grid.padding;

            float pitchX = cs.x + sp.x;
            float pitchY = cs.y + sp.y;

            // top-left of the footprint in grid space (padding-aware)
            float px = pad.left + it.x * pitchX;
            float py = pad.top + it.y * pitchY;

            // pixel size of the footprint
            float spanW = w * cs.x + (w - 1) * sp.x;
            float spanH = h * cs.y + (h - 1) * sp.y;

            // --- Container over the footprint (top-left anchored) ---
            var container = new GameObject($"ItemView_{def.displayName}_Container", typeof(RectTransform));
            var contRect = container.GetComponent<RectTransform>();
            container.transform.SetParent(gridRoot, false);

            var contLayout = container.AddComponent<LayoutElement>();
            contLayout.ignoreLayout = true;

            contRect.anchorMin = Vector2.up;   // (0,1)
            contRect.anchorMax = Vector2.up;
            contRect.pivot = new Vector2(0f, 1f);
            contRect.sizeDelta = new Vector2(spanW, spanH);
            contRect.anchoredPosition = new Vector2(px, -py);
            contRect.localRotation = Quaternion.identity;

            // Ask renderer for an RT that matches the footprint aspect
            int rtW = Mathf.Max(64, Mathf.RoundToInt(previewSize * w));
            int rtH = Mathf.Max(64, Mathf.RoundToInt(previewSize * h));
            var rt = ItemPreviewRenderer.Instance.Render(def, rtW, rtH);
            if (rt == null || !rt.IsCreated())
            {
                Destroy(container);
                continue;
            }

            // --- Item image filling the container ---
            var imgGO = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(container.transform, false);

            var imgRect = imgGO.GetComponent<RectTransform>(); // <-- keep this
            var ivRaw = imgGO.GetComponent<RawImage>();

            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.pivot = new Vector2(0.5f, 0.5f);
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;

            ivRaw.texture = rt;
            ivRaw.color = Color.white;
            ivRaw.raycastTarget = true;

            // Hover spin
            var hover = imgGO.AddComponent<ItemPreviewHover>();
            hover.def = def;
            hover.rtWidth = rtW;
            hover.rtHeight = rtH;
            hover.initialStaticTexture = rt;
            hover.spinDegreesPerSecond = 40f;
            hover.returnDegreesPerSecond = 180f;

            // Drag view hookup
            var view = imgGO.AddComponent<InventoryItemView>();
            view.item = it;
            view.container = contRect;
            view.raw = ivRaw;
            view.dragCtrl = dragController;
            view.previewTexture = rt;
            view.equipment = equipmentController;
            view.inventory = inventory;

            // Tooltip hookup
            var tip = imgGO.GetComponent<InventorySlotTooltip>() ?? imgGO.AddComponent<InventorySlotTooltip>();
            tip.itemInstance = new Game.Items.ItemInstance(def, def.defaultTier);
            tip.targetOverride = imgRect;

            // keep container on top
            contRect.SetAsLastSibling();
            _itemViews.Add(container);

            // --- Background overlay (always) + optional badge ---
            bool meetsReq = true;
            if (equipmentController != null && def != null)
                meetsReq = equipmentController.MeetsRequirementsPublic(def);

            // 1) Background overlay (put it ABOVE the item so it always shows)
            var bgGO = new GameObject("ItemBg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(container.transform, false);

            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImg = bgGO.GetComponent<Image>();
            var okTint = new Color(0f, 0f, 0f, 0.40f); // slightly stronger so it’s always visible
            bgImg.color = meetsReq ? okTint : requirementFailTint;
            bgImg.raycastTarget = false;

            // ⬇️ Put background as the LAST sibling (above the item image)
            bgRect.SetAsLastSibling();

            // 2) Optional fail badge (only when failing)
            if (!meetsReq && requirementFailIcon != null)
            {
                var badgeGO = new GameObject("ReqFailBadge", typeof(RectTransform), typeof(Image));
                badgeGO.transform.SetParent(container.transform, false);
                var bRect = badgeGO.GetComponent<RectTransform>();
                bRect.anchorMin = bRect.anchorMax = bRect.pivot = new Vector2(1f, 1f);
                bRect.anchoredPosition = new Vector2(-4f, -4f);
                bRect.sizeDelta = new Vector2(22f, 22f);

                var bImg = badgeGO.GetComponent<Image>();
                bImg.sprite = requirementFailIcon;
                bImg.preserveAspect = true;
                bImg.raycastTarget = false;

                // keep badge on top of the overlay
                bRect.SetAsLastSibling();
            }
        }

        if (IsOpen && EventSystem.current != null)
            StartCoroutine(TriggerTooltipUnderCursorNextFrame());
    }




    // --- Tooltip replay helpers ---
    private System.Collections.IEnumerator TriggerTooltipUnderCursorNextFrame()
    {
        yield return null; // next frame (after Refresh)

        var es = EventSystem.current;
        if (es == null || gridRoot == null) yield break;

        // mouse position
        Vector2 pos;
#if ENABLE_INPUT_SYSTEM
    pos = UnityEngine.InputSystem.Mouse.current != null
        ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
        : (Vector2)Input.mousePosition;
#else
        pos = (Vector2)Input.mousePosition;
#endif

        // only if cursor is inside the inventory grid
        var canvas = gridRoot.GetComponentInParent<Canvas>();
        var cam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;
        if (!RectTransformUtility.RectangleContainsScreenPoint(gridRoot, pos, cam))
            yield break;

        var ped = new PointerEventData(es) { position = pos };

        // ⬇️ you were missing this call
        var results = new List<RaycastResult>();
        es.RaycastAll(ped, results);

        for (int i = 0; i < results.Count; i++)
        {
            var tip = results[i].gameObject.GetComponent<InventorySlotTooltip>()
                      ?? results[i].gameObject.GetComponentInParent<InventorySlotTooltip>();
            if (tip != null && tip.enabled && tip.itemInstance != null && tip.itemInstance.def != null)
            {
                tip.OnPointerEnter(ped);
                break;
            }
        }
    }


    private System.Collections.IEnumerator ReplayPointerEnterUnderCursor()
    {
        yield return null;                    // 1 frame after open
        yield return new WaitForEndOfFrame(); // after layout/TMP

        var es = EventSystem.current;
        if (es == null) yield break;

        Vector2 pos;
#if ENABLE_INPUT_SYSTEM
        pos = UnityEngine.InputSystem.Mouse.current != null
            ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
#else
        pos = (Vector2)Input.mousePosition;
#endif

        var eventData = new PointerEventData(es) { position = pos };

        var results = new List<RaycastResult>();
        es.RaycastAll(eventData, results);
        if (results.Count == 0) yield break;

        for (int i = 0; i < results.Count; i++)
            ExecuteEvents.Execute(results[i].gameObject, eventData, ExecuteEvents.pointerMoveHandler);

        for (int i = 0; i < results.Count; i++)
            ExecuteEvents.Execute(results[i].gameObject, eventData, ExecuteEvents.pointerEnterHandler);

        InventorySlotTooltip foundTip = null;
        for (int i = 0; i < results.Count && foundTip == null; i++)
        {
            var go = results[i].gameObject;
            foundTip = go.GetComponent<InventorySlotTooltip>() ?? go.GetComponentInParent<InventorySlotTooltip>();
        }

        if (foundTip != null) foundTip.OnPointerEnter(eventData);
    }

    // --- Cell highlight API (used by drag) ---
    public void HighlightCells(int x, int y, int w, int h, Color color)
    {
        if (_cells == null) return;
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int cx = x + dx;
                int cy = y + dy;
                if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows) continue;
                _cells[cx, cy].color = color;
            }
    }

    public void ClearHighlights()
    {
        if (_cells == null) return;
        for (int y = 0; y < _rows; y++)
            for (int x = 0; x < _cols; x++)
            {
                var raw = _cells[x, y];
                if (emptyTexture)
                {
                    raw.texture = emptyTexture;
                    raw.color = Color.white;
                }
                else
                {
                    raw.color = new Color(1f, 1f, 1f, 0f);
                }
            }
    }

    private void ClearItemViews()
    {
        for (int i = _itemViews.Count - 1; i >= 0; i--)
            Destroy(_itemViews[i]);
        _itemViews.Clear();
    }
}
