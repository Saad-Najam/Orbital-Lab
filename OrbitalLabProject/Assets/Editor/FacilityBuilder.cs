#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using OrbitalLab.Player;
using OrbitalLab.Interaction;

namespace OrbitalLab.EditorTools
{
    /// <summary>
    /// Procedurally builds the greybox research facility scene (hangar, corridor, mission control),
    /// player rig, lighting and the rocket workstation. Run from the menu: OrbitalLab > Build Facility Scene.
    /// Builds automatically once if Assets/Scenes/Facility.unity does not exist.
    /// </summary>
    public static class FacilityBuilder
    {
        const string ScenePath = "Assets/Scenes/Facility.unity";
        const string MatDir = "Assets/Materials";
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) Build();
            };
        }

        [MenuItem("OrbitalLab/Build Facility Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir); Directory.CreateDirectory("Assets/Scenes");
            mats.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("Facility").transform;

            // ---------- lighting ----------
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.35f, 0.4f);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            // ---------- materials ----------
            Material concrete = Mat("Concrete", new Color(0.42f, 0.42f, 0.43f), 0f, 0.15f);
            Material floor = Mat("FloorEpoxy", new Color(0.22f, 0.24f, 0.26f), 0.2f, 0.55f);
            Material panel = Mat("AluminiumPanel", new Color(0.72f, 0.74f, 0.77f), 0.9f, 0.45f);
            Material steel = Mat("DarkSteel", new Color(0.12f, 0.13f, 0.15f), 0.9f, 0.4f);
            Material yellow = Mat("SafetyYellow", new Color(0.9f, 0.65f, 0.05f), 0.1f, 0.3f);
            Material desk = Mat("DeskLaminate", new Color(0.8f, 0.82f, 0.85f), 0f, 0.3f);
            Material screenCyan = Emissive("ScreenCyan", new Color(0.02f, 0.12f, 0.16f), new Color(0.1f, 0.8f, 1.0f), 1.6f);
            Material screenAmber = Emissive("ScreenAmber", new Color(0.12f, 0.07f, 0.01f), new Color(1f, 0.6f, 0.1f), 1.4f);
            Material lightPanel = Emissive("LightPanel", Color.white, new Color(1f, 0.97f, 0.9f), 2.2f);

            // ---------- HANGAR  (x -20..20, z -25..25, h 22) ----------
            Box(root, "HangarFloor", new Vector3(0, -0.1f, 0), new Vector3(40, 0.2f, 50), floor);
            Box(root, "HangarCeiling", new Vector3(0, 22.1f, 0), new Vector3(40, 0.2f, 50), panel);
            Box(root, "Wall_N", new Vector3(0, 11, 25.25f), new Vector3(40.5f, 22, 0.5f), concrete);
            Box(root, "Wall_S", new Vector3(0, 11, -25.25f), new Vector3(40.5f, 22, 0.5f), concrete);
            Box(root, "Wall_W", new Vector3(-20.25f, 11, 0), new Vector3(0.5f, 22, 50), concrete);
            // east wall with doorway (z -2..2, h 3.6)
            Box(root, "Wall_E_a", new Vector3(20.25f, 11, 13.5f), new Vector3(0.5f, 22, 23), concrete);
            Box(root, "Wall_E_b", new Vector3(20.25f, 11, -13.5f), new Vector3(0.5f, 22, 23), concrete);
            Box(root, "Wall_E_lintel", new Vector3(20.25f, 12.8f, 0), new Vector3(0.5f, 18.4f, 4), concrete);
            // safety stripe
            Box(root, "FloorStripe", new Vector3(0, 0.005f, -17), new Vector3(36, 0.01f, 0.35f), yellow, collider: false);
            // roof trusses + light panels
            for (int i = -2; i <= 2; i++)
            {
                Box(root, $"Truss_{i}", new Vector3(0, 21.2f, i * 9f), new Vector3(40, 0.7f, 0.5f), steel, collider: false);
                Box(root, $"LightBar_{i}", new Vector3(0, 20.8f, i * 9f), new Vector3(30, 0.1f, 0.6f), lightPanel, collider: false);
            }
            for (int ix = -1; ix <= 1; ix++) for (int iz = -2; iz <= 2; iz++)
                    PointLight(root, $"HangarLight_{ix}_{iz}", new Vector3(ix * 12f, 19f, iz * 9f), new Color(1f, 0.96f, 0.9f), 55f, 26f);
            // wall columns
            for (int i = -2; i <= 2; i++) { Box(root, $"ColW_{i}", new Vector3(-19.7f, 11, i * 10f), new Vector3(0.8f, 22, 0.8f), steel); Box(root, $"ColN_{i}", new Vector3(i * 9f, 11, 24.7f), new Vector3(0.8f, 22, 0.8f), steel, collider: false); }

            // Rocket display model (1:3.33) on pedestal
            var rocketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Rockets/OrbitalRocketV1.fbx");
            Cyl(root, "RocketPedestal", new Vector3(0, 0.3f, 6), new Vector3(5.5f, 0.3f, 5.5f), steel);
            Box(root, "PedestalRing", new Vector3(0, 0.62f, 6), new Vector3(0.1f, 0.05f, 0.1f), yellow, collider: false);
            if (rocketPrefab != null)
            {
                var rk = (GameObject)PrefabUtility.InstantiatePrefab(rocketPrefab, root);
                rk.name = "RocketDisplay_ORBITAL1"; rk.transform.position = new Vector3(0, 1.2f, 6); rk.transform.localScale = Vector3.one * 0.3f;
                var cap = rk.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, 28f, 0); cap.height = 58f; cap.radius = 2f; cap.direction = 1;
            }
            // service gantry beside the rocket
            for (int i = 0; i < 4; i++) Box(root, $"GantryLeg_{i}", new Vector3(5 + (i % 2) * 2.5f, 8.5f, 3f + (i / 2) * 2.5f), new Vector3(0.35f, 17, 0.35f), yellow);
            for (int lv = 1; lv <= 4; lv++) Box(root, $"GantryDeck_{lv}", new Vector3(6.25f, lv * 4f, 4.25f), new Vector3(3.2f, 0.15f, 3.2f), yellow, collider: lv == 1);

            // Workstation desk (left side)
            Vector3 wsPos = new Vector3(-9f, 0, -10f);
            var ws = Box(root, "WorkstationDesk", wsPos + new Vector3(0, 0.45f, 0), new Vector3(3.2f, 0.9f, 1.4f), desk);
            var screen = Box(ws.transform, "WorkstationScreen", wsPos + new Vector3(0, 1.45f, 0.35f), new Vector3(2.6f, 1.2f, 0.06f), screenCyan);
            screen.transform.rotation = Quaternion.Euler(-8, 0, 0);
            screen.transform.SetParent(root, true); // keep scale clean
            ws.AddComponent<RocketWorkstation>().prompt = "Access Rocket Engineering Workstation";
            // screen is a child-less sibling so the desk's raycast target is the desk; give the screen the same interactable
            screen.AddComponent<RocketWorkstationProxy>().target = ws.GetComponent<RocketWorkstation>();
            PointLight(root, "WorkstationGlow", wsPos + new Vector3(0, 2.2f, -0.8f), new Color(0.3f, 0.85f, 1f), 10f, 7f);
            // equipment racks along west wall
            for (int i = 0; i < 6; i++) Box(root, $"Rack_{i}", new Vector3(-18.6f, 1.1f, -20f + i * 2.2f), new Vector3(1.2f, 2.2f, 1.6f), steel);

            // ---------- CORRIDOR (x 20..32) ----------
            Box(root, "CorridorFloor", new Vector3(26, -0.1f, 0), new Vector3(12, 0.2f, 4), floor);
            Box(root, "CorridorCeiling", new Vector3(26, 3.7f, 0), new Vector3(12, 0.2f, 4), panel);
            Box(root, "CorridorWall_N", new Vector3(26, 1.8f, 2.1f), new Vector3(12, 3.6f, 0.2f), concrete);
            Box(root, "CorridorWall_S", new Vector3(26, 1.8f, -2.1f), new Vector3(12, 3.6f, 0.2f), concrete);
            for (int i = 0; i < 3; i++)
            {
                Box(root, $"CorridorLight_{i}", new Vector3(22 + i * 4f, 3.55f, 0), new Vector3(2.2f, 0.06f, 0.5f), lightPanel, collider: false);
                PointLight(root, $"CorridorPL_{i}", new Vector3(22 + i * 4f, 3.2f, 0), new Color(1f, 0.97f, 0.92f), 7f, 7f);
            }

            // ---------- MISSION CONTROL (x 32..56, z -12..12, h 6) ----------
            Box(root, "MC_Floor", new Vector3(44, -0.1f, 0), new Vector3(24, 0.2f, 24), Mat("MCFloor", new Color(0.08f, 0.09f, 0.11f), 0.3f, 0.6f));
            Box(root, "MC_Ceiling", new Vector3(44, 6.1f, 0), new Vector3(24, 0.2f, 24), steel);
            Box(root, "MC_Wall_E", new Vector3(56.1f, 3, 0), new Vector3(0.2f, 6, 24), steel);
            Box(root, "MC_Wall_N", new Vector3(44, 3, 12.1f), new Vector3(24, 6, 0.2f), steel);
            Box(root, "MC_Wall_S", new Vector3(44, 3, -12.1f), new Vector3(24, 6, 0.2f), steel);
            Box(root, "MC_Wall_W_a", new Vector3(32.1f, 3, 7f), new Vector3(0.2f, 6, 10), steel);
            Box(root, "MC_Wall_W_b", new Vector3(32.1f, 3, -7f), new Vector3(0.2f, 6, 10), steel);
            Box(root, "MC_Wall_W_top", new Vector3(32.1f, 4.9f, 0), new Vector3(0.2f, 2.2f, 4), steel);
            // video wall (3 panels)
            for (int i = 0; i < 3; i++)
            {
                var m = (i == 1) ? screenCyan : screenAmber;
                Box(root, $"VideoWall_{i}", new Vector3(55.9f, 3.4f, -6.2f + i * 6.2f), new Vector3(0.1f, 3.6f, 5.8f), m, collider: false);
            }
            Box(root, "VideoWallFrame", new Vector3(56f, 3.4f, 0), new Vector3(0.12f, 4.0f, 19.6f), steel, collider: false);
            // console rows
            for (int row = 0; row < 3; row++) for (int k = 0; k < 4; k++)
                {
                    float px = 40f + row * 4f, pz = -7.5f + k * 5f;
                    Box(root, $"Console_{row}_{k}", new Vector3(px, 0.45f, pz), new Vector3(1.4f, 0.9f, 3.4f), desk);
                    var sc = Box(root, $"ConsoleScreen_{row}_{k}", new Vector3(px + 0.2f, 1.25f, pz), new Vector3(0.08f, 0.7f, 2.4f), (k + row) % 2 == 0 ? screenCyan : screenAmber, collider: false);
                    sc.transform.rotation = Quaternion.Euler(0, 0, -12);
                }
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++)
                    PointLight(root, $"MC_Light_{i}_{j}", new Vector3(38f + i * 8f, 5.6f, -8f + j * 8f), new Color(0.6f, 0.78f, 1f), 9f, 11f);
            PointLight(root, "MC_WallGlow", new Vector3(53f, 3.5f, 0), new Color(0.35f, 0.8f, 1f), 18f, 14f);

            // ---------- PLAYER ----------
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.1f, -21f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f; cc.skinWidth = 0.05f;
            var camGo = new GameObject("PlayerCamera"); camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false); camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 70; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();
            var fpc = player.AddComponent<FirstPersonController>(); fpc.cameraPivot = camGo.transform;

            // teleport targets
            var t1 = Marker(root, "TP_Hangar", new Vector3(0, 0.1f, -21f), 0);
            var t2 = Marker(root, "TP_Workstation", wsPos + new Vector3(0, 0.1f, -2.5f), 0);
            var t3 = Marker(root, "TP_MissionControl", new Vector3(36f, 0.1f, 0), 90);
            var t4 = Marker(root, "TP_Launchpad_Display", new Vector3(10f, 0.1f, 6f), -90);
            var isys = player.AddComponent<InteractionSystem>();
            isys.cam = cam; isys.player = fpc;
            isys.teleportTargets = new[] { t1, t2, t3, t4 };
            isys.teleportNames = new[] { "Hangar", "Workstation", "Mission Control", "Rocket" };

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[OrbitalLab] Facility scene built: " + ScenePath + "  -> press Play.");
        }

        // ---------- helpers ----------
        static Transform Marker(Transform parent, string name, Vector3 pos, float yaw)
        { var g = new GameObject(name); g.transform.SetParent(parent); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0); return g.transform; }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material m, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            g.transform.SetParent(parent, false); g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(g.GetComponent<BoxCollider>());
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return g;
        }

        static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name;
            g.transform.SetParent(parent, false); g.transform.position = pos; g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }

        static void PointLight(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = pos;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
        }

        static Material Mat(string name, Color c, float metallic, float smooth)
        {
            if (mats.TryGetValue(name, out var existing)) return existing;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", c); m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smooth);
            AssetDatabase.CreateAsset(m, $"{MatDir}/{name}.mat");
            mats[name] = m; return m;
        }

        static Material Emissive(string name, Color baseC, Color emission, float intensity)
        {
            var m = Mat(name, baseC, 0f, 0.5f);
            m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission * intensity);
            EditorUtility.SetDirty(m); return m;
        }
    }
}
#endif
