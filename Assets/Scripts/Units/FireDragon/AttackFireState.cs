using UnityEngine;

public class AttackFireState : UnitBaseState<StateManagerFireDragon>
{
    public DamageFDC attackScript;
    public override void EnterState(StateManagerFireDragon manager)
    {
        manager.navMeshAgent.isStopped = true;

        attackScript = manager.damageCollider.GetComponent<DamageFDC>();

        manager.unitAnimator.SetBool("IsAttacking", true);
    }

    public override void ExitState(StateManagerFireDragon manager)
    {
        manager.navMeshAgent.isStopped = false;
        manager.unitAnimator.SetBool("IsAttacking", false);
        if (manager.damageCollider != null)
            manager.damageCollider.enabled = false;

        if (manager.attackEffect != null)
            manager.attackEffect.SetActive(false);

        manager.isAttackEffectActive = false;
    }
    public override void UpdateState(StateManagerFireDragon manager)
    {

        if (manager.target == null || Vector3.Distance(manager.transform.position, manager.target.position) > manager.attackDistance + 1f)
        {
            Transform newTarget = manager.GetTarget();
            manager.target = newTarget;

            if (newTarget == null || !manager.HasReachedTarget())
            {
                manager.SwitchState(manager.fireDrgRunState);
                return;
            }
        }
        Vector3 direction = (manager.target.position - manager.transform.position).normalized;
        direction.y = 0; // ����� �� ���������� �����/����

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
