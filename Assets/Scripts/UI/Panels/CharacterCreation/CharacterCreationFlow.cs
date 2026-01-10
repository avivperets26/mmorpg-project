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
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.CharacterCreator;
using SoftKitty;
using SoftKittyMcc = SoftKitty.MasterCharacterCreator;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class CharacterCreationFlow : MonoBehaviour
{
    [Header("MCC References")]
    [SerializeField] private CharacterEntity characterEntity;
    [SerializeField] private GameObject customizationRoot; // CanvasRoot/CharacterCustomization

    [Header("Our UI")]
    [SerializeField] private GameObject frontPanelRoot;          // CC_UI/FrontPanel
    [SerializeField] private UnityEngine.UI.InputField nameInput;
    [SerializeField] private Button customizeButton;
    [SerializeField] private Button classSelectButton;
    [SerializeField] private Button resetProfileButton;
    [SerializeField] private GameObject previewRoot;             // Optional root for preview characters
    [SerializeField] private CharacterNameInputValidator nameInputValidator;

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
    [Header("Customizer Lighting")]
    [SerializeField] private bool disableCustomizerShadows = true;
    [SerializeField] private bool forceSunCullingMaskEverything = true;
    [SerializeField, Range(0f, 2f)] private float customizerSunIntensity = 1f;
    [SerializeField] private Light customizerSunLight;
    [SerializeField] private string customizerSunLightName = "Directional Light";
    private bool previewDirectionalCached;
    private float previewDirectionalOriginalIntensity;
    private int previewDirectionalOriginalCullingMask;
    private int previewDirectionalOriginalRenderingMask;
    private readonly Dictionary<Light, LightDefaults> customizerLightDefaults = new Dictionary<Light, LightDefaults>();
    [SerializeField] private bool useUnselectedObjectLayer = true;
    [SerializeField, Range(0, 31)] private int unselectedPreviewLayer = 2;

    [Header("Preview Presets")]
    [SerializeField] private string knightPresetPath = "MccBlueprints/Characters_bytes/knight_preset.bytes";
    [SerializeField] private string magePresetPath = "MccBlueprints/Characters_bytes/mage_preset.bytes";
    [SerializeField] private string elfPresetPath = "MccBlueprints/Characters_bytes/elf_preset.bytes";

    [Header("Settings")]
    [SerializeField] private string worldSceneName = "World";
    [SerializeField] private string loadingSceneName = "Loading";
    [SerializeField] private string loadingScenePath = "Assets/Scenes/Loading.unity";
    [SerializeField] private string transitionScenePath = "Assets/Scenes/Transition.unity";
    [SerializeField] private PlayerClass selectedClass = PlayerClass.Knight;
    [SerializeField] private ClassDescriptionPanel classDescriptionPanel;

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

        TryWireResetProfileButton();
        EnsureClassSelectButton();

        var frontName = frontPanelRoot ? frontPanelRoot.transform.Find("NameInput") : null;
        if (frontName)
            frontName.gameObject.SetActive(false);

        if (!nameInput)
        {
            var customizer = customizationRoot ? customizationRoot.GetComponentInChildren<CharacterCusUI>(true) : null;
            if (customizer && customizer.NameInput)
                nameInput = customizer.NameInput;
        }
        if (nameInput)
        {
            if (!nameInputValidator)
                nameInputValidator = nameInput.GetComponent<CharacterNameInputValidator>();
            if (!nameInputValidator)
                nameInputValidator = nameInput.gameObject.AddComponent<CharacterNameInputValidator>();
            nameInputValidator.BindInputField(nameInput);
            if (!string.IsNullOrEmpty(nameInput.text))
                nameInput.text = string.Empty;
        }

        selectedClass = PlayerClass.Knight;
        CacheEmblems();
        CachePreviews();
        CachePreviewLights();
        CachePreviewDirectionalDefaults();
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

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        EnsureClassSelectButton();

        if (!isActiveAndEnabled)
            return;

        ApplyPreviewLightLayers();
        ApplyUnselectedLayerToSceneLights();
        ApplyPreviewDirectionalLight();
    }

    private void EnsureClassSelectButton()
    {
        if (!customizationRoot)
            return;

        if (!classSelectButton)
        {
            var existing = customizationRoot.transform.Find("Btn_ClassSelect");
            if (existing)
            {
                classSelectButton = existing.GetComponent<Button>();
                SetupClassSelectButton(existing.gameObject);
                return;
            }
        }

        if (classSelectButton)
            return;

        var createButtonTransform = customizationRoot.transform.Find("Btn_CreateCharacter");
        if (!createButtonTransform)
        {
            var buttons = customizationRoot.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] && buttons[i].name == "Btn_CreateCharacter")
                {
                    createButtonTransform = buttons[i].transform;
                    break;
                }
            }
        }

        if (!createButtonTransform)
        {
            Debug.LogWarning("CharacterCreationFlow: Btn_CreateCharacter not found; cannot create Class Select button.");
            return;
        }

