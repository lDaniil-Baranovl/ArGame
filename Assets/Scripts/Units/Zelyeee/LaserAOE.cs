using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LaserAOE : MonoBehaviour
{
    [Header("Percent Damage Per Wave (sum must be 100)")]
    [Range(0, 100)] public float wave1Percent = 30f;
    [Range(0, 100)] public float wave2Percent = 25f;
    [Range(0, 100)] public float wave3Percent = 20f;
    [Range(0, 100)] public float wave4Percent = 15f;
    [Range(0, 100)] public float wave5Percent = 10f;

    [SerializeField] private float waveInterval = 1.2f;

    [Header("Who take damage")]
    [SerializeField] private int teamID = 0;

    private bool isWorking = false;

    private readonly Dictionary<Health, int> unitWave = new Dictionary<Health, int>();
    private readonly HashSet<Health> unitsInZone = new HashSet<Health>();

    [Header("Wave Timing")]
    [SerializeField] private float waveActiveTime = 4f;
    [SerializeField] private float waveFadeTime = 0.4f;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip LAZERSound;

    [SerializeField] private float baseInterval = 0.3f;
    [SerializeField] private float randomOffset = 0.15f;

    [Header("Effect Lifetime")]
    [SerializeField] private float effectDuration = 15f;
    [SerializeField] private float blockEdgeTime = 0.3f;

    private AudioSource audioSource;
    private float startTime;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        startTime = Time.time;

        StartCoroutine(SoundRoutine());

        Destroy(gameObject, effectDuration);
    }

    private IEnumerator SoundRoutine()
    {
        float waveStart = Time.time;

        while (true)
        {
            float elapsedTotal = Time.time - startTime;
            if (elapsedTotal > effectDuration - blockEdgeTime)
                yield break;

            float waveElapsed = Time.time - waveStart;
            if (waveElapsed < waveActiveTime)
            {
                if (elapsedTotal >= blockEdgeTime)
                {
                    audioSource.PlayOneShot(LAZERSound);
                }

                float delay = baseInterval + Random.Range(-randomOffset, randomOffset);
                if (delay < 0.1f) delay = 0.1f;

                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return new WaitForSeconds(waveFadeTime);
                waveStart = Time.time;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Health hp = other.GetComponent<Health>();
        if (hp == null) return;

        if (hp.GetTeam() == teamID)
        {
            unitsInZone.Add(hp);
            unitWave[hp] = 0;
            TryStartDamage();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        Health hp = other.GetComponent<Health>();
        if (hp == null) return;

        unitsInZone.Remove(hp);
        unitWave.Remove(hp);
    }


    private void TryStartDamage()
    {
        if (!isWorking)
            StartCoroutine(DamageRoutine());
    }


    private IEnumerator DamageRoutine()
    {
        isWorking = true;

        while (unitsInZone.Count > 0)
        {
            foreach (var unit in unitsInZone)
            {
                if (unit == null || unit.IsDead)
                    continue;

                int wave = unitWave[unit];
                float damage = 0;

                switch (wave)
                {
                    case 0:
                        damage = unit.GetMaxHealth() * (wave1Percent / 100f);
                        unitWave[unit] = 1;
                        break;

                    case 1:
                        damage = unit.GetMaxHealth() * (wave2Percent / 100f);
                        unitWave[unit] = 2;
                        break;

                    case 2:
                        damage = unit.GetMaxHealth() * (wave3Percent / 100f);
                        unitWave[unit] = 3;
                        break;

                    case 3:
                        damage = unit.GetMaxHealth() * (wave4Percent / 100f);
                        unitWave[unit] = 4;
                        break;

                    case 4:
                        damage = unit.GetMaxHealth() * (wave5Percent / 100f);
                        unitWave[unit] = 5;
                        break;

                    case 5:
                        continue;
                }

                unit.ApplyDamage(Mathf.RoundToInt(damage), "Laser AOE % Wave");
            }

            yield return new WaitForSeconds(waveInterval);
        }

        isWorking = false;
    }
}
