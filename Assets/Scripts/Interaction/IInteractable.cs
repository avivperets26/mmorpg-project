using UnityEngine;

public interface IInteractable
{
    /// Transform used to measure distance from the interactor (usually the root).
    Transform Transform { get; }

    /// Max distance allowed for interaction (world units).
    float MaxUseDistance { get; }

    /// Called when the player interacts (click / key).
    void Interact(GameObject interactor);
}
