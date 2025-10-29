// Assets/Scripts/Inventory/InventoryUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerInventory inventory;     // drag Player Gameplay Object
    [SerializeField] private RectTransform gridRoot;        // InventoryPanel/GridRoot
    [SerializeField] private GameObject slotPrefab;         // Slot prefab with RawImage (for empty cells)

    [Header("Preview")]
    [SerializeField] private Texture2D emptyTexture;        // optional background for empty slots
    [SerializeField] private int previewSize = 256;         // RT size per item preview

    private int _cols, _rows;
    private RawImage[,] _cells;
    private RectTransform[,] _cellRects;
    private GridLayoutGroup _grid;

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

        _cols = inventory.Data.width;
        _rows = inventory.Data.height;

        _grid = gridRoot.GetComponent<GridLayoutGroup>();
        if (_grid == null)
        {
            Debug.LogError("GridRoot must have a GridLayoutGroup.");
            return;
        }

        BuildGrid();
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.Changed -= Refresh;
        ClearItemViews();
    }

    private void BuildGrid()
    {
        // Clear
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        _cells = new RawImage[_cols, _rows];
        _cellRects = new RectTransform[_cols, _rows];

        // Create slots (transparent by default)
        for (int y = 0; y < _rows; y++)
            for (int x = 0; x < _cols; x++)
            {
                var go = Instantiate(slotPrefab, gridRoot);
                go.name = $"Slot_{x}_{y}";

                var raw = go.GetComponent<RawImage>();
                if (!raw) raw = go.AddComponent<RawImage>();

                // EMPTY = transparent (fixes the white tiles)
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

    public void Refresh()
    {
        if (_cells == null) return;

        // 1) Clear existing item views (spanning images)
        ClearItemViews();

        // 2) Reset empty cell visuals
        for (int y = 0; y < _rows; y++)
            for (int x = 0; x < _cols; x++)
            {
                var raw = _cells[x, y];
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
                raw.uvRect = new Rect(0, 0, 1, 1);
            }

        Debug.Log($"[InventoryUI] Refresh. Items={inventory.Items.Count}");

        // 3) Create a single preview RawImage per item that SPANS its footprint
        foreach (var it in inventory.Items)
        {
            int x = it.x, y = it.y;
            if (x < 0 || x >= _cols || y < 0 || y >= _rows) continue;

            var def = it.def;
            if (def == null)
            {
                Debug.LogWarning("[InventoryUI] Item has null def.");
                continue;
            }

            Debug.Log($"[InventoryUI] Draw '{def.displayName}' at ({x},{y}) size {def.width}x{def.height}");
            // Render the 3D preview once
            var rt = ItemPreviewRenderer.Instance.Render(def, previewSize);
            if (rt == null || !rt.IsCreated())
            {
                Debug.LogWarning("[InventoryUI] RenderTexture missing/failed.");
                continue;
            }
            // Create view GO (child of gridRoot, but ignored by GridLayout)
            var ivGO = new GameObject($"ItemView_{def.displayName}", typeof(RectTransform), typeof(RawImage));
            ivGO.transform.SetParent(gridRoot, false);

            // 👇 IMPORTANT: ignore GridLayout sizing/positioning
            var layout = ivGO.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.ignoreLayout = true;

            var ivRect = ivGO.GetComponent<RectTransform>();
            var ivRaw = ivGO.GetComponent<RawImage>();
            ivRaw.texture = rt;
            ivRaw.color = Color.white;
            ivRaw.raycastTarget = false;
            ivRaw.material = null; // allow default
            ivRaw.color = Color.white; // keep model colors

            // Size = width×height cells (include spacing)
            var cellSize = _grid.cellSize;
            var spacing = _grid.spacing;
            int w = Mathf.Max(1, def.width);
            int h = Mathf.Max(1, def.height);

            float width = w * cellSize.x + (w - 1) * spacing.x;
            float height = h * cellSize.y + (h - 1) * spacing.y;
            ivRect.sizeDelta = new Vector2(width, height);

            // Position = top-left cell position (same parent)
            // Ensure we copy the same anchors/pivot so anchoredPosition matches
            var topLeftCell = _cellRects[x, y];
            ivRect.anchorMin = topLeftCell.anchorMin;
            ivRect.anchorMax = topLeftCell.anchorMax;
            ivRect.pivot = topLeftCell.pivot;
            ivRect.anchoredPosition = topLeftCell.anchoredPosition;

            // Keep on top
            ivRect.SetAsLastSibling();

            _itemViews.Add(ivGO);


            // Optionally tint covered cells slightly so the footprint is clear
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                {
                    int cx = x + dx, cy = y + dy;
                    if (cx < 0 || cx >= _cols || cy < 0 || cy >= _rows) continue;

                    var bg = _cells[cx, cy];
                    bg.color = new Color(0f, 0f, 0f, 0.5f);  // black @ 50% opacity
                }
            Debug.Log($"[InventoryUI] ItemView created size {ivRect.sizeDelta} pos {ivRect.anchoredPosition}");

        }
    }

    private void ClearItemViews()
    {
        for (int i = _itemViews.Count - 1; i >= 0; i--)
            Destroy(_itemViews[i]);
        _itemViews.Clear();
    }
}
