using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform _cam;
    void Awake() => _cam = Camera.main != null ? Camera.main.transform : null;
    void LateUpdate()
    {
        if (!_cam) { var c = Camera.main; if (c) _cam = c.transform; else return; }
        transform.forward = _cam.forward;
    }
}
