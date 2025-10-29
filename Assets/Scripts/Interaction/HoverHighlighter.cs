using UnityEngine;
using HighlightPlus;

// Add this if you use the new Input System
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HoverHighlighter : MonoBehaviour
{
    [Header("Raycasting")]
    public Camera raycastCamera;
    public LayerMask interactableMask;
    public float maxDistance = 100f;

    [Header("Optional: cursor center mode")]
    public bool useScreenCenter = false;

    HighlightEffect currentEffect;
    Transform lastRoot;

    void Update()
    {
        if (raycastCamera == null) return;

        // Get screen position
        Vector3 screenPos;

        if (useScreenCenter)
        {
            screenPos = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }
        else
        {
            // Support both input backends
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) { ClearCurrent(); return; }
            screenPos = Mouse.current.position.ReadValue();
#else
            screenPos = Input.mousePosition;
#endif
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableMask, QueryTriggerInteraction.Ignore))
        {
            var root = hit.transform.root;

            if (root != lastRoot)
            {
                ClearCurrent();

                if (root.TryGetComponent<HighlightEffect>(out var he) ||
                    hit.transform.TryGetComponent<HighlightEffect>(out he))
                {
                    he.highlighted = true;      // turn outline ON
                    currentEffect = he;
                    lastRoot = he.transform;
                }
            }
        }
        else
        {
            ClearCurrent();
        }
    }

    void OnDisable() => ClearCurrent();

    void ClearCurrent()
    {
        if (currentEffect != null)
        {
            currentEffect.highlighted = false; // turn outline OFF
            currentEffect = null;
        }
        lastRoot = null;
    }
}
