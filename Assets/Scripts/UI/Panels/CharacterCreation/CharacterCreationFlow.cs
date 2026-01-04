// Assets/Scripts/UI/Panels/CharacterCreation/CharacterCreationFlow.cs
// Wiring:
// - characterEntity: GameObject with CharacterEntity component (the MCC character instance).
// - customizationRoot: root GameObject for MCC UI (e.g., CanvasRoot/CharacterCustomization).
// - frontPanelRoot: root GameObject for our front panel (e.g., CC_UI/FrontPanel).
// - nameInput: InputField for the player name (customizer view).
// - knightButton: Knight button (OnClick -> SelectKnight).
// - mageButton: Mage button (OnClick -> SelectMage) [kept disabled].
// - elfButton: Elf button (OnClick -> SelectElf) [kept disabled].
// - Customize button (if present): OnClick -> OpenCustomizer.
// - Customizer "Create Character" button: OnClick -> CreateCharacter.
// Persistent folder: <persistentDataPath>/MccBlueprints/Characters
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.CharacterCreator;
using SoftKitty;
using SoftKittyMcc = SoftKitty.MasterCharacterCreator;

public class CharacterCreationFlow : MonoBehaviour
{
    public enum PlayerClass { Knight, Mage, Elf }

    [Header("MCC References")]
    [SerializeField] private CharacterEntity characterEntity;
    [SerializeField] private GameObject customizationRoot; // CanvasRoot/CharacterCustomization

    [Header("Our UI")]
    [SerializeField] private GameObject frontPanelRoot;          // CC_UI/FrontPanel
    [SerializeField] private UnityEngine.UI.InputField nameInput;
    [SerializeField] private Button customizeButton;
    [SerializeField] private GameObject previewRoot;             // Optional root for preview characters

    [Header("Class Buttons")]
    [SerializeField] private Button knightButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private Button elfButton;

    [Header("Class Emblems")]
    [SerializeField] private ClassEmblemButton knightEmblem;
    [SerializeField] private ClassEmblemButton mageEmblem;
    [SerializeField] private ClassEmblemButton elfEmblem;

    [Header("Class Previews")]
    [SerializeField] private GameObject knightPreview;
    [SerializeField] private GameObject magePreview;
    [SerializeField] private GameObject elfPreview;
    [SerializeField] private Vector3 unselectedOffset = Vector3.zero;
    [SerializeField] private Vector3 knightUnselectedOffset = new Vector3(-0.45f, 0f, 0.7f);
    [SerializeField] private Vector3 mageUnselectedOffset = new Vector3(-1.3f, 0f, 0.7f);
    [SerializeField] private Vector3 elfUnselectedOffset = new Vector3(0.2f, 0f, 0.7f);

    [Header("Preview Lighting")]
    [SerializeField] private Light knightPreviewLight;
    [SerializeField] private Light magePreviewLight;
    [SerializeField] private Light elfPreviewLight;
    [SerializeField] private Light previewDirectionalLight;
    [SerializeField, Range(0.1f, 2.5f)] private float previewDimLightIntensity = 0.25f;
    [SerializeField, Range(0.1f, 3f)] private float previewSelectedLightIntensity = 1.2f;
    [SerializeField, Range(0f, 2.5f)] private float previewDirectionalDimIntensity = 0.2f;
    [SerializeField] private Vector3 previewLightLocalPosition = new Vector3(0f, 1.6f, 1.2f);
    [SerializeField] private Vector3 previewLightLocalEuler = new Vector3(35f, 180f, 0f);
    [SerializeField] private bool limitPreviewLightingToLayer = true;
    [SerializeField, Range(0, 31)] private int previewLightingLayer = 1;
    [SerializeField] private Light[] excludeFromPreviewLayer;
    [SerializeField] private bool excludeAllSceneLights = true;
    [SerializeField] private bool useUnselectedObjectLayer = true;
    [SerializeField, Range(0, 31)] private int unselectedPreviewLayer = 2;

    [Header("Preview Presets")]
    [SerializeField] private string knightPresetPath = "MccBlueprints/Characters_bytes/knight_preset.bytes";
    [SerializeField] private string magePresetPath = "MccBlueprints/Characters_bytes/mage_preset.bytes";
    [SerializeField] private string elfPresetPath = "MccBlueprints/Characters_bytes/elf_preset.bytes";

    [Header("Settings")]
    [SerializeField] private string worldSceneName = "World";
    [SerializeField] private PlayerClass selectedClass = PlayerClass.Knight;
    [SerializeField] private ClassDescriptionPanel classDescriptionPanel;

