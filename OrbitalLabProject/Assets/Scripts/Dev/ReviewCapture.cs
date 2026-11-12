using System.Collections;
using System.IO;
using UnityEngine;
using OrbitalLab.Player;
using OrbitalLab.Explorer;

namespace OrbitalLab.Dev
{
    /// <summary>
    /// Development tool: when Review/pending.txt exists at Play start, drives the running scene through preset views
    /// and saves screenshots to Review/*.png, then writes Review/done.txt. Lets an external agent inspect the render.
    /// Does nothing in normal play (Editor only).
    /// </summary>
    public static class ReviewCapture
    {
        struct View { public string name; public Vector3 pos; public float yaw, pitch; public View(string n, Vector3 p, float y, float pi) { name = n; pos = p; yaw = y; pitch = pi; } }
        static readonly View[] FacilityViews =
        {
            new View("01_hangar_entry", new Vector3(0, 0.1f, -22f), 0, -4), new View("02_rocket_close", new Vector3(-5f, 0.1f, -3f), 25, -12),
            new View("03_workstation", new Vector3(-9f, 0.1f, -13.2f), 0, 8), new View("05_corridor", new Vector3(21f, 0.1f, 0), 90, 0),
            new View("06_mission_control", new Vector3(34f, 0.1f, 0), 90, 4), new View("07_mc_consoles", new Vector3(46f, 0.1f, -9f), 55, 5),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
#if UNITY_EDITOR
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Review"));
            if (!File.Exists(Path.Combine(dir, "pending.txt"))) return;
            new GameObject("ReviewCaptureRunner").AddComponent<Runner>().dir = dir;
#endif
        }

        class Runner : MonoBehaviour
        {
            public string dir;
            IEnumerator Shot(string name)
            {
                for (int i = 0; i < 25; i++) yield return null;
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 3);
                for (int i = 0; i < 8; i++) yield return null;
            }

            IEnumerator Start()
            {
                File.Delete(Path.Combine(dir, "pending.txt"));
                for (int i = 0; i < 30; i++) yield return null;
                var app = FindAnyObjectByType<ExplorerApp>();
                if (app != null)
                {
                    yield return Shot("e01_overview");
                    app.DebugSelect("S1E_Nozzle"); yield return Shot("e02_select_nozzle");
                    app.DebugXray(true); app.DebugSelect("AV_FlightComputer"); yield return Shot("e03_xray_flight_computer");
                    app.DebugXray(false); app.DebugSelect(null); app.DebugFrameAll(); app.DebugExplode(1f); yield return Shot("e04_exploded");
                    app.DebugExplode(0.5f); app.DebugFrameAll(); app.DebugSelect("S2E_NozzleExtension"); yield return Shot("e05_exploded_half_select");
                    app.DebugExplode(0f); app.DebugCamera(-30, 8, 14, new Vector3(0, 2, 0)); app.DebugSelect("S1_Engine_3"); yield return Shot("e06_engine_cluster");
                    app.DebugSelect(null); app.DebugBuildMode(true); yield return Shot("b01_build_empty");
                    app.DebugPlace("S1_ThrustStructure"); app.DebugPlace("S1E_Injector"); app.DebugPlace("S1E_Nozzle"); yield return Shot("b02_place_blocked_nozzle");
                    for (int st = 0; st < 3; st++) app.DebugPlaceStep(st); app.DebugFrameAll(); yield return Shot("b03_stage1_built");
                    for (int st = 3; st < 5; st++) app.DebugPlaceStep(st); app.DebugRemove("AV_FlightComputer"); app.DebugFrameAll(); yield return Shot("b04_missing_flight_computer");
                    app.DebugPlaceStep(4); app.DebugPlaceStep(5); app.DebugFrameAll(); app.DebugFly(); yield return Shot("b05_flight_result");
                }
                else
                {
                    var player = FindAnyObjectByType<FirstPersonController>(); var cam = Camera.main;
                    foreach (var v in FacilityViews)
                    {
                        if (player != null) player.TeleportTo(v.pos, v.yaw);
                        if (cam != null) cam.transform.localRotation = Quaternion.Euler(v.pitch, 0, 0);
                        yield return Shot(v.name);
                    }
                }
                File.WriteAllText(Path.Combine(dir, "done.txt"), "ok");
            }
        }
    }
}
