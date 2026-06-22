using System;
using TMPro;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("Main settings")]
    public float matchDuration = 180f;
    private float timer;

    [Header("UI")]
    public GameObject endBattleCanvas;

    [Header("text")]
    public GameObject victoryText;
    public GameObject defeatText;
    public GameObject drawText;

    [Header("Gold conditions")]
    public GameObject get_gold40;
    public GameObject get_gold20;
    public GameObject get_gold10;
    public GameObject get_case;

    private int blueDestroyed = 0;
    private int redDestroyed = 0;

    private bool battleEnded = false;
    private bool battleStarted = false;
    public TextMeshProUGUI timerText;


    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        ArenaPlacementEvents.OnArenaConfirmed += OnArenaConfirmed;
    }

    private void OnDisable()
    {
        ArenaPlacementEvents.OnArenaConfirmed -= OnArenaConfirmed;
    }

    private void Start()
    {
        timer = matchDuration;
        endBattleCanvas.SetActive(false);

        victoryText.SetActive(false);
        defeatText.SetActive(false);
        drawText.SetActive(false);
        get_gold10.SetActive(false);
        get_gold20.SetActive(false);
        get_gold40.SetActive(false);
        get_case.SetActive(false);
    }

    // Игровое поле появилось — можно запускать отсчёт времени
    private void OnArenaConfirmed()
    {
        battleStarted = true;
    }

    private void Update()
    {
        if (battleEnded || !battleStarted) return;

        timer -= Time.deltaTime;

        UpdateTimerUI();

        if (timer <= 0)
        {
            EndBattleByTime();
        }
    }

    // Вызывается таймером из динамически создаваемой локации,
    // когда он появляется в сцене
    public void SetTimerText(TextMeshProUGUI text)
    {
        timerText = text;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // Подписывайтесь, чтобы реагировать на разрушение башни конкретной команды
    public static event Action<int> OnTowerDestroyed;

    public void TowerDestroyed(int teamID)
    {
        OnTowerDestroyed?.Invoke(teamID);

        if (battleEnded) return;

        if (teamID == 0)
            blueDestroyed++;
        else
            redDestroyed++;

        CheckInstantWin();
    }

    private void CheckInstantWin()
    {
        if (blueDestroyed >= 3)
        {
            ShowResult("lose");
        }
        else if (redDestroyed >= 3)
        {
            ShowResult("win");
        }
    }

    private void EndBattleByTime()
    {
        if (battleEnded) return;

        int blueAlive = 3 - blueDestroyed;
        int redAlive = 3 - redDestroyed;

        if (blueAlive > redAlive)
            ShowResult("win");
        else if (redAlive > blueAlive)
            ShowResult("lose");
        else
            ShowResult("draw");
    }

    private void ShowResult(string result)
    {
        battleEnded = true;

        endBattleCanvas.SetActive(true);

        victoryText.SetActive(false);
        defeatText.SetActive(false);
        drawText.SetActive(false);
        get_gold10.SetActive(false);
        get_gold20.SetActive(false);
        get_gold40.SetActive(false);
        get_case.SetActive(false);

        if (result == "win") 
        {
            TryOpenCaseManager.Instance.AddCaseAttempt(1);
            victoryText.SetActive(true);
            get_gold40.SetActive(true);
            get_case.SetActive(true);
        }
        else if (result == "lose")
        {
            defeatText.SetActive(true);
            get_gold10.SetActive(true);
        }
        else
        {
            drawText.SetActive(true);
            get_gold20.SetActive(true);
        }

        if (result == "win")
            GoldManager.Instance.AddGold(40);
        else if (result == "draw")
            GoldManager.Instance.AddGold(20);
        else
            GoldManager.Instance.AddGold(10);

        GamePause.paused = true;
        Time.timeScale = 0f;
    }

    public void EndBattle()
    {
        Time.timeScale = 1f;
        GamePause.paused = false;
        ScreenFader.LoadScene("MainMenu");
    }
}
