using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools -> ArGame -> Setup Cinematic AI vs AI Battle
// Разово настраивает CinematicLoading.unity для боя ИИ против ИИ на фоне
// полёта дракона: добавляет вторую сторону (AI_Blue) рядом с уже существующим
// ИИ (переименовывается в AI_Red), зеркалит зону спауна "AIzone" на сторону
// синих башен и подтверждает арену через ArenaPlacementEvents.
//
// Важно: SmartAIOpponent / BattlefieldAnalyzer / AICardSelector НЕ изменяются —
// скрипт только конфигурирует их сериализуемые поля через SerializedObject,
// так же как обычно делается руками в инспекторе. Безопасно запускать повторно.
public static class SetupCinematicAIBattle
{
    private const string ScenePath = "Assets/Scenes/CinematicLoading.unity";
    private const string RedAiObjectName = "AI";
    private const string RedAiObjectNameAfterSetup = "AI_Red";
    private const string BlueAiObjectName = "AI_Blue";
    private const string RedZoneName = "AIzone";
    private const string BlueZoneName = "AIzoneBlue";
    private const string BootstrapObjectName = "CinematicAIDirector";

    [MenuItem("Tools/ArGame/Setup Cinematic AI vs AI Battle")]
    public static void Setup()
    {
        var scene = EnsureSceneOpen();

        GameObject redAi = GameObject.Find(RedAiObjectNameAfterSetup) ?? GameObject.Find(RedAiObjectName);
        if (redAi == null)
        {
            Debug.LogError($"[CinematicAIBattle] Не найден существующий объект ИИ ('{RedAiObjectNameAfterSetup}' или '{RedAiObjectName}'). Прерываю настройку.");
            return;
        }
        redAi.name = RedAiObjectNameAfterSetup;

        var redOpponent = redAi.GetComponent<SmartAIOpponent>();
        var redAnalyzer = redAi.GetComponent<BattlefieldAnalyzer>();
        var redSelector = redAi.GetComponent<AICardSelector>();
        if (redOpponent == null || redAnalyzer == null || redSelector == null)
        {
            Debug.LogError($"[CinematicAIBattle] На '{RedAiObjectNameAfterSetup}' не хватает SmartAIOpponent/BattlefieldAnalyzer/AICardSelector. Прерываю настройку.");
            return;
        }

        GameObject redZoneGo = GameObject.Find(RedZoneName);
        if (redZoneGo == null)
        {
            Debug.LogError($"[CinematicAIBattle] Не найдена существующая зона спауна '{RedZoneName}'. Прерываю настройку.");
            return;
        }
        BoxCollider redZone = redZoneGo.GetComponent<BoxCollider>();

        GameObject towerRed = GameObject.Find("TowerRed1");
        GameObject towerBlue = GameObject.Find("TowerBlue1");
        if (towerRed == null || towerBlue == null)
        {
            Debug.LogError("[CinematicAIBattle] Не найдены TowerRed1/TowerBlue1 — нечем определить ось арены для зеркалирования зоны.");
            return;
        }

        // Зеркалим зону Red относительно срединной плоскости между башнями,
        // перпендикулярной линии Red-башня -> Blue-башня. Считаем в мировых
        // координатах, поэтому не зависим от вложенности/масштаба ArenaRootCinematic.
        Vector3 mid = (towerRed.transform.position + towerBlue.transform.position) * 0.5f;
        Vector3 axis = (towerBlue.transform.position - towerRed.transform.position).normalized;
        Vector3 offset = redZoneGo.transform.position - mid;
        Vector3 along = Vector3.Project(offset, axis);
        Vector3 perp = offset - along;
        Vector3 mirroredPosition = mid - along + perp;

        GameObject blueZoneGo = GameObject.Find(BlueZoneName);
        if (blueZoneGo == null)
        {
            blueZoneGo = new GameObject(BlueZoneName);
            blueZoneGo.transform.SetParent(redZoneGo.transform.parent, true);
            Debug.Log($"[CinematicAIBattle] Создана зона спауна '{BlueZoneName}' (зеркало '{RedZoneName}').");
        }
        blueZoneGo.transform.position = mirroredPosition;
        blueZoneGo.transform.rotation = redZoneGo.transform.rotation;
        blueZoneGo.transform.localScale = redZoneGo.transform.localScale;

        BoxCollider blueZone = blueZoneGo.GetComponent<BoxCollider>();
        if (blueZone == null) blueZone = blueZoneGo.AddComponent<BoxCollider>();
        blueZone.isTrigger = redZone.isTrigger;
        blueZone.size = redZone.size;
        blueZone.center = redZone.center;

        GameObject blueAi = GameObject.Find(BlueAiObjectName);
        if (blueAi == null) blueAi = new GameObject(BlueAiObjectName);

        var blueAnalyzer = blueAi.GetComponent<BattlefieldAnalyzer>() ?? blueAi.AddComponent<BattlefieldAnalyzer>();
        var blueSelector = blueAi.GetComponent<AICardSelector>() ?? blueAi.AddComponent<AICardSelector>();
        var blueOpponent = blueAi.GetComponent<SmartAIOpponent>() ?? blueAi.AddComponent<SmartAIOpponent>();

        ConfigureBlueAnalyzer(blueAnalyzer, redAnalyzer);
        ConfigureBlueSelector(blueSelector, redSelector);
        ConfigureOpponent(redOpponent, redAnalyzer, redSelector, redZone);
        ConfigureOpponent(blueOpponent, blueAnalyzer, blueSelector, blueZone, mirrorTeamOf: redOpponent);
        ApplyCinematicPacing(redOpponent);
        ApplyCinematicPacing(blueOpponent);

        int redTeamId = new SerializedObject(redOpponent).FindProperty("aiTeamID").intValue;
        int blueTeamId = new SerializedObject(blueOpponent).FindProperty("aiTeamID").intValue;

        var redSpread = redAi.GetComponent<CinematicSpawnSpread>() ?? redAi.AddComponent<CinematicSpawnSpread>();
        redSpread.Configure(redZone, redTeamId);

        var blueSpread = blueAi.GetComponent<CinematicSpawnSpread>() ?? blueAi.AddComponent<CinematicSpawnSpread>();
        blueSpread.Configure(blueZone, blueTeamId);

        GameObject bootstrap = GameObject.Find(BootstrapObjectName);
        if (bootstrap == null)
        {
            bootstrap = new GameObject(BootstrapObjectName);
            bootstrap.AddComponent<CinematicAIBattleBootstrap>();
            Debug.Log($"[CinematicAIBattle] Создан '{BootstrapObjectName}' — подтверждает ArenaPlacementEvents для обоих ИИ при старте сцены.");
        }
        else if (bootstrap.GetComponent<CinematicAIBattleBootstrap>() == null)
        {
            bootstrap.AddComponent<CinematicAIBattleBootstrap>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CinematicAIBattle] Готово: AI_Red и AI_Blue настроены на свои зоны/башни, ArenaPlacementEvents подтверждается через CinematicAIDirector.");
    }

