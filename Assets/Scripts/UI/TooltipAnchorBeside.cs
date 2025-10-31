using UnityEngine;
using UnityEngine.UI; // for LayoutRebuilder/LayoutUtility

/// <summary>
/// Positions a tooltip to the left or right of a target RectTransform,
/// clamped inside the parent Canvas.
/// </summary>
public class TooltipAnchorBeside : MonoBehaviour
{
    [Tooltip("Gap from the item and canvas edges (x = horizontal, y = vertical clamp).")]
    public Vector2 padding = new(12, 12);

    [Tooltip("Tooltip RectTransform (self).")]
    public RectTransform rect;

    [Tooltip("Canvas containing the tooltip.")]
    public Canvas canvas;

    void Reset()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>Place tooltip beside the target, picking the side with more space.</summary>
    public void PlaceBeside(RectTransform target)
    {
        if (!canvas || !rect || !target) return;

        var canvasRect = canvas.transform as RectTransform;
        Rect c = canvasRect.rect;

        // Target world corners -> canvas local
        Vector3[] w = new Vector3[4];
        target.GetWorldCorners(w);
        Vector2 tl = WorldToCanvasLocal(w[1]); // top-left
        Vector2 br = WorldToCanvasLocal(w[3]); // bottom-right
        float targetLeft = tl.x;
        float targetRight = br.x;
        float targetTop = tl.y;
        float targetBottom = br.y;
        float targetMidY = (targetTop + targetBottom) * 0.5f;

        // Rebuild & read tooltip preferred size in canvas space
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        Vector2 tipSize = new(
            LayoutUtility.GetPreferredSize(rect, 0),
            LayoutUtility.GetPreferredSize(rect, 1)
        );

        // Choose side with more room
        float spaceRight = c.xMax - targetRight;
        float spaceLeft = targetLeft - c.xMin;
        bool placeRight = spaceRight >= spaceLeft;

        float x = placeRight
            ? Mathf.Min(targetRight + padding.x, c.xMax - padding.x - tipSize.x)
            : Mathf.Max(targetLeft - padding.x - tipSize.x, c.xMin + padding.x);

        // Vertically center to target then clamp to canvas
        float y = Mathf.Clamp(targetMidY - tipSize.y * 0.5f,
                              c.yMin + padding.y,
                              c.yMax - padding.y - tipSize.y);

        rect.anchoredPosition = new Vector2(x, y);
    }

    Vector2 WorldToCanvasLocal(Vector3 world)
    {
        var canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                world),
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out var local);
        return local;
    }
}
