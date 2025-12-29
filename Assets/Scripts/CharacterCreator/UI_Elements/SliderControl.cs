using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game.CharacterCreator
{
    public class SliderControl : MonoBehaviour, IPointerClickHandler
    {
        public bool isTitle = false;
        public int MyID = 0;
        public Slider SliderScript;
        public Text MyName;
        public Text MyValue;
        public float Mutiplier = 100F;
        public string ValueFormat = "0";
        public float DefaultValue = 0.5F;
        public GameObject InfoObj;
        private float noFeedback = 0F;
       
        
        public void SetName(string _name)
        {
            if (MyName) MyName.text = (isTitle? "<size=24>" :"<size=21>") +_name.Substring(0,1)+"</size>"+ _name.Substring(1,_name.Length-1);
        }

        public void UpdateValue(float _value)
        {
            if (isTitle) return;
            noFeedback = Time.time;
            SliderScript.SetValueWithoutNotify(_value);
            if (MyValue) MyValue.text = (SliderScript.value * Mutiplier).ToString(ValueFormat);
        }

        public void OnChange()
        {
            if (Time.time < noFeedback + 0.2F || isTitle) return;
            if (InfoObj) InfoObj.SendMessage("SliderValueChanged", MyID);
            if (MyValue) MyValue.text = (SliderScript.value * Mutiplier).ToString(ValueFormat);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isTitle) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                noFeedback = Time.time;
                SliderScript.value = DefaultValue;
                if (InfoObj) InfoObj.SendMessage("SliderValueChanged", MyID);
                if (MyValue) MyValue.text = (SliderScript.value * Mutiplier).ToString(ValueFormat);
            }
        }

    }

}
