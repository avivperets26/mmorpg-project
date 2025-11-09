// Assets/Scripts/Input/UiInputGuard.cs
//#define UI_INPUT_GUARD_LOGS

using System;
using UnityEngine; // RuntimeInitializeOnLoadMethod / RuntimeInitializeLoadType

/// <summary>
/// Tiny global blocker with stacking semantics.
/// Any UI that should *fully* block gameplay should Push on open and Pop on close.
/// </summary>
public static class UiInputGuard
{
    private static int _blockCount;

    public static bool IsBlocked => _blockCount > 0;
    public static int CurrentCount => _blockCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoResetOnSceneLoad() => _blockCount = 0;

    public static void Reset() => _blockCount = 0;

    public static void Push(object owner = null)
    {
        _blockCount++;
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && UI_INPUT_GUARD_LOGS
        Debug.Log($"[UiInputGuard] Push by {owner?.GetType().Name ?? "unknown"} -> count={_blockCount}");
#endif
    }

    public static void Pop(object owner = null)
    {
        if (_blockCount > 0) _blockCount--;
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && UI_INPUT_GUARD_LOGS
        Debug.Log($"[UiInputGuard] Pop by {owner?.GetType().Name ?? "unknown"} -> count={_blockCount}");
#endif
    }

    /// <summary> Optional helper: using (UiInputGuard.BlockScope()) { … } </summary>
    public static IDisposable BlockScope()
    {
        Push();
        return new Scope();
    }

    private readonly struct Scope : IDisposable
    {
        public void Dispose() => Pop();
    }
}
