using UnityEngine;

// Двигает дракона по сглаженному (Catmull-Rom) пути через waypoints.
// Игрок сидит на драконе как пассажир — вращение головы/корпуса игрока
// в VR никак не блокируется, скрипт двигает только сам дракон (и с ним рига игрока как ребёнка).
public class CinematicDragonFlight : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 4f;
    public float turnSpeed = 4f;
    public float lookAheadDistance = 1.5f;
    public float bankAmount = 1.2f;
    public float maxBankAngle = 35f;

    public event System.Action OnFlightComplete;

    private Animator animator;
    private float[] segmentLengths;
    private float totalLength;
    private float traveled;
    private bool finished;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        animator?.SetBool("IsRunningdragFlyCold", true);
        // Скрипт сам полностью управляет transform каждый кадр — root motion анимации
        // конфликтует с этим и даёт дёрганье (особенно заметно на голове/шее).
        if (animator != null) animator.applyRootMotion = false;

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("CinematicDragonFlight: нужно минимум 2 waypoint'а.");
            return;
        }

        BuildSpline();
        transform.position = waypoints[0].position;
    }

    void BuildSpline()
    {
        const int samplesPerSegment = 20;
        segmentLengths = new float[waypoints.Length - 1];
        totalLength = 0f;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector3 prev = CatmullRom(i, 0f);
            float length = 0f;
            for (int s = 1; s <= samplesPerSegment; s++)
            {
                Vector3 p = CatmullRom(i, s / (float)samplesPerSegment);
                length += Vector3.Distance(prev, p);
                prev = p;
            }
            segmentLengths[i] = length;
            totalLength += length;
        }
    }

    Vector3 CatmullRom(int segment, float t)
    {
        Vector3 p0 = waypoints[Mathf.Max(segment - 1, 0)].position;
        Vector3 p1 = waypoints[segment].position;
        Vector3 p2 = waypoints[Mathf.Min(segment + 1, waypoints.Length - 1)].position;
        Vector3 p3 = waypoints[Mathf.Min(segment + 2, waypoints.Length - 1)].position;

        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    Vector3 PositionAtDistance(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, totalLength - 0.0001f);
        float dist = distance;
        int segment = 0;
        while (segment < segmentLengths.Length - 1 && dist > segmentLengths[segment])
        {
            dist -= segmentLengths[segment];
            segment++;
        }
        float segT = Mathf.Clamp01(dist / segmentLengths[segment]);
        return CatmullRom(segment, segT);
    }

    void Update()
    {
        if (finished || segmentLengths == null) return;

        traveled += speed * Time.deltaTime;
        if (traveled >= totalLength)
        {
            finished = true;
            transform.position = waypoints[waypoints.Length - 1].position;
            OnFlightComplete?.Invoke();
            return;
        }

        Vector3 pos = PositionAtDistance(traveled);
        Vector3 aheadPos = PositionAtDistance(traveled + lookAheadDistance);
        Vector3 farAheadPos = PositionAtDistance(traveled + lookAheadDistance * 3f);

        Vector3 dir = aheadPos - pos;
        Vector3 farDir = farAheadPos - aheadPos;

        transform.position = pos;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            Vector3 bankedUp = Vector3.up;
            if (farDir.sqrMagnitude > 0.0001f)
            {
                // Угол поворота пути впереди -> наклон (roll) корпуса в сторону поворота,
                // как у самолёта/птицы в вираже, вместо плоского "скольжения" по кривой.
                float turnAngle = Vector3.SignedAngle(dir, farDir.normalized, Vector3.up);
                float bankAngle = Mathf.Clamp(-turnAngle * bankAmount, -maxBankAngle, maxBankAngle);
                bankedUp = Quaternion.AngleAxis(bankAngle, dir) * Vector3.up;
            }

            Quaternion targetRot = Quaternion.LookRotation(dir, bankedUp);
            float lerpFactor = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lerpFactor);
        }
    }
}
