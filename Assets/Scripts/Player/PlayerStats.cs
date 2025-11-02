using UnityEngine;
using Game.Items;


public class PlayerStats : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base run speed without any gear.")]
    public float baseMoveSpeed = 5f;

    // Multiplicative bonus from boots (or other items)
    private float moveSpeedMultiplier = 1f;

    // --- Combat aggregates from equipped items (additive for MVP) ---
    [Header("Derived (equipment)")]
    public int equipDefense;
    public int equipMagicResist;
    public int equipDamageMin;
    public int equipDamageMax;
    public float equipWizardry;      // for magic builds (if you use it)
    public float equipHpOnKill;
    public float equipManaOnKill;

    public bool BootsEquipped => moveSpeedMultiplier > 1f;

    public float GetEffectiveMoveSpeed() => baseMoveSpeed * moveSpeedMultiplier;

    // ---- Movement from boots (you already had these) ----
    public void EquipBoots(float speedMultiplier)
    {
        moveSpeedMultiplier = Mathf.Max(speedMultiplier, 0.01f);
    }
    public void UnequipBoots()
    {
        moveSpeedMultiplier = 1f;
    }

    // ---- Armor aggregations ----
    public void AddArmor(int defense, int magicResist)
    {
        equipDefense += Mathf.Max(0, defense);
        equipMagicResist += Mathf.Max(0, magicResist);
    }
    public void RemoveArmor(int defense, int magicResist)
    {
        equipDefense = Mathf.Max(0, equipDefense - Mathf.Max(0, defense));
        equipMagicResist = Mathf.Max(0, equipMagicResist - Mathf.Max(0, magicResist));
    }

    // ---- Weapon aggregations (very simple for MVP) ----
    public void AddWeapon(DamageProfile dmg)
    {
        equipDamageMin += Mathf.Max(0, dmg.min);
        equipDamageMax += Mathf.Max(0, dmg.max);
        equipWizardry += Mathf.Max(0, dmg.wizardry);
        // Optionally factor crits & speed elsewhere
    }
    public void RemoveWeapon(DamageProfile dmg)
    {
        equipDamageMin = Mathf.Max(0, equipDamageMin - Mathf.Max(0, dmg.min));
        equipDamageMax = Mathf.Max(0, equipDamageMax - Mathf.Max(0, dmg.max));
        equipWizardry = Mathf.Max(0, equipWizardry - Mathf.Max(0, dmg.wizardry));
    }

    // ---- On-kill bonuses ----
    public void AddOnKill(float hp, float mana)
    {
        equipHpOnKill += Mathf.Max(0f, hp);
        equipManaOnKill += Mathf.Max(0f, mana);
    }
    public void RemoveOnKill(float hp, float mana)
    {
        equipHpOnKill = Mathf.Max(0f, equipHpOnKill - Mathf.Max(0f, hp));
        equipManaOnKill = Mathf.Max(0f, equipManaOnKill - Mathf.Max(0f, mana));
    }
}
