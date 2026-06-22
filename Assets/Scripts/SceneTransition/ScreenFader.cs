using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Полноэкранное чёрное затемнение между сценами - скрывает за собой рывок активации
// сцены (даже когда сама загрузка шла параллельно в фоне). Сам создаёт себе Canvas
// и переживает смену сцен через DontDestroyOnLoad, поэтому не требует ручной настройки
// сцен: достаточно вызывать ScreenFader.LoadScene(...) вместо SceneManager.LoadScene(...),
// либо ScreenFader.Instance.FadeOut() перед ручной активацией асинхронной загрузки.
// Появление в новой сцене (FadeIn) запускается автоматически по SceneManager.sceneLoaded.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public float fadeDuration = 0.4f;

    private CanvasGroup canvasGroup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        new GameObject("ScreenFader").AddComponent<ScreenFader>();
    }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        StartCoroutine(FadeRoutine(0f));
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FadeRoutine(0f));
    }

    void BuildOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        gameObject.AddComponent<CanvasScaler>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;

        var imageGO = new GameObject("Black");
        imageGO.transform.SetParent(transform, false);
        var image = imageGO.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public IEnumerator FadeOut()
    {
        yield return FadeRoutine(1f);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeRoutine(0f);
    }

    IEnumerator FadeRoutine(float target)
    {
        canvasGroup.blocksRaycasts = true;
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = target;
        canvasGroup.blocksRaycasts = target > 0.01f;
    }

    // Затемнение, затем обычная (синхронная) загрузка сцены - используется в местах,
    // где раньше был прямой SceneManager.LoadScene(...).
    public static void LoadScene(string sceneName)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.FadeOutAndLoad(sceneName));
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        yield return FadeRoutine(1f);
        SceneManager.LoadScene(sceneName);
    }
}
