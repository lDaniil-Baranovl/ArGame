using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CaseSpawnAnimation : MonoBehaviour
{
    [Header("Появление (спин + опускание)")]
    public float duration = 2f;
    public float descendDistance = 0.2f;
    public float spinSpeed = 360f;

    [Header("Парение после появления")]
    public float floatHeight = 0.03f;
    public float floatSpeed = 1.5f;

    [Header("Эффекты появления и вращения")]
    public ParticleSystem appearEffectPrefab;
    public float appearEffectScale = 0.3f;
    public ParticleSystem spinEffectPrefab;
    public float spinEffectScale = 0.7f;

    private Rigidbody rb;
    private XRGrabInteractable grab;

    private Vector3 floatBasePosition;
    private float timeCounter;
    private bool isFloating;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.enabled = false;
            grab.selectEntered.AddListener(OnGrabbed);
        }

        if (rb != null)
            rb.isKinematic = true;
    }

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * descendDistance;
        Quaternion faceRotation = transform.rotation;

        if (appearEffectPrefab != null)
        {
            ParticleSystem appearEffect = Instantiate(appearEffectPrefab, startPos, transform.rotation);
            appearEffect.transform.localScale = Vector3.one * appearEffectScale;
            Destroy(appearEffect.gameObject, duration);
        }

        if (spinEffectPrefab != null)
        {
            ParticleSystem spinEffect = Instantiate(spinEffectPrefab, transform.position, transform.rotation, transform);
            spinEffect.transform.localScale = Vector3.one * spinEffectScale;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = faceRotation;
        floatBasePosition = endPos;
        isFloating = true;

        if (grab != null)
            grab.enabled = true;
    }

    private void Update()
    {
        if (!isFloating) return;

        timeCounter += Time.deltaTime * floatSpeed;

        Vector3 pos = floatBasePosition;
        pos.y += Mathf.Sin(timeCounter) * floatHeight;
        transform.position = pos;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isFloating = false;

        if (rb != null)
            rb.isKinematic = false;
    }
}
