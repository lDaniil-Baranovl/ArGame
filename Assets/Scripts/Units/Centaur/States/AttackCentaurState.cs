using UnityEngine;

public class AttackCentaurState : UnitBaseState<CentaurStateManager>
{
    private DamageCentaur attackScript;
    public override void EnterState(CentaurStateManager manager)
    {
        manager.navMeshAgent.isStopped = true;

        attackScript = manager.damageCollider.GetComponent<DamageCentaur>();
        
        if (manager.centaur_runTime >= 3f)
        {
            manager.unitAnimator.SetTrigger("SpecialAttack");
            if (attackScript != null)
                attackScript.SetSpecialAttack(true);
        }
        else
        {
            manager.unitAnimator.SetBool("IsAttacking", true);
            if (attackScript != null)
                attackScript.SetSpecialAttack(false);
        }

        manager.centaur_runTime = 0f;
    }

    public override void ExitState(CentaurStateManager manager)
    {
        manager.navMeshAgent.isStopped = false;
        manager.unitAnimator.SetBool("IsAttacking", false);
        if (manager.damageCollider != null)
            manager.damageCollider.enabled = false;

        if (manager.attackEffect != null)
            manager.attackEffect.SetActive(false);

        manager.isAttackEffectActive = false;
    }

    public override void UpdateState(CentaurStateManager manager)
    {

        if (manager.target == null || Vector3.Distance(manager.transform.position, manager.target.position) > manager.attackDistance + 1f)
        {
            Transform newTarget = manager.GetTarget();
            manager.target = newTarget;

            if (newTarget == null || !manager.HasReachedTarget())
            {
                manager.SwitchState(manager.runCentaurState);
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