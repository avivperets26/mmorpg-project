using UnityEngine;
using System.Collections;

public class UiCoroutineRunner : MonoBehaviour
{
    private static UiCoroutineRunner _instance;
    public static void Run(IEnumerator routine)
    {
        if (routine == null) return;
        if (_instance == null)
        {
            var go = new GameObject("~UiCoroutineRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UiCoroutineRunner>();
        }
        _instance.StartCoroutine(routine);
    }
}
