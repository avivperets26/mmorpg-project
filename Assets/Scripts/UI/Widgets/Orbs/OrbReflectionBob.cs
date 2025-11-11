using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class OrbReflectionBob : MonoBehaviour
{
    [SerializeField] float bobAmplitude = 2f;   // pixels
    [SerializeField] float bobFrequency = 0.5f; // Hz
    [SerializeField] Vector2 sway = new Vector2(6f, 0f); // small horizontal sway

    RectTransform rt;
    Vector2 basePos;

    void Awake() { rt = GetComponent<RectTransform>(); basePos = rt.anchoredPosition; }

    void Update()
    {
        float t = Time.unscaledTime;
        var p = basePos;
        p.y += Mathf.Sin(t * Mathf.PI * 2f * bobFrequency) * bobAmplitude;
        p.x += Mathf.Sin(t * 0.8f) * sway.x;
        rt.anchoredPosition = p;
    }
}
