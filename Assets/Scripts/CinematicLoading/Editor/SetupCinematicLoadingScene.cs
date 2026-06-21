using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Tools -> ArGame -> Setup Cinematic Loading Scene
// Разово настраивает CinematicLoading.unity: добавляет VR-риг (без passthrough),
// дракона с местом для игрока и плейсхолдер-путь полёта. Безопасно запускать
// повторно — если объекты уже созданы, шаг пропускается.
public static class SetupCinematicLoadingScene
{
    private const string ScenePath = "Assets/Scenes/CinematicLoading.unity";
    private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.2.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    private const string DragonPrefabPath = "Assets/PrfUnitFromCards/DragonFlyCold (3).prefab";
    private const string TestMechanicSceneName = "testMechanic";

    [MenuItem("Tools/ArGame/Setup Cinematic Loading Scene")]
    public static void Setup()
    {
        var scene = EnsureSceneOpen();

        DisableStrayCameras(scene);

        GameObject xrOrigin = FindOrInstantiate("CinematicXROrigin", XrOriginPrefabPath);
        ForceOpaqueVR(xrOrigin);

        GameObject dragon = FindOrInstantiate("CinematicDragon", DragonPrefabPath);
        StripGameplayAI(dragon);

        Transform seat = dragon.transform.Find("PlayerSeat");
        if (seat == null)
        {
            var seatGo = new GameObject("PlayerSeat");
            seatGo.transform.SetParent(dragon.transform, false);
            seatGo.transform.localPosition = new Vector3(0f, 2.2f, -1f);
            seat = seatGo.transform;
            Debug.Log("[CinematicSetup] PlayerSeat создан со стартовым офсетом — поправь позицию руками, чтобы камера сидела на спине дракона без клиппинга.");
        }
        xrOrigin.transform.SetParent(seat, false);
        xrOrigin.transform.localPosition = Vector3.zero;
        xrOrigin.transform.localRotation = Quaternion.identity;

        GameObject pathContainer = GameObject.Find("FlightPath");
        if (pathContainer == null)
        {
            pathContainer = new GameObject("FlightPath");
            Vector3 start = dragon.transform.position;
            Vector3[] offsets =
            {
                new Vector3(0, 12, 0),
                new Vector3(15, 16, 20),
                new Vector3(0, 18, 45),
                new Vector3(-15, 14, 70),
                new Vector3(0, 10, 95),
            };
            foreach (var offset in offsets)
            {
                var wp = new GameObject("Waypoint_" + pathContainer.transform.childCount);
                wp.transform.SetParent(pathContainer.transform);
                wp.transform.position = start + offset;
            }
            Debug.Log("[CinematicSetup] FlightPath создан с плейсхолдер-точками — перетащи их в Scene View над реальным полем боя.");
        }

        var flight = dragon.GetComponent<CinematicDragonFlight>();
        if (flight == null) flight = dragon.AddComponent<CinematicDragonFlight>();
        var waypoints = new Transform[pathContainer.transform.childCount];
        for (int i = 0; i < waypoints.Length; i++) waypoints[i] = pathContainer.transform.GetChild(i);
        flight.waypoints = waypoints;

        GameObject manager = GameObject.Find("CinematicLoadingManager");
        if (manager == null) manager = new GameObject("CinematicLoadingManager");
        var controller = manager.GetComponent<CinematicLoadingController>();
        if (controller == null) controller = manager.AddComponent<CinematicLoadingController>();
        controller.nextSceneName = TestMechanicSceneName;
        controller.dragonFlight = flight;

        EnsureScenesInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CinematicSetup] Готово. Проверь позицию PlayerSeat и точки FlightPath в Scene View.");
    }

    private static Scene EnsureSceneOpen()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return scene;
    }

    private static void DisableStrayCameras(Scene scene)
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.gameObject.scene != scene) continue;
            if (cam.transform.root.name == "CinematicXROrigin") continue;
            cam.gameObject.SetActive(false);
        }
    }

    private static GameObject FindOrInstantiate(string instanceName, string prefabPath)
    {
        var existing = GameObject.Find(instanceName);
        if (existing != null) return existing;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[CinematicSetup] Не найден префаб: {prefabPath}");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = instanceName;
        return instance;
    }

    // Камера рига по умолчанию рендерит с альфой 0 — это специально сделано
    // для MR/passthrough сцен (testMechanic). Здесь альфа=0 означает, что
    // компоновщик OpenXR показывает реальную комнату вместо виртуального фона.
    // Делаем альфу непрозрачной только для этого экземпляра рига, не трогая сам префаб.
    private static void ForceOpaqueVR(GameObject xrOrigin)
    {
        if (xrOrigin == null) return;
        foreach (var cam in xrOrigin.GetComponentsInChildren<Camera>(true))
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            var c = cam.backgroundColor;
            c.a = 1f;
            cam.backgroundColor = c;
        }
    }

    // Отключаем боевой AI (NavMesh-путь, атака, поиск цели) — для кинематика
    // дракон ведётся только CinematicDragonFlight по заданным waypoint'ам.
    private static void StripGameplayAI(GameObject dragon)
    {
        if (dragon == null) return;
        var navAgent = dragon.GetComponent<NavMeshAgent>();
        if (navAgent != null) navAgent.enabled = false;

        var stateManager = dragon.GetComponent<StateManagerFlyColdDragon>();
        if (stateManager != null) stateManager.enabled = false;

        var hpCanvas = dragon.transform.Find("UI_inteface/HP_Canvas");
        if (hpCanvas != null) hpCanvas.gameObject.SetActive(false);
    }

    // Tools -> ArGame -> Setup Cinematic Atmosphere (Demo Day Style)
    // Добавляет локальный Global Volume с тёплой пересветкой (Bloom/ColorAdjustments/Vignette)
    // поверх общего DefaultVolumeProfile проекта — действует только пока активна эта сцена,
    // остальные сцены не затрагивает. Безопасно запускать повторно.
    private const string AtmosphereProfilePath = "Assets/Scripts/CinematicLoading/CinematicAtmosphereProfile.asset";

    [MenuItem("Tools/ArGame/Setup Cinematic Atmosphere (Demo Day Style)")]
    public static void SetupAtmosphere()
    {
        var scene = EnsureSceneOpen();

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(AtmosphereProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, AtmosphereProfilePath);
        }

        ConfigureBloom(profile);
        ConfigureColorAdjustments(profile);
        ConfigureVignette(profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        GameObject volumeGo = GameObject.Find("CinematicAtmosphereVolume");
        if (volumeGo == null) volumeGo = new GameObject("CinematicAtmosphereVolume");
        var volume = volumeGo.GetComponent<Volume>();
        if (volume == null) volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.profile = profile;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[CinematicSetup] Тёплая атмосфера в стиле Demo Day добавлена (Bloom/ColorAdjustments/Vignette).");
    }

    private static void ConfigureBloom(VolumeProfile profile)
    {
        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.6f);
        bloom.tint.Override(new Color(1f, 0.8f, 0.6f));
    }

    private static void ConfigureColorAdjustments(VolumeProfile profile)
    {
        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.2f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.92f, 0.85f));
        colorAdjustments.saturation.Override(10f);
        colorAdjustments.contrast.Override(5f);
    }

    private static void ConfigureVignette(VolumeProfile profile)
    {
        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.4f);
    }

    private static void EnsureScenesInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        bool hasCinematic = false;
        foreach (var s in scenes)
            if (s.path == ScenePath) hasCinematic = true;

        if (hasCinematic) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[CinematicSetup] CinematicLoading.unity добавлена в Build Settings.");
    }
}
