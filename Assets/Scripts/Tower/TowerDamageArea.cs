using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerSingleTargetDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damagePerTick = 10f;
    public float tickRate = 0.5f;

    [Header("Team Settings")]
    public int teamID = 1;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;
    public Transform projectileOrigin;
    public float projectileSpeed = 6f;
    public float minTravelTime = 0.8f;
    public float projectileScale = 0.25f;
    public float arcHeight = 2f;
    public float impactScale = 0.4f;
    public float impactEffectLifetime = 2f;

    private readonly List<Health> unitsInRange = new List<Health>();
    private Health currentTarget;
    private Coroutine attackRoutine;

    private void Start()
    {
        if (projectileOrigin == null)
            projectileOrigin = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        Health unit = other.GetComponent<Health>();
        if (unit == null) return;
        if (unit.GetTeam() == teamID) return;
        if (unit.IsDead) return;

        if (!unitsInRange.Contains(unit))
            unitsInRange.Add(unit);

        TryAcquireTarget();
    }

    private void OnTriggerExit(Collider other)
    {
        Health unit = other.GetComponent<Health>();
        if (unit == null) return;

        unitsInRange.Remove(unit);

        if (unit == currentTarget)
        {
            currentTarget = null;
            TryAcquireTarget();
        }
    }

    private void TryAcquireTarget()
    {
        if (currentTarget != null && !currentTarget.IsDead)
            return;

        currentTarget = GetClosestUnit();

        if (currentTarget != null)
        {
            if (attackRoutine == null)
                attackRoutine = StartCoroutine(AttackRoutine());
        }
        else
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }
    }

    private Health GetClosestUnit()
    {
        float minDist = float.MaxValue;
        Health closest = null;

        foreach (var unit in unitsInRange)
        {
            if (unit == null || unit.IsDead) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = unit;
            }
        }

        return closest;
    }
    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                attackRoutine = null;
                TryAcquireTarget();
                yield break;
            }

            FireProjectile(currentTarget);
            yield return new WaitForSeconds(tickRate);
        }
    }

    private void FireProjectile(Health target)
    {
        GameObject projectileObj = projectilePrefab != null
            ? Instantiate(projectilePrefab, projectileOrigin.position, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (projectilePrefab == null)
            projectileObj.transform.SetPositionAndRotation(projectileOrigin.position, Quaternion.identity);

        projectileObj.transform.localScale = Vector3.one * projectileScale;

        TowerProjectile projectile = projectileObj.GetComponent<TowerProjectile>();
        if (projectile == null)
            projectile = projectileObj.AddComponent<TowerProjectile>();

        projectile.Init(target, Mathf.RoundToInt(damagePerTick), projectileSpeed, minTravelTime, arcHeight, impactEffectPrefab, impactScale, impactEffectLifetime);
    }
}