#if UNITY_EDITOR
        GameObject newButtonObj;
        if (!Application.isPlaying)
        {
            newButtonObj = PrefabUtility.InstantiatePrefab(createButtonTransform.gameObject, createButtonTransform.parent) as GameObject;
            if (newButtonObj == null)
                newButtonObj = Instantiate(createButtonTransform.gameObject, createButtonTransform.parent);
            Undo.RegisterCreatedObjectUndo(newButtonObj, "Create Class Select Button");
            EditorSceneManager.MarkSceneDirty(newButtonObj.scene);
        }
        else
        {
            newButtonObj = Instantiate(createButtonTransform.gameObject, createButtonTransform.parent);
        }
#else
        var newButtonObj = Instantiate(createButtonTransform.gameObject, createButtonTransform.parent);
#endif
        newButtonObj.name = "Btn_ClassSelect";
        classSelectButton = newButtonObj.GetComponent<Button>();
        SetupClassSelectButton(newButtonObj);
    }

    private void SetupClassSelectButton(GameObject newButtonObj)
    {
        if (!newButtonObj)
            return;

        if (classSelectButton)
        {
            classSelectButton.onClick = new Button.ButtonClickedEvent();
            classSelectButton.onClick.AddListener(BackToClassSelect);
        }

        var rect = newButtonObj.GetComponent<RectTransform>();
        if (rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(14.8f, 12.6f);
        }

        var textTransform = newButtonObj.transform.Find("Text");
        if (textTransform)
        {
            var text = textTransform.GetComponent<Text>();
            if (text)
                text.text = "Class Select";

            var textRect = textTransform.GetComponent<RectTransform>();
            if (textRect)
            {
                var pos = textRect.anchoredPosition;
                textRect.anchoredPosition = new Vector2(18f, pos.y);
            }
        }

        var iconTransform = newButtonObj.transform.Find("Icon") as RectTransform;
        if (iconTransform)
        {
            iconTransform.anchoredPosition = new Vector2(-60f, 0f);
            var scale = iconTransform.localScale;
            iconTransform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
        }

        var hover = newButtonObj.GetComponent<HoverEffect>();
        if (hover)
        {
            hover._hoverPosition = new Vector2(-80f, 0f);
            hover._hoverScale = new Vector3(-1.1f, 1.1f, 1f);
        }

        var hint = newButtonObj.GetComponent<SoftKitty.HintText>();
        if (hint)
            hint.HintString = "Class Select";
    }

    private Button FindClassSelectButton()
    {
        if (classSelectButton)
            return classSelectButton;
        if (!customizationRoot)
            return null;

        var existing = customizationRoot.transform.Find("Btn_ClassSelect");
        if (existing)
            return existing.GetComponent<Button>();

        var buttons = customizationRoot.GetComponentsInChildren<Button>(true);
        for (var i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] && buttons[i].name == "Btn_ClassSelect")
                return buttons[i];
        }

        return null;
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
            elfPreviewEntityMcc = elfPreviewEntity == null
                ? elfPreview.GetComponent<SoftKittyMcc.CharacterEntity>()
                : null;
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

    private void CachePreviewDirectionalDefaults()
    {
        if (!previewDirectionalLight || previewDirectionalCached)
            return;

        previewDirectionalOriginalIntensity = previewDirectionalLight.intensity;
        previewDirectionalOriginalCullingMask = previewDirectionalLight.cullingMask;
        previewDirectionalOriginalRenderingMask = previewDirectionalLight.renderingLayerMask;
        previewDirectionalCached = true;
    }

    private void CacheCustomizerLightDefaults()
    {
        if (customizerLightDefaults.Count > 0)
            return;

        foreach (var light in EnumerateCustomizerDirectionalLights())
        {
            if (!light)
                continue;
            if (customizerLightDefaults.ContainsKey(light))
                continue;

            customizerLightDefaults.Add(light, new LightDefaults
            {
                Intensity = light.intensity,
                CullingMask = light.cullingMask,
                RenderingLayerMask = light.renderingLayerMask
            });
        }
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

        if (customizerSunLight)
            RemoveLightLayer(customizerSunLight, mask);
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

    private void RestoreSceneLightingForCustomizer()
    {
        var mask = 1u << previewLightingLayer;
        var unselectedMask = 1 << unselectedPreviewLayer;
        var sceneLights = GetSceneLightsExcludingPreviews();
        for (var i = 0; i < sceneLights.Length; i++)
        {
            var light = sceneLights[i];
            if (!light)
                continue;
            light.renderingLayerMask |= (int)mask;
            light.cullingMask |= unselectedMask;
        }

        ApplyCustomizerSunLighting(mask, unselectedMask);

        if (previewDirectionalLight)
        {
            previewDirectionalLight.intensity = customizerSunIntensity;
            previewDirectionalLight.cullingMask = forceSunCullingMaskEverything ? ~0 : previewDirectionalOriginalCullingMask;
            previewDirectionalLight.renderingLayerMask = forceSunCullingMaskEverything ? -1 : previewDirectionalOriginalRenderingMask;
        }
    }

    private void ApplyCustomizerShadowSettings(CharacterCusUI customizer)
    {
        if (!disableCustomizerShadows || customizer == null)
            return;

        var character = customizer.MyCharacter;
        if (!character)
            return;

        var renderers = character.GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (!renderer)
                continue;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
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
        if (elfPreviewEntity == null)
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
        if (elfPreviewEntity != null)
            ApplyPreviewPreset(elfPreviewEntity, elfPresetPath, "Elf");
        else
            ApplyPreviewPreset(elfPreviewEntityMcc, elfPresetPath, "Elf");
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

        EnsureClassSelectButton();
        classSelectButton = FindClassSelectButton();
        if (classSelectButton)
            SetupClassSelectButton(classSelectButton.gameObject);
        else
            Debug.LogWarning("CharacterCreationFlow: Btn_ClassSelect not found; back button will not work.");

        RestoreSceneLightingForCustomizer();

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

        var appearance = BuildCustomizerAppearance();
        CharacterCusUI.InitialData = appearance.Copy();
        if (characterEntity)
            characterEntity.mCharacterAppearance = appearance.Copy();
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

        ApplyCustomizerShadowSettings(customizer);

        return true;
    }

    private CharacterAppearance BuildCustomizerAppearance()
    {
        var targetSex = selectedClass == PlayerClass.Elf ? Sex.Female : Sex.Male;
        var appearance = new CharacterAppearance(CharacterData.Create((byte)targetSex));
        NormalizeAppearance(appearance);
        return appearance;
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
        RestoreSunDefaults();
        ApplyPreviewLightLayers();
        ApplyUnselectedLayerToSceneLights();
        ApplyPreviewDirectionalLight();
    }

    public void BackToClassSelect()
    {
        if (!frontPanelRoot || !customizationRoot)
        {
            Debug.LogError("CharacterCreationFlow: UI roots not assigned; cannot return to class select.");
            return;
        }

        var customizer = customizationRoot.GetComponentInChildren<CharacterCusUI>(true);
        if (customizer)
        {
            customizer.StopAllCoroutines();
            customizer.Initialized = false;
            var canvasGroup = customizer.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        if (CharacterManager.instance != null)
        {
            CharacterManager.instance.RemovePreviewCharacter("cc_Male");
            CharacterManager.instance.RemovePreviewCharacter("cc_Female");
        }

        customizationRoot.SetActive(false);
        frontPanelRoot.SetActive(true);
        SetPreviewVisible(true);
        RestoreSunDefaults();
        ApplyPreviewLightLayers();
        ApplyUnselectedLayerToSceneLights();
        ApplyPreviewDirectionalLight();
        ApplyPreviewPresets();
        RefreshPreviewCaches();
        ApplyClassHighlight();
        RefreshClassDescription();
    }

    private void RestoreSunDefaults()
    {
        if (customizerLightDefaults.Count == 0)
            return;

        foreach (var kvp in customizerLightDefaults)
        {
            if (!kvp.Key)
                continue;
            kvp.Key.intensity = kvp.Value.Intensity;
            kvp.Key.cullingMask = kvp.Value.CullingMask;
            kvp.Key.renderingLayerMask = kvp.Value.RenderingLayerMask;
        }

        customizerLightDefaults.Clear();
    }

    private void ApplyCustomizerSunLighting(uint previewMask, int unselectedMask)
    {
        CacheCustomizerLightDefaults();

        var appliedCount = 0;
        foreach (var light in EnumerateCustomizerDirectionalLights())
        {
            if (!light)
                continue;

            light.renderingLayerMask |= (int)previewMask;
            light.cullingMask |= unselectedMask;

            if (forceSunCullingMaskEverything)
            {
                light.cullingMask = ~0;
                light.renderingLayerMask = -1;
            }

            light.intensity = customizerSunIntensity;
            appliedCount++;
        }

        if (appliedCount == 0)
            Debug.LogWarning("CharacterCreationFlow: No directional lights found to apply customizer lighting.");
    }

    private IEnumerable<Light> EnumerateCustomizerDirectionalLights()
    {
        if (customizerSunLight)
        {
            yield return customizerSunLight;
            yield break;
        }

        var namedLight = ResolveDirectionalLightByName(customizerSunLightName);
        if (namedLight)
            yield return namedLight;

        var fallbackNames = new[] { "_ENV/Directional Light", "Sun" };
        for (var i = 0; i < fallbackNames.Length; i++)
        {
            if (namedLight && namedLight.name == fallbackNames[i])
                continue;
            var fallback = ResolveDirectionalLightByName(fallbackNames[i]);
            if (fallback)
                yield return fallback;
        }

        var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i];
            if (!light || light.type != LightType.Directional)
                continue;
            if (light == previewDirectionalLight)
                continue;
            yield return light;
        }
    }

    private Light ResolveDirectionalLightByName(string lightName)
    {
        if (string.IsNullOrWhiteSpace(lightName))
            return null;

        var obj = GameObject.Find(lightName);
        if (!obj)
            return null;

        var light = obj.GetComponent<Light>();
        if (!light || light.type != LightType.Directional)
            return null;

        return light;
    }

    private struct LightDefaults
    {
        public float Intensity;
        public int CullingMask;
        public int RenderingLayerMask;
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
        if (nameInputValidator != null)
        {
            if (!nameInputValidator.TryGetValidatedName(out playerName, out var reason))
            {
                Debug.LogError($"CharacterCreationFlow: {reason}");
                return;
            }
        }
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

        byte[] bytes = null;
        if (customizationRoot)
        {
            var customizer = customizationRoot.GetComponentInChildren<CharacterCusUI>(true);
            if (customizer != null && customizer.MyCharacter != null && customizer.MyCharacter.MyData != null)
                bytes = customizer.MyCharacter.MyData.ToBytes(BlurPrintType.AllAppearance);
        }

        if (bytes == null || bytes.Length == 0)
            bytes = characterEntity.GetSaveBytes(BlurPrintType.AllAppearance);
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogError("CharacterCreationFlow: Failed to read customization bytes.");
            return;
        }

        var profile = PlayerCharacterProfile.CreateNew(playerName, selectedClass, bytes);
        PlayerProfileStore.SetActive(profile);

        if (nameInputValidator != null)
            nameInputValidator.RegisterCreatedName(playerName);

        StartTransitionToLoading();
    }

    public void ResetProfile()
    {
        PlayerProfileStore.Clear();
        if (nameInput)
            nameInput.text = string.Empty;
        Debug.Log("CharacterCreationFlow: Profile reset (runtime only).");
    }

    private void TryWireResetProfileButton()
    {
        if (!resetProfileButton)
        {
            var resetTransform = frontPanelRoot ? frontPanelRoot.transform.Find("Btn_ResetProfile") : null;
            if (resetTransform)
                resetProfileButton = resetTransform.GetComponent<Button>();
        }

        if (!resetProfileButton && customizationRoot)
        {
            var resetTransform = customizationRoot.transform.Find("Btn_ResetProfile");
            if (resetTransform)
                resetProfileButton = resetTransform.GetComponent<Button>();
        }

        if (resetProfileButton)
        {
            resetProfileButton.onClick.RemoveListener(ResetProfile);
            resetProfileButton.onClick.AddListener(ResetProfile);
        }
    }

    private void StartTransitionToLoading()
    {
#if UNITY_EDITOR
        if (Application.isEditor && !string.IsNullOrWhiteSpace(loadingScenePath))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                loadingScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
    }
}
