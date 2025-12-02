//Assets\Scripts\UI\Widgets\Tooltips\HideTooltipsOnPanelClose.cs
using UnityEngine;

/// <summary>
/// Attach this to any panel root (InventoryPanel, CharacterStatPanel, etc.).
/// Whenever that panel is disabled/closed, it tells the TooltipCompareOrchestrator
/// to hide all item tooltips so nothing floats on screen.
/// </summary>
[DisallowMultipleComponent]
public class HideTooltipsOnPanelClose : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TooltipCompareOrchestrator orchestrator;

    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false;
    private string Tag => "[HideTooltipsOnPanelClose]";

    private void Awake()
    {
        if (!orchestrator)
        {
#if UNITY_2023_1_OR_NEWER
            orchestrator = FindFirstObjectByType<TooltipCompareOrchestrator>(FindObjectsInactive.Include);
#else
            orchestrator = Object.FindObjectOfType<TooltipCompareOrchestrator>(true);
#endif
            if (enableLogs)
            {
                Debug.Log($"{Tag} Auto-wired orchestrator = {orchestrator}", this);
            }
        }
    }

    private void OnDisable()
    {
        if (!orchestrator) return;

        if (enableLogs)
        {
            Debug.Log($"{Tag} Panel disabled → HideBoth()", this);
        }

        orchestrator.HideBoth();
    }

    private void OnDestroy()
    {
        // Safety: if the panel is destroyed while open, also hide.
        if (!orchestrator) return;

        if (enableLogs)
        {
            Debug.Log($"{Tag} Panel destroyed → HideBoth()", this);
        }

        orchestrator.HideBoth();
    }
}
