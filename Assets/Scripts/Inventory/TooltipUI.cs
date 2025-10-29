using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform root;   // assign this Tooltip's RectTransform
    public TMP_Text text;        // assign TooltipText

    [Header("Behavior")]
    public Vector2 screenOffset = new Vector2(16f, -16f);

    void Awake()
    {
        if (!root) root = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        // Follow mouse (UI space)
        Vector2 pos = Input.mousePosition;
        pos += screenOffset;

        // Keep inside screen bounds (optional clamp)
        var canvas = GetComponentInParent<Canvas>();
        if (canvas && canvas.renderMode != RenderMode.WorldSpace)
        {
            var size = root.sizeDelta;
            float maxX = Screen.width - size.x - 8f;
            float maxY = Screen.height - size.y - 8f;
            pos.x = Mathf.Clamp(pos.x, 8f, maxX);
            pos.y = Mathf.Clamp(pos.y, size.y + 8f, maxY);
        }

        root.position = pos;
    }

    public void Show(string content)
    {
        if (text) text.text = content;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
