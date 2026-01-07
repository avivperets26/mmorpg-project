using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CharacterNameInputValidator : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputField inputField;
    [SerializeField] private RectTransform uiRootOverride;

    [Header("Visuals")]
    [SerializeField] private Text statusLabel;
    [SerializeField] private RectTransform spinnerRoot;
    [SerializeField] private Image spinnerImage;
    [SerializeField] private Sprite spinnerSprite;
    [SerializeField] private RectTransform validationContainer;
    [SerializeField] private bool autoLayout = true;
    [SerializeField] private float spinnerRotationSpeed = 180f;
    [SerializeField] private float spinnerYOffset = -8f;
    [SerializeField] private float labelHeight = 20f;
    [SerializeField] private float labelLeftPadding = 22f;
    [SerializeField] private float labelTopPadding = -2f;
    [SerializeField] private float labelExtraWidth = 80f;
    [SerializeField] private bool syncLabelFont = true;
    [SerializeField] private int labelFontSize = 18;
    [SerializeField] private Color availableColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color unavailableColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    [Header("Timing")]
    [SerializeField] private float debounceSeconds = 1.0f;
    [SerializeField] private float simulatedCheckSeconds = 0.6f;

    [Header("Rules")]
    [SerializeField] private int minLength = 3;
    [SerializeField] private int maxLength = 15;
    [SerializeField] private bool requireStartsWithLetter = true;
    [SerializeField] private bool requireLetter = true;
    [SerializeField] private bool requireNumber;
    [SerializeField] private bool forbidWhitespace = true;
    [SerializeField] private string allowedExtraCharacters = "";

    [Header("Texts")]
    [SerializeField] private string availableText = "Name is available";
    [SerializeField] private string unavailableText = "Name is already taken";
    [SerializeField] private string emptyText = "Name is required";
    [SerializeField] private string lengthText = "Use {min}-{max} characters";
    [SerializeField] private string invalidCharText = "Only letters and numbers";
    [SerializeField] private string invalidStartText = "Must start with a letter";
    [SerializeField] private string requireLetterText = "Must include a letter";
    [SerializeField] private string requireNumberText = "Must include a number";
    [SerializeField] private string whitespaceText = "No spaces allowed";

    [Header("Test Names")]
    [SerializeField]
    private List<string> reservedNames = new List<string>
    {
        "Arthas",
        "Sylvanas",
        "Thrall",
        "Jaina",
        "Illidan",
        "Tyrande",
        "Uther",
        "Varian",
        "Malfurion",
        "Kaelthas"
    };

    private static readonly HashSet<string> CreatedNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private int changeVersion;
    private Coroutine checkRoutine;
    private bool isChecking;
    private string currentNormalizedName = "";
    private bool ignoreNextValueChanged;

    public bool IsNameValid { get; private set; }
    public bool IsNameAvailable { get; private set; }
    public string CurrentName => currentNormalizedName;

    private void OnEnable()
    {
        EnsureInputField();
        EnsureVisuals();
        HookInput();
        ignoreNextValueChanged = true;
        HideStatusAndSpinner();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        QueueEditorRefresh();
#endif
    }

    private void OnDisable()
    {
        UnhookInput();
        StopChecking();
    }

    private void Update()
    {
        if (isChecking && spinnerRoot != null)
            spinnerRoot.Rotate(0f, 0f, -spinnerRotationSpeed * Time.deltaTime);
    }

    public void BindInputField(InputField field)
    {
        if (!field)
            return;

        UnhookInput();
        inputField = field;
        EnsureVisuals();
        HookInput();
        HandleValueChanged(inputField.text);
    }

    public bool TryGetValidatedName(out string name, out string failureReason)
    {
        name = "";
        failureReason = "";

        if (inputField == null)
        {
            failureReason = "Name input is missing.";
            return false;
        }

        if (!TryValidate(inputField.text, out var normalized, out failureReason))
            return false;

        if (!IsAvailable(normalized))
        {
            failureReason = unavailableText;
            return false;
        }

        name = normalized;
        return true;
    }

    public void RegisterCreatedName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        CreatedNames.Add(name.Trim());
    }

    private void EnsureInputField()
    {
        if (!inputField)
            inputField = GetComponent<InputField>();
    }

    private void HookInput()
    {
        if (!inputField)
            return;

        inputField.onValidateInput = ValidateInputChar;
        inputField.onValueChanged.RemoveListener(HandleValueChanged);
        inputField.onValueChanged.AddListener(HandleValueChanged);

        if (maxLength > 0)
            inputField.characterLimit = maxLength;
    }

    private void UnhookInput()
    {
        if (!inputField)
            return;

        inputField.onValueChanged.RemoveListener(HandleValueChanged);
        if (inputField.onValidateInput == ValidateInputChar)
            inputField.onValidateInput = null;
    }

    private char ValidateInputChar(string text, int charIndex, char addedChar)
    {
        if (IsAllowedChar(addedChar))
            return addedChar;
        return '\0';
    }

    private void HandleValueChanged(string rawText)
    {
        if (ignoreNextValueChanged)
        {
            ignoreNextValueChanged = false;
            return;
        }
        changeVersion++;
        StopChecking();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            currentNormalizedName = "";
            IsNameValid = false;
            IsNameAvailable = false;
            HideStatusAndSpinner();
            return;
        }

        StartDebouncedCheck(changeVersion, rawText);
    }

    private void StartDebouncedCheck(int version, string rawText)
    {
        if (checkRoutine != null)
            StopCoroutine(checkRoutine);
        checkRoutine = StartCoroutine(DebouncedCheck(version, rawText));
    }

    private IEnumerator DebouncedCheck(int version, string rawText)
    {
        if (spinnerRoot)
            spinnerRoot.gameObject.SetActive(false);

        if (statusLabel)
            statusLabel.gameObject.SetActive(false);

        yield return new WaitForSeconds(debounceSeconds);
        if (version != changeVersion)
            yield break;

        if (!TryValidate(rawText, out var normalized, out var reason))
        {
            currentNormalizedName = normalized;
            IsNameValid = false;
            IsNameAvailable = false;
            ShowStatus(reason, unavailableColor);
            yield break;
        }

        currentNormalizedName = normalized;
        IsNameValid = true;
        IsNameAvailable = false;

        isChecking = true;
        if (spinnerRoot)
            spinnerRoot.gameObject.SetActive(true);

        yield return new WaitForSeconds(simulatedCheckSeconds);
        if (version != changeVersion)
            yield break;

        isChecking = false;
        if (spinnerRoot)
            spinnerRoot.gameObject.SetActive(false);

        if (IsAvailable(normalized))
        {
            IsNameAvailable = true;
            ShowStatus(availableText, availableColor);
        }
        else
        {
            IsNameAvailable = false;
            ShowStatus(unavailableText, unavailableColor);
        }
    }

    private void StopChecking()
    {
        isChecking = false;
        if (checkRoutine != null)
        {
            StopCoroutine(checkRoutine);
            checkRoutine = null;
        }
        if (spinnerRoot)
            spinnerRoot.gameObject.SetActive(false);
    }

    private void HideStatusAndSpinner()
    {
        if (spinnerRoot)
            spinnerRoot.gameObject.SetActive(false);
        if (statusLabel)
            statusLabel.gameObject.SetActive(false);
    }

    private bool TryValidate(string rawText, out string normalized, out string reason)
    {
        normalized = (rawText ?? string.Empty).Trim();
        reason = string.Empty;

        if (string.IsNullOrEmpty(normalized))
        {
            reason = emptyText;
            return false;
        }

        if (forbidWhitespace && HasWhitespace(rawText))
        {
            reason = whitespaceText;
            return false;
        }

        if (minLength > 0 && (normalized.Length < minLength || normalized.Length > maxLength))
        {
            reason = lengthText.Replace("{min}", minLength.ToString())
                .Replace("{max}", maxLength.ToString());
            return false;
        }

        if (requireStartsWithLetter && normalized.Length > 0 && !char.IsLetter(normalized[0]))
        {
            reason = invalidStartText;
            return false;
        }

        var hasLetter = false;
        var hasNumber = false;
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            if (!IsAllowedChar(c))
            {
                reason = invalidCharText;
                return false;
            }
            if (char.IsLetter(c))
                hasLetter = true;
            if (char.IsDigit(c))
                hasNumber = true;
        }

        if (requireLetter && !hasLetter)
        {
            reason = requireLetterText;
            return false;
        }

        if (requireNumber && !hasNumber)
        {
            reason = requireNumberText;
            return false;
        }

        return true;
    }

    private bool IsAllowedChar(char c)
    {
        if (forbidWhitespace && char.IsWhiteSpace(c))
            return false;
        if (c > 127)
            return false;
        if (char.IsLetterOrDigit(c))
            return true;
        return allowedExtraCharacters.IndexOf(c) >= 0;
    }

    private bool IsAvailable(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
            return false;
        for (var i = 0; i < reservedNames.Count; i++)
        {
            if (string.Equals(reservedNames[i], normalized, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        if (CreatedNames.Contains(normalized))
            return false;
        return true;
    }

    private bool HasWhitespace(string rawText)
    {
        for (var i = 0; i < rawText.Length; i++)
        {
            if (char.IsWhiteSpace(rawText[i]))
                return true;
        }
        return false;
    }

    private void ShowStatus(string message, Color color)
    {
        if (!statusLabel)
            return;
        statusLabel.text = message;
        statusLabel.color = color;
        statusLabel.gameObject.SetActive(true);
    }

    private void EnsureVisuals()
    {
        if (!inputField)
            return;

        var inputRect = inputField.GetComponent<RectTransform>();
        if (!inputRect)
            return;

        var parentRoot = uiRootOverride ? uiRootOverride : inputRect;
        if (!parentRoot)
            parentRoot = inputRect;
        var fontSource = inputField.textComponent;
        if (fontSource == null)
            return;

        if (!validationContainer)
        {
            var existing = inputRect.Find("NameValidationContainer") as RectTransform;
            if (!existing && parentRoot != null && parentRoot != inputRect)
                existing = parentRoot.Find("NameValidationContainer") as RectTransform;
            validationContainer = existing;
        }
        if (!validationContainer)
        {
            var container = new GameObject("NameValidationContainer", typeof(RectTransform));
            validationContainer = container.GetComponent<RectTransform>();
        }
        validationContainer.SetParent(parentRoot, false);
        if (autoLayout)
        {
            CopyRectTransformLayout(inputRect, validationContainer);
            var belowOffset = -(inputRect.rect.height * 0.5f) - (labelHeight * 0.5f) + spinnerYOffset;
            if (parentRoot == inputRect)
            {
                validationContainer.anchoredPosition = inputRect.anchoredPosition + new Vector2(0f, belowOffset);
                validationContainer.sizeDelta = new Vector2(inputRect.sizeDelta.x + labelExtraWidth, labelHeight);
            }
            else
            {
                var worldPos = inputRect.TransformPoint(new Vector3(0f, belowOffset, 0f));
                validationContainer.position = worldPos;
                validationContainer.sizeDelta = new Vector2(inputRect.rect.width + labelExtraWidth, labelHeight);
            }
            var siblingIndex = inputRect.GetSiblingIndex();
            if (parentRoot == inputRect.parent)
                validationContainer.SetSiblingIndex(Mathf.Min(siblingIndex + 1, parentRoot.childCount - 1));
        }
        SetLayerRecursive(validationContainer.gameObject, inputField.gameObject.layer);

        if (!spinnerRoot && validationContainer)
        {
            var existingSpinner = validationContainer.Find("NameValidationSpinner") as RectTransform;
            if (existingSpinner)
                spinnerRoot = existingSpinner;
        }
        if (!spinnerRoot)
        {
            var spinnerObj = new GameObject("NameValidationSpinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spinnerRoot = spinnerObj.GetComponent<RectTransform>();
        }
        spinnerRoot.SetParent(validationContainer, false);
        if (autoLayout)
        {
            spinnerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerRoot.pivot = new Vector2(0.5f, 0.5f);
            spinnerRoot.anchoredPosition = Vector2.zero;
            spinnerRoot.sizeDelta = new Vector2(20f, 20f);
        }

        if (!spinnerImage)
            spinnerImage = spinnerRoot.GetComponent<Image>();
        var resolvedSpinner = spinnerSprite;
        if (resolvedSpinner == null && spinnerImage.sprite != null)
            resolvedSpinner = spinnerImage.sprite;
        if (resolvedSpinner == null && inputField.targetGraphic is Image targetImage)
            resolvedSpinner = targetImage.sprite;
        if (resolvedSpinner == null)
            resolvedSpinner = GetDefaultSpinnerSprite();
        spinnerImage.sprite = resolvedSpinner;
        spinnerImage.color = fontSource.color;
        spinnerImage.enabled = resolvedSpinner != null;
        spinnerImage.preserveAspect = true;
        spinnerRoot.gameObject.SetActive(false);

        RectTransform labelRect;
        if (!statusLabel && validationContainer)
        {
            var existingLabel = validationContainer.Find("NameValidationStatus");
            if (existingLabel)
                statusLabel = existingLabel.GetComponent<Text>();
        }
        if (!statusLabel)
        {
            var labelObj = new GameObject("NameValidationStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusLabel = labelObj.GetComponent<Text>();
        }
        labelRect = statusLabel.GetComponent<RectTransform>();
        labelRect.SetParent(validationContainer, false);
        if (autoLayout)
        {
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            labelRect.offsetMin = new Vector2(labelLeftPadding, 0f);
            labelRect.offsetMax = new Vector2(0f, -labelTopPadding);
        }

        if (syncLabelFont)
        {
            statusLabel.font = fontSource.font;
            statusLabel.fontSize = fontSource.fontSize;
            statusLabel.fontStyle = fontSource.fontStyle;
        }
        else
        {
            statusLabel.fontSize = labelFontSize;
        }
        statusLabel.alignment = TextAnchor.MiddleCenter;
        statusLabel.color = unavailableColor;
        statusLabel.raycastTarget = false;
        statusLabel.gameObject.SetActive(false);
    }

    private static RectTransform FindNonMaskedParent(RectTransform start)
    {
        if (!start)
            return null;

        var current = start;
        while (current != null)
        {
            if (!HasMask(current))
                return current;
            current = current.parent as RectTransform;
        }

        return start;
    }

    private static bool HasMask(RectTransform rect)
    {
        if (!rect)
            return false;
        return rect.GetComponent<Mask>() != null || rect.GetComponent<RectMask2D>() != null;
    }

    private static void CopyRectTransformLayout(RectTransform source, RectTransform target)
    {
        if (!source || !target)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        if (!obj)
            return;
        obj.layer = layer;
        for (var i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i);
            if (child)
                SetLayerRecursive(child.gameObject, layer);
        }
    }

#if UNITY_EDITOR
    private bool editorRefreshQueued;

    private void QueueEditorRefresh()
    {
        if (editorRefreshQueued)
            return;
        editorRefreshQueued = true;
        var self = this;
        EditorApplication.delayCall += () =>
        {
            if (self == null)
                return;
            self.editorRefreshQueued = false;
            self.EnsureInputField();
            self.EnsureVisuals();
        };
    }
#endif

    private static Sprite defaultSpinnerSprite;

    private static Sprite GetDefaultSpinnerSprite()
    {
        if (defaultSpinnerSprite != null)
            return defaultSpinnerSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSave;
        var fill = new Color(1f, 1f, 1f, 1f);
        var clear = new Color(1f, 1f, 1f, 0f);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var isCutout = x < 5 && y > 10;
                tex.SetPixel(x, y, isCutout ? clear : fill);
            }
        }
        tex.Apply();

        defaultSpinnerSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return defaultSpinnerSprite;
    }
}
