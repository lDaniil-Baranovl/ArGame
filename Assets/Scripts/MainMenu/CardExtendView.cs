using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class CardExtendView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("XR Input (правый контроллер, кнопка A)")]
    public InputActionProperty flipAction;

    [Header("Смена спрайта")]
    [Tooltip("Image карты — если пусто, берётся с этого же объекта")]
    public Image targetImage;
    [Tooltip("Спрайт с информацией (обратная сторона карты)")]
    public Sprite infoSprite;

    [Header("Точка показа карты")]
    public Transform placeCardExtend;
    public float expandedScaleMultiplier = 2.2f;

    [Header("Кнопка улучшения (дочерний объект карты)")]
    [Tooltip("Transform кнопки апгрейда — она масштабируется отдельно от карты")]
    public Transform upgradeButton;
    [Tooltip("Во сколько раз кнопка вырастет при раскрытии (должно быть меньше expandedScaleMultiplier)")]
    public float buttonScaleMultiplier = 1.3f;

    [Header("Анимация")]
    public float collapseDuration = 0.20f;
    public float expandDuration   = 0.25f;
    public float moveDuration     = 0.30f;

    [Header("Оверлей ShopPanel")]
    [Tooltip("Image компонент объекта DimOverlay")]
    public Image dimOverlay;
    [Range(0f, 1f)]
    public float dimTargetAlpha  = 0.70f;
    public float dimFadeDuration = 0.18f;

    [Header("Блюр (URP)")]
    [Tooltip(
        "Material с шейдером Custom/ShopBlur.\n" +
        "Шаги: Project → Create → Material → шейдер Custom/ShopBlur → перетащи сюда.\n" +
        "Если пусто — работает просто тёмный оверлей."
    )]
    public Material blurMaterial;

    // ── состояние ──────────────────────────────────────────────────────────
    private bool isHovered;
    private bool isExtended;
    private bool isAnimating;

    private Sprite     frontSprite;
    private Vector3    savedWorldPos;
    private Quaternion savedWorldRot;
    private Vector3    savedLocalScale;
    private Vector3    savedButtonScale;

    private Button upgradeBtn;      // Button компонент кнопки улучшения
    private Canvas cardCanvasOverride;

    // ── инициализация ──────────────────────────────────────────────────────
    private void Awake()
    {
        flipAction.action.Enable();

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetImage != null)
            frontSprite = targetImage.sprite;

        // Кнопка улучшения неактивна пока карта не раскрыта — предотвращает
        // случайное нажатие при наведении луча в режиме колоды
        if (upgradeButton != null)
        {
            upgradeBtn = upgradeButton.GetComponent<Button>();
            if (upgradeBtn != null) upgradeBtn.interactable = false;
        }

        if (dimOverlay != null)
        {
            if (blurMaterial != null)
            {
                // Назначаем блюр-материал; видимость = Image.color.a (как у любого UI Image)
                dimOverlay.material = blurMaterial;
                // Для блюр-режима цвет белый — нам нужен только alpha
                dimOverlay.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                // Без блюра — просто чёрный оверлей
                dimOverlay.color = new Color(0f, 0f, 0f, 0f);
            }
            dimOverlay.gameObject.SetActive(false);
        }
    }

    // ── ввод ───────────────────────────────────────────────────────────────
    private void Update()
    {
        if (isAnimating) return;
        if (!flipAction.action.WasPressedThisFrame()) return;

        if (!isExtended && isHovered)
            StartCoroutine(ExtendRoutine());
        else if (isExtended)
            StartCoroutine(ReturnRoutine());
    }

    public void OnPointerEnter(PointerEventData _) => isHovered = true;
    public void OnPointerExit(PointerEventData _)  => isHovered = false;

    // ── главные корутины ───────────────────────────────────────────────────
    private IEnumerator ExtendRoutine()
    {
        isAnimating = true;

        savedWorldPos   = transform.position;
        savedWorldRot   = transform.rotation;
        savedLocalScale = transform.localScale;

        yield return AnimateScaleX(0f, collapseDuration);

        if (targetImage != null && infoSprite != null)
            targetImage.sprite = infoSprite;

        EnableCanvasOverride();

        if (dimOverlay != null)
        {
            dimOverlay.gameObject.SetActive(true);
            StartCoroutine(FadeOverlay(0f, dimTargetAlpha));
        }

        yield return MoveTowards(placeCardExtend.position, placeCardExtend.rotation, moveDuration);

        // Корректируем масштаб кнопки: она должна вырасти меньше чем вся карта
        if (upgradeButton != null)
        {
            savedButtonScale = upgradeButton.localScale;
            upgradeButton.localScale = savedButtonScale * (buttonScaleMultiplier / expandedScaleMultiplier);
        }

        yield return AnimateScaleXTo(savedLocalScale * expandedScaleMultiplier, expandDuration);

        // Разрешаем нажать кнопку улучшения только когда карта полностью раскрыта
        if (upgradeBtn != null) upgradeBtn.interactable = true;

        isExtended  = true;
        isAnimating = false;
    }

    private IEnumerator ReturnRoutine()
    {
        isAnimating = true;

        // Блокируем кнопку сразу при начале возврата
        if (upgradeBtn != null) upgradeBtn.interactable = false;

        if (dimOverlay != null)
            StartCoroutine(FadeOverlay(dimOverlay.color.a, 0f));

        yield return AnimateScaleX(0f, collapseDuration);

        if (targetImage != null && frontSprite != null)
            targetImage.sprite = frontSprite;

        yield return MoveTowards(savedWorldPos, savedWorldRot, moveDuration);

        // Возвращаем оригинальный масштаб кнопки
        if (upgradeButton != null)
            upgradeButton.localScale = savedButtonScale;

        DisableCanvasOverride();

        yield return AnimateScaleXTo(savedLocalScale, expandDuration);

        isExtended  = false;
        isAnimating = false;
    }

    // ── анимации ───────────────────────────────────────────────────────────

    private IEnumerator AnimateScaleX(float targetX, float duration)
    {
        float startX  = transform.localScale.x;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Lerp(startX, targetX, elapsed / duration), s.y, s.z);
            yield return null;
        }
        var sf = transform.localScale;
        transform.localScale = new Vector3(targetX, sf.y, sf.z);
    }

    private IEnumerator AnimateScaleXTo(Vector3 target, float duration)
    {
        transform.localScale = new Vector3(0f, target.y, target.z);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = new Vector3(Mathf.Lerp(0f, target.x, elapsed / duration), target.y, target.z);
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator MoveTowards(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3    startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    // Анимирует Image.color.a — работает и для блюр-шейдера, и для простого чёрного оверлея
    private IEnumerator FadeOverlay(float from, float to)
    {
        if (dimOverlay == null) yield break;
        float elapsed = 0f;
        Color col = dimOverlay.color;
        while (elapsed < dimFadeDuration)
        {
            elapsed += Time.deltaTime;
            col.a = Mathf.Lerp(from, to, elapsed / dimFadeDuration);
            dimOverlay.color = col;
            yield return null;
        }
        col.a = to;
        dimOverlay.color = col;
        if (to <= 0f) dimOverlay.gameObject.SetActive(false);
    }

    // ── Canvas sorting override ────────────────────────────────────────────

    private void EnableCanvasOverride()
    {
        if (cardCanvasOverride != null) return;
        cardCanvasOverride = gameObject.AddComponent<Canvas>();
        cardCanvasOverride.overrideSorting = true;
        cardCanvasOverride.sortingOrder    = 200;
        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void DisableCanvasOverride()
    {
        if (cardCanvasOverride == null) return;
        var raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null) Destroy(raycaster);
        Destroy(cardCanvasOverride);
        cardCanvasOverride = null;
    }
}
