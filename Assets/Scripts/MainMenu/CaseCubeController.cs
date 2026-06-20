using UnityEngine;
using UnityEngine.InputSystem;

public class CaseCubeController : MonoBehaviour
{
    [Header("Возможные карты для выпадения")]
    public UnitCost[] possibleCards;

    [Header("Награда (всплывающая карточка)")]
    public CardRewardPopupController rewardPopupPrefab;
    public float popupDuration = 2f;
    public float rewardSpawnDistance = 0.1f;

    [Header("Настройки выпадения")]
    public int minFragments = 3;
    public int maxFragments = 10;

    [Header("Эффекты открытия сундука")]
    public ParticleSystem openEffectPrefab1;
    public float openEffectScale1 = 0.15f;
    public ParticleSystem openEffectPrefab2;
    public float openEffectScale2 = 0.3f;
    public float openEffectLifetime = 4f;

    [Header("Открытие сундука (триггер контроллера)")]
    public InputActionProperty leftTriggerAction;
    public InputActionProperty rightTriggerAction;

    private bool rewardGiven = false;

    private void Awake()
    {
        leftTriggerAction.action.Enable();
        rightTriggerAction.action.Enable();
    }

    private void OnDestroy()
    {
        DisableActions();
    }

    public void DisableActions()
    {
        leftTriggerAction.action.Disable();
        rightTriggerAction.action.Disable();
    }

    private void Update()
    {
        if (rewardGiven)
            return;

        if (leftTriggerAction.action.WasPressedThisFrame() || rightTriggerAction.action.WasPressedThisFrame())
            OpenCase();
    }

    private void OpenCase()
    {
        if (rewardGiven) return;
        rewardGiven = true;

        SpawnOpenEffect(openEffectPrefab1, openEffectScale1);
        SpawnOpenEffect(openEffectPrefab2, openEffectScale2);

        if (possibleCards != null && possibleCards.Length > 0)
        {
            UnitCost card = possibleCards[Random.Range(0, possibleCards.Length)];
            int amount = Random.Range(minFragments, maxFragments + 1);

            CardUpgradeManager.Instance.AddFragments(card, amount);

            if (rewardPopupPrefab != null)
            {
                Vector3 spawnPos = transform.position;

                GameObject rightControllerObj = GameObject.FindWithTag("RightController");
                if (rightControllerObj != null)
                {
                    Transform rightController = rightControllerObj.transform;
                    spawnPos = rightController.position + rightController.forward * rewardSpawnDistance;
                }

                CardRewardPopupController popup = Instantiate(rewardPopupPrefab, spawnPos, transform.rotation);
                popup.Show(card.icon, amount, popupDuration);
            }

            Debug.Log($"Сундук открыт! +{amount} осколков {card.unitName}");
        }
        else
        {
            Debug.Log("Сундук открыт! (награды пока не настроены)");
        }

        Destroy(gameObject);
    }

    private void SpawnOpenEffect(ParticleSystem effectPrefab, float scale)
    {
        if (effectPrefab == null) return;

        ParticleSystem effect = Instantiate(effectPrefab, transform.position, transform.rotation);
        effect.transform.localScale = Vector3.one * scale;
        Destroy(effect.gameObject, openEffectLifetime);
    }
}
