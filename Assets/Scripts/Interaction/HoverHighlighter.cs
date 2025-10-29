using UnityEngine;

public class HoverHighlighter : MonoBehaviour, IHoverHighlight
{
    public string outlineLayerName = "Outline";
    int _defaultLayer, _outlineLayer;
    bool _active;

    void Awake()
    {
        _defaultLayer = gameObject.layer;
        _outlineLayer = LayerMask.NameToLayer(outlineLayerName);
        if (_outlineLayer < 0) Debug.LogError($"Layer '{outlineLayerName}' not found. Create it in Project Settings → Tags and Layers.");
    }

    public void OnHoverEnter()
    {
        if (_active) return;
        _active = true;
        SetLayerRecursively(gameObject, _outlineLayer);
    }

    public void OnHoverExit()
    {
        if (!_active) return;
        _active = false;
        SetLayerRecursively(gameObject, _defaultLayer);
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
    }
}
