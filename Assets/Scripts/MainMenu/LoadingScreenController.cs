using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Логотипы")]
    public RectTransform logo1;
    public RectTransform logo2;

    [Header("Фон")]
    public RectTransform background;

    [Header("Тайминги (сек.)")]
    [Tooltip("Сколько времени показывается каждый логотип")]
    public float logoDuration = 2f;

    [Header("Анимация увеличения (эффект приближения)")]
    [Tooltip("Во сколько раз увеличится логотип за время своего показа")]
    public float logoGrowthFactor = 1.1f;
    [Tooltip("Во сколько раз увеличится фон за всё время загрузки")]
    public float backgroundGrowthFactor = 1.02f;

    [Header("Переход")]
    public string nextSceneName = "MainMenu";

    private Vector3 logo1BaseScale;
    private Vector3 logo2BaseScale;
    private Vector3 backgroundBaseScale;

    private void Start()
    {
        logo1BaseScale = logo1.localScale;
        logo2BaseScale = logo2.localScale;

        logo2.gameObject.SetActive(false);

        if (background != null)
        {
            backgroundBaseScale = background.localScale;
            StartCoroutine(ScaleOverTime(background, backgroundBaseScale, backgroundBaseScale * backgroundGrowthFactor, logoDuration * 2f));
        }

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        loadOp.allowSceneActivation = false;

        yield return StartCoroutine(ShowLogo(logo1, logo1BaseScale));
        logo1.gameObject.SetActive(false);

        yield return StartCoroutine(ShowLogo(logo2, logo2BaseScale));

        while (loadOp.progress < 0.9f)
            yield return null;

        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        loadOp.allowSceneActivation = true;
    }

    private IEnumerator ShowLogo(RectTransform logo, Vector3 baseScale)
    {
        logo.gameObject.SetActive(true);
        logo.localScale = baseScale;
        yield return StartCoroutine(ScaleOverTime(logo, baseScale, baseScale * logoGrowthFactor, logoDuration));
    }

    private IEnumerator ScaleOverTime(RectTransform target, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            target.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }
        target.localScale = to;
    }
}
