using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Tools -> ArGame -> Setup BattleMatch Atmosphere (Demo Day Style)
// BattleMatch — MR/passthrough-сцена (камера рисует SolidColor с alpha=0, скайбокс не
// рендерится), поэтому тёплая атмосфера даётся только Bloom/ColorAdjustments/Vignette
// поверх виртуальной арены и юнитов — реальная комната через Volume не тонируется.
// Volume локальный (isGlobal, но живёт только в этой сцене) — не трогает DefaultVolumeProfile
// остальных сцен. Безопасно запускать повторно.
public static class SetupBattleMatchAtmosphere
{
    private const string ScenePath = "Assets/Scenes/BattleMatch.unity";
    private const string AtmosphereProfilePath = "Assets/Scripts/MR3.0/BattleMatchAtmosphereProfile.asset";

    [MenuItem("Tools/ArGame/Setup BattleMatch Atmosphere (Demo Day Style)")]
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

        GameObject volumeGo = GameObject.Find("BattleMatchAtmosphereVolume");
        if (volumeGo == null) volumeGo = new GameObject("BattleMatchAtmosphereVolume");
        var volume = volumeGo.GetComponent<Volume>();
        if (volume == null) volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        volume.profile = profile;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[BattleMatchAtmosphere] Тёплая атмосфера добавлена. Свет арены поменял цвет/интенсивность — пере-запеки Lightmapping (Window -> Rendering -> Lighting -> Generate Lighting), иначе запечённые тени останутся под старый свет.");
    }

    private static Scene EnsureSceneOpen()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return scene;
    }

    // Значения ниже, чем в CinematicLoading — арена в AR смотрится с близкой дистанции
    // через passthrough-композитинг, сильный Bloom/Vignette там выглядит грязно.
    private static void ConfigureBloom(VolumeProfile profile)
    {
        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.4f);
        bloom.tint.Override(new Color(1f, 0.85f, 0.7f));
    }

    private static void ConfigureColorAdjustments(VolumeProfile profile)
    {
        if (!profile.TryGet(out ColorAdjustments colorAdjustments))
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(0.1f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.94f, 0.88f));
        colorAdjustments.saturation.Override(8f);
        colorAdjustments.contrast.Override(4f);
    }

    private static void ConfigureVignette(VolumeProfile profile)
    {
        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.4f);
    }
}
