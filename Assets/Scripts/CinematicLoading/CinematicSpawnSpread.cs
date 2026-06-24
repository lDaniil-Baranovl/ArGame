using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// SmartAIOpponent (не трогаем) всегда спавнит юнита в одной расчётной точке —
// оборонительной у своей башни или атакующей к чужой, без разброса по площади.
// Для кинематика это выглядит как юниты в одну линию. Этот компонент чисто
// визуально расталкивает только что появившихся юнитов своей команды по всей
// зоне спауна, не трогая логику принятия решений ИИ.
public class CinematicSpawnSpread : MonoBehaviour
{
    private BoxCollider zone;
    private int teamID;
    private readonly HashSet<int> seen = new HashSet<int>();

    public void Configure(BoxCollider spawnZone, int aiTeamID)
    {
        zone = spawnZone;
        teamID = aiTeamID;
    }

    void Update()
    {
        if (zone == null) return;

        Bounds bounds = zone.bounds;
        Health[] units = FindObjectsByType<Health>(FindObjectsSortMode.None);

        foreach (var unit in units)
        {
            if (unit == null) continue;

            int id = unit.gameObject.GetInstanceID();
            if (seen.Contains(id)) continue;
            seen.Add(id);

            if (unit.GetTeam() != teamID) continue;

            Vector3 pos = unit.transform.position;
            if (!bounds.Contains(new Vector3(pos.x, bounds.center.y, pos.z))) continue;

            Vector3 target = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                pos.y,
                Random.Range(bounds.min.z, bounds.max.z));

            var agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.Warp(target);
            else
                unit.transform.position = target;
        }
    }
}
