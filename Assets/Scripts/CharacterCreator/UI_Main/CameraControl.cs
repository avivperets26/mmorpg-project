using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.CharacterCreator
{
    public class CameraControl : MonoBehaviour
    {

        #region variables
        public static CameraControl instance;
        [HideInInspector]
        public bool Initialized = false;
        public Transform RotY;
        public Transform PosY;
        public Camera Cam;
        private float targetAngleY = 0F;
        private float angleY = 0F;
        private float posY = 1F;
        private float fov = 43F;
        private float targetFov = 43F;
        private float yAdd = 5F;
        private bool disabled = false;
        #endregion

        #region internal methods
        void Awake()
        {
            instance = this;
        }


        void Update()
        {
            if (!CharacterCusUI.instance.Initialized || disabled) return;
            yAdd = Mathf.Lerp(yAdd, Initialized?0F: 5F,Time.deltaTime*5F);
            if (Input.GetMouseButton(1)) targetAngleY = (targetAngleY + Input.GetAxis("Mouse X") * 5F) % 360F;
            angleY = Mathf.Lerp(angleY, targetAngleY, Time.deltaTime * 5F);
            fov = Mathf.Lerp(fov, targetFov, Time.deltaTime * 5F);
            if (CharacterCusUI.instance != null && CharacterCusUI.instance.MyCharacter != null && CharacterCusUI.instance.MyCharacter.isInited)
            {
                float _lerp = (fov - 13F) / 30F;
                posY = Mathf.Lerp(CharacterCusUI.instance.MyCharacter.BoneDictionary["Bip001 Head"].position.y, CharacterCusUI.instance.MyCharacter.BoneDictionary["Bip001 Pelvis"].position.y, _lerp);
            }
            if (EventSystem.current.IsPointerOverGameObject() && CharacterManager.PhotoMode==0) return;
            targetFov = Mathf.Clamp(targetFov- Input.GetAxis("Mouse ScrollWheel") * 25F,13F,43F);
        }

        public void DisableForTime(float _time)
        {
            StartCoroutine(DisableForTimeCo(_time));
        }

        IEnumerator DisableForTimeCo(float _time)
        {
            disabled = true;
            yield return new WaitForSeconds(_time);
            disabled = false;
        }

        private void LateUpdate()
        {
            RotY.transform.localEulerAngles = new Vector3(0F, angleY, 0F);
            PosY.transform.localPosition = new Vector3(0F, posY+ yAdd, 0F);
            Cam.fieldOfView = fov;
            
        }
        #endregion
    }
}
