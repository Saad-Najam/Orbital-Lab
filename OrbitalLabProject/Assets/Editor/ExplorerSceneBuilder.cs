#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using OrbitalLab.Explorer;

namespace OrbitalLab.EditorTools
{
    /// <summary>Builds Assets/Scenes/Explorer.unity: studio lighting, post-processing volume, rocket model, explorer app and UI settings.</summary>
    public static class ExplorerSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Explorer.unity";
        const string ModelPath = "Assets/Models/Explorer/OrbitalRocketExplorer.fbx";

        [InitializeOnLoadMethod]
        static void Auto()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null && !EditorApplication.isPlayingOrWillChangePlaymode) Build();
            };
        }

        [MenuItem("OrbitalLab/Build Explorer Scene")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogError("[OrbitalLab] Missing model " + ModelPath); return; }
            Directory.CreateDirectory("Assets/Resources"); Directory.CreateDirectory("Assets/Settings"); Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Materials");

            // ---- panel settings (UI Toolkit)
            var psPath = "Assets/Resources/ExplorerPanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, psPath); }
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize; ps.referenceResolution = new Vector2Int(1920, 1080); ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ps.match = 0.5f;
            foreach (var g in AssetDatabase.FindAssets("UnityDefaultRuntimeTheme t:ThemeStyleSheet"))
            { var th = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(g)); if (th != null) { ps.themeStyleSheet = th; break; } }
            EditorUtility.SetDirty(ps);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- skybox (studio gradient + metal reflections)
            var sky = new Material(Shader.Find("Skybox/Procedural")) { name = "StudioSky" };
            sky.SetColor("_SkyTint", new Color(0.12f, 0.2f, 0.32f)); sky.SetColor("_GroundColor", new Color(0.04f, 0.05f, 0.07f));
            sky.SetFloat("_Exposure", 0.9f); sky.SetFloat("_AtmosphereThickness", 0.5f); sky.SetFloat("_SunSize", 0.02f);
            AssetDatabase.CreateAsset(sky, "Assets/Materials/StudioSky.mat");
            RenderSettings.skybox = sky; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.38f, 0.50f); RenderSettings.ambientEquatorColor = new Color(0.20f, 0.24f, 0.30f); RenderSettings.ambientGroundColor = new Color(0.06f, 0.07f, 0.09f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox; RenderSettings.reflectionIntensity = 1.1f;
            DynamicGI.UpdateEnvironment();

            // ---- lights
            var key = new GameObject("KeyLight").AddComponent<Light>(); key.type = LightType.Directional; key.color = new Color(1f, 0.96f, 0.9f); key.intensity = 2.6f; key.shadows = LightShadows.Soft; key.shadowStrength = 0.85f;
            key.transform.rotation = Quaternion.Euler(38, -38, 0);
            var fill = new GameObject("FillLight").AddComponent<Light>(); fill.type = LightType.Directional; fill.color = new Color(0.55f, 0.7f, 1f); fill.intensity = 0.6f; fill.shadows = LightShadows.None; fill.transform.rotation = Quaternion.Euler(20, 140, 0);
            var rim = new GameObject("RimLight").AddComponent<Light>(); rim.type = LightType.Directional; rim.color = new Color(0.7f, 0.85f, 1f); rim.intensity = 1.1f; rim.shadows = LightShadows.None; rim.transform.rotation = Quaternion.Euler(-10, 200, 0);

            // ---- floor + ring
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "StudioFloor" }; floorMat.SetColor("_BaseColor", new Color(0.05f, 0.06f, 0.08f)); floorMat.SetFloat("_Metallic", 0.6f); floorMat.SetFloat("_Smoothness", 0.78f);
            AssetDatabase.CreateAsset(floorMat, "Assets/Materials/StudioFloor.mat");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name = "StudioFloor"; floor.transform.position = new Vector3(0, -0.6f, 0); floor.transform.localScale = new Vector3(90, 0.05f, 90);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat; Object.DestroyImmediate(floor.GetComponent<Collider>());
            var ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "FloorRing" }; ringMat.SetColor("_BaseColor", new Color(0.02f, 0.05f, 0.08f)); ringMat.EnableKeyword("_EMISSION"); ringMat.SetColor("_EmissionColor", new Color(0.15f, 0.55f, 0.9f) * 1.4f);
            AssetDatabase.CreateAsset(ringMat, "Assets/Materials/FloorRing.mat");
            foreach (var r in new[] { 9f, 16f, 26f })
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "FloorRing_" + r; ring.transform.position = new Vector3(0, -0.57f, 0); ring.transform.localScale = new Vector3(r * 2, 0.005f, r * 2);
                ring.GetComponent<MeshRenderer>().sharedMaterial = ringMat; Object.DestroyImmediate(ring.GetComponent<Collider>());
                var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder); inner.name = "FloorRingInner_" + r; inner.transform.position = new Vector3(0, -0.56f, 0); inner.transform.localScale = new Vector3(r * 2 - 0.12f, 0.007f, r * 2 - 0.12f);
                inner.GetComponent<MeshRenderer>().sharedMaterial = floorMat; Object.DestroyImmediate(inner.GetComponent<Collider>());
            }

            // ---- rocket
            var rocket = (GameObject)PrefabUtility.InstantiatePrefab(model); rocket.name = "ORBITAL1_Rocket"; rocket.transform.position = Vector3.zero;

            // ---- camera + post
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 38; cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f; cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.Skybox; camGo.AddComponent<AudioListener>();
            var cd = camGo.AddComponent<UniversalAdditionalCameraData>(); cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cd.antialiasingQuality = AntialiasingQuality.High;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(1.0f); bloom.intensity.Override(0.22f); bloom.scatter.Override(0.55f);
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.26f); vig.smoothness.Override(0.45f);
            var ca = profile.Add<ColorAdjustments>(true); ca.contrast.Override(8f); ca.saturation.Override(6f);
            AssetDatabase.CreateAsset(profile, "Assets/Settings/ExplorerVolumeProfile.asset");
            var vol = new GameObject("PostProcessVolume").AddComponent<Volume>(); vol.isGlobal = true; vol.sharedProfile = profile;

            // ---- app
            var app = new GameObject("ExplorerApp").AddComponent<ExplorerApp>(); app.rocketInstance = rocket;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath); list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[OrbitalLab] Explorer scene built: " + ScenePath + " -> press Play.");
        }
    }
}
#endif
