using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-200)]
public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }

    private readonly Dictionary<string, HashSet<UIPanel>> _groups = new();
    private readonly List<UIPanel> _allPanels = new();

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Unity 2022.2+: this finds *scene* objects and can include inactive
#if UNITY_2022_2_OR_NEWER
        var panels = Object.FindObjectsByType<UIPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        foreach (var p in panels)
            if (p.gameObject.scene.IsValid()) Register(p);
#else
        // Fallback: filter out prefab assets (no valid scene)
        var panels = Resources.FindObjectsOfTypeAll<UIPanel>();
        foreach (var p in panels)
            if (p && p.gameObject.scene.IsValid())
                Register(p);
#endif
    }

    public void Register(UIPanel panel)
    {
        if (!panel || string.IsNullOrEmpty(panel.GroupId)) return;

        if (!_groups.TryGetValue(panel.GroupId, out var set))
        {
            set = new HashSet<UIPanel>();
            _groups[panel.GroupId] = set;
        }

        // HashSet prevents dupes
        bool firstTime = set.Add(panel);
        if (!_allPanels.Contains(panel)) _allPanels.Add(panel);

        if (firstTime)
        {
            // Apply start state ONCE, no events
            if (panel.StartHidden) panel.InternalClose(invokeBackend: false);
            else panel.InternalOpen(invokeBackend: false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIPanelManager] Registered '{panel.name}' (group='{panel.GroupId}', startHidden={panel.StartHidden})");
#endif
    }

    public void Unregister(UIPanel panel)
    {
        if (panel == null) return;
        if (_groups.TryGetValue(panel.GroupId, out var set)) set.Remove(panel);
        _allPanels.Remove(panel);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[UIPanelManager] Unregistered '{panel.name}'");
#endif
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        for (int i = 0; i < _allPanels.Count; i++)
        {
            var p = _allPanels[i];
            if (!p) continue;

            var key = p.ToggleKey;
            if (key == Key.None) continue;

            if (kb[key].wasPressedThisFrame)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[UIPanelManager] Key '{key}' pressed -> toggle '{p.name}'");
#endif
                Toggle(p);
            }
        }
    }
    public void Open(UIPanel panel)
    {
        if (panel == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"[UIPanelManager] Open '{panel.name}'\n{Environment.StackTrace}");
#endif

        if (_groups.TryGetValue(panel.GroupId, out var set))
        {
            var snapshot = new List<UIPanel>(set);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var other = snapshot[i];
                if (other && !ReferenceEquals(other, panel) && other.IsOpen)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[UIPanelManager] Close sibling '{other.name}' (group '{panel.GroupId}')");
#endif
                    other.InternalClose(invokeBackend: true);
                }
            }
        }

        panel.InternalOpen(invokeBackend: true);
    }
    public void Close(UIPanel panel)
    {
        if (panel == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"[UIPanelManager] Close '{panel.name}'\n{Environment.StackTrace}");
#endif
        panel.InternalClose(invokeBackend: true);
    }

    public void Toggle(UIPanel panel)
    {
        if (panel == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"[UIPanelManager] Toggle '{panel.name}' (isOpen={panel.IsOpen})\n{Environment.StackTrace}");
#endif
        if (panel.IsOpen) Close(panel);
        else Open(panel);
    }
}
