//Assets\Scripts\UI\Widgets\Tooltips\TooltipCompareOrchestrator.cs
using UnityEngine;
using Game.Items;

public class TooltipCompareOrchestrator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemTooltipUI mainTooltip;    // ItemTooltip_Main
    [SerializeField] private ItemTooltipUI compareTooltip; // ItemTooltip_Compare

    [Header("Debugging")]
    [SerializeField] private bool enableLogs = false;      // DEBUG: per-instance
    private string _tag => "Orchestrator";

    private bool _suppressInventoryCompare = false;
    private EquipmentController _equip;

    void Awake()
    {
#if UNITY_2023_1_OR_NEWER
        _equip ??= FindFirstObjectByType<EquipmentController>(FindObjectsInactive.Include);
#else
        _equip = _equip ?? FindObjectOfType<EquipmentController>(true);
#endif
        UITooltipDebug.Log(enableLogs, this, _tag, "Awake() equip=" + (_equip ? "ok" : "null"));
        HideBoth();
    }

    public void HideBoth()
    {
        _suppressInventoryCompare = false;
        if (mainTooltip) mainTooltip.Hide();
        if (compareTooltip) compareTooltip.Hide();
        UITooltipDebug.Log(enableLogs, this, _tag, "HideBoth() -> suppression OFF, both hidden");
    }

    /// <summary>Show tooltips for an inventory slot (main + optional equipped compare).</summary>
    public void ShowForInventory(ItemInstance invInst, RectTransform target)
    {

        if (target && target.GetComponentInParent<EquipmentSlotUI>() != null)
        {
            ShowForEquipped(invInst, target);
            return;
        }

        UITooltipDebug.Log(enableLogs, this, _tag,
            $"ShowForInventory(inst={(invInst?.def?.displayName ?? "null")}, target={target?.name}) " +
            $"suppress={_suppressInventoryCompare}");

        if (_suppressInventoryCompare)
        {
            // inventory still shows the single main tooltip but NEVER compare
            if (mainTooltip && invInst != null && invInst.def != null)
            {
                var a = mainTooltip.GetComponent<TooltipAnchorBeside>();
                if (a) a.Attach(target);
                mainTooltip.SetContext(TooltipContext.Inventory);
                mainTooltip.ClearInlineComparison();
                mainTooltip.Show(invInst, target);
                UITooltipDebug.Log(enableLogs, this, _tag, "Suppressed compare (equipped hover active) – showed MAIN only");
            }
            return;
        }

        if (!mainTooltip || invInst == null || invInst.def == null)
        {
            UITooltipDebug.Warn(enableLogs, this, _tag, "ShowForInventory() aborted (null args)");
            return;
        }

        // --- compute baseline (equipped) ---
        ItemDefinition[] baselineDefs = null;
        EquipmentSlot resolved = default;
        string note = null;
        var baseline = ItemStatsSnapshot.Zero;

        if (_equip != null && _equip.GetEquippedForComparison(invInst.def, out resolved, out baselineDefs, out note))
            baseline = _equip.GetCombinedStats(baselineDefs);

        if (!baseline.IsAllZero()) mainTooltip.SetInlineComparisonBaseline(baseline);
        else mainTooltip.ClearInlineComparison();

        UITooltipDebug.Log(enableLogs, this, _tag,
            $"Resolved compare: slot={resolved} defs={(baselineDefs == null ? 0 : baselineDefs.Length)} note='{note}'");


        // ----- MAIN (INVENTORY) -----
        var mainAnchor = mainTooltip.GetComponent<TooltipAnchorBeside>();
        if (mainAnchor)
        {
            mainAnchor.hMode = TooltipAnchorBeside.HMode.PreferThenFit;
            mainAnchor.prefer = TooltipAnchorBeside.HPrefer.RightThenLeft;
            mainAnchor.verticalAlign = TooltipAnchorBeside.VAlign.Top;
            mainAnchor.gapX = 10f;
            mainAnchor.gapY = 0f;
            mainAnchor.Attach(target);
        }

        mainTooltip.SetContext(TooltipContext.Inventory);
        if (!baseline.IsAllZero()) mainTooltip.SetInlineComparisonBaseline(baseline);
        else mainTooltip.ClearInlineComparison();
        mainTooltip.Show(invInst, target);

        UITooltipDebug.Log(enableLogs, this, _tag, "MAIN shown (inventory)");

        // ----- COMPARE -----
        if (!compareTooltip) return;

        var equippedDef = (baselineDefs != null && baselineDefs.Length > 0) ? baselineDefs[0] : null;
        if (equippedDef == null) { compareTooltip.Hide(); UITooltipDebug.Log(enableLogs, this, _tag, "COMPARE hidden (no baseline)"); return; }

        var eqInst = ItemInstance.FromDefinition(equippedDef);

        var cmpAnchor = compareTooltip.GetComponent<TooltipAnchorBeside>();
        if (cmpAnchor)
        {
            var mainRt = mainTooltip.transform as RectTransform;
            cmpAnchor.hMode = TooltipAnchorBeside.HMode.PreferThenFit;
            cmpAnchor.prefer = TooltipAnchorBeside.HPrefer.LeftThenRight;
            cmpAnchor.verticalAlign = TooltipAnchorBeside.VAlign.Top;
            cmpAnchor.gapX = 8f;
            cmpAnchor.gapY = 0f;
            cmpAnchor.Attach(mainRt);
        }

        compareTooltip.ClearContext();
        compareTooltip.SetContext(TooltipContext.Equipped);
        compareTooltip.Show(eqInst, mainTooltip.transform as RectTransform);
        UITooltipDebug.Log(enableLogs, this, _tag, "COMPARE shown (equipped snapshot)");
    }

    /// <summary>Show single tooltip for an equipped slot.</summary>
    public void ShowForEquipped(ItemInstance inst, RectTransform target)
    {
        _suppressInventoryCompare = true;
        UITooltipDebug.Log(enableLogs, this, _tag,
            $"ShowForEquipped(inst={(inst?.def?.displayName ?? "null")}, target={target?.name}) -> suppression ON");

        if (compareTooltip) compareTooltip.Hide();
        mainTooltip.SetContext(TooltipContext.Equipped);
        mainTooltip.ClearInlineComparison();

        var a = mainTooltip.GetComponent<TooltipAnchorBeside>();
        if (a) a.Detach();

        mainTooltip.Show(inst, target);
        UITooltipDebug.Log(enableLogs, this, _tag, "EQUIPPED MAIN shown (no compare)");
    }

    public void HideCompareOnly()
    {
        _suppressInventoryCompare = false;
        if (compareTooltip) compareTooltip.Hide();
        UITooltipDebug.Log(enableLogs, this, _tag, "HideCompareOnly() -> suppression OFF, COMPARE hidden");
    }
}
