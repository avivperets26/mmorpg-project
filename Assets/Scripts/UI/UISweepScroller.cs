using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UISweepScroller : MonoBehaviour
{
    public float speed = 0.25f; // UV units per second
    RawImage ri;
    Rect uv;

    void Awake() { ri = GetComponent<RawImage>(); uv = ri.uvRect; }
    void Update()
    {
        uv.x += speed * Time.unscaledDeltaTime;
        if (uv.x > 1f) uv.x -= 1f;
        ri.uvRect = uv;
    }
}
