using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.CharacterCreator
{
    public class SimpleDynamicMsg : MonoBehaviour
    {
        #region variables
        private static SimpleDynamicMsg instance;
        public Animation mAni;
        public Text mText;
        #endregion

        #region internal methods
        private void Awake()
        {
            instance = this;
        }
        public void PopMsgInstance(string _text)
        {
            mText.text = _text;
            mAni.Stop();
            mAni.Play();
            SoundManager.Play2D("msg");
        }
        #endregion

        public static void PopMsg(string _text)//Pop message
        {
            if (instance != null) instance.PopMsgInstance(_text);
        }

       
    }
}
