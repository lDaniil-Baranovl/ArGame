using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StateManagerFlyColdDragon : UnitStateManager
{
    public DragFlyColdRunState dragFlyColdRunState = new DragFlyColdRunState();
    public DragFlyColdAttackState dragFlyColdAttackState = new DragFlyColdAttackState();
    public DragFlyColdDeathState deathState = new DragFlyColdDeathState();

    private UnitBaseState<StateManagerFlyColdDragon> currentState;

    [SerializeField] public GameObject attackEffect;
    public bool isAttackEffectActive = false;

    public AudioClip attackSound;
    private AudioSource audioSource;

    protected override void Start()
    {
        base.Start();

        // Отключаем NavMeshAgent для летающего юнита
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        attackEffect.SetActive(false);
        SwitchState(dragFlyColdRunState);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    protected override void Update()
    {
        base.Update();
        currentState?.UpdateState(this);
    }

    public void SwitchState(UnitBaseState<StateManagerFlyColdDragon> newState)
    {
        currentState?.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    public override void OnUnitDie()
    {
        if(isDead) return;
        isDead = true;

        gameObject.layer = LayerMask.NameToLayer("Dead");

        if (damageCollider != null) damageCollider.enabled = false;
        if (attackEffect != null) attackEffect.SetActive(false);

        if (unitAnimator != null)
        {
            unitAnimator.SetBool("IsAttacking", false);
            unitAnimator.SetBool("IsRunning", false);
        }
        SwitchState(deathState);
    }

    // Animation Events:
    public void OnOffDamagerFDC(int isOff)
    {
        if (isOff == 0 || isAttackEffectActive == true)
        {
            damageCollider.enabled = false;
            attackEffect.SetActive(false);
            isAttackEffectActive = false;
        }
        else
        {
            if (target != null && attackEffect != null)
            {
                Vector3 hitPos = target.position;
                if (TryGetGroundHeight(hitPos, out float groundHeight))
                    hitPos.y = groundHeight;

                attackEffect.transform.position = hitPos;
            }

            damageCollider.enabled = true;
            attackEffect.SetActive(true);
            isAttackEffectActive = true;
        }
    }

    // Высота поля боя под заданной XZ-позицией, чтобы эффект атаки всегда бил по земле
    private bool TryGetGroundHeight(Vector3 position, out float groundHeight)
    {
        Vector3 rayStart = new Vector3(position.x, position.y + 50f, position.z);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("BattleField")))
        {
            groundHeight = hit.point.y;
            return true;
        }

        groundHeight = position.y;
        return false;
    }
    public void FLCAnimationEvent_ResetDamage()
    {
        if (damageCollider == null) return;

        var damageScript = damageCollider.GetComponent<DamageFDC>();
        damageScript?.DFC_ResetDamageFromAnimation();
    }

    public void PlayAttackSound()
    {
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
}