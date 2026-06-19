using UnityEngine;

public class SkeletAttackSt : UnitBaseState<SkeletStMan>
{
    private DamageSkel attackScript;
    public override void EnterState(SkeletStMan manager)
    {
        manager.navMeshAgent.isStopped = true;
        
        attackScript = manager.damageCollider.GetComponent<DamageSkel>();
        manager.unitAnimator.SetBool("IsAttacking", true);
    }
    public override void ExitState(SkeletStMan manager)
    {
        manager.navMeshAgent.isStopped = false;
        manager.unitAnimator.SetBool("IsAttacking", false);
        if (manager.damageCollider != null)
            manager.damageCollider.enabled = false;

        if (manager.attackEffect != null)
            manager.attackEffect.SetActive(false);

        manager.isAttackEffectActive = false;
    }

    public override void UpdateState(SkeletStMan manager)
    {

        if (manager.target == null || Vector3.Distance(manager.transform.position, manager.target.position) > manager.attackDistance + 1f)
        {
            Transform newTarget = manager.GetTarget();
            manager.target = newTarget;

            if (newTarget == null || !manager.HasReachedTarget())
            {
                manager.SwitchState(manager.skeletRunSt);
                return;
            }
        }
        Vector3 direction = (manager.target.position - manager.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
