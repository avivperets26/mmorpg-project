// Assets/Scripts/Inventory/InventoryUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;     // drag Player Gameplay Object
    [SerializeField] private RectTransform gridRoot;        // InventoryPanel/GridRoot (with GridLayoutGroup)
    [SerializeField] private GameObject slotPrefab;         // Slot prefab (must have RawImage component)

    [Header("Preview")]
    [SerializeField] private Texture2D emptyTexture;        // optional background for empty cells
    [SerializeField] private int previewSize = 256;         // RT size per item preview (square)

    private int _cols, _rows;
    private RawImage[,] _cells;
    private RectTransform[,] _cellRects;
    private GridLayoutGroup _grid;

    private bool _built;

    // Item views that span multiple cells
    private readonly List<GameObject> _itemViews = new();

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

        // Do NOT access inventory.Data here; it may not be ready yet.
    }

    private void Start()
    {
        // Kick off deferred init so PlayerInventory.Awake() can run first.
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

        // now safe to subscribe & draw
        inventory.Changed += Refresh;
        Refresh();
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

            // --- Footprint in cells ---
            int w = Mathf.Max(1, it.Width);
            int h = Mathf.Max(1, it.Height);

            var cellSize = _grid.cellSize;
            var spacing = _grid.spacing;

            // total pixel span of the footprint
            float spanW = w * cellSize.x + (w - 1) * spacing.x;
            float spanH = h * cellSize.y + (h - 1) * spacing.y;

            // Ask renderer for a RT that matches the aspect of the footprint.
            // Use previewSize per cell to keep good resolution.
            int rtW = Mathf.Max(64, Mathf.RoundToInt(previewSize * w));
            int rtH = Mathf.Max(64, Mathf.RoundToInt(previewSize * h));

            var rt = ItemPreviewRenderer.Instance.Render(def, rtW, rtH);
            if (rt == null || !rt.IsCreated()) continue;

            // --- Position a container over the footprint center ---
            var tlCellRect = _cellRects[it.x, it.y];
            Vector2 tlCenter = tlCellRect.anchoredPosition;
            Vector2 toCenter = new Vector2((spanW - cellSize.x) * 0.5f, -(spanH - cellSize.y) * 0.5f);
            Vector2 center = tlCenter + toCenter;

            var container = new GameObject($"ItemView_{def.displayName}_Container", typeof(RectTransform));
            var contRect = container.GetComponent<RectTransform>();
            container.transform.SetParent(gridRoot, false);

            var contLayout = container.AddComponent<UnityEngine.UI.LayoutElement>();
            contLayout.ignoreLayout = true;

            contRect.anchorMin = tlCellRect.anchorMin;
            contRect.anchorMax = tlCellRect.anchorMax;
            contRect.pivot = tlCellRect.pivot;          // typically 0.5,0.5
            contRect.sizeDelta = new Vector2(spanW, spanH); // EXACT footprint (e.g., 1×3)
            if (def.preview != null)
            {
                contRect.anchoredPosition += def.preview.uiOffsetPx;
            }
            contRect.localRotation = Quaternion.identity;

            // --- RawImage child that just fills the footprint (no squashing now) ---
            var imgGO = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(container.transform, false);

            var imgRect = imgGO.GetComponent<RectTransform>();
            var ivRaw = imgGO.GetComponent<RawImage>();

            // Fill container
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.pivot = new Vector2(0.5f, 0.5f);
            imgRect.offsetMin = Vector2.zero;
            imgRect.offsetMax = Vector2.zero;
            imgRect.localRotation = Quaternion.identity;

            ivRaw.texture = rt;     // RT already has the correct aspect
            ivRaw.color = Color.white;
            ivRaw.raycastTarget = true;  // <-- enable hit tests so hover works

            var hover = imgGO.AddComponent<ItemPreviewHover>();
            hover.def = def;
            hover.rtWidth = rtW;
            hover.rtHeight = rtH;
            hover.initialStaticTexture = rt;
            // Optionally tweak speeds:
            hover.spinDegreesPerSecond = 40f;
            hover.returnDegreesPerSecond = 180f;

            // draw on top
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



    private void ClearItemViews()
    {
        for (int i = _itemViews.Count - 1; i >= 0; i--)
            Destroy(_itemViews[i]);
        _itemViews.Clear();
    }
}
