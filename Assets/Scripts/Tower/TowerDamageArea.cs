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

    [Header("Impact Effect Settings")]
    public GameObject impactEffectPrefab;
    public GameObject zapEffectPrefab;
    public float impactScale = 0.4f;
    public float zapEffectScale = 0.25f;
    public float impactEffectLifetime = 2f;

    private readonly List<Health> unitsInRange = new List<Health>();
    private Health currentTarget;
    private Coroutine attackRoutine;

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

            Attack(currentTarget);
            yield return new WaitForSeconds(tickRate);
        }
    }

    private void Attack(Health target)
    {
        if (target == null || target.IsDead)
            return;

        target.ApplyDamage(Mathf.RoundToInt(damagePerTick), "Tower");

        Vector3 targetPoint = target.transform.position + Vector3.up * 0.5f;
        SpawnImpactEffect(impactEffectPrefab, targetPoint, impactScale);
        SpawnImpactEffect(zapEffectPrefab, GetGroundPosition(targetPoint), zapEffectScale);
    }

    private Vector3 GetGroundPosition(Vector3 origin)
    {
        int battleFieldLayer = LayerMask.NameToLayer("BattleField");
        if (battleFieldLayer >= 0)
        {
            Vector3 rayOrigin = origin + Vector3.up * 10f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 50f, 1 << battleFieldLayer))
                return hit.point;
        }

        return origin;
    }

    private void SpawnImpactEffect(GameObject prefab, Vector3 position, float scale)
    {
        if (prefab == null)
            return;

        GameObject impact = Instantiate(prefab, position, Quaternion.identity);
        impact.transform.localScale = Vector3.one * scale;

        foreach (var ps in impact.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.loop = false;
        }

        Destroy(impact, impactEffectLifetime);
    }
}
