using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Game.CharacterCreator
{
    public class ColorPicker : MonoBehaviour
    {
        #region variables
        public static ColorPicker instance;
        public Transform Root;
        public Slider[] Sliders;
        public Image[] ColorSamples;
        public Image ColorSample;
        public Animation ani;
        private GameObject Source;
        private bool Opened = false;
        private Color BackupColor;
        private Color _oldColor;
        #endregion

        #region internal methods
        private void Start()
        {
            instance = this;
        }

        private void Update()
        {
            if (Opened)
            {
                ColorSample.color = new Color(Sliders[0].value, Sliders[1].value, Sliders[2].value, Sliders[3].value);
                if(Source && ColorSample.color!= _oldColor) Source.SendMessage("SetColor", ColorSample.color);
                _oldColor = ColorSample.color;
            }
        }
        private static void CreateInstance()
        {
            CanvasScaler _scaler = Object.FindObjectOfType<CanvasScaler>();
            if (_scaler == null)
            {
                var rootPrefab = Resources.Load<GameObject>("CharacterCreator/UI/CanvasRoot");
                if (rootPrefab == null)
                    rootPrefab = Resources.Load<GameObject>("MasterCharacterCreator/UI/CanvasRoot");
                if (rootPrefab != null)
                {
                    GameObject _newRoot = Instantiate(rootPrefab);
                    _scaler = _newRoot.GetComponent<CanvasScaler>();
                }
                if (_scaler == null)
                {
                    var root = new GameObject("ColorPickerCanvasRoot");
                    var canvas = root.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    root.AddComponent<CanvasScaler>();
                    root.AddComponent<GraphicRaycaster>();
                    _scaler = root.GetComponent<CanvasScaler>();
                }
            }
            var colorPickerPrefab = Resources.Load<GameObject>("CharacterCreator/UI/ColorPicker");
            if (colorPickerPrefab == null)
                colorPickerPrefab = Resources.Load<GameObject>("MasterCharacterCreator/UI/ColorPicker");
            if (colorPickerPrefab == null)
            {
                Debug.LogWarning("ColorPicker: Missing prefab at Resources/CharacterCreator/UI/ColorPicker or Resources/MasterCharacterCreator/UI/ColorPicker. Using fallback picker.");
                CreateFallbackInstance(_scaler);
                return;
            }
            GameObject _newInstance = Instantiate(colorPickerPrefab, _scaler.transform);
            _newInstance.transform.localPosition = Vector3.zero;
            _newInstance.transform.localScale = Vector3.one;
            instance = _newInstance.GetComponent<ColorPicker>();
        }

        private static void CreateFallbackInstance(CanvasScaler scaler)
        {
            var root = new GameObject("ColorPickerFallback");
            root.transform.SetParent(scaler.transform, false);
            var rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 260f);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            var picker = root.AddComponent<ColorPicker>();
            picker.Root = root.transform;
            picker.ani = null;

            var sample = new GameObject("ColorSample");
            sample.transform.SetParent(root.transform, false);
            var sampleRect = sample.AddComponent<RectTransform>();
            sampleRect.sizeDelta = new Vector2(40f, 40f);
            sampleRect.anchorMin = new Vector2(0f, 1f);
            sampleRect.anchorMax = new Vector2(0f, 1f);
            sampleRect.anchoredPosition = new Vector2(10f, -10f);
            picker.ColorSample = sample.AddComponent<Image>();

            picker.Sliders = new Slider[4];
            var sliderStartY = -15f;
            for (int i = 0; i < picker.Sliders.Length; i++)
            {
                var slider = CreateFallbackSlider(root.transform, $"Slider_{i}");
                var sliderRect = slider.GetComponent<RectTransform>();
                sliderRect.anchorMin = new Vector2(0f, 1f);
                sliderRect.anchorMax = new Vector2(1f, 1f);
                sliderRect.anchoredPosition = new Vector2(0f, sliderStartY - (i * 25f));
                sliderRect.sizeDelta = new Vector2(-60f, 18f);
                picker.Sliders[i] = slider;
            }

            var paletteRoot = new GameObject("Palette");
            paletteRoot.transform.SetParent(root.transform, false);
            var paletteRect = paletteRoot.AddComponent<RectTransform>();
            paletteRect.anchorMin = new Vector2(0f, 0f);
            paletteRect.anchorMax = new Vector2(1f, 0f);
            paletteRect.anchoredPosition = new Vector2(0f, 10f);
            paletteRect.sizeDelta = new Vector2(-20f, 120f);
            var grid = paletteRoot.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(18f, 18f);
            grid.spacing = new Vector2(4f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;

            picker.ColorSamples = CreateFallbackPalette(paletteRoot.transform, 50);
            instance = picker;
        }

        private static Slider CreateFallbackSlider(Transform parent, string name)
        {
            var sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 18f);

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.15f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-5f, 0f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 1f, 1f, 0.6f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(sliderObj.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(12f, 12f);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            return slider;
        }

        private static Image[] CreateFallbackPalette(Transform parent, int count)
        {
            var samples = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var sampleObj = new GameObject($"Palette_{i}");
                sampleObj.transform.SetParent(parent, false);
                var image = sampleObj.AddComponent<Image>();
                image.color = Color.HSVToRGB((i / (float)count), 0.5f, 0.9f);
                var button = sampleObj.AddComponent<Button>();
                var index = i;
                button.onClick.AddListener(() => instance.SetSample(samples[index]));
                samples[i] = image;
            }
            return samples;
        }

        public void OpenInstance(GameObject _source, Color _oriColor, bool _alpha = true)
        {
            if (Root == null || Sliders == null || Sliders.Length < 4 || ColorSample == null)
                return;
            BackupColor = _oriColor;
            _oldColor = _oriColor;
            Source = _source;
            ColorSample.color = _oriColor;
            Sliders[0].value = _oriColor.r;
            Sliders[1].value = _oriColor.g;
            Sliders[2].value = _oriColor.b;
            Sliders[3].value = _oriColor.a;
            Sliders[3].gameObject.SetActive(_alpha);
            Root.position = _source.transform.position + new Vector3(75f, 0f, 0f);
            Root.GetComponent<RectTransform>().anchoredPosition = new Vector2(Root.GetComponent<RectTransform>().anchoredPosition.x,
                Mathf.Max(Root.GetComponent<RectTransform>().anchoredPosition.y, -270F));
            if (ani != null)
                ani.Play("colorpicker_open");
            Opened = true;
        }

        public void Open(GameObject _source, Color _oriColor, GameObject msgTarget, bool _alpha = true)
        {
            if (Root == null || Sliders == null || Sliders.Length < 4 || ColorSample == null)
                return;
            BackupColor = _oriColor;
            Source = msgTarget;
            ColorSample.color = _oriColor;
            Sliders[0].value = _oriColor.r;
            Sliders[1].value = _oriColor.g;
            Sliders[2].value = _oriColor.b;
            Sliders[3].value = _oriColor.a;
            Sliders[3].gameObject.SetActive(_alpha);
            Root.transform.position = _source.transform.position + new Vector3(75f, 0f, 0f);
            if (ani != null)
                ani.Play("colorpicker_open");
            Opened = true;
        }

        public void Confirm()
        {
            Source.SendMessage("SetColor", ColorSample.color);
            if (ani != null)
                ani.Play("colorpicker_close");
            Opened = false;
        }

        public void Cancel()
        {
            Opened = false;
            if (Root.gameObject.activeSelf)
            {
                if (ani != null)
                    ani.Play("colorpicker_close");
                if (Source) Source.SendMessage("SetColor", BackupColor);
            }
        }

        public void SetSample(Image _sample)
        {
            Sliders[0].value = _sample.color.r;
            Sliders[1].value = _sample.color.g;
            Sliders[2].value = _sample.color.b;
        }
        #endregion

        public static void Open(GameObject _source, Color _oriColor, bool _alpha = true)//Open the color picker
        {
            if (instance == null) CreateInstance();
            if (instance == null)
            {
                Debug.LogWarning("ColorPicker: Failed to create picker instance.");
                return;
            }
            instance.OpenInstance(_source, _oriColor, _alpha);
            SoundManager.Play2D("Paper");
        }

        public static Color[] GetColorPalette()//Get all colors of the palette
        {
            if (instance == null) CreateInstance();
            if (instance == null || instance.ColorSamples == null || instance.ColorSamples.Length == 0)
            {
                Debug.LogWarning("ColorPicker: Missing color samples; using fallback palette.");
                var fallback = new Color[50];
                for (int i = 0; i < fallback.Length; i++)
                    fallback[i] = Color.HSVToRGB((i / (float)fallback.Length), 0.5f, 0.9f);
                return fallback;
            }
            Color[] _colors = new Color[instance.ColorSamples.Length];
            for (int i=0;i< _colors.Length;i++) {
                _colors[i] = instance.ColorSamples[i].color;
            }
            return _colors;
        }

    }
}