    private static Scene EnsureSceneOpen()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return scene;
    }

    // Blue защищает TowerPlayer (свои башни) и атакует TowerEnemy (башни Red) —
    // зеркально настройкам Red, у которого playerTowerLayerName/aiTowerLayerName
    // указывают на TowerEnemy/TowerPlayer соответственно (см. поле в инспекторе Red).
    private static void ConfigureBlueAnalyzer(BattlefieldAnalyzer blue, BattlefieldAnalyzer red)
    {
        var redSo = new SerializedObject(red);
        var blueSo = new SerializedObject(blue);

        string redPlayerLayer = redSo.FindProperty("playerTowerLayerName").stringValue;
        string redAiLayer = redSo.FindProperty("aiTowerLayerName").stringValue;
        int redAiTeam = redSo.FindProperty("aiTeamID").intValue;
        int redPlayerTeam = redSo.FindProperty("playerTeamID").intValue;

        blueSo.FindProperty("aiTeamID").intValue = redPlayerTeam;
        blueSo.FindProperty("playerTeamID").intValue = redAiTeam;
        blueSo.FindProperty("playerTowerLayerName").stringValue = redAiLayer;
        blueSo.FindProperty("aiTowerLayerName").stringValue = redPlayerLayer;
        blueSo.FindProperty("dangerZoneRadius").floatValue = redSo.FindProperty("dangerZoneRadius").floatValue;
        blueSo.FindProperty("midFieldRadius").floatValue = redSo.FindProperty("midFieldRadius").floatValue;

        blueSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBlueSelector(AICardSelector blue, AICardSelector red)
    {
        var redSo = new SerializedObject(red);
        var blueSo = new SerializedObject(blue);

        blueSo.FindProperty("defenseUrgencyMultiplier").floatValue = redSo.FindProperty("defenseUrgencyMultiplier").floatValue;
        blueSo.FindProperty("spellEfficiencyThreshold").floatValue = redSo.FindProperty("spellEfficiencyThreshold").floatValue;
        blueSo.FindProperty("antiAirPriorityBonus").floatValue = redSo.FindProperty("antiAirPriorityBonus").floatValue;

        CopyObjectArray(redSo.FindProperty("aiDeck"), blueSo.FindProperty("aiDeck"));

        blueSo.ApplyModifiedPropertiesWithoutUndo();
    }

    // Прописывает spawnZone напрямую (минуя поиск по тегу "AISpawnZone"), чтобы
    // при двух ИИ каждый гарантированно нашёл свою зону, а не первую попавшуюся
    // с этим тегом. Для Red переносит остальные настройки без изменений —
    // только переподключает ссылки на компоненты на этом же GameObject.
    private static void ConfigureOpponent(SmartAIOpponent opponent, BattlefieldAnalyzer analyzer, AICardSelector selector, BoxCollider zone, SmartAIOpponent mirrorTeamOf = null)
    {
        var so = new SerializedObject(opponent);

        so.FindProperty("battlefieldAnalyzer").objectReferenceValue = analyzer;
        so.FindProperty("cardSelector").objectReferenceValue = selector;
        so.FindProperty("spawnZone").objectReferenceValue = zone;
        so.FindProperty("useSpawnArea").boolValue = true;

        if (mirrorTeamOf != null)
        {
            var redSo = new SerializedObject(mirrorTeamOf);
            int redTeam = redSo.FindProperty("aiTeamID").intValue;
            so.FindProperty("aiTeamID").intValue = redTeam == 0 ? 1 : 0;

            so.FindProperty("maxElixir").intValue = redSo.FindProperty("maxElixir").intValue;
            so.FindProperty("startingElixir").intValue = redSo.FindProperty("startingElixir").intValue;
            so.FindProperty("elixirRegenRate").floatValue = redSo.FindProperty("elixirRegenRate").floatValue;
            so.FindProperty("elixirRegenAmount").intValue = redSo.FindProperty("elixirRegenAmount").intValue;
            so.FindProperty("minThinkDelay").floatValue = redSo.FindProperty("minThinkDelay").floatValue;
            so.FindProperty("maxThinkDelay").floatValue = redSo.FindProperty("maxThinkDelay").floatValue;
            so.FindProperty("aggressiveThinkDelay").floatValue = redSo.FindProperty("aggressiveThinkDelay").floatValue;
            so.FindProperty("elixirReserve").intValue = redSo.FindProperty("elixirReserve").intValue;
            so.FindProperty("passivityThreshold").floatValue = redSo.FindProperty("passivityThreshold").floatValue;
            so.FindProperty("handSize").intValue = redSo.FindProperty("handSize").intValue;

            CopyObjectArray(redSo.FindProperty("aiDeck"), so.FindProperty("aiDeck"));
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Только темп для кинематика: думает чаще и не копит лишний эликсир "в
    // запас", чтобы карты выходили заметно чаще, чем в обычном BattleMatch.
    private static void ApplyCinematicPacing(SmartAIOpponent opponent)
    {
        var so = new SerializedObject(opponent);
        so.FindProperty("minThinkDelay").floatValue = 0.4f;
        so.FindProperty("maxThinkDelay").floatValue = 1.2f;
        so.FindProperty("aggressiveThinkDelay").floatValue = 0.3f;
        so.FindProperty("elixirRegenRate").floatValue = 0.8f;
        so.FindProperty("elixirReserve").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyObjectArray(SerializedProperty source, SerializedProperty destination)
    {
        destination.arraySize = source.arraySize;
        for (int i = 0; i < source.arraySize; i++)
        {
            destination.GetArrayElementAtIndex(i).objectReferenceValue = source.GetArrayElementAtIndex(i).objectReferenceValue;
        }
    }
}
