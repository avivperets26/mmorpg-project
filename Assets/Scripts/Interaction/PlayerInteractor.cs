using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Casts a ray from the camera through the mouse position each frame.
/// - Highlights anything with IHoverHighlight
/// - Interacts with anything implementing IInteractable (click/E) if within distance
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask interactMask = ~0;      // or set to a dedicated "Interactable" layer
    public float maxRayDistance = 100f;

    [Header("Controls")]
#if ENABLE_INPUT_SYSTEM
    public Key interactKey = Key.E;
#else
    public KeyCode interactKeyLegacy = KeyCode.E;
#endif
    public bool allowMouseClick = true;

    Camera _cam;
    IHoverHighlight _currentHighlight;
    IInteractable _currentInteractable;

    void Awake()
    {
        _cam = Camera.main;
        if (!_cam) Debug.LogWarning("PlayerInteractor: No Main Camera found (Camera.main is null).");
    }

    void Update()
    {
        if (!_cam) return;

        // --- 1) Raycast from mouse ---
        Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
        mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
#else
        mousePos = Input.mousePosition;
#endif
        var ray = _cam.ScreenPointToRay(mousePos);
        bool hitSomething = Physics.Raycast(ray, out var hit, maxRayDistance, interactMask, QueryTriggerInteraction.Ignore);

        IHoverHighlight newHighlight = null;
        IInteractable newInteract = null;

        if (hitSomething)
        {
            // Use GetComponentInParent<T>() instead of non-existent TryGetComponentInParent
            newHighlight = hit.collider.GetComponentInParent<IHoverHighlight>();
            newInteract = hit.collider.GetComponentInParent<IInteractable>();
        }

        // --- 2) Swap hover highlight if target changed ---
        if (!ReferenceEquals(newHighlight, _currentHighlight))
        {
            _currentHighlight?.OnHoverExit();
            _currentHighlight = newHighlight;
            _currentHighlight?.OnHoverEnter();
        }
        _currentInteractable = newInteract;

        // --- 3) Confirm interaction (click or E) ---
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb[Key.T].wasPressedThisFrame) pressed = true;
        if (allowMouseClick && mouse != null && mouse.rightButton.wasPressedThisFrame) pressed = true;
#else
        if (Input.GetKeyDown(KeyCode.T)) pressed = true;
        if (allowMouseClick && Input.GetMouseButtonDown(1)) pressed = true; // 1 = right click
#endif


        if (pressed && _currentInteractable != null)
        {
            float dist = Vector3.Distance(transform.position, _currentInteractable.Transform.position);
            if (dist <= _currentInteractable.MaxUseDistance)
            {
                _currentInteractable.Interact(gameObject);
            }
            else
            {
                // Optional: show "Too far" feedback
                // Debug.Log("Too far to interact.");
            }
        }
    }

    void OnDisable()
    {
        _currentHighlight?.OnHoverExit();
        _currentHighlight = null;
        _currentInteractable = null;
    }
}
