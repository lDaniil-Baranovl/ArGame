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
    // Параметры толчка от земли вверх по Animation Event "jumpDRG" в клипе "Fly Take off".
    public float jumpHeight = 0.15f;
    public float jumpDuration = 0.8f;

    public event System.Action OnFlightComplete;

    // Пороги для выбора варианта анимации полёта по крену/тангажу пути.
    // Разные пороги входа/выхода (гистерезис) - иначе на повороте bankAngle колеблется
    // около границы и CrossFade между Fly Forward <-> Fly Foward L/R дёргается каждый Dwell.
    private const float TurnEnterThreshold = 10f;
    private const float TurnExitThreshold = 4f;
    private const float PitchAnimThreshold = 0.12f;
    private const float AnimCrossFadeTime = 0.3f;
    private const float AnimStateMinDwell = 0.35f;
    private const float FlapPhaseDuration = 2.2f;
    private const float GlidePhaseDuration = 1.4f;

    private Animator animator;
    private float[] segmentLengths;
    private float totalLength;
    private float traveled;
    private bool finished;

    // "Fly Take off" - стартовое состояние аниматора, само переходит в "Fly Forward"
    // (Has Exit Time в контроллере) - ждём этого, прежде чем начать переключать варианты.
    private bool hasTakenOff;
    private string currentAnimState = "Fly Take off";
    private float animStateDwellTimer;
    private bool isGliding;
    private float glidePhaseTimer = FlapPhaseDuration;
    private int turnZone; // -1 влево, 0 прямо, 1 вправо - с гистерезисом, см. UpdateTurnZone

    private bool jumpTriggered;
    private float jumpTimer;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
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

        // Толчок считаем один раз за кадр и накладываем сверху на текущую базовую позицию -
        // независимо от того, стоит дракон на месте или уже летит по сплайну. Так толчок
        // остаётся гладким, даже если попадёт на стык "Fly Take off" -> "Fly Forward".
        float jumpOffset = GetJumpOffset();

        if (!hasTakenOff)
        {
            // Пока играет "Fly Take off", дракон сидит на месте и отталкивается от земли -
            // не двигаем его по сплайну.
            Vector3 groundPos = waypoints[0].position;
            groundPos.y += jumpOffset;
            transform.position = groundPos;

            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Fly Take off")) return;
            hasTakenOff = true;
        }

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

        pos.y += jumpOffset;
        transform.position = pos;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            Vector3 bankedUp = Vector3.up;
            float bankAngle = 0f;
            if (farDir.sqrMagnitude > 0.0001f)
            {
                // Угол поворота пути впереди -> наклон (roll) корпуса в сторону поворота,
                // как у самолёта/птицы в вираже, вместо плоского "скольжения" по кривой.
                float turnAngle = Vector3.SignedAngle(dir, farDir.normalized, Vector3.up);
                bankAngle = Mathf.Clamp(-turnAngle * bankAmount, -maxBankAngle, maxBankAngle);
                bankedUp = Quaternion.AngleAxis(bankAngle, dir) * Vector3.up;
            }

            Quaternion targetRot = Quaternion.LookRotation(dir, bankedUp);
            float lerpFactor = 1f - Mathf.Exp(-turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lerpFactor);

            UpdateFlightAnimation(dir, bankAngle);
        }
    }

    // Animation Event на клипе "Fly Take off" - момент, когда дракон отталкивается от земли.
    public void jumpDRG()
    {
        jumpTriggered = true;
        jumpTimer = 0f;
    }

    // Толчок только вверх, без падения обратно: плавно (ease-in-out, без рывка на старте)
    // поднимается до jumpHeight и остаётся там - дракон просто оказывается чуть выше.
    float GetJumpOffset()
    {
        if (!jumpTriggered) return 0f;
        if (jumpTimer >= jumpDuration) return jumpHeight;

        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / jumpDuration);
        float eased = t * t * (3f - 2f * t); // smoothstep
        return jumpHeight * eased;
    }

    // Подбирает конкретный клип полёта (вверх/вниз, влево/вправо, машущий/планирующий)
    // под текущий крен и тангаж пути, вместо одного зацикленного "Fly Forward".
    void UpdateFlightAnimation(Vector3 dir, float bankAngle)
    {
        if (animator == null) return;

        UpdateTurnZone(bankAngle);

        animStateDwellTimer -= Time.deltaTime;

        glidePhaseTimer -= Time.deltaTime;
        if (glidePhaseTimer <= 0f)
        {
            isGliding = !isGliding;
            glidePhaseTimer = isGliding ? GlidePhaseDuration : FlapPhaseDuration;
        }

        string targetState;
        if (dir.y > PitchAnimThreshold)
            targetState = turnZone > 0 ? "Fly Up Foward R"
                : turnZone < 0 ? "Fly Up Foward L"
                : "Fly UP Forward";
        else if (dir.y < -PitchAnimThreshold)
            targetState = turnZone > 0 ? "Fly Down R"
                : turnZone < 0 ? "Fly Down L"
                : "Fly Down Forward";
        else if (turnZone > 0)
            targetState = isGliding ? "Fly Glide R" : "Fly Foward R";
        else if (turnZone < 0)
            targetState = isGliding ? "Fly Glide L" : "Fly Foward L";
        else
            targetState = isGliding ? "Fly Glide" : "Fly Forward";

        if (targetState != currentAnimState && animStateDwellTimer <= 0f)
        {
            animator.CrossFadeInFixedTime(targetState, AnimCrossFadeTime);
            currentAnimState = targetState;
            animStateDwellTimer = AnimStateMinDwell;
        }
    }

    // Гистерезис: чтобы войти в поворот нужно превысить TurnEnterThreshold, а чтобы выйти
    // обратно на прямую - упасть ниже TurnExitThreshold. Без зазора bankAngle на повороте
    // колеблется около одной границы, и CrossFade перезапускается каждый Dwell - дёргание.
    void UpdateTurnZone(float bankAngle)
    {
        if (turnZone > 0)
        {
            if (bankAngle < TurnExitThreshold) turnZone = 0;
        }
        else if (turnZone < 0)
        {
            if (bankAngle > -TurnExitThreshold) turnZone = 0;
        }
        else
        {
            if (bankAngle > TurnEnterThreshold) turnZone = 1;
            else if (bankAngle < -TurnEnterThreshold) turnZone = -1;
        }
    }
}
