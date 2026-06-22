using UnityEngine;

public class TowerProjectile : MonoBehaviour
{
    private Transform target;
    private Health targetHealth;
    private int damage;
    private float speed;
    private float arcHeight;
    private GameObject impactEffectPrefab;
    private float impactScale;
    private float impactLifetime;

    private Vector3 startPos;
    private float travelTime;
    private float elapsed;

    public void Init(Health targetHealth, int damage, float speed, float minTravelTime, float arcHeight, GameObject impactEffectPrefab, float impactScale, float impactLifetime)
    {
        this.targetHealth = targetHealth;
        this.target = targetHealth.transform;
        this.damage = damage;
        this.speed = speed;
        this.arcHeight = arcHeight;
        this.impactEffectPrefab = impactEffectPrefab;
        this.impactScale = impactScale;
        this.impactLifetime = impactLifetime;

        startPos = transform.position;
        float distance = Vector3.Distance(startPos, GetTargetPoint());
        travelTime = Mathf.Max(distance / speed, minTravelTime);
        elapsed = 0f;
    }

    private Vector3 GetTargetPoint()
    {
        return target.position + Vector3.up * 0.5f;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / travelTime);

        Vector3 targetPoint = GetTargetPoint();
        Vector3 flatPos = Vector3.Lerp(startPos, targetPoint, t);
        float height = arcHeight * Mathf.Sin(t * Mathf.PI);

        Vector3 prevPos = transform.position;
        Vector3 newPos = flatPos + Vector3.up * height;

        Vector3 dir = newPos - prevPos;
        if (dir.sqrMagnitude > 0.0001f)
            transform.forward = dir.normalized;

        transform.position = newPos;

        if (t >= 1f)
            Arrive(targetPoint);
    }

    private void Arrive(Vector3 position)
    {
        if (targetHealth != null && !targetHealth.IsDead)
            targetHealth.ApplyDamage(damage, "Tower");

        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, position, Quaternion.identity);
            impact.transform.localScale = Vector3.one * impactScale;

            foreach (var ps in impact.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = false;
            }

            Destroy(impact, impactLifetime);
        }

        Destroy(gameObject);
    }
}
