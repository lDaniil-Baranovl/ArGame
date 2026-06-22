using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Грузит BattleMatch в фоне на время полёта дракона,
// чтобы переход в боевую сцену происходил сразу после посадки.
public class CinematicLoadingController : MonoBehaviour
{
    public string nextSceneName = "BattleMatch";
    public CinematicDragonFlight dragonFlight;

    [Header("Скип")]
    [Tooltip("Кнопка для мгновенного пропуска полёта дракона.")]
    public InputActionProperty skipAction;

    private AsyncOperation loadOp;
    private bool activating;

    void Start()
    {
        loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        loadOp.allowSceneActivation = false;

        dragonFlight.OnFlightComplete += HandleFlightComplete;

        skipAction.action.Enable();
    }

    void Update()
    {
        // Скип разрешён только когда BattleMatch уже фактически прогрузилась
        // (progress доходит до 0.9 и дальше просто ждёт allowSceneActivation) -
        // иначе мгновенный переход всё равно подвиснет на недогруженной сцене.
        if (!activating && loadOp.progress >= 0.9f && skipAction.action.WasPressedThisFrame())
        {
            HandleFlightComplete();
        }
    }

    void HandleFlightComplete()
    {
        if (activating) return;
        activating = true;
        StartCoroutine(FadeOutAndActivate());
    }

    IEnumerator FadeOutAndActivate()
    {
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut();

        loadOp.allowSceneActivation = true;
    }

    void OnDestroy()
    {
        skipAction.action?.Disable();
    }
}
