using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Lightweight opt-in logger for UI tooltip flow.</summary>
public static class UITooltipDebug
{
    /// <summary>Master switch. If false, only components with local logging enabled will print.</summary>
    public static bool Global = false;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(bool localEnabled, Object ctx, string tag, string msg)
    {
        if (!Global && !localEnabled) return;
        Debug.Log($"[TT/{tag}] {msg}", ctx);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Warn(bool localEnabled, Object ctx, string tag, string msg)
    {
        if (!Global && !localEnabled) return;
        Debug.LogWarning($"[TT/{tag}] {msg}", ctx);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Error(bool localEnabled, Object ctx, string tag, string msg)
    {
        if (!Global && !localEnabled) return;
        Debug.LogError($"[TT/{tag}] {msg}", ctx);
    }

#if UNITY_EDITOR
    [MenuItem("Tools/Tooltip Debug/Toggle Global %#t")] // Ctrl/Cmd+Shift+T
    private static void ToggleGlobal()
    {
        Global = !Global;
        Debug.Log($"[TT/Global] Debug {(Global ? "ENABLED" : "DISABLED")}");
    }
#endif
}
