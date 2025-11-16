using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class DebugForcePostFX : MonoBehaviour
{
    void Awake()
    {
        var camData = GetComponent<UniversalAdditionalCameraData>();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            Debug.Log("[DebugForcePostFX] Post-processing forced ON at Awake.");
        }
        else
        {
            Debug.LogWarning("[DebugForcePostFX] UniversalAdditionalCameraData not found on this camera.");
        }
    }
}
