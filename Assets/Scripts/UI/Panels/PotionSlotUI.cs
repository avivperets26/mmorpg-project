using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Items;

[DisallowMultipleComponent]
public class PotionSlotUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private RawImage iconRaw;
    [SerializeField] private Image cooldownOverlay;

    [Header("Icon Preview")]
    [Tooltip("Size of the preview RenderTexture for this slot.")]
    [SerializeField] private Vector2Int iconSize = new Vector2Int(60, 60);

    [Header("Debug")]
    [SerializeField] private string debugKeyLabel = "Q";

    [NonSerialized] public PotionItemDefinition currentDef;
    [NonSerialized] public int totalCount;

    private void Reset()
    {
        if (!keyLabel) keyLabel = transform.Find("KeyLabel")?.GetComponent<TMP_Text>();
        if (!countLabel) countLabel = transform.Find("CountLabel")?.GetComponent<TMP_Text>();
        if (!iconRaw) iconRaw = transform.Find("Icon")?.GetComponent<RawImage>();
        if (!cooldownOverlay) cooldownOverlay = transform.Find("CooldownOverlay")?.GetComponent<Image>();
    }

    private void Awake()
    {
        if (keyLabel && !string.IsNullOrEmpty(debugKeyLabel))
            keyLabel.text = debugKeyLabel;

        RefreshVisuals();
    }

    public void SetKeyLabel(string key)
    {
        debugKeyLabel = key;
        if (keyLabel) keyLabel.text = key;
    }

    public void SetData(PotionItemDefinition def, int count)
    {
        currentDef = def;
        totalCount = count;
        RefreshVisuals();
    }

    public void SetCooldown01(float value01)
    {
        if (!cooldownOverlay) return;
        value01 = Mathf.Clamp01(value01);
        cooldownOverlay.gameObject.SetActive(value01 > 0f);
        cooldownOverlay.fillAmount = value01;
    }

    private void RefreshVisuals()
    {
        // Count label --------------------------------------------------------
        if (countLabel)
            countLabel.text = totalCount > 0 ? totalCount.ToString() : string.Empty;

        bool hasPotion = currentDef != null && totalCount > 0;

        // Icon preview -------------------------------------------------------
        if (!iconRaw)
            return;

        if (!hasPotion)
        {
            iconRaw.enabled = false;
            iconRaw.texture = null;
            return;
        }

        iconRaw.enabled = true;

        var renderer = ItemPreviewRenderer.Instance;
        if (renderer == null)
            return;

        // Use the same preview pipeline as InventoryUI, but with our small icon size
        int rtW = Mathf.Max(32, iconSize.x);
        int rtH = Mathf.Max(32, iconSize.y);

        var rt = renderer.Render(currentDef, rtW, rtH);
        if (rt == null || !rt.IsCreated())
            return;

        iconRaw.texture = rt;
    }

}
