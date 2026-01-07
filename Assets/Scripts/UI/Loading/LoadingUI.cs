using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class LoadingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image progressMask;
    [SerializeField] private Text statusText;

    [Header("Target Scene")]
    [SerializeField] private string targetSceneName = "World";
    [SerializeField] private string targetScenePath = "Assets/Scenes/World/Zone_01_StarterField.unity";
    [SerializeField] private bool loadByPathInEditor = true;

    [Header("Copy")]
    [SerializeField] private string loadingText = "Loading...";
    [SerializeField] private string readyText = "Press any button";

    private AsyncOperation loadOp;

    private IEnumerator Start()
    {
        if (!progressMask)
            progressMask = FindProgressMask();
        if (!statusText)
            statusText = FindStatusText();
        DisableOtherStatusText();

        if (statusText)
            statusText.text = loadingText;
        if (progressMask)
            progressMask.fillAmount = 0f;

        yield return null;

        var loadingScene = SceneManager.GetSceneByName("Loading");
        if (loadingScene.IsValid())
            SceneManager.SetActiveScene(loadingScene);

#if UNITY_EDITOR
        if (Application.isEditor && loadByPathInEditor && !string.IsNullOrWhiteSpace(targetScenePath))
        {
            loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode(
                targetScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
        }
        else
#endif
        {
            loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
        }

        if (loadOp == null)
            yield break;

        loadOp.allowSceneActivation = false;

        bool ready = false;
        while (!loadOp.isDone)
        {
            float progress = Mathf.Clamp01(loadOp.progress / 0.9f);
            if (progressMask)
                progressMask.fillAmount = progress;

            if (!ready && loadOp.progress >= 0.9f)
            {
                ready = true;
                if (progressMask)
                    progressMask.fillAmount = 1f;
                if (statusText)
                    statusText.text = readyText;
            }

            if (ready && Input.anyKeyDown)
                loadOp.allowSceneActivation = true;

            yield return null;
        }
    }

    private Image FindProgressMask()
    {
        var barRoot = GameObject.Find("Progressbar");
        if (barRoot)
        {
            var progress = barRoot.transform.Find("Progress");
            if (progress)
            {
                var image = progress.GetComponent<Image>();
                if (image)
                    return image;
            }
        }

        var images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            if (img && img.type == Image.Type.Filled)
                return img;
        }

        return null;
    }

    private Text FindStatusText()
    {
        var textObj = GameObject.Find("Text");
        if (textObj)
        {
            var t = textObj.GetComponent<Text>();
            if (t)
                return t;
        }

        var textProgress = GameObject.Find("TextProgress");
        if (textProgress)
        {
            var t = textProgress.GetComponent<Text>();
            if (t)
                return t;
        }

        return GetComponentInChildren<Text>(true);
    }

    private void DisableOtherStatusText()
    {
        var textObj = GameObject.Find("Text");
        var textProgress = GameObject.Find("TextProgress");

        if (statusText && textObj && statusText.gameObject != textObj)
            textObj.SetActive(false);

        if (statusText && textProgress && statusText.gameObject != textProgress)
            textProgress.SetActive(false);
    }
}
