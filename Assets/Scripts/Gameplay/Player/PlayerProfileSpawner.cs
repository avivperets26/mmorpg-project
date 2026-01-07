using TMPro;
using UnityEngine;
using Game.CharacterCreator;
using Game.Equipment;

public class PlayerProfileSpawner : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private bool disableKnightModelWhenProfileLoaded = true;

    [Header("Optional References")]
    [SerializeField] private Transform knightRootOverride;
    [SerializeField] private GameObject knightModelOverride;
    [SerializeField] private EquipmentVisualsController equipmentVisualsOverride;
    [SerializeField] private PlayerStats playerStatsOverride;
    [SerializeField] private PlayerWallet playerWalletOverride;
    [SerializeField] private PlayerInventory playerInventoryOverride;

    private void Start()
    {
        if (PlayerProfileStore.TryGetActive(out var profile))
        {
            ApplyProfile(profile);
        }
    }

    private void ApplyProfile(PlayerCharacterProfile profile)
    {
        if (profile == null || profile.customizationBytes == null || profile.customizationBytes.Length == 0)
        {
            Debug.LogWarning("[PlayerProfileSpawner] Missing customization data; keeping default player.");
            return;
        }

        if (CharacterManager.instance == null)
        {
            var managerGo = new GameObject("CharacterManager_Runtime");
            managerGo.AddComponent<CharacterManager>();
        }

        var knightRoot = ResolveKnightRoot();
        if (!knightRoot)
        {
            Debug.LogError("[PlayerProfileSpawner] Knight root not found; cannot apply profile.");
            return;
        }

        if (disableKnightModelWhenProfileLoaded)
            DisableKnightModel(knightRoot);

        var entity = knightRoot.GetComponent<CharacterEntity>();
        if (!entity)
            entity = knightRoot.gameObject.AddComponent<CharacterEntity>();

        var animator = knightRoot.GetComponent<Animator>();
        if (animator != null)
        {
            entity.MaleController = animator.runtimeAnimatorController;
            entity.FemaleController = animator.runtimeAnimatorController;
        }

        entity.AutoInitializeWithDefaultLooking = false;
        entity.LoadFromBytes(profile.customizationBytes);

        if (entity.mCharacterBoneControl != null)
        {
            var rig = CharacterSocketUtility.EnsureSockets(entity.mCharacterBoneControl.transform);
            var visuals = ResolveEquipmentVisuals(knightRoot);
            if (visuals != null)
            {
                visuals.RebindSockets(rig.socketsRoot, rig.avatarRoot, animator);
            }
        }

        ApplyProgression(profile);
        ApplyNameplate(profile);
    }

    private Transform ResolveKnightRoot()
    {
        if (knightRootOverride)
            return knightRootOverride;

        var root = transform.Find("KnightRoot");
        if (root)
            return root;

        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged)
            return tagged.transform;

        return null;
    }

    private void DisableKnightModel(Transform knightRoot)
    {
        if (knightModelOverride)
        {
            knightModelOverride.SetActive(false);
            return;
        }

        var model = knightRoot.Find("KnightModel");
        if (model)
        {
            model.gameObject.SetActive(false);
        }
    }

    private EquipmentVisualsController ResolveEquipmentVisuals(Transform knightRoot)
    {
        if (equipmentVisualsOverride)
            return equipmentVisualsOverride;

        return knightRoot.GetComponent<EquipmentVisualsController>();
    }

    private void ApplyProgression(PlayerCharacterProfile profile)
    {
        var stats = playerStatsOverride ? playerStatsOverride : GetComponent<PlayerStats>();
        if (!stats)
            stats = GetComponentInChildren<PlayerStats>(true);

        if (stats)
            stats.ApplyProgression(profile.progression, fillVitals: true);

        var wallet = playerWalletOverride ? playerWalletOverride : GetComponent<PlayerWallet>();
        if (!wallet)
            wallet = GetComponentInChildren<PlayerWallet>(true);

        if (wallet)
            wallet.SetCoins(profile.progression.coins);

        var inventory = playerInventoryOverride ? playerInventoryOverride : GetComponent<PlayerInventory>();
        if (!inventory)
            inventory = GetComponentInChildren<PlayerInventory>(true);

        if (inventory)
            inventory.ClearAll();
    }

    private void ApplyNameplate(PlayerCharacterProfile profile)
    {
        var nameplate = GameObject.Find("PlayerNameplate");
        if (!nameplate)
            return;

        var text = nameplate.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text)
            text.text = profile.playerName;
    }
}
