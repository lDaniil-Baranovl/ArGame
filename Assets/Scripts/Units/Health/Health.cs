using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int CurrentHealth;

    [SerializeField] private UnitStateManager stateManager;
    [SerializeField] private int teamID;
    public bool CanFly = false;
    private HealthBar healthBar;

    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        healthBar = GetComponentInChildren<HealthBar>();
    }

    public int GetTeam() => teamID;
    public void SetTeam(int id) => teamID = id;

    public void ApplyDamage(int damage, string damageSource)
    {
        if (IsDead) return;

        if (CurrentHealth <= 0) return;

        CurrentHealth -= damage;
        Debug.Log($"{gameObject.name} ������� {damage} ����� �� {damageSource}. ��������: {CurrentHealth}");

        if (healthBar != null)
            healthBar.UpdateHealthBar();

        if (CurrentHealth <= 0)
        {
            IsDead = true;

            if (healthBar != null)
                healthBar.OnUnitDeath();

            if (stateManager != null)
                stateManager.OnUnitDie();
        }
    }

    public int GetMaxHealth() => maxHealth;

    // ================= FREEZE SYSTEM =================
    public void Freeze()
    {
        if (stateManager == null) return;

        stateManager.isFrozen = true;

        if (stateManager.navMeshAgent != null)
            stateManager.navMeshAgent.speed = 0;

        if (stateManager.unitAnimator != null)
            stateManager.unitAnimator.speed = 0;
    }

    public void Unfreeze()
    {
        if (stateManager == null) return;

        stateManager.isFrozen = false;

        if (stateManager.navMeshAgent != null)
            stateManager.navMeshAgent.speed = stateManager.walkSpeed;

        if (stateManager.unitAnimator != null)
            stateManager.unitAnimator.speed = 1;
    }


}