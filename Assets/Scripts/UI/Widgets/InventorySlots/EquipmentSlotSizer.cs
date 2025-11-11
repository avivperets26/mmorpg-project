using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class EquipmentSlotSizer : MonoBehaviour
{
    [Header("Grid Footprint (slot size)")]
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    [Header("Visual Cell Settings")]
    public float cellSize = 64f;   // one grid cell in pixels
    public float spacing = 6f;     // gap between cells (visual)

    [Header("Optional: auto-fit inner Icon")]
    public Image icon;             // drag your Icon image if you want auto padding
    public float iconPadding = 6f;

    void OnEnable()
    {
        ApplyNow();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Defer changes to avoid "SendMessage during OnValidate" warnings
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;            // object got deleted
            if (!gameObject) return;
            ApplyNow();
        };
    }
#endif

    private void ApplyNow()
    {
        var rt = (RectTransform)transform;

        float w = width * cellSize + (width - 1) * spacing;
        float h = height * cellSize + (height - 1) * spacing;

        rt.sizeDelta = new Vector2(w, h);

        if (icon)
        {
            var irt = icon.rectTransform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(iconPadding, iconPadding);
            irt.offsetMax = new Vector2(-iconPadding, -iconPadding);

            icon.preserveAspect = true;
            // keep raycast off; the frame handles clicks
            icon.raycastTarget = false;
        }
    }
}
