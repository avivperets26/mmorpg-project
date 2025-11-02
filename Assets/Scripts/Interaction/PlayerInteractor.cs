using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public Camera mainCamera;                 // assign your gameplay camera
    public LayerMask interactMask = ~0;       // or a dedicated "Interactable" layer
    public float rayMaxDistance = 100f;

    [Header("Use Distance Check")]
    public bool requireInRange = true;        // if true, also checks IInteractable.MaxUseDistance

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!mainCamera) mainCamera = Camera.main;
    }

    void Update()
    {
        // If any blocking dialog is open, ignore interactions entirely
        if (UiInputGuard.IsBlocked) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // When a blocking dialog is open, we already returned.
            // When no dialog is open, we ALLOW world clicks even if pointer is over HUD.
            // (If you want a specific "blocker" panel to prevent clicks, make THAT dialog push the guard.)
            TryInteractAtMouse();
        }

    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Mouse & touch-safe check
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        return false;
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
