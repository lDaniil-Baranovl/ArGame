/// <summary>
/// public InputActionProperty LeftGrip;
/// Awake: LeftGrip.action.Enable();
/// LeftGrip.action.ReadValue<float>
/// <XRController>{RightHand}/{Grip}
/// </summary>
/// 
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class CardDragXR : MonoBehaviour
{
    [Header("XR")]
    public Transform rightController;
    public InputActionProperty gripAction;

    [Tooltip("Время анимации подлёта карты к контроллеру при подхвате")]
    public float pickupDuration = 0.15f;

    [Header("Battlefield")]
    public LayerMask battlefieldMask;

    [Tooltip("Дополнительные слои, на которых можно разместить карту, только если CardType = Spell (например, BattleField)")]
    public LayerMask spellOnlyMask;

    public GameObject summonCirclePrefab;
    [Tooltip("Максимальная дистанция raycast для размещения карт")]
    public float maxRaycastDistance = 25f;

    private GameObject summonCircleInstance;
    private bool isHeld = false;

    public UnitCost data;
    public Transform homeSlot;

    private bool isReturning = false;
    private Coroutine returnRoutine;

    private bool isHovered = false;
    public static CardDragXR currentHeldCard = null;

    public static event System.Action OnUnitCardHeld;
    public static event System.Action OnUnitCardReleased;

    private Coroutine pickupRoutine;

    private XRGrabInteractable grab;

    void Awake()
    {
        gripAction.action.Enable();
        grab = GetComponent<XRGrabInteractable>();
    }

    void Start()
    {
        rightController = XRPlayer.Instance.rightController;

        // Подхват карты целиком управляется через Update() (isHovered + grip + IsActuallyUnderControllerRay),
        // поэтому нативный select XRGrabInteractable отключаем полностью — иначе при зажатом grip без
        // наведения луча XRI сам вызывает selectEntered раньше нашей проверки, grab.enabled остаётся
        // включённым, и собственный dynamic/far attach карты тащит её на дистанцию вместо руки.
        if (grab != null)
            grab.selectFilters.Add(new XRSelectFilterDelegate((interactor, interactable) => false));
    }

    public void Init(UnitCost cardData)
    {
        data = cardData;
    }

    void Update()
    {
        float grip = gripAction.action.ReadValue<float>();

        if (currentHeldCard != null && currentHeldCard != this)
            return;

        if (!isHeld && isHovered && grip > 0.7f)
        {
            if (!IsActuallyUnderControllerRay()) return;
            if (grab != null) grab.enabled = false;
            StartHolding();
        }
        else if (isHeld && grip < 0.2f)
        {
            Release();
            if (grab != null) grab.enabled = true;
        }

        if (isHeld)
            UpdateSummonCircle();
    }
    public void OnHoverEntered()
    {
        if (currentHeldCard != null) return;
        isHovered = true;
    }

    public void OnHoverExited()
    {
        if (currentHeldCard != null) return;
        isHovered = false;
    }

    private void StartHolding()
    {
        if (currentHeldCard != null && currentHeldCard != this)
            return;

        currentHeldCard = this;
        isHeld = true;

        if (isReturning)
        {
            isReturning = false;
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }

        transform.SetParent(null, true);

        if (data != null && data.IsUnit())
            OnUnitCardHeld?.Invoke();

        if (pickupRoutine != null)
            StopCoroutine(pickupRoutine);
        pickupRoutine = StartCoroutine(PickupAnimation());
    }

    private IEnumerator PickupAnimation()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / pickupDuration;
            float smooth = Mathf.SmoothStep(0, 1, t);

            transform.position = Vector3.Lerp(startPos, rightController.position, smooth);
            transform.rotation = Quaternion.Slerp(startRot, rightController.rotation, smooth);

            yield return null;
        }

        transform.position = rightController.position;
        transform.rotation = rightController.rotation;

        transform.SetParent(rightController, true);

        pickupRoutine = null;
    }

    private void Release()
    {
        if (currentHeldCard != this)
            return;

        if (pickupRoutine != null)
        {
            StopCoroutine(pickupRoutine);
            pickupRoutine = null;
        }

        transform.SetParent(null, true);

        isHeld = false;
        currentHeldCard = null;

        OnUnitCardReleased?.Invoke();

        if (summonCircleInstance != null)
            summonCircleInstance.SetActive(false);

        bool used = false;

        if (Physics.Raycast(rightController.position, rightController.forward, out RaycastHit hit, maxRaycastDistance, GetPlacementMask()))
        {
            if (ElixirManager.Instance.TrySpend(data.elixirCost))
            {
                used = true;
                SpawnUnit(hit.point);
                if (summonCircleInstance != null)
                {
                    Destroy(summonCircleInstance);
                    summonCircleInstance = null;
                }
                FindObjectOfType<CardCycleManager>().OnCardUsed(this);
            }
        }

        if (!used)
        {
            if (grab != null)
            {
                grab.enabled = false;
            }

            ReturnToHome();

        }
    }

    public void ReturnToHome()
    {
        if (isReturning)
            return;

        if (summonCircleInstance != null)
        {
            Destroy(summonCircleInstance);
            summonCircleInstance = null;
        }

        if (homeSlot == null)
        {
            Debug.LogError("homeSlot �� ��������!");
            return;
        }

        isReturning = true;

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        transform.SetParent(null);

        returnRoutine = StartCoroutine(ReturnAnimation());
    }

    private IEnumerator ReturnAnimation()
    {
        Vector3 startPos = transform.position;

        float duration = 0.25f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float smooth = Mathf.SmoothStep(0, 1, t);

            // homeSlot закреплён на руке игрока и постоянно двигается,
            // поэтому цель берём "вживую" каждый кадр, а не один раз.
            transform.position = Vector3.Lerp(startPos, homeSlot.position, smooth);
            transform.Rotate(0, 900f * Time.deltaTime, 0, Space.Self);

            yield return null;
        }

        // Снэп делаем по актуальным координатам слота, и только потом
        // репарентим — в этот момент относительный поворот нулевой,
        // поэтому SetParent не ломает localScale (без этого был stretch).
        transform.position = homeSlot.position;
        transform.rotation = homeSlot.rotation;

        transform.SetParent(homeSlot, true);

        if (grab != null)
            grab.enabled = true;

        isReturning = false;
        returnRoutine = null;
    }

    private void UpdateSummonCircle()
    {
        if (!isHeld) return;

        // raycast (указка)
        Debug.DrawRay(rightController.position, rightController.forward * maxRaycastDistance, Color.green);

        if (Physics.Raycast(rightController.position, rightController.forward,
            out RaycastHit hit, maxRaycastDistance, GetPlacementMask()))
        {
            if (summonCircleInstance == null)
                summonCircleInstance = Instantiate(summonCirclePrefab);

            summonCircleInstance.SetActive(true);
            summonCircleInstance.transform.position = hit.point + Vector3.up * 0.05f;
        }
        else
        {
            if (summonCircleInstance != null)
                summonCircleInstance.SetActive(false);
        }
    }

    private void SpawnUnit(Vector3 pos)
    {
        for (int i = 0; i < data.prefabs.Length; i++)
        {
            Vector3 spawnPos = pos;

            if (data.spawnOffsets != null && i < data.spawnOffsets.Length)
                spawnPos += data.spawnOffsets[i];

            Instantiate(data.prefabs[i], spawnPos, Quaternion.identity);
        }
    }

    private LayerMask GetPlacementMask()
    {
        if (data != null && data.IsSpell())
            return battlefieldMask | spellOnlyMask;

        return battlefieldMask;
    }

    private bool IsActuallyUnderControllerRay()
    {
        if (rightController == null) return false;

        if (Physics.Raycast(
                rightController.position,
                rightController.forward,
                out RaycastHit hit,
                maxRaycastDistance))
        {
            CardDragXR card = hit.collider.GetComponentInParent<CardDragXR>();
            return card == this;
        }

        return false;
    }

}


