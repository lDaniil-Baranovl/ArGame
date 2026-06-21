using UnityEngine;
using UnityEngine.SceneManagement;

// Грузит testMechanic в фоне на время полёта дракона,
// чтобы переход в боевую сцену происходил сразу после посадки.
public class CinematicLoadingController : MonoBehaviour
{
    public string nextSceneName = "testMechanic";
    public CinematicDragonFlight dragonFlight;

    private AsyncOperation loadOp;

    void Start()
    {
        loadOp = SceneManager.LoadSceneAsync(nextSceneName);
        loadOp.allowSceneActivation = false;

        dragonFlight.OnFlightComplete += HandleFlightComplete;
    }

    void HandleFlightComplete()
    {
        loadOp.allowSceneActivation = true;
    }
}
