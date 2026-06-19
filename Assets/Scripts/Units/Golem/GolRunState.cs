using UnityEngine;

public class GolRunState : UnitBaseState<GolStateMan>
{
    private float delayTimer = 0f;
    private float delayBeforeRun = 0.5f;
    private bool hasStartedRunning = false;

    public override void EnterState(GolStateMan manager)
    {
        delayTimer = 0f;
        hasStartedRunning = false;

        manager.SetSpeed(manager.walkSpeed);
        manager.navMeshAgent.isStopped = true;
        manager.unitAnimator.SetBool("IsRunning", false);
        manager.unitAnimator.SetBool("IsAttacking", false);
    }

    public override void ExitState(GolStateMan manager)
    {
        manager.unitAnimator.SetBool("IsRunning", false);
    }

    public override void UpdateState(GolStateMan manager)
    {
        if (!manager.canMove) return;

        if (!hasStartedRunning)
        {
            delayTimer += Time.deltaTime;

            if (delayTimer >= delayBeforeRun)
            {
                hasStartedRunning = true;

                Transform target = manager.GetTarget();
                if (target != null)
                {
                    manager.target = target;
                    manager.unitAnimator.SetBool("IsRunning", true);
                    manager.navMeshAgent.isStopped = false;
                    manager.SetDestination(target);
                }
            }
            return;
        }

        Transform newTarget = manager.GetTarget();

        if (newTarget != null)
        {
            manager.target = newTarget;
            manager.unitAnimator.SetBool("IsRunning", true);
            manager.navMeshAgent.isStopped = false;

            Vector3 targetPos = manager.GetAttackPosition(newTarget);
            manager.navMeshAgent.SetDestination(targetPos);

            if (manager.HasReachedTarget())
            {
                manager.SwitchState(manager.golAttackState);
            }
        }
        else
        {
            manager.unitAnimator.SetBool("IsRunning", false);
            manager.navMeshAgent.isStopped = true;
        }
    }
}
