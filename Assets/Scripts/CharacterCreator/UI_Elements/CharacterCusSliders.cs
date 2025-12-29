using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game.CharacterCreator
{
    public class CharacterCusSliders : MonoBehaviour
    {
        public CharacterCusUI MainScript;
        public string SliderName;
        private Dictionary<int, SliderControl> SliderObjs = new Dictionary<int, SliderControl>();
        private Dictionary<int, CC_Thumb> ThumbObjs = new Dictionary<int, CC_Thumb>();
        public GameObject SliderItem;
        public GameObject TitleItem;
        private CharacterAppearance MyData;
        public string TurnOffAnimation;

        public enum SliderTypes
        {
            SliderList,
            AccessoryList,
            OutfitList
        }
        public SliderTypes Type;
        public CharacterDataSetting.CharacterBoneSliderNames[] MySliders;
        public int AccessoryList = -1;
        public OutfitSlots OutfitSlot;

        [HideInInspector]
        public int SelThumb = 0;

        public void InitData(CharacterAppearance _Data)
        {
            MyData = _Data;
            switch (Type)
            {
                case SliderTypes.SliderList:
                    foreach (CharacterDataSetting.CharacterBoneSliderNames obj in MySliders)
                    {
                        if (!SliderObjs.ContainsKey((int)obj))
                        {
                            if (obj.ToString().Contains("Title_"))
                            {
                                GameObject newSlider = Instantiate(TitleItem) as GameObject;
                                newSlider.transform.SetParent(TitleItem.transform.parent);
                                newSlider.transform.localScale = TitleItem.transform.localScale;
                                newSlider.GetComponent<SliderControl>().MyID = (int)obj;
                                newSlider.GetComponent<SliderControl>().SetName(obj.ToString().Replace("_", " ").Replace("Title",""));
                                newSlider.SetActive(true);
                                SliderObjs.Add((int)obj, newSlider.GetComponent<SliderControl>());
                            }
                            else
                            {
                                GameObject newSlider = Instantiate(SliderItem) as GameObject;
                                newSlider.transform.SetParent(SliderItem.transform.parent);
                                newSlider.transform.localScale = SliderItem.transform.localScale;
                                newSlider.GetComponent<SliderControl>().MyID = (int)obj;
                                newSlider.GetComponent<SliderControl>().DefaultValue = _Data._CharacterData.DataFloat[(int)obj] * 0.01F;
                                newSlider.GetComponent<SliderControl>().SetName(obj.ToString().Replace("_", " "));
                                newSlider.GetComponent<SliderControl>().UpdateValue(_Data._CharacterData.DataFloat[(int)obj] * 0.01F);
                                newSlider.SetActive(true);
                                SliderObjs.Add((int)obj, newSlider.GetComponent<SliderControl>());
                            }
                        }
                        else
                        {
                            if (!SliderObjs[(int)obj].isTitle)
                                SliderObjs[(int)obj].UpdateValue(_Data._CharacterData.DataFloat[(int)obj] * 0.01F);
                        }
                    }
                    break;

                case SliderTypes.AccessoryList:

                    for (int i = 0; i < ThumbObjs.Count; i++)
                    {
                        Destroy(ThumbObjs[i].gameObject);
                    }
                    ThumbObjs.Clear();

                    for (int i = 0; i <= GetAccessoryInfo(AccessoryList).MaxValue; i++)
                    {
                        if (!ThumbObjs.ContainsKey(i))
                        {
                            GameObject newSlider = Instantiate(SliderItem) as GameObject;
                            newSlider.transform.SetParent(SliderItem.transform.parent);
                            newSlider.transform.localScale = SliderItem.transform.localScale;
                            newSlider.GetComponent<CC_Thumb>().MyID = i;
                            if (i == 0)
                            {
                                newSlider.GetComponent<CC_Thumb>().Icon.texture = Resources.Load<Texture>("CharacterCreator/Icons/Empty");
                            }
                            else
                            {
                                Texture _icon = Resources.Load<Texture>(GetAccessoryInfo(AccessoryList).ThumbPath + i.ToString());
                                if (GetOutfitList()[i].Icon != null)
                                    newSlider.GetComponent<CC_Thumb>().Icon.texture = _icon;
                                else
                                    newSlider.GetComponent<CC_Thumb>().Icon.texture = Resources.Load<Texture>("CharacterCreator/Icons/Unknown");
                            }
                            newSlider.SetActive(true);
                            ThumbObjs.Add(i, newSlider.GetComponent<CC_Thumb>());
                        }
                    }
                    SelThumb = (int)GetAccessoryInfo(AccessoryList).Slot>9? MyData._OutfitID[(int)GetAccessoryInfo(AccessoryList).Slot-5] : MyData._CharacterData.DataInt[(int)GetAccessoryInfo(AccessoryList).Slot];

                    break;

                case SliderTypes.OutfitList:

                    for (int i = 0; i < ThumbObjs.Count; i++)
                    {
                        Destroy(ThumbObjs[i].gameObject);
                    }
                    ThumbObjs.Clear();

                    for (int i = 0; i < GetOutfitList().Length; i++)
                    {
                        if (!ThumbObjs.ContainsKey(i))
                        {
                            GameObject newSlider = Instantiate(SliderItem) as GameObject;
                            newSlider.transform.SetParent(SliderItem.transform.parent);
                            newSlider.transform.localScale = SliderItem.transform.localScale;
                            newSlider.GetComponent<CC_Thumb>().MyID = i;
                            if (GetOutfitList()[i].Icon != null)
                                newSlider.GetComponent<CC_Thumb>().Icon.texture = GetOutfitList()[i].Icon;
                            else
                                newSlider.GetComponent<CC_Thumb>().Icon.texture = Resources.Load<Texture>("CharacterCreator/Icons/Unknown");
                            newSlider.SetActive(true);
                            ThumbObjs.Add(i, newSlider.GetComponent<CC_Thumb>());
                        }
                    }
                    SelThumb = MyData._OutfitID[(int)OutfitSlot];
                    break;
            }
        }
        public AccessoryInfo GetAccessoryInfo(int _id)
        {
            return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleAccessorySettings[_id] : CharacterDataSetting.instance.FemaleAccessorySettings[_id];
        }
        private OutfitInfo[] GetOutfitList()
        {
            switch (OutfitSlot)
            {
                case OutfitSlots.Armor:
                    return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleArmorSetting : CharacterDataSetting.instance.FemaleArmorSetting;
                case OutfitSlots.Helmet:
                    return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleHelmetSetting : CharacterDataSetting.instance.FemaleHelmetSetting;
                case OutfitSlots.Gauntlet:
                    return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleGloveSetting : CharacterDataSetting.instance.FemaleGloveSetting;
                case OutfitSlots.Pants:
                    return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MalePantsSetting : CharacterDataSetting.instance.FemalePantsSetting;
                default:
                    return MyData._CharacterData.Sex == 0 ? CharacterDataSetting.instance.MaleBootSetting : CharacterDataSetting.instance.FemaleBootSetting;
            }
        }

        public void ThumbSelect(int _id)
        {
            SelThumb = _id;
            if (Type == SliderTypes.AccessoryList)
            {
                if ((int)GetAccessoryInfo(AccessoryList).Slot > 9)
                {
                    MyData._OutfitID[(int)GetAccessoryInfo(AccessoryList).Slot-5] = (byte)SelThumb;
                }
                else
                {
                    MyData._CharacterData.DataInt[(int)GetAccessoryInfo(AccessoryList).Slot] = (byte)SelThumb;
                }
                MainScript.MyCharacter.UpdateMaterialData();
            }
            else if (Type == SliderTypes.OutfitList)
            {
                MainScript.SwitchOutfit(OutfitSlot, _id);
            }

        }

        private void UpdateData(CharacterData _Data)
        {
            MyData._CharacterData = _Data;
            for (int i=0;i< _Data.DataFloat.Length;i++)
            {
                if (SliderObjs.ContainsKey(i) && !SliderObjs[i].isTitle)
                {
                    SliderObjs[i].UpdateValue(_Data.DataFloat[i] * 0.01F);
                }
            }
            if (AccessoryList > -1)
            {
                if ((int)GetAccessoryInfo(AccessoryList).Slot > 9)
                {
                    SelThumb = MyData._OutfitID[(int)GetAccessoryInfo(AccessoryList).Slot-5];
                }
                else
                {
                    SelThumb = MyData._CharacterData.DataInt[(int)GetAccessoryInfo(AccessoryList).Slot];
                }
            }
        }

        private void SliderValueChanged(int _id)
        {
            if (SliderObjs.ContainsKey(_id) && !SliderObjs[_id].isTitle)
                MyData._CharacterData.DataFloat[_id]= (byte)Mathf.FloorToInt(SliderObjs[_id].SliderScript.value*100);

            if (_id>=78 && _id<=80) {
                MainScript.MyCharacter.SetEmotion("OpenMouth", 3F);
            }

            if (_id == 87)
            {
                MainScript.MyCharacter.UpdateBreastSize();
            }
        }


        public void FadeAway()
        {
            GetComponent<Animation>().Play(TurnOffAnimation);
        }

        public void TurnOff()
        {
            gameObject.SetActive(false);
        }
    }
}
