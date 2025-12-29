using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CharacterCreator
{
    public class CC_Thumb : MonoBehaviour
    {
        public int MyID;
        public CharacterCusSliders MainScript;
        public GameObject Check;
        public RawImage Icon;

        private void Update()
        {
            Check.SetActive(MainScript.SelThumb==MyID);
        }

        public void Click()
        {
            MainScript.ThumbSelect(MyID);
        }

    }
}
