using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CharacterCreator
{
    public class cc_id_color : MonoBehaviour
    {
        public enum ColorSettingTypes
        {
            Body,
            Outfit,
            Race
        }
        public ColorSettingTypes Type = ColorSettingTypes.Body;
        public int SettingTexture=0;
        public int SettingColor = 0;
        public CharacterCusUI MainScript;
        public Text nameText;
        public Text valueText;
        public Image ColorSample;
        public bool EnableAlpha = true;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            EnsureMainScript();
        }

        private void EnsureMainScript()
        {
            if (!MainScript)
                MainScript = GetComponentInParent<CharacterCusUI>(true);
        }

        private void Update()
        {
            EnsureMainScript();
            if (!MainScript || !MainScript.Initialized || MainScript.MyCharacter == null || MainScript.MyCharacter.MyData == null)
                return;
            if (CharacterDataSetting.instance == null)
                return;
            if (Type == ColorSettingTypes.Race) {
                if (valueText)
                {
                    if (CharacterDataSetting.instance.RaceSettings == null || CharacterDataSetting.instance.RaceSettings.Length == 0)
                        return;
                    int _id = Mathf.Clamp((int)MainScript.MyCharacter.MyData._CharacterData.Race, 0, CharacterDataSetting.instance.RaceSettings.Length - 1);
                    valueText.text = CharacterDataSetting.instance.RaceSettings[_id].RaceName;
                }
            }
            else if (Type == ColorSettingTypes.Body)
            {
                if (CharacterDataSetting.instance.MaleMaterialSettings == null)
                    return;
                if (SettingColor < 0 || SettingColor >= CharacterDataSetting.instance.MaleMaterialSettings.Length)
                    return;
                if (SettingTexture < 0 || SettingTexture >= CharacterDataSetting.instance.MaleMaterialSettings.Length)
                    return;
                if (ColorSample)
                    ColorSample.color = Uint8Color.Get(MainScript.MyCharacter.MyData._CharacterData.DataColor[(int)CharacterDataSetting.instance.MaleMaterialSettings[SettingColor].ColorType]);
                if (valueText)
                    valueText.text = ((int)MainScript.MyCharacter.MyData._CharacterData.DataInt[(int)CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureType]).ToString();
            }
            else
            {
                if (MainScript.GetCurrentOutfitSettingId() > -1 && CharacterCusUI.Settings.AllowCustomOutfit) {
                    if (canvasGroup)
                        canvasGroup.interactable = isEnable();
                    switch (SettingColor)
                    {
                        case 1:
                            if (MainScript.MyCharacter.MyData._CusColor1 != null && MainScript.GetCurrentOutfitSettingId() < MainScript.MyCharacter.MyData._CusColor1.Length)
                                ColorSample.color = Uint8Color.Get(MainScript.MyCharacter.MyData._CusColor1[MainScript.GetCurrentOutfitSettingId()]);
                            break;
                        case 2:
                            if (MainScript.MyCharacter.MyData._CusColor2 != null && MainScript.GetCurrentOutfitSettingId() < MainScript.MyCharacter.MyData._CusColor2.Length)
                                ColorSample.color = Uint8Color.Get(MainScript.MyCharacter.MyData._CusColor2[MainScript.GetCurrentOutfitSettingId()]);
                            break;
                        case 3:
                            if (MainScript.MyCharacter.MyData._CusColor3 != null && MainScript.GetCurrentOutfitSettingId() < MainScript.MyCharacter.MyData._CusColor3.Length)
                                ColorSample.color = Uint8Color.Get(MainScript.MyCharacter.MyData._CusColor3[MainScript.GetCurrentOutfitSettingId()]);
                            break;

                    }
                } else {
                    if (canvasGroup)
                        canvasGroup.interactable = false;
                }
                if (canvasGroup)
                    canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, canvasGroup.interactable ? 1F : 0F, Time.deltaTime * 5F);
            }
            
        }

        private bool isEnable()
        {
            if (MainScript.GetCurrentOutfitSettingId() >= 5) return true;
            OutfitColorSetting _setting = MainScript.GetOutfitSetting();
            switch (SettingColor)
            {
                case 1:
                    return _setting.ToggleCustomColor1;
                case 2:
                    return _setting.ToggleCustomColor2;
                case 3:
                    return _setting.ToggleCustomColor3;
            }
            return false;
        }

        public void OpenColorTab()
        {
            ColorPicker.Open(gameObject, ColorSample.color, EnableAlpha);
        }

        private void SetColor(Color _color)
        {
            if (Type == ColorSettingTypes.Race)
            {
                return;
            }
            else if (Type == ColorSettingTypes.Body)
            {
                MainScript.MyCharacter.MyData._CharacterData.DataColor[(int)CharacterDataSetting.instance.MaleMaterialSettings[SettingColor].ColorType] = Uint8Color.Set(_color);
                MainScript.MyCharacter.UpdateMaterialData();
            }
            else
            {
                switch (SettingColor)
                {
                    case 1:
                        MainScript.MyCharacter.MyData._CusColor1[MainScript.GetCurrentOutfitSettingId()]= Uint8Color.Set(_color);
                        break;
                    case 2:
                        MainScript.MyCharacter.MyData._CusColor2[MainScript.GetCurrentOutfitSettingId()] = Uint8Color.Set(_color);
                        break;
                    case 3:
                        MainScript.MyCharacter.MyData._CusColor3[MainScript.GetCurrentOutfitSettingId()] = Uint8Color.Set(_color);
                        break;

                }
            }
        }

        public void AddValue(int _change)
        {
            EnsureMainScript();
            if (!MainScript || !MainScript.Initialized || MainScript.MyCharacter == null || MainScript.MyCharacter.MyData == null)
                return;
            if (CharacterDataSetting.instance == null)
                return;
            if (Type == ColorSettingTypes.Race)
            {
                if (CharacterDataSetting.instance.RaceSettings == null || CharacterDataSetting.instance.RaceSettings.Length == 0)
                    return;
                int temp = Mathf.Clamp((int)MainScript.MyCharacter.MyData._CharacterData.Race, 0, CharacterDataSetting.instance.RaceSettings.Length - 1) + _change;
                if (temp > CharacterDataSetting.instance.RaceSettings.Length - 1) {
                    temp = 0;
                } else if (temp < 0) {
                    temp = CharacterDataSetting.instance.RaceSettings.Length - 1;
                }
                MainScript.MyCharacter.MyData._CharacterData.Race = (byte)temp;
                MainScript.MyCharacter.MyData._CharacterData.DataColor[1] = Uint8Color.Set(CharacterDataSetting.instance.RaceSettings[temp].DefaultSkinColor);
                MainScript.MyCharacter.MyData._CharacterData.DataColor[7] = Uint8Color.Set(CharacterDataSetting.instance.RaceSettings[temp].DefaultEyeColor);
                MainScript.MyCharacter.MyData._CharacterData.DataFloat[24] = (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultEarsScale;
                MainScript.MyCharacter.MyData._CharacterData.DataFloat[25] = (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultEarsShape;
                MainScript.MyCharacter.MyData._CharacterData.DataFloat[26] = (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultEarsTipOffset;
                MainScript.MyCharacter.MyData._CharacterData.DataInt[5] = (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultEyeID;
                byte _defaultHeadAcc = MainScript.MyCharacter.MyData._Sex == Sex.Male ? (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultMaleHeadAccessory : (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultFemaleHeadAccessory;
                MainScript.MyCharacter.MyData._CharacterData.DataInt[9] = _defaultHeadAcc;
                if (_defaultHeadAcc > 0)
                {
                    MainScript.MyCharacter.MyData._CharacterData.DataColor[4] = MainScript.MyCharacter.MyData._CharacterData.DataColor[1];
                }
                byte _defaultTail =  (byte)CharacterDataSetting.instance.RaceSettings[temp].DefaultTail ;
                MainScript.MyCharacter.MyData._OutfitID[6] = _defaultTail;
                valueText.text = CharacterDataSetting.instance.RaceSettings[temp].RaceName;
                MainScript.MyCharacter.SetEmotion("OpenMouth", 1F);
                MainScript.RefreshData();
            }
            else
            {
                if (CharacterDataSetting.instance.MaleMaterialSettings == null)
                    return;
                if (SettingTexture < 0 || SettingTexture >= CharacterDataSetting.instance.MaleMaterialSettings.Length)
                    return;
                int temp = MainScript.MyCharacter.MyData._CharacterData.DataInt[(int)CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureType] + _change;
                if (temp > CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureIdMax)
                {
                    temp = CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureIdMin;
                }
                else if (temp < CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureIdMin)
                {
                    temp = CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureIdMax;
                }
                MainScript.MyCharacter.MyData._CharacterData.DataInt[(int)CharacterDataSetting.instance.MaleMaterialSettings[SettingTexture].TextureType] = (byte)temp;
                valueText.text = temp.ToString();
            }
            MainScript.MyCharacter.UpdateMaterialData();
            
        }

    }
}
