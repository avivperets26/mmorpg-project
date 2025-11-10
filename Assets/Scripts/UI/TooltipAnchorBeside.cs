// Assets/Scripts/UI/TooltipAnchorBeside.cs
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TooltipAnchorBeside : MonoBehaviour
{
    public enum VAlign { Top, Center, Bottom }
    public enum HPrefer { RightThenLeft, LeftThenRight }

    // NEW: horizontal placement mode
    public enum HMode
    {
        PreferThenFit,  // try prefer side first; if it doesn't fit, fall back
        ByAvailableSpace // ignore prefer; pick the side with more free space
    }

    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false; // replaces old 'debug'
    private string _tag => "Anchor";

    [Header("Behavior")]
    public HMode hMode = HMode.PreferThenFit;
    public HPrefer prefer = HPrefer.RightThenLeft;
    public float gapX = 10f;
    public float gapY = 8f; // <— NEW: vertical offset
    public Vector2 clampPadding = new Vector2(12f, 12f);
    public VAlign verticalAlign = VAlign.Top; // default to Top
    public Vector2 nudge = Vector2.zero;

    [Header("Wiring")]
    public RectTransform rect;   // tooltip rect (auto)
    public Canvas canvas;        // canvas hosting the tooltip (auto)
    public Canvas clampCanvasOverride; // leave null to auto-use root canvas

    private RectTransform _canvasRect;
    private RectTransform _clampCanvasRect;
    private RectTransform _target;

    private void Reset()
    {
        rect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        if (!rect) rect = transform as RectTransform;
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        var clampCanvas = clampCanvasOverride ? clampCanvasOverride : (canvas ? canvas.rootCanvas : null);
        _canvasRect = canvas ? canvas.transform as RectTransform : null;
        _clampCanvasRect = clampCanvas ? clampCanvas.transform as RectTransform : null;

        // Ensure tooltip lives under the clamp canvas for correct local coords
        if (rect && clampCanvas && rect.parent != clampCanvas.transform)
            rect.SetParent(clampCanvas.transform, worldPositionStays: false);

        // Top-left anchored/pivoted so anchoredPosition is absolute inside clamp canvas
        if (rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        if (_target) RepositionNow();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!rect) rect = transform as RectTransform;
        if (rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot     = new Vector2(0f, 1f);
        }
    }
#endif

    private void LateUpdate()
    {
        if (_target && rect && canvas)
            RepositionNow();
    }

    // ---------------- API ----------------
    public void Attach(RectTransform target)
    {
        _target = target;
        UITooltipDebug.Log(enableLogs, this, _tag, $"Attach target='{_target?.name}'");
        RepositionNow();
    }

    public void Detach()
    {
        UITooltipDebug.Log(enableLogs, this, _tag, "Detach target");
        _target = null;
    }
    public void PlaceBeside(RectTransform target)
    {
        Attach(target);
        RepositionNow();
    }

    public void RepositionNow()
    {
        if (!_target || !_clampCanvasRect || !rect || !canvas) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

        Vector2 pref = new(
            Mathf.Max(1f, LayoutUtility.GetPreferredSize(rect, 0)),
            Mathf.Max(1f, LayoutUtility.GetPreferredSize(rect, 1))
        );
        Vector2 actual = rect.rect.size;
        Vector2 tipSize = new(Mathf.Max(pref.x, actual.x), Mathf.Max(pref.y, actual.y));

        Rect clampCanvasRect = _clampCanvasRect.rect;
        Rect targetRectInClamp = GetTargetRectInCanvasSpace(
            _target, _clampCanvasRect, _clampCanvasRect.GetComponent<Canvas>());

        float left = targetRectInClamp.xMin;
        float right = targetRectInClamp.xMax;
        float bottom = targetRectInClamp.yMin;
        float top = targetRectInClamp.yMax;
        float midY = 0.5f * (top + bottom);

        float padX = clampPadding.x;
        float padY = clampPadding.y;

        float minX = clampCanvasRect.xMin + padX;
        float maxX = clampCanvasRect.xMax - padX - tipSize.x;

        float spaceLeft = (left - clampCanvasRect.xMin) - padX;
        float spaceRight = (clampCanvasRect.xMax - right) - padX;

        UITooltipDebug.Log(enableLogs, this, _tag,
            $"BEGIN tip=({tipSize.x:0},{tipSize.y:0}) target=({left:0},{bottom:0})–({right:0},{top:0}) " +
            $"spaceL={spaceLeft:0} spaceR={spaceRight:0} prefer={prefer} mode={hMode}");

        float AlignY(float t, float b, float m, float h)
        {
            return verticalAlign switch
            {
                VAlign.Top => t - h - gapY,
                VAlign.Bottom => b + gapY,
                _ => m - h * 0.5f
            };
        }

        Vector2 PlaceRight()
        {
            float x = right + gapX;
            float y = AlignY(top, bottom, midY, tipSize.y);
            float minY = clampCanvasRect.yMin + padY;
            float maxY = clampCanvasRect.yMax - padY - tipSize.y;
            y = Mathf.Clamp(y, minY, maxY);
            return new Vector2(Mathf.Clamp(x, minX, maxX), y);
        }

        Vector2 PlaceLeft()
        {
            float x = left - gapX - tipSize.x;
            float y = AlignY(top, bottom, midY, tipSize.y);
            float minY = clampCanvasRect.yMin + padY;
            float maxY = clampCanvasRect.yMax - padY - tipSize.y;
            y = Mathf.Clamp(y, minY, maxY);
            return new Vector2(Mathf.Clamp(x, minX, maxX), y);
        }

        bool FitsRight()
        {
            float x = right + gapX;
            bool fits = x <= maxX + 0.0001f;
            UITooltipDebug.Log(enableLogs, this, _tag, $"FitsRight={fits} (x={x:0.0}, maxX={maxX:0.0})");
            return fits;
        }

        bool FitsLeft()
        {
            float x = left - gapX - tipSize.x;
            bool fits = x >= minX - 0.0001f;
            UITooltipDebug.Log(enableLogs, this, _tag, $"FitsLeft={fits} (x={x:0.0}, minX={minX:0.0})");
            return fits;
        }

        Vector2 pos;

        UITooltipDebug.Log(enableLogs, this, _tag,
            $"[TAB] clampRect size=({clampCanvasRect.width:0.0},{clampCanvasRect.height:0.0}) " +
            $"min=({clampCanvasRect.xMin:0.0},{clampCanvasRect.yMin:0.0}) " +
            $"max=({clampCanvasRect.xMax:0.0},{clampCanvasRect.yMax:0.0})");
        UITooltipDebug.Log(enableLogs, this, _tag,
            $"[TAB] target L/R/T/B: {left:0.0}/{right:0.0}/{top:0.0}/{bottom:0.0} midY={midY:0.0} " +
            $"minX={minX:0.0} maxX={maxX:0.0}");

        if (hMode == HMode.PreferThenFit)
        {
            if (prefer == HPrefer.RightThenLeft)
            {
                if (FitsRight()) { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → using Right"); }
                else if (FitsLeft()) { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → Right fails, using Left"); }
                else { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → neither fits, clamped Right"); }
            }
            else
            {
                if (FitsLeft()) { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → using Left"); }
                else if (FitsRight()) { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → Left fails, using Right"); }
                else { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "PreferThenFit → neither fits, clamped Left"); }
            }
        }
        else // ByAvailableSpace
        {
            bool rightFirst = spaceRight >= spaceLeft;
            UITooltipDebug.Log(enableLogs, this, _tag,
                $"Mode=ByAvailableSpace → rightFirst={rightFirst} (spaceR={spaceRight:0.0} vs spaceL={spaceLeft:0.0})");

            if (rightFirst)
            {
                if (FitsRight()) { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → using Right"); }
                else if (FitsLeft()) { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → Right fails, using Left"); }
                else { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → neither fits, clamped Right"); }
            }
            else
            {
                if (FitsLeft()) { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → using Left"); }
                else if (FitsRight()) { pos = PlaceRight(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → Left fails, using Right"); }
                else { pos = PlaceLeft(); UITooltipDebug.Log(enableLogs, this, _tag, "ByAvailableSpace → neither fits, clamped Left"); }
            }
        }

        // final position (single assignment)
        Vector2 topLeft = new Vector2(_clampCanvasRect.rect.xMin, _clampCanvasRect.rect.yMax);
        Vector2 anchored = (pos - topLeft) + nudge;
        rect.anchoredPosition = anchored;

        UITooltipDebug.Log(enableLogs, this, _tag,
            $"anchor/pivot={rect.anchorMin}..{rect.anchorMax} pivot={rect.pivot} " +
            $"topLeft={topLeft} pos(centerSpace)={pos} → anchored(topLeftSpace)={anchored}");
    }


    // ---------------- helpers ----------------
    private static Rect GetTargetRectInCanvasSpace(RectTransform target, RectTransform canvasRect, Canvas clampCanvas)
    {
        // Always compute via world corners -> canvas local. This avoids nested-canvas quirks.
        Vector3[] world = new Vector3[4];
        target.GetWorldCorners(world);

        Camera cam = (clampCanvas && clampCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? clampCanvas.worldCamera
            : null;

        // Convert two opposite corners
        Vector2 a = WorldToCanvasLocal(world[0], canvasRect, cam); // bottom-left
        Vector2 b = WorldToCanvasLocal(world[2], canvasRect, cam); // top-right

        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private static Vector2 WorldToCanvasLocal(Vector3 world, RectTransform canvasRect, Camera cam)
    {
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, cam, out Vector2 lp);
        return lp;
    }
}
