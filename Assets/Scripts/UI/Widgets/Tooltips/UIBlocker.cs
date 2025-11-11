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
    // ---------------- Logging control ----------------
    [Header("Logging")]
    [Tooltip("If true, this component will print logs (unless Use Global Logging is also true and the global switch is off).")]
    [SerializeField] private bool enableLogs = false;

    [Tooltip("If true, this instance follows the global logging switch set via UIBlocker.SetGlobalLogging().")]
    [SerializeField] private bool useGlobalLogging = false;

    private static bool s_globalLogging = false; // master switch for all UIBlockers

    /// <summary>Enable/disable logs for all UIBlocker instances that use global logging.</summary>
    public static void SetGlobalLogging(bool on) => s_globalLogging = on;

    private bool ShouldLog =>
        useGlobalLogging ? s_globalLogging : enableLogs;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void Log(string msg)
    {
        if (ShouldLog) Debug.Log(msg, this);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogWarn(string msg)
    {
        if (ShouldLog) Debug.LogWarning(msg, this);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogError(string msg)
    {
        if (ShouldLog) Debug.LogError(msg, this);
    }

    // ---------------- Config ----------------
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
        Log("[UIBlocker] Awake → actions cached.");
    }

    private void OnEnable()
    {
        if (pushGuardOnEnable) PushGuard();

        SwitchToUiMap();
        DisableActions(true);
        SetBehavioursEnabled(false);

        if (manageCursor) RememberAndShowCursor();

        Log("[UIBlocker] OnEnable complete.");
    }

    private void OnDisable()
    {
        // Revert in reverse order
        if (manageCursor) RestoreCursor();

        SetBehavioursEnabled(true);
        DisableActions(false);
        SwitchToGameplayMap();

        if (pushGuardOnEnable) PopGuard();

        Log("[UIBlocker] OnDisable complete.");
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
            LogWarn("[UIBlocker] Destroyed while pushed; performed safety revert.");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep the cached actions fresh in editor when fields change
        CacheActions();
    }
#endif

    // ---------- Guard ----------
    private void PushGuard()
    {
        if (_pushed) return;
        UiInputGuard.Push(this);
        _pushed = true;
        Log("[UIBlocker] Pushed UiInputGuard.");
    }

    private void PopGuard()
    {
        if (!_pushed) return;
        UiInputGuard.Pop(this);
        _pushed = false;
        Log("[UIBlocker] Popped UiInputGuard.");
    }

    // ---------- PlayerInput maps ----------
    private void SwitchToUiMap()
    {
        if (!playerInput || string.IsNullOrEmpty(uiActionMap)) return;
        var map = playerInput.actions?.FindActionMap(uiActionMap);
        if (map != null)
        {
            playerInput.SwitchCurrentActionMap(uiActionMap);
            Log($"[UIBlocker] Switched PlayerInput map → '{uiActionMap}'");
        }
        else
        {
            LogWarn($"[UIBlocker] UI action map '{uiActionMap}' not found.");
        }
    }

    private void SwitchToGameplayMap()
    {
        if (!playerInput || string.IsNullOrEmpty(gameplayActionMap)) return;
        var map = playerInput.actions?.FindActionMap(gameplayActionMap);
        if (map != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayActionMap);
            Log($"[UIBlocker] Restored PlayerInput map → '{gameplayActionMap}'");
        }
        else
        {
            LogWarn($"[UIBlocker] Gameplay action map '{gameplayActionMap}' not found.");
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
            else LogWarn($"[UIBlocker] Action '{name}' not found in PlayerInput.");
        }
    }

    private void DisableActions(bool disable)
    {
        if (_cachedActions.Count == 0) return;
        foreach (var act in _cachedActions)
        {
            if (disable) { act.Disable(); Log($"[UIBlocker] Disabled action '{act.name}'"); }
            else { act.Enable(); Log($"[UIBlocker] Enabled action '{act.name}'"); }
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
        Log($"[UIBlocker] Behaviours set enabled = {enabled}");
    }

    // ---------- Cursor ----------
    private void RememberAndShowCursor()
    {
        _cursorPrevVisible = Cursor.visible;
        _cursorPrevLock = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Log("[UIBlocker] Cursor unlocked + visible.");
    }

    private void RestoreCursor()
    {
        Cursor.visible = _cursorPrevVisible;
        Cursor.lockState = _cursorPrevLock;
        Log("[UIBlocker] Cursor restored.");
    }
}
