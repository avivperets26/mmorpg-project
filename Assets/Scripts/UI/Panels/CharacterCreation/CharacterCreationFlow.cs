// Assets/Scripts/UI/Panels/CharacterCreation/CharacterCreationFlow.cs
// Wiring:
// - characterEntity: GameObject with CharacterEntity component (the MCC character instance).
// - customizationRoot: root GameObject for MCC UI (e.g., CanvasRoot/CharacterCustomization).
// - frontPanelRoot: root GameObject for our front panel (e.g., CC_UI/FrontPanel).
// - nameInput: TMP_InputField for the player name.
// - createButton: Create button (OnClick -> CreateCharacter).
// - knightButton: Knight button (OnClick -> SelectKnight).
// - mageButton: Mage button (OnClick -> SelectMage) [kept disabled].
// - elfButton: Elf button (OnClick -> SelectElf) [kept disabled].
// - Customize button (if present): OnClick -> OpenCustomizer.
// - Customizer "Done" button: OnClick -> OnCustomizerDone.
// Persistent folder: <persistentDataPath>/MccBlueprints/Characters
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Game.CharacterCreator;

public class CharacterCreationFlow : MonoBehaviour
{
    public enum PlayerClass { Knight, Mage, Elf }

    [Header("MCC References")]
    [SerializeField] private CharacterEntity characterEntity;
    [SerializeField] private GameObject customizationRoot; // CanvasRoot/CharacterCustomization

    [Header("Our UI")]
    [SerializeField] private GameObject frontPanelRoot;          // CC_UI/FrontPanel
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button createButton;

    [Header("Class Buttons")]
    [SerializeField] private Button knightButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private Button elfButton;

    [Header("Settings")]
    [SerializeField] private string worldSceneName = "World";
    [SerializeField] private PlayerClass selectedClass = PlayerClass.Knight;

    private const string PlayerNameKey = "PlayerName";
    private const string PlayerClassKey = "PlayerClass";
    private const string PlayerPresetPathKey = "PlayerPresetPath";
    private const string SaveFolderRoot = "MccBlueprints";
    private const string SaveFolderCharacters = "Characters";
    private const string SaveFileExtension = ".bytes";

    private string SaveDir => Path.Combine(Application.persistentDataPath, SaveFolderRoot, SaveFolderCharacters);

    private void Awake()
    {
        if (!characterEntity)
            Debug.LogError("CharacterCreationFlow: Missing CharacterEntity reference.");
        if (!frontPanelRoot)
            Debug.LogError("CharacterCreationFlow: Missing frontPanelRoot reference.");
        if (!customizationRoot)
            Debug.LogError("CharacterCreationFlow: Missing customizationRoot reference.");
        if (!nameInput)
            Debug.LogError("CharacterCreationFlow: Missing nameInput reference.");
        if (!createButton)
            Debug.LogError("CharacterCreationFlow: Missing createButton reference.");

        TryEnsureSaveDir();

        // Start state
        if (frontPanelRoot) frontPanelRoot.SetActive(true);
        if (customizationRoot) customizationRoot.SetActive(false);

        // Only Knight for now
        if (mageButton) mageButton.interactable = false;
        if (elfButton) elfButton.interactable = false;

        if (nameInput)
            nameInput.onValueChanged.AddListener(_ => RefreshCreateButton());

        RefreshCreateButton();
    }

    public void SelectKnight()
    {
        selectedClass = PlayerClass.Knight;
        RefreshCreateButton();
    }

    public void SelectMage()
    {
        selectedClass = PlayerClass.Mage;
        RefreshCreateButton();
    }

    public void SelectElf()
    {
        selectedClass = PlayerClass.Elf;
        RefreshCreateButton();
    }

    public void OpenCustomizer()
    {
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

        if (frontPanelRoot) frontPanelRoot.SetActive(false);
        if (customizationRoot) customizationRoot.SetActive(true);

        // Opens MCC customization UI flow
        characterEntity.CustomizeCharacter();
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
            Debug.LogError("CharacterCreationFlow: Missing name input; cannot create character.");
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

    private void RefreshCreateButton()
    {
        if (!createButton) return;

        var hasName = !string.IsNullOrWhiteSpace(nameInput?.text);
        var classOk = selectedClass == PlayerClass.Knight; // only Knight allowed now
        createButton.interactable = hasName && classOk;
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
