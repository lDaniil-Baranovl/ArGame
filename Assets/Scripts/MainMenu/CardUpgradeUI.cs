using System.Collections;
using TMPro;
using UnityEngine;

public class CardUpgradeUI : MonoBehaviour
{
    public UnitCost unit;
    public int upgradeCost = 100;

    public TextMeshProUGUI textUI;
    [Tooltip("Размер шрифта. Уменьши если текст не помещается в кнопку.")]
    public float fontSize = 8f;

    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        // Авто-поиск текста среди дочерних объектов если не назначен вручную
        if (textUI == null)
            textUI = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void OnEnable()
    {
        CardUpgradeManager.OnCardChanged += HandleCardChanged;
    }

    private void OnDisable()
    {
        CardUpgradeManager.OnCardChanged -= HandleCardChanged;
    }

    private void HandleCardChanged(UnitCost changedUnit)
    {
        if (changedUnit == unit)
            UpdateUI();
    }

    public void UpdateUI()
    {
        var data = CardUpgradeManager.Instance.GetCard(unit);
        textUI.fontSize = fontSize;
        textUI.text = $"Lv.{data.level} {data.fragments}/10";
    }

    public void TryUpgrade()
    {
        bool ok = CardUpgradeManager.Instance.TryUpgrade(unit, upgradeCost);

        if (!ok)
        {
            Debug.Log("Недостаточно осколков или золота для улучшения!");
        }
        else
        {
            Debug.Log($"Карта {unit.unitName} улучшена!");
        }

        UpdateUI();

        if (feedbackCoroutine != null)
            StopCoroutine(feedbackCoroutine);

        feedbackCoroutine = StartCoroutine(FlashColor(ok ? Color.green : Color.red));
    }

    private IEnumerator FlashColor(Color color)
    {
        textUI.color = color;

        yield return new WaitForSeconds(1.5f);

        textUI.color = Color.white;
    }
}
