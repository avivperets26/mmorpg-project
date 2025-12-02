//Assets\Scripts\UI\Widgets\Tooltips\EquipmentSlotTooltip.cs
using UnityEngine;
using UnityEngine.EventSystems;
using Game.Items;

public class EquipmentSlotTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipCompareOrchestrator orchestrator;
    [SerializeField] private ItemInstance itemInstance;
    [SerializeField] private RectTransform targetOverride;

    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false;
    private string _tag => "EquippedSlot";

    void Reset()
    {
#if UNITY_2023_1_OR_NEWER
        orchestrator = FindFirstObjectByType<TooltipCompareOrchestrator>(FindObjectsInactive.Include);
#else
        orchestrator = Object.FindObjectOfType<TooltipCompareOrchestrator>(true);
#endif
    }

    void Awake()
    {
        if (!orchestrator)
        {
#if UNITY_2023_1_OR_NEWER
        orchestrator = FindFirstObjectByType<TooltipCompareOrchestrator>(FindObjectsInactive.Include);
#else
            orchestrator = Object.FindObjectOfType<TooltipCompareOrchestrator>(true);
#endif
        }
    }


    public void OnPointerEnter(PointerEventData e)
    {
        if (!orchestrator || itemInstance == null || itemInstance.def == null) return;

        UITooltipDebug.Log(enableLogs, this, _tag, $"Enter on '{gameObject.name}' item='{itemInstance.def.displayName}'");
        orchestrator.HideBoth(); // clear any stale inventory tooltip

        var target = targetOverride ? targetOverride : (RectTransform)transform;
        orchestrator.ShowForEquipped(itemInstance, target);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (orchestrator)
        {
            orchestrator.HideBoth();
            UITooltipDebug.Log(enableLogs, this, _tag, "Exit -> HideBoth()");
        }
    }
    public void SetItem(ItemInstance inst) => itemInstance = inst;

}
