// Assets/Scripts/UI/UiCoroutineRunner.cs
using System.Collections;
using UnityEngine;

public class UiCoroutineRunner : MonoBehaviour
{
    private static UiCoroutineRunner _inst;
    public static void Run(IEnumerator routine)
    {
        if (routine == null) return;
        if (_inst == null)
        {
            var go = new GameObject("_UiCoroutineRunner");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<UiCoroutineRunner>();
        }
        _inst.StartCoroutine(routine);
    }
}
