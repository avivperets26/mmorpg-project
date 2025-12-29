using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CharacterCreator
{
    public class cc_Mat_Setting : MonoBehaviour
    {
        public CharacterCusUI MainScipt;
        public SliderControl[] MySliders;
        private float noFeedback = 0F;

        IEnumerator Start()
        {
            while (!MainScipt.Initialized) yield return 1;
            UpdateValue();
        }

        public void UpdateValue()
        {
            noFeedback = Time.time;
            for (int i = 0; i < MySliders.Length; i++)
            {
                MySliders[i].UpdateValue(MainScipt.MyCharacter.MyData._CharacterData.DataFloat[(int)CharacterDataSetting.instance.MaleMaterialSettings[MySliders[i].MyID].FloatType]*0.01F);
            }
        }

        public void UpdateValue(CharacterData _data)
        {
            noFeedback = Time.time;
            for (int i = 0; i < MySliders.Length; i++)
            {
                MySliders[i].UpdateValue(_data.DataFloat[(int)CharacterDataSetting.instance.MaleMaterialSettings[MySliders[i].MyID].FloatType] * 0.01F);
            }
        }


        public void SliderValueChanged(int _id)
        {
            if (Time.time < noFeedback + 0.2F) return;
            for (int i = 0; i < MySliders.Length; i++)
            {
                MainScipt.MyCharacter.MyData._CharacterData.DataFloat[(int)CharacterDataSetting.instance.MaleMaterialSettings[MySliders[i].MyID].FloatType] = (byte)Mathf.FloorToInt(MySliders[i].SliderScript.value* MySliders[i].Mutiplier);
            }
            MainScipt.MyCharacter.UpdateMaterialData();
        }
    }
}
