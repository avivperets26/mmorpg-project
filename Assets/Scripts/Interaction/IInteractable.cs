using UnityEngine;

public interface IInteractable
{
    /// World-space position used to measure distance (usually the object’s transform).
    Transform Transform { get; }

    /// Max distance the interactor must be within to interact.
    float MaxUseDistance { get; }

    /// Called when the player confirms interaction (click/E). Return true if consumed.
    bool Interact(GameObject interactor);
}
