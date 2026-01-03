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
    [SerializeField] private GameObject previewRoot;             // CC_PreviewCharacter (front-panel preview)

    [Header("Class Buttons")]
    [SerializeField] private Button knightButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private Button elfButton;

    [Header("Class Emblems")]
    [SerializeField] private ClassEmblemButton knightEmblem;
    [SerializeField] private ClassEmblemButton mageEmblem;
    [SerializeField] private ClassEmblemButton elfEmblem;

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
        ApplyClassHighlight();
        RefreshClassDescription();

        if (!previewRoot)
            previewRoot = GameObject.Find("CC_PreviewCharacter");
    }

    private void Start()
    {
        StartCoroutine(SelectKnightNextFrame());
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

    private void CacheEmblems()
    {
        if (!knightEmblem && knightButton)
            knightEmblem = knightButton.GetComponentInChildren<ClassEmblemButton>(true);
        if (!mageEmblem && mageButton)
            mageEmblem = mageButton.GetComponentInChildren<ClassEmblemButton>(true);
        if (!elfEmblem && elfButton)
            elfEmblem = elfButton.GetComponentInChildren<ClassEmblemButton>(true);
    }

    private void ApplyClassHighlight()
    {
        if (knightEmblem) knightEmblem.SetSelected(selectedClass == PlayerClass.Knight);
        if (mageEmblem) mageEmblem.SetSelected(selectedClass == PlayerClass.Mage);
        if (elfEmblem) elfEmblem.SetSelected(selectedClass == PlayerClass.Elf);
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
