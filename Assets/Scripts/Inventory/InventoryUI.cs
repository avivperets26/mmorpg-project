// Assets/Scripts/Inventory/InventoryUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class InventoryUI : MonoBehaviour
{
    [Header("Panel / Input Lock")]
    [SerializeField] private GameObject inventoryPanel; // drag InventoryPanel root
    [SerializeField] private PlayerInput playerInput;   // drag the Player's PlayerInput
    [SerializeField] private Behaviour cinInput;   // <- generic, no Cinemachine namespace

    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;     // drag Player Gameplay Object
    [SerializeField] private RectTransform gridRoot;        // InventoryPanel/GridRoot (with GridLayoutGroup)
    [SerializeField] private GameObject slotPrefab;         // Slot prefab (must have RawImage component)

    [Header("Preview")]
    [SerializeField] private Texture2D emptyTexture;        // optional background for empty cells
    [SerializeField] private int previewSize = 256;         // RT size per item preview (square)
    [SerializeField] private InventoryDragController dragController; // optional drag controller
    [HideInInspector] public InventoryItem dragHiddenItem;


    private int _cols, _rows;
    private RawImage[,] _cells;
    private RectTransform[,] _cellRects;
    private GridLayoutGroup _grid;

    private bool _built;
    private bool isOpen;

    // Item views that span multiple cells
    private readonly List<GameObject> _itemViews = new();

    private InputAction moveAction, lookAction, dodgeAction, attackAction, moveClickAction;

    private void Awake()
    {
        // Auto-find PlayerInput if not assigned
        if (playerInput == null)
#if UNITY_2023_1_OR_NEWER
            playerInput = Object.FindFirstObjectByType<PlayerInput>();
#else
            playerInput = Object.FindObjectOfType<PlayerInput>();
#endif
    }


    private void Start()
    {
        // cache input actions (your existing code)
        if (playerInput && playerInput.actions != null)
        {
            var a = playerInput.actions;
            moveAction = a.FindAction("Move");
            lookAction = a.FindAction("Look");
            dodgeAction = a.FindAction("Dodge");
            attackAction = a.FindAction("PrimaryAttack");
            moveClickAction = a.FindAction("MoveClick");
        }

        // ⬇️ this line is required to build the cell grid & hook events
        StartCoroutine(InitWhenReady());

        if (!dragController) dragController = GetComponent<InventoryDragController>();
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

        // now safe to subscribe & draw
        inventory.Changed += Refresh;
        Refresh();
    }

    public void Toggle() { if (isOpen) Close(); else Open(); }


    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (inventoryPanel) inventoryPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Preferred: switch to UI map if it exists
        if (playerInput && playerInput.actions.FindActionMap("UI") != null)
            playerInput.SwitchCurrentActionMap("UI");

        // Hard stop gameplay actions regardless of maps
        moveAction?.Disable();
        lookAction?.Disable();
        dodgeAction?.Disable();
        attackAction?.Disable();
        moveClickAction?.Disable();

        if (cinInput) cinInput.enabled = false;
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        if (inventoryPanel) inventoryPanel.SetActive(false);

        if (playerInput && playerInput.actions.FindActionMap("Gameplay") != null)
            playerInput.SwitchCurrentActionMap("Gameplay");

        // Re-enable actions
        moveAction?.Enable();
        lookAction?.Enable();
        dodgeAction?.Enable();
        attackAction?.Enable();
        moveClickAction?.Enable();

        if (cinInput) cinInput.enabled = true;
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

        // 2) Reset all cells
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
                    raw.texture = null;
                    raw.color = new Color(1f, 1f, 1f, 0f);
                }
                raw.uvRect = new Rect(0, 0, 1, 1);
            }

        // 3) One preview per placed item
        // 3) One preview per placed item
        foreach (var it in inventory.Items)
        {
            var def = it.def;
            if (!def) continue;
            if (dragHiddenItem == it) continue;

            // --- Footprint in cells ---
            int w = Mathf.Max(1, it.Width);
            int h = Mathf.Max(1, it.Height);

            var cs = _grid.cellSize;
            var sp = _grid.spacing;

            float pitchX = cs.x + sp.x;
            float pitchY = cs.y + sp.y;

            // top-left of the footprint in grid space
            float px = it.x * pitchX;
            float py = it.y * pitchY;

            // pixel size of the footprint
            float spanW = w * cs.x + (w - 1) * sp.x;
            float spanH = h * cs.y + (h - 1) * sp.y;

            // Ask renderer for an RT that matches the footprint aspect
            int rtW = Mathf.Max(64, Mathf.RoundToInt(previewSize * w));
            int rtH = Mathf.Max(64, Mathf.RoundToInt(previewSize * h));
            var rt = ItemPreviewRenderer.Instance.Render(def, rtW, rtH);
            if (rt == null || !rt.IsCreated()) continue;

            // --- Container over the footprint (top-left anchored) ---
            var container = new GameObject($"ItemView_{def.displayName}_Container", typeof(RectTransform));
            var contRect = container.GetComponent<RectTransform>();
            container.transform.SetParent(gridRoot, false);

            // ignore layout
            var contLayout = container.AddComponent<UnityEngine.UI.LayoutElement>();
            contLayout.ignoreLayout = true;

            // anchor to grid top-left so math matches footprint preview
            contRect.anchorMin = Vector2.up;   // (0,1)
            contRect.anchorMax = Vector2.up;
            contRect.pivot = new Vector2(0f, 1f);

            // EXACT footprint size
            contRect.sizeDelta = new Vector2(spanW, spanH);

            // place container so its center is at the footprint center
            contRect.anchoredPosition = new Vector2(px + spanW * 0.5f, -(py + spanH * 0.5f));

            // optional 2D nudge
            if (def.preview != null) contRect.anchoredPosition += def.preview.uiOffsetPx;

            contRect.localRotation = Quaternion.identity;

            // --- RawImage child filling the container ---
            var imgGO = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(container.transform, false);

            var imgRect = imgGO.GetComponent<RectTransform>();
            var ivRaw = imgGO.GetComponent<RawImage>();

            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.pivot = new Vector2(0.5f, 0.5f);
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;

            ivRaw.texture = rt;
            ivRaw.color = Color.white;
            ivRaw.raycastTarget = true;

            // hover + drag hookup (unchanged)
            var hover = imgGO.AddComponent<ItemPreviewHover>();
            hover.def = def; hover.rtWidth = rtW; hover.rtHeight = rtH; hover.initialStaticTexture = rt;
            hover.spinDegreesPerSecond = 40f; hover.returnDegreesPerSecond = 180f;

            var view = imgGO.AddComponent<InventoryItemView>();
            view.item = it; view.container = contRect; view.raw = ivRaw; view.dragCtrl = dragController; view.previewTexture = rt;

            contRect.SetAsLastSibling();
            _itemViews.Add(container);


            // Optional: dim covered cells
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                {
                    int cx = it.x + dx, cy = it.y + dy;
                    if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows) continue;
                    _cells[cx, cy].color = new Color(0f, 0f, 0f, 0.5f);
                }
        }

    }

    // --- Cell highlighting (used during drag preview) ---
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