    private const string PlayerNameKey = "PlayerName";
    private const string PlayerClassKey = "PlayerClass";
    private const string PlayerPresetPathKey = "PlayerPresetPath";
    private const string SaveFolderRoot = "MccBlueprints";
    private const string SaveFolderCharacters = "Characters";
    private const string SaveFileExtension = ".bytes";
    private const string KnightPreviewName = "CC_PreviewCharacter_Knight";
    private const string MagePreviewName = "CC_PreviewCharacter_Mage";
    private const string ElfPreviewName = "CC_PreviewCharacter_Elf";

    private Renderer[] knightPreviewRenderers;
    private Renderer[] magePreviewRenderers;
    private Renderer[] elfPreviewRenderers;
    private PreviewRendererState[] knightRendererStates;
    private PreviewRendererState[] mageRendererStates;
    private PreviewRendererState[] elfRendererStates;
    private PreviewObjectLayerState[] knightObjectLayers;
    private PreviewObjectLayerState[] mageObjectLayers;
    private PreviewObjectLayerState[] elfObjectLayers;
    private CharacterEntity knightPreviewEntity;
    private CharacterEntity magePreviewEntity;
    private CharacterEntity elfPreviewEntity;
    private SoftKittyMcc.CharacterEntity elfPreviewEntityMcc;
    private Vector3 knightSelectedPosition;
    private Vector3 mageSelectedPosition;
    private Vector3 elfSelectedPosition;

    private string SaveDir => Path.Combine(Application.persistentDataPath, SaveFolderRoot, SaveFolderCharacters);

    private void Awake()
    {
        Debug.Log("CharacterCreationFlow: Awake");
        if (!characterEntity)
            Debug.LogError("CharacterCreationFlow: Missing CharacterEntity reference.");
        if (!frontPanelRoot)
            Debug.LogError("CharacterCreationFlow: Missing frontPanelRoot reference.");
        if (!customizationRoot)
            Debug.LogError("CharacterCreationFlow: Missing customizationRoot reference.");
        // Front panel NameInput is not used in this flow.
        if (!classDescriptionPanel && frontPanelRoot)
            classDescriptionPanel = frontPanelRoot.GetComponentInChildren<ClassDescriptionPanel>(true);

        TryEnsureSaveDir();

        // Start state
        if (frontPanelRoot) frontPanelRoot.SetActive(true);
        if (customizationRoot) customizationRoot.SetActive(false);

        if (knightButton)
        {
            knightButton.onClick.RemoveListener(SelectKnight);
            knightButton.onClick.AddListener(SelectKnight);
        }
        if (mageButton)
        {
            mageButton.onClick.RemoveListener(SelectMage);
            mageButton.onClick.AddListener(SelectMage);
        }
        if (elfButton)
        {
            elfButton.onClick.RemoveListener(SelectElf);
            elfButton.onClick.AddListener(SelectElf);
        }

        if (!customizeButton && frontPanelRoot)
        {
            var customizeTransform = frontPanelRoot.transform.Find("Btn_CustomizeCharacter")
                ?? frontPanelRoot.transform.Find("Btn_Customize");
            if (customizeTransform)
                customizeButton = customizeTransform.GetComponent<Button>();
        }

        if (customizeButton)
        {
            customizeButton.onClick.RemoveListener(OpenCustomizer);
            customizeButton.onClick.AddListener(OpenCustomizer);
        }

        var frontName = frontPanelRoot ? frontPanelRoot.transform.Find("NameInput") : null;
        if (frontName)
            frontName.gameObject.SetActive(false);

        if (!nameInput)
        {
            var customizer = customizationRoot ? customizationRoot.GetComponentInChildren<CharacterCusUI>(true) : null;
            if (customizer && customizer.NameInput)
                nameInput = customizer.NameInput;
        }

        selectedClass = PlayerClass.Knight;
        CacheEmblems();
        CachePreviews();
        CachePreviewLights();
        ApplyPreviewLightLayers();
        ApplyUnselectedLayerToSceneLights();
        ApplyPreviewDirectionalLight();
        ResolveCharacterEntity();
        EnsurePreviewSex();
        ApplyClassHighlight();
        RefreshClassDescription();

        if (!previewRoot)
            previewRoot = GameObject.Find("PreviewCharacters");
    }

    private void Start()
    {
        StartCoroutine(SelectKnightNextFrame());
        StartCoroutine(ApplyPreviewPresetsNextFrame());
    }

    private System.Collections.IEnumerator SelectKnightNextFrame()
    {
        yield return null;
        if (!knightButton)
            yield break;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(knightButton.gameObject);
        ApplyClassHighlight();
        RefreshClassDescription();
    }

