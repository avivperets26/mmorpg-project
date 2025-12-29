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
                GameObject _newRoot = Instantiate(Resources.Load<GameObject>("CharacterCreator/UI/CanvasRoot"));
                _scaler = _newRoot.GetComponent<CanvasScaler>();
            }
            GameObject _newInstance = Instantiate(Resources.Load<GameObject>("CharacterCreator/UI/ColorPicker"), _scaler.transform);
            _newInstance.transform.localPosition = Vector3.zero;
            _newInstance.transform.localScale = Vector3.one;
            instance = _newInstance.GetComponent<ColorPicker>();
        }

        public void OpenInstance(GameObject _source, Color _oriColor, bool _alpha = true)
        {
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
            ani.Play("colorpicker_open");
            Opened = true;
        }

        public void Open(GameObject _source, Color _oriColor, GameObject msgTarget, bool _alpha = true)
        {
            BackupColor = _oriColor;
            Source = msgTarget;
            ColorSample.color = _oriColor;
            Sliders[0].value = _oriColor.r;
            Sliders[1].value = _oriColor.g;
            Sliders[2].value = _oriColor.b;
            Sliders[3].value = _oriColor.a;
            Sliders[3].gameObject.SetActive(_alpha);
            Root.transform.position = _source.transform.position + new Vector3(75f, 0f, 0f);
            ani.Play("colorpicker_open");
            Opened = true;
        }

        public void Confirm()
        {
            Source.SendMessage("SetColor", ColorSample.color);
            ani.Play("colorpicker_close");
            Opened = false;
        }

        public void Cancel()
        {
            Opened = false;
            if (Root.gameObject.activeSelf)
            {
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
            instance.OpenInstance(_source, _oriColor, _alpha);
            SoundManager.Play2D("Paper");
        }

        public static Color[] GetColorPalette()//Get all colors of the palette
        {
            if (instance == null) CreateInstance();
            Color[] _colors = new Color[instance.ColorSamples.Length];
            for (int i=0;i< _colors.Length;i++) {
                _colors[i] = instance.ColorSamples[i].color;
            }
            return _colors;
        }

    }
}
