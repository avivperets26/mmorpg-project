using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClassEmblemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wiring")]
    [SerializeField] private Image emblemImage;

    [Header("Visuals")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float transitionSpeed = 12f;

    private bool isHovered;
    private bool isSelected;
    private Color targetColor;
    private float targetScale;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; Refresh(); }

    private void Reset() { emblemImage = GetComponent<Image>(); }

    private void Awake()
    {
        if (!emblemImage) emblemImage = GetComponent<Image>();
        DisableHoverEffects();
        Refresh(true);
    }

    private void OnEnable()
    {
        Refresh(true);
    }

    private void DisableHoverEffects()
    {
        var hoverEffects = GetComponentsInChildren<HoverEffect>(true);
        for (var i = 0; i < hoverEffects.Length; i++)
            hoverEffects[i].enabled = false;
    }

    private void Update()
    {
        var t = Time.unscaledDeltaTime * transitionSpeed;
        if (emblemImage)
            emblemImage.color = Color.Lerp(emblemImage.color, targetColor, t);

        var currentScale = transform.localScale.x;
        var nextScale = Mathf.Lerp(currentScale, targetScale, t);
        transform.localScale = Vector3.one * nextScale;
    }

    private void Refresh(bool instant = false)
    {
        targetColor = isSelected ? selectedColor : (isHovered ? hoverColor : normalColor);
        targetScale = isSelected ? selectedScale : (isHovered ? hoverScale : normalScale);

        if (instant)
        {
            if (emblemImage) emblemImage.color = targetColor;
            transform.localScale = Vector3.one * targetScale;
        }
    }
}
