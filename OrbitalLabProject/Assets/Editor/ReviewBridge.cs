#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrbitalLab.EditorTools
{
    /// <summary>
    /// Dev bridge for the AI agent: polls Review/request.txt. Contents: "explorer" or "facility", optionally with "rebuild".
    /// Opens (or rebuilds) that scene, enters Play mode with Review/pending.txt set; when Review/done.txt appears, leaves Play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class ReviewBridge
    {
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Review"));
        static double next;

        static ReviewBridge() { Directory.CreateDirectory(Dir); EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 1.0;
            string req = Path.Combine(Dir, "request.txt"), done = Path.Combine(Dir, "done.txt");

            if (EditorApplication.isPlaying && File.Exists(done)) { File.Delete(done); EditorApplication.isPlaying = false; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(req)) return;

            string text = File.ReadAllText(req).ToLowerInvariant(); File.Delete(req);
            foreach (var f in Directory.GetFiles(Dir, "*.png")) File.Delete(f);
            bool explorer = text.Contains("explorer"); bool rebuild = text.Contains("rebuild");
            if (explorer) { if (rebuild || !File.Exists(ExplorerSceneBuilder.ScenePath)) ExplorerSceneBuilder.Build(); else EditorSceneManager.OpenScene(ExplorerSceneBuilder.ScenePath); }
            else { if (rebuild) FacilityBuilder.Build(); else EditorSceneManager.OpenScene("Assets/Scenes/Facility.unity"); }
            File.WriteAllText(Path.Combine(Dir, "pending.txt"), "1");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