    private System.Collections.IEnumerator ApplyPreviewPresetsNextFrame()
    {
        yield return null;
        ApplyPreviewPresets();
        yield return null;
        RefreshPreviewCaches();
        ApplyClassHighlight();
    }

    private void CacheEmblems()
    {
        if (!knightEmblem && knightButton)
            knightEmblem = knightButton.GetComponentInChildren<ClassEmblemButton>(true);
        if (!mageEmblem && mageButton)
            mageEmblem = mageButton.GetComponentInChildren<ClassEmblemButton>(true);
        if (!elfEmblem && elfButton)
            elfEmblem = elfButton.GetComponentInChildren<ClassEmblemButton>(true);
    }

    private void CachePreviews()
    {
        if (!knightPreview)
            knightPreview = GameObject.Find(KnightPreviewName);
        if (!magePreview)
            magePreview = GameObject.Find(MagePreviewName);
        if (!elfPreview)
            elfPreview = GameObject.Find(ElfPreviewName);

        if (knightPreview)
        {
            knightPreviewRenderers = knightPreview.GetComponentsInChildren<Renderer>(true);
            knightRendererStates = CacheRendererStates(knightPreviewRenderers);
            knightObjectLayers = CacheObjectLayers(knightPreview);
            knightPreviewEntity = knightPreview.GetComponent<CharacterEntity>();
            knightSelectedPosition = knightPreview.transform.localPosition;
        }
        if (magePreview)
        {
            magePreviewRenderers = magePreview.GetComponentsInChildren<Renderer>(true);
            mageRendererStates = CacheRendererStates(magePreviewRenderers);
            mageObjectLayers = CacheObjectLayers(magePreview);
            magePreviewEntity = magePreview.GetComponent<CharacterEntity>();
            mageSelectedPosition = magePreview.transform.localPosition;
        }
        if (elfPreview)
        {
            elfPreviewRenderers = elfPreview.GetComponentsInChildren<Renderer>(true);
            elfRendererStates = CacheRendererStates(elfPreviewRenderers);
            elfObjectLayers = CacheObjectLayers(elfPreview);
            elfPreviewEntity = elfPreview.GetComponent<CharacterEntity>();
            elfPreviewEntityMcc = elfPreview.GetComponent<SoftKittyMcc.CharacterEntity>();
            elfSelectedPosition = elfPreview.transform.localPosition;
        }
    }

    private void RefreshPreviewCaches()
    {
        RefreshPreviewCache(knightPreview, ref knightPreviewRenderers, ref knightRendererStates, ref knightObjectLayers);
        RefreshPreviewCache(magePreview, ref magePreviewRenderers, ref mageRendererStates, ref mageObjectLayers);
        RefreshPreviewCache(elfPreview, ref elfPreviewRenderers, ref elfRendererStates, ref elfObjectLayers);
    }

    private void RefreshPreviewCache(
        GameObject preview,
        ref Renderer[] renderers,
        ref PreviewRendererState[] rendererStates,
        ref PreviewObjectLayerState[] objectLayers)
    {
        if (!preview)
            return;

        renderers = preview.GetComponentsInChildren<Renderer>(true);
        rendererStates = CacheRendererStates(renderers);
        objectLayers = CacheObjectLayers(preview);
    }

    private void CachePreviewLights()
    {
        EnsurePreviewLight(knightPreview, ref knightPreviewLight, "PreviewLight_Knight");
        EnsurePreviewLight(magePreview, ref magePreviewLight, "PreviewLight_Mage");
        EnsurePreviewLight(elfPreview, ref elfPreviewLight, "PreviewLight_Elf");
    }

    private void ApplyPreviewLightLayers()
    {
        if (!limitPreviewLightingToLayer)
            return;

        var mask = 1u << previewLightingLayer;
        SetLightRenderingLayer(knightPreviewLight, mask);
        SetLightRenderingLayer(magePreviewLight, mask);
        SetLightRenderingLayer(elfPreviewLight, mask);

        if (excludeAllSceneLights)
        {
            var sceneLights = GetSceneLightsExcludingPreviews();
            for (var i = 0; i < sceneLights.Length; i++)
                RemoveLightLayer(sceneLights[i], mask);
            return;
        }

        if (excludeFromPreviewLayer != null && excludeFromPreviewLayer.Length > 0)
        {
            for (var i = 0; i < excludeFromPreviewLayer.Length; i++)
                RemoveLightLayer(excludeFromPreviewLayer[i], mask);
            return;
        }

        RemoveLightLayer(RenderSettings.sun, mask);
    }

