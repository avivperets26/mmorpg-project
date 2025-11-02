// Assets/Scripts/Interaction/PlayerInteractor.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public Camera mainCamera;                 // Assign your gameplay camera
    public LayerMask interactMask = ~0;       // Or a dedicated "Interactable" layer
    public float rayMaxDistance = 100f;

    [Header("Use Distance Check")]
    public bool requireInRange = true;        // If true, also checks IInteractable.MaxUseDistance

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!mainCamera) mainCamera = Camera.main;
    }

    void Update()
    {
        // Hard block when any modal UI is open
        if (UiInputGuard.IsBlocked) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // By design we ALLOW clicks through non-blocking HUD.
            // If you need a UI to block, give that panel a UIBlocker.
            TryInteractAtMouse();
        }
    }

    private void TryInteractAtMouse()
    {
        if (!mainCamera) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        var ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out var hit, rayMaxDistance, interactMask, QueryTriggerInteraction.Collide))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (requireInRange)
                {
                    float dist = Vector3.Distance(transform.position, interactable.Transform.position);
                    if (dist > interactable.MaxUseDistance)
                    {
                        Debug.Log($"Too far to interact ({dist:0.00}m > {interactable.MaxUseDistance}m)");
                        return;
                    }
                }

                interactable.Interact(gameObject);
            }
        }
    }
}
