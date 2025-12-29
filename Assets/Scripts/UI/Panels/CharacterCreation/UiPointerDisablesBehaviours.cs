using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UiPointerDisablesBehaviours : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> behavioursToDisable = new List<MonoBehaviour>();

    private readonly HashSet<MonoBehaviour> disabledByUs = new HashSet<MonoBehaviour>();
    private bool pointerOverUi;

    private void Update()
    {
        var shouldDisable = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (shouldDisable == pointerOverUi)
            return;

        pointerOverUi = shouldDisable;
        if (pointerOverUi)
            DisableBehaviours();
        else
            RestoreBehaviours();
    }

    private void OnDisable()
    {
        RestoreBehaviours();
    }

    private void DisableBehaviours()
    {
        if (behavioursToDisable == null)
            return;

        foreach (var behaviour in behavioursToDisable)
        {
            if (!behaviour || !behaviour.enabled)
                continue;

            behaviour.enabled = false;
            disabledByUs.Add(behaviour);
        }
    }

    private void RestoreBehaviours()
    {
        if (disabledByUs.Count == 0)
            return;

        foreach (var behaviour in disabledByUs)
        {
            if (behaviour)
                behaviour.enabled = true;
        }

        disabledByUs.Clear();
    }
}
