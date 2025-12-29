using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CharacterCreator
{
    public class PhotoHouse : MonoBehaviour
    {
        public Transform mCameraRoot;
        public Camera mCamera;
        public GameObject mLight;
        public Renderer mBackground;

        public static Texture2D TakePhoto(Vector2 _size, Transform _parent, Color _bgColor, float _angle=0F,bool _lightOn=true)
        {
            GameObject instance = Instantiate(Resources.Load<GameObject>("CharacterCreator/Core/PhotoHouse"));
            instance.GetComponent<PhotoHouse>().DestroyLater();
            return instance.GetComponent<PhotoHouse>().GetPhoto(_size, _parent, _bgColor, _angle, _lightOn);
        }

        IEnumerator DestroyLaterCo()
        {
            yield return 1;
            Destroy(gameObject);
        }

        public void DestroyLater()
        {
            StartCoroutine(DestroyLaterCo());
        }

        public Texture2D GetPhoto(Vector2 _size, Transform _parent, Color _bgColor, float _angle, bool _lightOn)
        {
            if (_parent != null)
            {
                transform.SetParent(_parent);
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = new Vector3(0F, 0F, 90F);
            }
            mCameraRoot.localEulerAngles = new Vector3(0F,_angle,0F);
            mLight.SetActive(_lightOn);
            mBackground.material.SetColor("_EmissionColor", _bgColor);
            mBackground.material.SetColor("_EmissiveColor", _bgColor);
            mBackground.material.SetColor("_EmissiveColorLDR", _bgColor);
            RenderTexture rt = new RenderTexture(Mathf.FloorToInt(_size.x), Mathf.FloorToInt(_size.y), 24, UnityEngine.Experimental.Rendering.DefaultFormat.LDR);
            mCamera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(Mathf.FloorToInt(_size.x), Mathf.FloorToInt(_size.y), TextureFormat.RGB24, false);
            mCamera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, Mathf.FloorToInt(_size.x), Mathf.FloorToInt(_size.y)), 0, 0);
            screenShot.Apply();
            mCamera.targetTexture = null;
            RenderTexture.active = null; 
            Destroy(rt);
            return screenShot;
        }
    }
}
