using UnityEngine;
using Game.Items;

public enum GemEffectType
{
    // examples—expand later
    CritDamage, OverpowerDamage, BasicSkillDamage, Armor, Thorns,
    DamageReductionWhileFortified, ResistFire, ResistLightning, ResistAll
}

[CreateAssetMenu(menuName = "MMO/Gem Definition", fileName = "GemDefinition")]
public class GemDefinition : ScriptableObject
{
    public string displayName;
    public SocketSlotType allowedSlot;
    public GemEffectType effectType;
    public float value; // magnitude; interpret by effect type

    public string GetSummary()
    {
        return effectType switch
        {
            GemEffectType.CritDamage => $"+{Mathf.RoundToInt(value)}% Critical Strike Damage",
            GemEffectType.OverpowerDamage => $"+{Mathf.RoundToInt(value)}% Overpower Damage",
            GemEffectType.BasicSkillDamage => $"+{Mathf.RoundToInt(value)}% Basic Skill Damage",
            GemEffectType.Armor => $"+{Mathf.RoundToInt(value)} Armor",
            GemEffectType.Thorns => $"+{Mathf.RoundToInt(value)} Thorns",
            GemEffectType.DamageReductionWhileFortified => $"-{Mathf.RoundToInt(value)}% Damage while Fortified",
            GemEffectType.ResistFire => $"+{Mathf.RoundToInt(value)}% Fire Resist",
            GemEffectType.ResistLightning => $"+{Mathf.RoundToInt(value)}% Lightning Resist",
            GemEffectType.ResistAll => $"+{Mathf.RoundToInt(value)}% All Resistances",
            _ => $"{effectType} {value}"
        };
    }
}
