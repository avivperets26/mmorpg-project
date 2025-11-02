// Assets/Scripts/Input/UiInputGuard.cs
using System;
using UnityEngine;                // ← needed for RuntimeInitializeOnLoadMethod & RuntimeInitializeLoadType
using UnityEngine.SceneManagement;

public static class UiInputGuard
{
    private static int _blockCount;

    public static bool IsBlocked => _blockCount > 0;
    public static int CurrentCount => _blockCount; // handy for logs

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoResetOnSceneLoad()
    {
        _blockCount = 0;
    }

    public static void Reset() => _blockCount = 0;

    public static void Push(object owner = null)
    {
        _blockCount++;
        UnityEngine.Debug.Log($"[UiInputGuard] Push by {owner?.GetType().Name ?? "unknown"} -> count={_blockCount}");
    }

    public static void Pop(object owner = null)
    {
        if (_blockCount > 0) _blockCount--;
        UnityEngine.Debug.Log($"[UiInputGuard] Pop by {owner?.GetType().Name ?? "unknown"} -> count={_blockCount}");
    }

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
