using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class UIPanel : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Root GameObject of this panel (can be this GO or a child).")]
    [SerializeField] private GameObject root;
    [Tooltip("Optional Close button; auto-found if left empty.")]
    [SerializeField] private Button closeButton;

    [Header("Behaviour")]
    [Tooltip("Panels with the same GroupId are mutually exclusive.")]
    [SerializeField] private string groupId = "MainPanels";
    [SerializeField] private bool startHidden = true;
    [Tooltip("Optional hotkey for toggle (New Input System).")]
    [SerializeField] private Key toggleKey = Key.None;

    [Header("Backend Hooks (no custom controller needed)")]
    public UnityEvent OnOpen;   // e.g., InventoryUI.Open
    public UnityEvent OnClose;  // e.g., InventoryUI.Close or StatAllocationUI.CancelAndCloseUI

    public string GroupId => groupId;
    public bool StartHidden => startHidden;
    public bool IsOpen => root && root.activeSelf;
    public Key ToggleKey => toggleKey;     // <- expose for manager

    [HideInInspector] public bool _startApplied;

    private void Reset()
    {
        root = gameObject;
        TryAutoFindClose();
    }

    private void OnEnable()
    {
        if (!root) root = gameObject;
        if (!closeButton) TryAutoFindClose();
        if (closeButton) closeButton.onClick.AddListener(HandleCloseClicked);
        // ❌ Do NOT register here
    }

    private void OnDisable()
    {
        if (closeButton) closeButton.onClick.RemoveListener(HandleCloseClicked);
        // ❌ Do NOT unregister here
    }

    private void OnDestroy()
    {
        // ✅ Unregister only when truly gone
        if (UIPanelManager.Instance) UIPanelManager.Instance.Unregister(this);
    }

    private void HandleCloseClicked()
    {
        UIPanelManager.Instance.Close(this);
    }

    // Internal open/close used by manager
    public void InternalOpen(bool invokeBackend)
    {
        if (!root) root = gameObject;
        Debug.Log($"[UIPanel] InternalOpen '{name}', invoke={invokeBackend}, BEFORE active={root.activeSelf}");
        root.SetActive(true);
        Debug.Log($"[UIPanel] InternalOpen '{name}', AFTER active={root.activeSelf}");
        if (invokeBackend) OnOpen?.Invoke();
    }

    public void InternalClose(bool invokeBackend)
    {
        if (invokeBackend) OnClose?.Invoke();
        if (!root) root = gameObject;
        root.SetActive(false);
    }
    // Optional helpers
    public void Open() => UIPanelManager.Instance.Open(this);
    public void Close() => UIPanelManager.Instance.Close(this);
    public void Toggle() => UIPanelManager.Instance.Toggle(this);

    private void TryAutoFindClose()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var b in buttons)
        {
            var n = b.name.ToLowerInvariant();
            if (n.Contains("close") || n == "x") { closeButton = b; break; }
        }
    }
}
