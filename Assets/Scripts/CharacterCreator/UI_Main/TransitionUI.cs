using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.CharacterCreator
{
    public class TransitionUI : MonoBehaviour
    {
        #region variables
        public static Scene LastScene;
        public static TransitionUI instance;
        public static string NextScene;
        public CanvasGroup LoadingCanvas;
        public AudioListener Listener;
        public Image ProgressMask;
        #endregion
        IEnumerator Start()
        {
            SoundManager.Play2D("SwitchScene");
            instance = this;
            LoadingCanvas.alpha = 0F;
            yield return 1;
            while (LoadingCanvas.alpha< 1F) {
                LoadingCanvas.alpha = Mathf.MoveTowards(LoadingCanvas.alpha, 1F,Time.deltaTime);
                yield return 1;
            }
            LoadingCanvas.alpha = 1F;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Transition"));
            yield return SceneManager.UnloadSceneAsync(LastScene);
            Listener.enabled = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(NextScene, LoadSceneMode.Additive);
            while (!operation.isDone)
            {
                yield return 1;
                ProgressMask.fillAmount = operation.progress;
            }
            Listener.enabled = false;
            ProgressMask.fillAmount = 1F;
           
            while (LoadingCanvas.alpha > 0F)
            {
                LoadingCanvas.alpha = Mathf.MoveTowards(LoadingCanvas.alpha, 0F, Time.deltaTime*2F);
                yield return 1;
            }
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("Transition"));
        }


    }
}
