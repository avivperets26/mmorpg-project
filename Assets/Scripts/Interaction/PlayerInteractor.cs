using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Casts a ray from the camera through the mouse each frame:
/// - Highlights anything with IHoverHighlight
/// - Interacts with IInteractable on Left Click or key (default: T) if in range
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [Tooltip("Layer(s) that can be interacted with.")]
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private float maxRayDistance = 100f;

    [Header("Controls")]
    [Tooltip("Keyboard key that can trigger interaction (in addition to mouse).")]
    [SerializeField] private Key interactKey = Key.T;
    [SerializeField] private bool allowMouseClick = true;

    private Camera _cam;
    private IHoverHighlight _currentHighlight;
    private IInteractable _currentInteractable;

    private void Awake()
    {
        _cam = Camera.main;
        if (!_cam) Debug.LogWarning("PlayerInteractor: No Main Camera found.");
    }

    private void Update()
    {
        if (_cam == null) return;

        // 0) Don’t interact through UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearHoverIfAny();
            return;
        }

        // 1) Raycast from mouse
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = _cam.ScreenPointToRay(mousePos);

        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, maxRayDistance, interactMask, QueryTriggerInteraction.Ignore);

        IHoverHighlight newHighlight = null;
        IInteractable newInteract = null;

        if (hit)
        {
            newHighlight = hitInfo.collider.GetComponentInParent<IHoverHighlight>();
            newInteract = hitInfo.collider.GetComponentInParent<IInteractable>();
        }

        // 2) Swap hover highlight if changed
        if (!ReferenceEquals(newHighlight, _currentHighlight))
        {
            _currentHighlight?.OnHoverExit();
            _currentHighlight = newHighlight;
            _currentHighlight?.OnHoverEnter();
        }
        _currentInteractable = newInteract;

        // 3) Confirm interaction: Left mouse or key
        bool pressed = false;
        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame) pressed = true;
        if (allowMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) pressed = true;

        if (!pressed || _currentInteractable == null) return;

        float dist = Vector3.Distance(transform.position, _currentInteractable.Transform.position);
        if (dist <= _currentInteractable.MaxUseDistance)
        {
            _currentInteractable.Interact(gameObject);
        }
        // else: optional “too far” feedback
    }

    private void OnDisable()
    {
        ClearHoverIfAny();
        _currentInteractable = null;
    }

    private void ClearHoverIfAny()
    {
        if (_currentHighlight != null)
        {
            _currentHighlight.OnHoverExit();
            _currentHighlight = null;
        }
    }
}
