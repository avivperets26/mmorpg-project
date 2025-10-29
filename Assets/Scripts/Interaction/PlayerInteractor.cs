using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera overrideCamera;           // drag Main Camera here (recommended)
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private float maxRayDistance = 100f;

    [Header("Distance / Actor")]
    [Tooltip("Which transform represents the PLAYER position for distance checks (e.g., KnightRoot). Defaults to this.transform.")]
    [SerializeField] private Transform distanceFrom;          // drag KnightRoot here
    [Tooltip("Require the player to be within the interactable's MaxUseDistance.")]
    [SerializeField] private bool requirePlayerDistance = true;
    [Tooltip("Also limit by camera→hit distance (useful to avoid sniping from across the map).")]
    [SerializeField] private bool requireCameraDistance = false;

    [Header("Controls")]
    [SerializeField] private Key interactKey = Key.T;
    [SerializeField] private bool allowMouseClick = true;

    [Header("UI Blocking")]
    [SerializeField] private bool blockWhenOverUI = true;

    private Camera _cam;
    private IHoverHighlight _currentHighlight;
    private IInteractable _currentInteractable;
    private float _lastRayDistance;

    private void Awake()
    {
        _cam = overrideCamera != null ? overrideCamera : Camera.main;
        if (_cam == null) Debug.LogWarning("PlayerInteractor: No camera assigned and Camera.main is null.");
        if (distanceFrom == null) distanceFrom = transform; // fallback
    }

    private void Update()
    {
        if (_cam == null) return;

        // UI block
        if (blockWhenOverUI && IsPointerOverUI(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero))
        {
            ClearHoverIfAny();
            return;
        }

        // Raycast from mouse
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = _cam.ScreenPointToRay(mousePos);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, maxRayDistance, interactMask, QueryTriggerInteraction.Ignore);

        IHoverHighlight newHighlight = null;
        IInteractable newInteract = null;

        if (hit)
        {
            _lastRayDistance = hitInfo.distance; // camera → hit distance
            newHighlight = hitInfo.collider.GetComponentInParent<IHoverHighlight>();
            newInteract = hitInfo.collider.GetComponentInParent<IInteractable>();
        }

        // Hover swap
        if (!ReferenceEquals(newHighlight, _currentHighlight))
        {
            _currentHighlight?.OnHoverExit();
            _currentHighlight = newHighlight;
            _currentHighlight?.OnHoverEnter();
        }
        _currentInteractable = newInteract;

        // Input
        bool pressed = (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
                       || (allowMouseClick && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!pressed) return;

        if (_currentInteractable == null)
        {
            Debug.Log("Clicked, but no IInteractable under cursor (mask/collider?).");
            return;
        }

        // Distance checks
        float limit = _currentInteractable.MaxUseDistance;

        if (requirePlayerDistance)
        {
            float playerDist = Vector3.Distance(distanceFrom.position, _currentInteractable.Transform.position);
            if (playerDist > limit)
            {
                Debug.Log($"Too far (PLAYER). PlayerDist={playerDist:0.00}, Limit={limit:0.00}");
                return;
            }
        }

        if (requireCameraDistance)
        {
            if (_lastRayDistance > limit)
            {
                Debug.Log($"Too far (CAMERA). RayDist={_lastRayDistance:0.00}, Limit={limit:0.00}");
                return;
            }
        }

        _currentInteractable.Interact(gameObject);
        // Debug.Log("Interacted.");
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
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
