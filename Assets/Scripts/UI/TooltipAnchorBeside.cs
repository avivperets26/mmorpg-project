// Assets/Scripts/UI/TooltipAnchorBeside.cs
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TooltipAnchorBeside : MonoBehaviour
{
    public enum VAlign { Top, Center, Bottom }

    [Header("Behavior")]
    [Tooltip("Pixels between the slot edge and the tooltip box.")]
    public float gapX = 10f;

    [Tooltip("Keep some room from top/bottom canvas edges when clamping.")]
    public float clampPaddingY = 12f;

    [Tooltip("Vertical alignment relative to the slot.")]
    public VAlign verticalAlign = VAlign.Center;

    [Tooltip("Optional fine-tune after auto placement (X right, Y up).")]
    public Vector2 nudge = Vector2.zero;

    [Header("Wiring")]
    [Tooltip("Tooltip RectTransform (self).")]
    public RectTransform rect;

    [Tooltip("Canvas containing the tooltip.")]
    public Canvas canvas;

    // runtime
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

        // Ensure tooltip lives under this canvas and uses top-left pivot/anchors
        if (rect && canvas && rect.parent != canvas.transform)
            rect.SetParent(canvas.transform, worldPositionStays: false);

        if (rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        if (_target) RepositionNow();
    }

    void LateUpdate()
    {
        if (_target && rect && canvas) RepositionNow();
    }

    /// <summary>Attach to a target and start following it.</summary>
    public void Attach(RectTransform target)
    {
        _target = target;
        RepositionNow();
    }

    public void Detach() => _target = null;

    /// <summary>Legacy one-shot call.</summary>
    public void PlaceBeside(RectTransform target)
    {
        Attach(target);
        RepositionNow();
    }

    /// <summary>Compute and apply anchoredPosition in canvas space.</summary>
    public void RepositionNow()
    {
        if (!_target || !_canvasRect || !rect || !canvas) return;

        // Make sure layout size is current
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        // Tooltip preferred size in canvas units
        Vector2 tipSize = new(
            LayoutUtility.GetPreferredSize(rect, 0),
            LayoutUtility.GetPreferredSize(rect, 1)
        );

        // Target bounds relative to the canvas (robust to scaling)
        var b = RectTransformUtility.CalculateRelativeRectTransformBounds(_canvasRect, _target);
        float targetLeft = b.min.x;
        float targetRight = b.max.x;
        float targetBottom = b.min.y;
        float targetTop = b.max.y;
        float targetMidY = 0.5f * (targetTop + targetBottom);

        // Canvas rect (also in canvas space)
        Rect c = _canvasRect.rect;

        // Prefer RIGHT, flip LEFT if needed
        bool canRight = targetRight + gapX + tipSize.x <= c.xMax;
        bool canLeft = targetLeft - gapX - tipSize.x >= c.xMin;

        float x;
        if (canRight || !canLeft)
            x = targetRight + gapX;
        else
            x = targetLeft - gapX - tipSize.x;

        // Vertical alignment
        float y;
        switch (verticalAlign)
        {
            case VAlign.Top: y = targetTop - tipSize.y; break;
            case VAlign.Bottom: y = targetBottom; break;
            default: y = targetMidY - tipSize.y * 0.5f; break;
        }

        // Clamp to canvas
        y = Mathf.Clamp(y, c.yMin + clampPaddingY, c.yMax - clampPaddingY - tipSize.y);

        rect.anchoredPosition = new Vector2(x, y) + nudge;
    }
}
