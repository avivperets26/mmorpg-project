using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class LoadingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image progressMask;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private RectTransform marker;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.08f;
    [SerializeField] private Color pulseColor = Color.white;
    [SerializeField] private float markerInset = 10f;
    [SerializeField] private Vector2 statusTextSize = new Vector2(220f, 50f);

    [Header("Target Scene")]
    [SerializeField] private string targetSceneName = "World";
    [SerializeField] private string targetScenePath = "Assets/Scenes/World/Zone_01_StarterField.unity";
    [SerializeField] private bool loadByPathInEditor = true;

    [Header("Copy")]
    [SerializeField] private string loadingText = "Loading...";
    [SerializeField] private string readyText = "Press any button";

    private AsyncOperation loadOp;
    private float displayProgress;
    private float progressVelocity;
    private RectTransform barRoot;
    private Color statusBaseColor;
    private Vector3 statusBaseScale;

    private IEnumerator Start()
    {
        if (!progressMask)
            progressMask = FindProgressMask();
        if (!statusText)
            statusText = FindStatusText();
        if (!percentText)
            percentText = FindPercentText();
        if (!marker)
            marker = FindMarker();
        if (!barRoot)
            barRoot = FindBarRoot();
        ReparentPercentText();
        ReparentMarker();
        DisableOtherStatusText();

        if (statusText)
        {
            statusText.text = loadingText;
            statusBaseColor = statusText.color;
            statusBaseScale = statusText.rectTransform.localScale;
            var rect = statusText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = statusTextSize;
        }
        if (progressMask)
            progressMask.fillAmount = 0f;
        if (percentText)
            percentText.text = "0%";

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
            float targetProgress = Mathf.Clamp01(loadOp.progress / 0.9f);
            if (!ready && loadOp.progress >= 0.9f)
            {
                ready = true;
                targetProgress = 1f;
                if (statusText)
                    statusText.text = readyText;
            }

            displayProgress = Mathf.SmoothDamp(displayProgress, targetProgress, ref progressVelocity, smoothTime);
            UpdateProgressUI(displayProgress);
            UpdateReadyPulse(ready);

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

    private TMP_Text FindStatusText()
    {
        var textObj = GameObject.Find("Text");
        if (textObj)
        {
            var t = textObj.GetComponent<TMP_Text>();
            if (t)
                return t;
        }

        var textProgress = GameObject.Find("TextProgress");
        if (textProgress)
        {
            var t = textProgress.GetComponent<TMP_Text>();
            if (t)
                return t;
        }

        return GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Text FindPercentText()
    {
        var textObj = GameObject.Find("TextProgress");
        if (textObj)
        {
            var t = textObj.GetComponent<TMP_Text>();
            if (t)
                return t;
        }

        var allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        for (int i = 0; i < allTexts.Length; i++)
        {
            var t = allTexts[i];
            if (t && t.name == "TextProgress")
                return t;
        }

        return null;
    }

    private RectTransform FindMarker()
    {
        var barRoot = GameObject.Find("Progressbar");
        if (!barRoot)
            return null;

        var markerTransform = barRoot.transform.Find("Progress/Marker");
        if (!markerTransform)
            return null;

        return markerTransform.GetComponent<RectTransform>();
    }

    private void ReparentPercentText()
    {
        if (!percentText)
            return;

        var barRoot = GameObject.Find("Progressbar");
        if (!barRoot)
            return;

        if (percentText.transform.parent != barRoot.transform)
            percentText.transform.SetParent(barRoot.transform, false);

        var rect = percentText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(25f, 1.5f);
        percentText.fontSize = 24f;
        percentText.gameObject.SetActive(true);
    }

    private void ReparentMarker()
    {
        if (!marker)
            return;

        if (!barRoot)
            barRoot = FindBarRoot();

        if (barRoot && marker.transform.parent != barRoot)
            marker.SetParent(barRoot, false);

        marker.anchorMin = new Vector2(0.5f, 0.5f);
        marker.anchorMax = new Vector2(0.5f, 0.5f);
        marker.anchoredPosition = new Vector2(marker.anchoredPosition.x, 0f);
    }

    private void DisableOtherStatusText()
    {
        var textObj = GameObject.Find("Text");
        var textProgress = GameObject.Find("TextProgress");

        if (statusText && textObj && statusText.gameObject != textObj)
            textObj.SetActive(false);

        if (statusText && textProgress && statusText.gameObject != textProgress && percentText == null)
            textProgress.SetActive(false);
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressMask)
            progressMask.fillAmount = progress;

        if (percentText)
        {
            int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
            percentText.text = pct + "%";
        }

        if (marker && (barRoot || progressMask))
        {
            var progressRect = barRoot ? barRoot : progressMask.rectTransform;
            float width = progressRect.rect.width;
            float markerHalf = marker.rect.width * 0.5f;
            float minX = -width * 0.5f + markerHalf + markerInset;
            float maxX = width * 0.5f - markerHalf - markerInset;
            float x = Mathf.Lerp(minX, maxX, progress);
            var pos = marker.anchoredPosition;
            pos.x = x;
            marker.anchoredPosition = pos;
        }
    }

    private RectTransform FindBarRoot()
    {
        var bar = GameObject.Find("Progressbar");
        if (!bar)
            return null;

        return bar.GetComponent<RectTransform>();
    }

    private void UpdateReadyPulse(bool ready)
    {
        if (!statusText)
            return;

        if (!ready)
        {
            statusText.color = statusBaseColor;
            statusText.rectTransform.localScale = statusBaseScale;
            return;
        }

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        statusText.color = Color.Lerp(statusBaseColor, pulseColor, t);
        float scale = Mathf.Lerp(1f, pulseScale, t);
        statusText.rectTransform.localScale = statusBaseScale * scale;
    }
}
