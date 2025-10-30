// Assets/Scripts/Interaction/IInteractable.cs
using UnityEngine;

public interface IInteractable
{
    Transform Transform { get; }
    float MaxUseDistance { get; }
    void Interact(GameObject interactor);
}
