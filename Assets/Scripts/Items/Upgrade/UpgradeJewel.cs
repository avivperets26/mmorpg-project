using UnityEngine;
using Game.Items;

[CreateAssetMenu(menuName = "MMO/Upgrade Jewel", fileName = "UpgradeJewel")]
public class UpgradeJewel : ScriptableObject
{
    [Range(0, 10)] public int maxUpgrade = 10;
    public float perLevelBonusPercent = 2f; // mirrored in ItemInstance.UpgradeMultiplier

    public bool TryApply(ItemInstance inst)
    {
        if (inst.upgradeLevel >= maxUpgrade) return false;
        inst.upgradeLevel++;
        return true;
    }
}
