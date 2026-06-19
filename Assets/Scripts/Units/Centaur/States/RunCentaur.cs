using UnityEngine;

public class RunCentaurState : UnitBaseState<CentaurStateManager>
{
    private float delayTimer = 0f;
    private float delayBeforeRun = 0.5f; 
    private bool hasStartedRunning = false;

    public override void EnterState(CentaurStateManager manager)
    {
        delayTimer = 0f;
        hasStartedRunning = false;
        manager.centaur_runTime = 0f;
       
        manager.SetSpeed(manager.walkSpeed);
        manager.navMeshAgent.isStopped = true;
        manager.unitAnimator.SetBool("IsRunningCentaur", false);
        manager.unitAnimator.SetBool("IsAttackingCentaur", false);
    }

    public override void ExitState(CentaurStateManager manager)
    {
        manager.unitAnimator.SetBool("IsRunningCentaur", false);
    }

    public override void UpdateState(CentaurStateManager manager)
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
                    manager.unitAnimator.SetBool("IsRunningCentaur", true);
                    manager.navMeshAgent.isStopped = false;
                    manager.SetDestination(target);
                }
            }
            return;
        }

        manager.centaur_runTime += Time.deltaTime;

        Transform newTarget = manager.GetTarget();

        if (newTarget != null)
        {
            manager.target = newTarget;
            manager.unitAnimator.SetBool("IsRunningCentaur", true);
            manager.navMeshAgent.isStopped = false;

            Vector3 targetPos = manager.GetAttackPosition(newTarget);
            manager.navMeshAgent.SetDestination(targetPos);

            if (manager.HasReachedTarget())
            {
                manager.SwitchState(manager.attackCentaurState);
            }
        }
        else
        {
            manager.unitAnimator.SetBool("IsRunningCentaur", false);
            manager.navMeshAgent.isStopped = true;
        }
    }
}