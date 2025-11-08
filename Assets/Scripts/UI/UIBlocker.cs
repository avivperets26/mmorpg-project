using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any UI panel root. When the GameObject is enabled, it:
///  - Pushes UiInputGuard (blocks gameplay interaction globally)
///  - (Optional) switches PlayerInput to a UI action map
///  - (Optional) disables specific actions by name
///  - (Optional) disables listed Behaviours (e.g., Cinemachine input)
///  - (Optional) shows/unlocks cursor
/// On disable/destroy, it reverts all of the above and Pops the guard.
/// </summary>
[DisallowMultipleComponent]
public class UIBlocker : MonoBehaviour
{
    [Header("Guard")]
    [Tooltip("If true, push the UiInputGuard on OnEnable and pop on OnDisable/OnDestroy.")]
    [SerializeField] private bool pushGuardOnEnable = true;

    [Header("Input Switching")]
    [SerializeField] private PlayerInput playerInput;     // drag your Player's PlayerInput (optional)
    [Tooltip("Action map to use while this UI is active (optional).")]
    [SerializeField] private string uiActionMap = "UI";
    [Tooltip("Action map to restore when closing (optional).")]
    [SerializeField] private string gameplayActionMap = "Gameplay";

    [Header("Disable Specific Actions (by name)")]
    [Tooltip("Optional: action names to disable while UI is open (e.g., \"Move\", \"PrimaryAttack\").")]
    [SerializeField] private string[] actionNamesToDisable;

    [Header("Disable Behaviours While Open")]
    [Tooltip("Optional: behaviours to disable while UI is open (e.g., Cinemachine input components).")]
    [SerializeField] private Behaviour[] behavioursToDisable;

    [Header("Cursor Control")]
    [Tooltip("Show cursor and unlock while UI is active; restore previous state on close.")]
    [SerializeField] private bool manageCursor = true;

    // --- runtime ---
    private bool _pushed;
    private bool _cursorPrevVisible;
    private CursorLockMode _cursorPrevLock;

    private readonly List<InputAction> _cachedActions = new();

    private void Awake()
    {
        // Auto-find PlayerInput if not assigned
        if (!playerInput)
        {
#if UNITY_2023_1_OR_NEWER
            playerInput = Object.FindFirstObjectByType<PlayerInput>();
#else
            playerInput = Object.FindObjectOfType<PlayerInput>();
#endif
        }

        CacheActions();
    }

    private void OnEnable()
    {
        if (pushGuardOnEnable) PushGuard();

        SwitchToUiMap();
        DisableActions(true);
        SetBehavioursEnabled(false);

        if (manageCursor) RememberAndShowCursor();
    }

    private void OnDisable()
    {
        // Revert in reverse order
        if (manageCursor) RestoreCursor();

        SetBehavioursEnabled(true);
        DisableActions(false);
        SwitchToGameplayMap();

        if (pushGuardOnEnable) PopGuard();
    }

    private void OnDestroy()
    {
        // Safety: if destroyed while enabled, ensure we pop
        if (_pushed)
        {
            // Revert cursor/behaviours/actions/maps if OnDisable didn't run
            if (manageCursor) RestoreCursor();
            SetBehavioursEnabled(true);
            DisableActions(false);
            SwitchToGameplayMap();

            PopGuard();
        }
    }

    // ---------- Guard ----------
    private void PushGuard()
    {
        if (_pushed) return;
        UiInputGuard.Push(this);
        _pushed = true;
    }

    private void PopGuard()
    {
        if (!_pushed) return;
        UiInputGuard.Pop(this);
        _pushed = false;
    }

    // ---------- PlayerInput maps ----------
    private void SwitchToUiMap()
    {
        if (!playerInput || string.IsNullOrEmpty(uiActionMap)) return;
        var map = playerInput.actions?.FindActionMap(uiActionMap);
        if (map != null)
        {
            playerInput.SwitchCurrentActionMap(uiActionMap);
            Debug.Log($"[UIBlocker] Switched PlayerInput map -> '{uiActionMap}'");
        }
    }

    private void SwitchToGameplayMap()
    {
        if (!playerInput || string.IsNullOrEmpty(gameplayActionMap)) return;
        var map = playerInput.actions?.FindActionMap(gameplayActionMap);
        if (map != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayActionMap);
            Debug.Log($"[UIBlocker] Restored PlayerInput map -> '{gameplayActionMap}'");
        }
    }

    // ---------- Specific actions ----------
    private void CacheActions()
    {
        _cachedActions.Clear();
        if (!playerInput || playerInput.actions == null || actionNamesToDisable == null) return;

        foreach (var name in actionNamesToDisable)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var act = playerInput.actions.FindAction(name);
            if (act != null) _cachedActions.Add(act);
        }
    }

    private void DisableActions(bool disable)
    {
        if (_cachedActions.Count == 0) return;
        foreach (var act in _cachedActions)
        {
            if (disable) { act.Disable(); Debug.Log($"[UIBlocker] Disabled action '{act.name}'"); }
            else { act.Enable(); Debug.Log($"[UIBlocker] Enabled action '{act.name}'"); }
        }
    }

    // ---------- Behaviours ----------
    private void SetBehavioursEnabled(bool enabled)
    {
        if (behavioursToDisable == null) return;
        foreach (var b in behavioursToDisable)
        {
            if (!b) continue;
            b.enabled = enabled;
        }
    }

    // ---------- Cursor ----------
    private void RememberAndShowCursor()
    {
        _cursorPrevVisible = Cursor.visible;
        _cursorPrevLock = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursor()
    {
        Cursor.visible = _cursorPrevVisible;
        Cursor.lockState = _cursorPrevLock;
    }
}
