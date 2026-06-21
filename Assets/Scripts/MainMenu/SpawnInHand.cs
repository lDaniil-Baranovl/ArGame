using TMPro;
using UnityEngine;

public class SpawnInHand : MonoBehaviour
{
    public GameObject itemPrefab;
    public TextMeshProUGUI priceText;
    public int casePrice = 150;

    [Header("Появление сундука перед игроком")]
    public float spawnDistance = 0.8f;
    public float spawnHeightOffset = -0.3f;

    [Header("Точка спавна (если не задана — спавн от камеры игрока)")]
    [Tooltip("Назначь Transform, расположенный перед канвасом в свободном месте. Сундук будет появляться от него, а не от камеры — это исключает появление сундука внутри/за канвасом.")]
    public Transform spawnAnchor;

    [Tooltip("Коррекция разворота модели сундука по Y, если её визуальный 'фасад' не совпадает с осью forward префаба.")]
    public float modelYawOffset = -90f;

    private GameObject currentItem;

    private void Start()
    {
        UpdatePriceUI();
    }

    private void UpdatePriceUI()
    {
        if (priceText != null)
            priceText.text = casePrice.ToString();
    }

    public void SpawnItem()
    {
        // 1. проверяем, есть ли бесплатная попытка
        bool canUse = TryOpenCaseManager.Instance.UseCaseAttempt();

        if (!canUse)
        {
            // 2. если попыток нет — покупаем за золото
            bool bought = TryOpenCaseManager.Instance.BuyCase(casePrice);

            if (!bought)
            {
                Debug.Log("Не хватает золота для покупки сундука!");
                return;
            }

            Debug.Log("Сундук куплен за золото!");
        }

        // 3. убираем предыдущий сундук, если он ещё валяется
        if (currentItem != null)
        {
            var oldCase = currentItem.GetComponent<CaseCubeController>();
            if (oldCase != null)
                oldCase.DisableActions();

            Destroy(currentItem);
        }

        // 4. спавним сундук перед игроком (или от фиксированной точки, если она назначена)
        Transform reference = spawnAnchor != null ? spawnAnchor : Camera.main.transform;
        Vector3 spawnPos = reference.position + reference.forward * spawnDistance;
        spawnPos.y += spawnHeightOffset;

        Quaternion spawnRot = Quaternion.LookRotation(reference.forward, Vector3.up) * Quaternion.Euler(0f, modelYawOffset, 0f);

        currentItem = Instantiate(itemPrefab, spawnPos, spawnRot);

        Debug.Log("Сундук появился перед игроком, можно бросать!");
    }
}
