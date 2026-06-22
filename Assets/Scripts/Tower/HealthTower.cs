using UnityEngine;

public class HealthTower : MonoBehaviour
{
    [Header("��������� �������� �����")]
    public int tower_maxHealth = 1000;
    public int tower_currentHealth;

    [Header("������ �����")]
    public GameObject tower;
    [Header("���� - �����")]
    public GameObject tower_ruines;
    [SerializeField] public GameObject effectBroken1;
    [SerializeField] public GameObject effectBroken2;

    // 0 = �����, 1 = �������
    [SerializeField] private int teamID;
    void Start()
    {
        if (effectBroken1 != null && effectBroken2 != null)
        {
            effectBroken1.SetActive(false);
            effectBroken2.SetActive(false);
        }
        tower_currentHealth = tower_maxHealth;
        tower_ruines.SetActive(false);
    }
    public void Tower_ApplyDamage(int damageAmount)
    {
        tower_currentHealth -= damageAmount;
        tower_currentHealth = Mathf.Clamp(tower_currentHealth, 0, tower_maxHealth);
        Debug.Log($"����� �������� ���� {damageAmount} ������� ������� {tower_currentHealth}");
        TowerBarHealth healthBar = GetComponentInChildren<TowerBarHealth>();
        if (healthBar != null)
            healthBar.UpdateHealthBarTower();
        if (tower_currentHealth <= 0)
        {
            Tower_HandleDestruction();
        }
    }
    private void Tower_HandleDestruction()
    {
        if (tower_ruines != null && effectBroken1 != null && effectBroken2 != null)
        {
            Destroy(tower);
            tower_ruines.SetActive(true);
            effectBroken1.SetActive(true);
            effectBroken2.SetActive(true);
        }

        BattleManager.Instance.TowerDestroyed(teamID);
    }
    public void Tower_Destroy()
    {
        tower_currentHealth = 0;
        Tower_HandleDestruction();
    }
    public void OnDamageDetected(int damageAmount)
    {
        Debug.Log($"[DamageDetector] ������� ����: {damageAmount}");
        Tower_ApplyDamage(damageAmount);
    }
    public int GetTeam()
    {
        return teamID;
    }
    public void SetTeam(int id)
    {
        teamID = id;
    }

    public void BackToMenu()
    {
        ScreenFader.LoadScene("MainMenu");
    }
}