// Assets/Scripts/Interaction/PlayerInteractor.cs
using UnityEngine;
using UnityEngine.InputSystem;

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
        // New Input System mouse click
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
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
                // Optional range gate
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
