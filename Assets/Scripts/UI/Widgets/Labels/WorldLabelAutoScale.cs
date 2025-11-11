using UnityEngine;

public class WorldLabelAutoScale : MonoBehaviour
{
    public float baseDistance = 6f;
    public float baseScale = 1f;
    public float minScale = 0.6f, maxScale = 1.4f;

    Transform _cam;
    void Awake() => _cam = Camera.main ? Camera.main.transform : null;

    void LateUpdate()
    {
        if (!_cam) { var c = Camera.main; if (c) _cam = c.transform; else return; }
        float d = Vector3.Distance(_cam.position, transform.position);
        float s = Mathf.Clamp((d / baseDistance) * baseScale, minScale, maxScale);
        transform.localScale = Vector3.one * s;
    }
}
