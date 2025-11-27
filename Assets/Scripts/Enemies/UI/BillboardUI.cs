using UnityEngine;

namespace Game.Enemies.UI
{
    [ExecuteAlways]
    public class BillboardUI : MonoBehaviour
    {
        public Camera targetCamera;

        private void LateUpdate()
        {
            if (!targetCamera)
            {
                targetCamera = Camera.main;
                if (!targetCamera) return;
            }

            // Face the camera straight-on
            var camForward = targetCamera.transform.forward;
            transform.forward = camForward;
        }
    }
}
