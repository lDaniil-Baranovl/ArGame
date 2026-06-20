using UnityEngine;
using TMPro;

public class CardRewardPopupController : MonoBehaviour
{
    public CardVisual cardVisual;
    public TextMeshProUGUI amountText;

    [Header("Эффект появления карты")]
    public ParticleSystem cardAppearEffectPrefab;
    public float cardAppearEffectScale = 0.3f;

    public void Show(Sprite icon, int amount, float duration)
    {
        if (cardVisual != null)
            cardVisual.SetIcon(icon);

        if (amountText != null)
            amountText.text = $"+{amount}";

        if (Camera.main != null)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        if (cardAppearEffectPrefab != null)
        {
            ParticleSystem cardEffect = Instantiate(cardAppearEffectPrefab, transform.position, transform.rotation, transform);
            cardEffect.transform.localScale = Vector3.one * cardAppearEffectScale;
        }

        Destroy(gameObject, duration);
    }
}
