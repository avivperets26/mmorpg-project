using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TooltipAnchorBeside : MonoBehaviour
{
    public enum VAlign { Top, Center, Bottom }

    [Header("Behavior")]
    public float gapX = 10f;
    public float clampPaddingY = 12f;
    public VAlign verticalAlign = VAlign.Center;
    public Vector2 nudge = Vector2.zero;

    [Header("Wiring")]
    public RectTransform rect;
    public Canvas canvas;

    RectTransform _canvasRect;
    RectTransform _target;

    void Reset()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        if (!rect) rect = transform as RectTransform;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        _canvasRect = canvas ? canvas.transform as RectTransform : null;

        // Ensure tooltip lives under this canvas and uses TL anchors/pivot
        if (rect && canvas && rect.parent != canvas.transform)
            rect.SetParent(canvas.transform, worldPositionStays: false);

        if (rect) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); }
        if (_target) RepositionNow();
    }

    void LateUpdate()
    {
        if (_target && rect && canvas) RepositionNow();
    }

    public void Attach(RectTransform target)
    {
        _target = target;
        RepositionNow();
    }
    public void Detach() => _target = null;
    public void PlaceBeside(RectTransform target) { Attach(target); RepositionNow(); }

    public void RepositionNow()
    {
        if (!_target || !_canvasRect || !rect || !canvas) return;

        // Guard: if target is not under this canvas, positions will be wrong
        if (!_target.IsChildOf(_canvasRect))
        {
            Debug.LogWarning("[TooltipAnchorBeside] Target is not a child of the assigned Canvas. " +
                             "Move tooltip into the same canvas (InventoryCanvas) or set the Canvas field correctly.");
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        Vector2 tipSize = new(
            LayoutUtility.GetPreferredSize(rect, 0),
            LayoutUtility.GetPreferredSize(rect, 1)
        );

        // Bounds of target in canvas space (robust to scaling)
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(_canvasRect, _target);
        float left = b.min.x;
        float right = b.max.x;
        float bottom = b.min.y;
        float top = b.max.y;
        float midY = 0.5f * (top + bottom);

        Rect c = _canvasRect.rect;

        // Prefer right, flip if needed
        bool canRight = right + gapX + tipSize.x <= c.xMax;
        bool canLeft = left - gapX - tipSize.x >= c.xMin;
        float x = (canRight || !canLeft) ? right + gapX : left - gapX - tipSize.x;

        float y = verticalAlign switch
        {
            VAlign.Top => top - tipSize.y,
            VAlign.Bottom => bottom,
            _ => midY - tipSize.y * 0.5f
        };

        y = Mathf.Clamp(y, c.yMin + clampPaddingY, c.yMax - clampPaddingY - tipSize.y);

        rect.anchoredPosition = new Vector2(x, y) + nudge;
    }
}
