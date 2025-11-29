// Assets/Scripts/Enemies/Runtime/EnemyTargetInteractable.cs
using System;
using UnityEngine;
using Game.Enemies;

[DisallowMultipleComponent]
public class EnemyTargetInteractable : MonoBehaviour, IInteractable
{
    [Header("Wiring")]
    public EnemyStats stats;
    public EnemyHealth health;

    [Header("Interaction")]
    [Tooltip("Maximum distance from player allowed to select this enemy.")]
    public float maxUseDistance = 30f;

    public static event Action<EnemyTargetInteractable> OnEnemyTargeted;
    public static event Action<EnemyTargetInteractable> OnEnemyHoverChanged;

    private static EnemyTargetInteractable _currentHover;

    public Transform Transform => transform;
    public float MaxUseDistance => maxUseDistance;

    private void Reset()
    {
        if (!stats) stats = GetComponent<EnemyStats>();
        if (!health) health = GetComponent<EnemyHealth>();
    }

    private void Awake()
    {
        if (!stats) stats = GetComponent<EnemyStats>();
        if (!health) health = GetComponent<EnemyHealth>();
    }
    public static void ClearSelection()
    {
        OnEnemyTargeted?.Invoke(null);
        SetHover(null);
    }

    public static void SetHover(EnemyTargetInteractable target)
    {
        if (_currentHover == target) return;
        _currentHover = target;
        OnEnemyHoverChanged?.Invoke(target);
    }

    public void Interact(GameObject interactor)
    {
        // In the future you could pass interactor (player) if needed.
        OnEnemyTargeted?.Invoke(this);
    }
}