    private void ApplyUnselectedLayerToSceneLights()
    {
        if (!useUnselectedObjectLayer)
            return;

        var mask = 1 << unselectedPreviewLayer;
        var sceneLights = GetSceneLightsExcludingPreviews();
        for (var i = 0; i < sceneLights.Length; i++)
            RemoveLightCullingLayer(sceneLights[i], mask);

        RemoveLightCullingLayer(knightPreviewLight, mask);
        RemoveLightCullingLayer(magePreviewLight, mask);
        RemoveLightCullingLayer(elfPreviewLight, mask);
    }

    private void ApplyPreviewDirectionalLight()
    {
        if (!previewDirectionalLight)
            return;

        previewDirectionalLight.intensity = previewDirectionalDimIntensity;

        if (useUnselectedObjectLayer)
            previewDirectionalLight.cullingMask = 1 << unselectedPreviewLayer;

        if (limitPreviewLightingToLayer)
            previewDirectionalLight.renderingLayerMask = (int)(1u << previewLightingLayer);
    }

    private static void RemoveLightCullingLayer(Light light, int mask)
    {
        if (!light)
            return;

        light.cullingMask &= ~mask;
    }


    private static void SetPreviewRenderingLayer(Renderer[] renderers, uint mask)
    {
        if (renderers == null)
            return;

        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer)
                renderer.renderingLayerMask = mask;
        }
    }

    private static void SetLightRenderingLayer(Light previewLight, uint mask)
    {
        if (previewLight)
            previewLight.renderingLayerMask = (int)mask;
    }

    private static void RemoveLightLayer(Light sceneLight, uint mask)
    {
        if (!sceneLight)
            return;

        sceneLight.renderingLayerMask &= (int)~mask;
    }

    private Light[] GetSceneLightsExcludingPreviews()
    {
#if UNITY_2023_1_OR_NEWER
        var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
        var allLights = FindObjectsOfType<Light>();
#endif
        if (allLights == null || allLights.Length == 0)
            return System.Array.Empty<Light>();

        var result = new System.Collections.Generic.List<Light>(allLights.Length);
        for (var i = 0; i < allLights.Length; i++)
        {
            var light = allLights[i];
            if (!light)
                continue;
            if (light == knightPreviewLight || light == magePreviewLight || light == elfPreviewLight)
                continue;
            if (light == previewDirectionalLight)
                continue;
            result.Add(light);
        }

        return result.ToArray();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        ApplyPreviewLightLayers();
        ApplyUnselectedLayerToSceneLights();
        ApplyPreviewDirectionalLight();
    }

    private void EnsurePreviewLight(GameObject preview, ref Light previewLight, string fallbackName)
    {
        if (!preview)
            return;

        if (!previewLight)
            previewLight = preview.GetComponentInChildren<Light>(true);

        if (!previewLight)
        {
            var lightObject = new GameObject(fallbackName);
            lightObject.transform.SetParent(preview.transform, false);
            previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Spot;
            previewLight.range = 8f;
            previewLight.spotAngle = 80f;
            previewLight.shadows = LightShadows.None;
        }

        previewLight.transform.localPosition = previewLightLocalPosition;
        previewLight.transform.localRotation = Quaternion.Euler(previewLightLocalEuler);
        previewLight.intensity = previewSelectedLightIntensity;
    }

    private void ResolveCharacterEntity()
    {
        if (characterEntity && characterEntity.gameObject.scene.IsValid())
            return;

        if (knightPreviewEntity)
            characterEntity = knightPreviewEntity;
    }

    private void EnsurePreviewSex()
    {
        EnsurePreviewSex(knightPreviewEntity, Sex.Male);
        EnsurePreviewSex(magePreviewEntity, Sex.Male);
        EnsurePreviewSex(elfPreviewEntity, Sex.Female);
        EnsurePreviewSex(elfPreviewEntityMcc, SoftKittyMcc.Sex.Female);
    }

    private static void EnsurePreviewSex(CharacterEntity entity, Sex targetSex)
    {
        if (!entity || entity.sex == targetSex)
            return;

        entity.Initialize(targetSex);
    }

    private static void EnsurePreviewSex(SoftKittyMcc.CharacterEntity entity, SoftKittyMcc.Sex targetSex)
    {
        if (!entity || entity.sex == targetSex)
            return;

        entity.Initialize(targetSex);
    }

    private void ApplyPreviewPresets()
    {
        ApplyPreviewPreset(knightPreviewEntity, knightPresetPath, "Knight");
        ApplyPreviewPreset(magePreviewEntity, magePresetPath, "Mage");
        if (elfPreviewEntityMcc != null)
            ApplyPreviewPreset(elfPreviewEntityMcc, elfPresetPath, "Elf");
        else
            ApplyPreviewPreset(elfPreviewEntity, elfPresetPath, "Elf");
    }

    private static void ApplyPreviewPreset(CharacterEntity entity, string presetPath, string label)
    {
        if (!entity)
            return;
        if (string.IsNullOrWhiteSpace(presetPath))
            return;

        var fullPath = Path.Combine(Application.dataPath, presetPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"CharacterCreationFlow: {label} preset not found at '{fullPath}'.");
            return;
        }

        entity.LoadFromByteFileFromDisk(fullPath);
    }

    private static void ApplyPreviewPreset(SoftKittyMcc.CharacterEntity entity, string presetPath, string label)
    {
        if (!entity)
            return;
        if (string.IsNullOrWhiteSpace(presetPath))
            return;

        var fullPath = Path.Combine(Application.dataPath, presetPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"CharacterCreationFlow: {label} preset not found at '{fullPath}'.");
            return;
        }

        entity.LoadFromByteFileFromDisk(fullPath);
    }

    private void ApplyClassHighlight()
    {
        if (knightEmblem) knightEmblem.SetSelected(selectedClass == PlayerClass.Knight);
        if (mageEmblem) mageEmblem.SetSelected(selectedClass == PlayerClass.Mage);
        if (elfEmblem) elfEmblem.SetSelected(selectedClass == PlayerClass.Elf);
        ApplyPreviewHighlight();
    }

    private void ApplyPreviewHighlight()
    {
        if (limitPreviewLightingToLayer)
        {
            var mask = 1u << previewLightingLayer;
            SetPreviewRenderingLayerForSelection(knightRendererStates, selectedClass == PlayerClass.Knight, mask);
            SetPreviewRenderingLayerForSelection(mageRendererStates, selectedClass == PlayerClass.Mage, mask);
            SetPreviewRenderingLayerForSelection(elfRendererStates, selectedClass == PlayerClass.Elf, mask);
        }
        if (useUnselectedObjectLayer)
        {
            SetPreviewObjectLayerForSelection(knightObjectLayers, selectedClass == PlayerClass.Knight, unselectedPreviewLayer);
            SetPreviewObjectLayerForSelection(mageObjectLayers, selectedClass == PlayerClass.Mage, unselectedPreviewLayer);
            SetPreviewObjectLayerForSelection(elfObjectLayers, selectedClass == PlayerClass.Elf, unselectedPreviewLayer);
        }
        SetPreviewLight(knightPreviewLight, selectedClass == PlayerClass.Knight);
        SetPreviewLight(magePreviewLight, selectedClass == PlayerClass.Mage);
        SetPreviewLight(elfPreviewLight, selectedClass == PlayerClass.Elf);
        SetPreviewPosition(knightPreview, knightSelectedPosition, knightUnselectedOffset, selectedClass == PlayerClass.Knight);
        SetPreviewPosition(magePreview, mageSelectedPosition, mageUnselectedOffset, selectedClass == PlayerClass.Mage);
        SetPreviewPosition(elfPreview, elfSelectedPosition, elfUnselectedOffset, selectedClass == PlayerClass.Elf);
    }

    private void SetPreviewPosition(GameObject preview, Vector3 selectedPosition, Vector3 specificOffset, bool isSelected)
    {
        if (!preview)
            return;

        var offset = specificOffset == Vector3.zero ? unselectedOffset : specificOffset;
        preview.transform.localPosition = isSelected ? selectedPosition : selectedPosition + offset;
    }

    private struct PreviewRendererState
    {
        public Renderer Renderer;
        public uint RenderingLayerMask;
    }

    private struct PreviewObjectLayerState
    {
        public Transform Transform;
        public int OriginalLayer;
    }

    private static PreviewRendererState[] CacheRendererStates(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
            return null;

        var states = new PreviewRendererState[renderers.Length];
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (!renderer)
                continue;

            states[i] = new PreviewRendererState
            {
                Renderer = renderer,
                RenderingLayerMask = renderer.renderingLayerMask
            };
        }

        return states;
    }

    private static PreviewObjectLayerState[] CacheObjectLayers(GameObject preview)
    {
        if (!preview)
            return null;

        var transforms = preview.GetComponentsInChildren<Transform>(true);
        if (transforms == null || transforms.Length == 0)
            return null;

        var states = new PreviewObjectLayerState[transforms.Length];
        for (var i = 0; i < transforms.Length; i++)
        {
            var transform = transforms[i];
            if (!transform)
                continue;

            states[i] = new PreviewObjectLayerState
            {
                Transform = transform,
                OriginalLayer = transform.gameObject.layer
            };
        }

        return states;
    }

    private static void SetPreviewObjectLayerForSelection(PreviewObjectLayerState[] states, bool isSelected, int unselectedLayer)
    {
        if (states == null || states.Length == 0)
            return;

        for (var i = 0; i < states.Length; i++)
        {
            var state = states[i];
            if (!state.Transform)
                continue;

            state.Transform.gameObject.layer = isSelected ? state.OriginalLayer : unselectedLayer;
        }
    }

    private static void SetPreviewRenderingLayerForSelection(PreviewRendererState[] states, bool isSelected, uint unselectedMask)
    {
        if (states == null || states.Length == 0)
            return;

        for (var i = 0; i < states.Length; i++)
        {
            var state = states[i];
            if (!state.Renderer)
                continue;

            state.Renderer.renderingLayerMask = isSelected ? state.RenderingLayerMask : unselectedMask;
        }
    }

    private void SetPreviewLight(Light previewLight, bool isSelected)
    {
        if (!previewLight)
            return;

        previewLight.intensity = isSelected ? previewSelectedLightIntensity : previewDimLightIntensity;
    }

    public void SelectKnight()
    {
        Debug.Log("CharacterCreationFlow: SelectKnight");
        selectedClass = PlayerClass.Knight;
        ApplyClassHighlight();
        if (classDescriptionPanel) classDescriptionPanel.SetKnight();
    }

    public void SelectMage()
    {
        Debug.Log("CharacterCreationFlow: SelectMage");
        selectedClass = PlayerClass.Mage;
        ApplyClassHighlight();
        if (classDescriptionPanel) classDescriptionPanel.SetMage();
    }

    public void SelectElf()
    {
        Debug.Log("CharacterCreationFlow: SelectElf");
        selectedClass = PlayerClass.Elf;
        ApplyClassHighlight();
        if (classDescriptionPanel) classDescriptionPanel.SetElf();
    }

    public void OpenCustomizer()
    {
        Debug.Log("CharacterCreationFlow: OpenCustomizer");
        if (!frontPanelRoot || !customizationRoot)
        {
            Debug.LogError("CharacterCreationFlow: UI roots not assigned; cannot open customizer.");
            return;
        }
        if (!characterEntity)
        {
            Debug.LogError("CharacterCreationFlow: Missing CharacterEntity; cannot open customizer.");
            return;
        }

        Debug.Log($"CharacterCreationFlow: frontPanelRoot active={frontPanelRoot.activeSelf}, customizationRoot active={customizationRoot.activeSelf}");
        if (frontPanelRoot) frontPanelRoot.SetActive(false);
        if (customizationRoot) customizationRoot.SetActive(true);
        Debug.Log($"CharacterCreationFlow: frontPanelRoot active={frontPanelRoot.activeSelf}, customizationRoot active={customizationRoot.activeSelf}");

        if (TryOpenEmbeddedCustomizer())
        {
            SetPreviewVisible(false);
            return;
        }

        // Fallback to MCC transition flow
        characterEntity.CustomizeCharacter();
    }

    private bool TryOpenEmbeddedCustomizer()
    {
        var customizer = customizationRoot.GetComponentInChildren<CharacterCusUI>(true);
        if (customizer == null)
            return false;

        var validationIssues = ValidateCustomizer(customizer);
        if (!string.IsNullOrEmpty(validationIssues))
        {
            Debug.LogError($"CharacterCreationFlow: CharacterCusUI is missing references:\n{validationIssues}");
            if (customizationRoot) customizationRoot.SetActive(false);
            if (frontPanelRoot) frontPanelRoot.SetActive(true);
            return true;
        }

        _ = CharacterDataSetting.instance;
        if (CharacterManager.instance == null)
        {
            Debug.LogError("CharacterCreationFlow: Missing CharacterManager; cannot open embedded customizer.");
            if (customizationRoot) customizationRoot.SetActive(false);
            if (frontPanelRoot) frontPanelRoot.SetActive(true);
            return false;
        }

        if (characterEntity.mCharacterAppearance == null)
            characterEntity.ResetCharacter();

        var appearance = characterEntity.mCharacterAppearance ?? new CharacterAppearance(CharacterData.Create((byte)Sex.Male));
        NormalizeAppearance(appearance);
        CharacterCusUI.InitialData = appearance.Copy();
        CharacterCusUI.SaveRootPath = CharacterManager.instance.BlueprintPath;
        CharacterCusUI.SaveFormat = SaveMethod.PngFile;
        CharacterCusUI.Settings = new CharacterCusSetting()
        {
            AllowCustomOutfit = CharacterManager.instance.AllowOutfitsWhenCustomize,
            AllowNameChange = true,
            AllowSexSwitch = CharacterManager.instance.AllowChangeSexWhenCustomize,
            AllowRaceChange = CharacterManager.instance.AllowChangeRaceWhenCustomize,
            RaceSettingVisible = CharacterManager.instance.RaceSettingVisible,
            BackCategoryVisible = CharacterManager.instance.BackCategoryVisible,
            TailCategoryVisible = CharacterManager.instance.TailCategoryVisible
        };

        if (!customizer.Initialized)
        {
            customizer.StopAllCoroutines();
            customizer.Initialize();
            var canvasGroup = customizer.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            var anim = customizer.GetComponent<Animation>();
            if (anim && anim.GetClip("cc_in") != null)
                anim.Play("cc_in");
            if (Game.CharacterCreator.CameraControl.instance != null)
                Game.CharacterCreator.CameraControl.instance.Initialized = true;
        }

        return true;
    }

    private void SetPreviewVisible(bool isVisible)
    {
        if (previewRoot)
        {
            previewRoot.SetActive(isVisible);
            return;
        }
        if (knightPreview) knightPreview.SetActive(isVisible);
        if (magePreview) magePreview.SetActive(isVisible);
        if (elfPreview) elfPreview.SetActive(isVisible);
        if (characterEntity)
            characterEntity.gameObject.SetActive(isVisible);
    }
    private void RefreshClassDescription()
    {
        if (!classDescriptionPanel) return;

        switch (selectedClass)
        {
            case PlayerClass.Knight:
                classDescriptionPanel.SetKnight();
                break;
            case PlayerClass.Mage:
                classDescriptionPanel.SetMage();
                break;
            case PlayerClass.Elf:
                classDescriptionPanel.SetElf();
                break;
        }
    }

    private static void NormalizeAppearance(CharacterAppearance appearance)
    {
        if (appearance == null)
            return;

        var outfitSlotsCount = System.Enum.GetValues(typeof(OutfitSlots)).Length;
        if (appearance._OutfitID == null || appearance._OutfitID.Length != outfitSlotsCount)
        {
            var old = appearance._OutfitID;
            appearance._OutfitID = new byte[outfitSlotsCount];
            if (old != null)
            {
                for (var i = 0; i < Mathf.Min(old.Length, appearance._OutfitID.Length); i++)
                    appearance._OutfitID[i] = old[i];
            }
        }

        if (appearance._CusColor1 == null || appearance._CusColor1.Length != outfitSlotsCount)
        {
            var old = appearance._CusColor1;
            appearance._CusColor1 = new Uint8Color[outfitSlotsCount];
            for (var i = 0; i < outfitSlotsCount; i++)
                appearance._CusColor1[i] = (old != null && i < old.Length) ? old[i] : Uint8Color.Set(Color.gray);
        }
        if (appearance._CusColor2 == null || appearance._CusColor2.Length != outfitSlotsCount)
        {
            var old = appearance._CusColor2;
            appearance._CusColor2 = new Uint8Color[outfitSlotsCount];
            for (var i = 0; i < outfitSlotsCount; i++)
                appearance._CusColor2[i] = (old != null && i < old.Length) ? old[i] : Uint8Color.Set(Color.gray);
        }
        if (appearance._CusColor3 == null || appearance._CusColor3.Length != outfitSlotsCount)
        {
            var old = appearance._CusColor3;
            appearance._CusColor3 = new Uint8Color[outfitSlotsCount];
            for (var i = 0; i < outfitSlotsCount; i++)
                appearance._CusColor3[i] = (old != null && i < old.Length) ? old[i] : Uint8Color.Set(Color.gray);
        }

        if (appearance._CharacterData == null)
            appearance._CharacterData = CharacterData.Create((byte)Sex.Male);
        if (appearance._CharacterData.DataInt == null || appearance._CharacterData.DataInt.Length != 12)
        {
            var old = appearance._CharacterData.DataInt;
            appearance._CharacterData.DataInt = new byte[12];
            if (old != null)
            {
                for (var i = 0; i < Mathf.Min(old.Length, appearance._CharacterData.DataInt.Length); i++)
                    appearance._CharacterData.DataInt[i] = old[i];
            }
        }
    }

    private static string ValidateCustomizer(CharacterCusUI customizer)
    {
        if (!customizer)
            return "CharacterCusUI reference is null.";

        var issues = new System.Text.StringBuilder();
        if (customizer.MyLeftSliders == null || customizer.MyLeftSliders.Length <= 1)
            issues.AppendLine("- MyLeftSliders is missing or too short (needs at least 2 entries).");
        else
        {
            for (var i = 0; i < customizer.MyLeftSliders.Length; i++)
            {
                if (customizer.MyLeftSliders[i] == null)
                    issues.AppendLine($"- MyLeftSliders[{i}] is not assigned.");
            }
        }

        if (!customizer.NamePanel) issues.AppendLine("- NamePanel is not assigned.");
        if (!customizer.OutfitCategoryPanel) issues.AppendLine("- OutfitCategoryPanel is not assigned.");
        if (!customizer.OutfitSavePanel) issues.AppendLine("- OutfitSavePanel is not assigned.");
        if (!customizer.SexPanel) issues.AppendLine("- SexPanel is not assigned.");
        if (!customizer.RacePanel) issues.AppendLine("- RacePanel is not assigned.");
        if (!customizer.MatScript) issues.AppendLine("- MatScript is not assigned.");
        if (!customizer.NameInput) issues.AppendLine("- NameInput is not assigned.");
        if (customizer.MainBts == null || customizer.MainBts.Length == 0)
            issues.AppendLine("- MainBts is missing.");
        if (customizer.BtSels == null || customizer.BtSels.Length == 0)
            issues.AppendLine("- BtSels is missing.");
        if (customizer.SexSels == null || customizer.SexSels.Length == 0)
            issues.AppendLine("- SexSels is missing.");

        return issues.ToString().Trim();
    }

    // Hook this to the customizer "Done" button
    public void OnCustomizerDone()
    {
        if (!frontPanelRoot || !customizationRoot)
        {
            Debug.LogError("CharacterCreationFlow: UI roots not assigned; cannot return from customizer.");
            return;
        }
        if (customizationRoot) customizationRoot.SetActive(false);
        if (frontPanelRoot) frontPanelRoot.SetActive(true);
        SetPreviewVisible(true);
    }

    public void CreateCharacter()
    {
        if (!characterEntity)
        {
            Debug.LogError("CharacterCreationFlow: Missing CharacterEntity; cannot create character.");
            return;
        }
        if (!nameInput)
        {
            Debug.LogError("CharacterCreationFlow: Missing customizer name input; cannot create character.");
            return;
        }

        var playerName = (nameInput.text ?? "").Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogError("CharacterCreationFlow: Player name is empty.");
            return;
        }
        if (playerName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Debug.LogError("CharacterCreationFlow: Player name contains invalid file name characters.");
            return;
        }
        if (!TryEnsureSaveDir(out var saveDir))
            return;

        var safeFileName = GetUniqueFileName(saveDir, playerName, SaveFileExtension);
        var presetPath = Path.Combine(saveDir, safeFileName + SaveFileExtension);

        try
        {
            // Save CHARACTER ONLY (no outfits)
            characterEntity.SaveByteFileToDisk(presetPath, BlurPrintType.Character);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CharacterCreationFlow: Failed to save preset file. {ex.Message}");
            return;
        }

        if (!File.Exists(presetPath))
        {
            Debug.LogError($"CharacterCreationFlow: Preset file was not created at '{presetPath}'.");
            return;
        }

        // Minimal v1 profile storage
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.SetInt(PlayerClassKey, (int)selectedClass);
        PlayerPrefs.SetString(PlayerPresetPathKey, presetPath);
        PlayerPrefs.Save();

        SceneManager.LoadScene(worldSceneName);
    }

    private bool TryEnsureSaveDir()
    {
        return TryEnsureSaveDir(out _);
    }

    private bool TryEnsureSaveDir(out string saveDir)
    {
        saveDir = SaveDir;
        if (string.IsNullOrWhiteSpace(saveDir))
        {
            Debug.LogError("CharacterCreationFlow: Invalid save directory path.");
            return false;
        }

        try
        {
            Directory.CreateDirectory(saveDir);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CharacterCreationFlow: Failed to create save directory. {ex.Message}");
            return false;
        }
    }

    private static string GetUniqueFileName(string folderPath, string baseName, string extension)
    {
        var candidate = baseName;
        var index = 2;
        while (File.Exists(Path.Combine(folderPath, candidate + extension)))
        {
            candidate = $"{baseName}_{index}";
            index++;
        }
        return candidate;
    }
}
