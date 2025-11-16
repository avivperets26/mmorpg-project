using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DebugForcePostFX : MonoBehaviour
{
    private void Awake()
    {
        // Only ever touch the MAIN camera, never all cameras.
        var cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("[DebugForcePostFX] No Camera.main found, aborting.");
            return;
        }

        var urp = cam.GetComponent<UniversalAdditionalCameraData>();
        if (!urp)
            urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        urp.renderPostProcessing = true;
        // Use whatever volume mask you already use for the main camera:
        // here I just keep "Everything" for simplicity.
        urp.volumeLayerMask = ~0;

        Debug.Log($"[DebugForcePostFX] Post-processing forced ON at Awake on main camera '{cam.name}'.");
    }
}
