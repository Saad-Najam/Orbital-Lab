using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalLab.Explorer
{
    /// <summary>Runtime state of one rocket part: renderers, materials, home transform, visibility, highlight.</summary>
    public class PartView
    {
        public PartDef def;
        public Transform tf;
        public Renderer[] renderers;
        public List<Material> mats = new List<Material>();
        public List<Color> baseColors = new List<Color>();
        public Vector3 homeLocalPos;
        public Bounds bounds;           // world bounds at home pose
        public bool hidden;
        public float ghost;             // 0 = opaque, 1 = fully ghosted (alpha ~0.08)
        public Color emission;          // current highlight emission
    }

    /// <summary>
    /// Owns all part views of the rocket: builds URP materials, applies highlight / ghost / hide, and the exploded view.
    /// Presentation only; contains no physics.
    /// </summary>
    public class PartVisuals : MonoBehaviour
    {
        public readonly Dictionary<string, PartView> Parts = new Dictionary<string, PartView>();
        public readonly Dictionary<Collider, PartView> ByCollider = new Dictionary<Collider, PartView>();
        public Transform rocketRoot;
        public float explode;            // 0..1
        public float explodeSpread = 1f;
        public HashSet<string> visibleOnly;                       // Build mode: only placed parts are shown (null = all)
        readonly Dictionary<string, float> placeT = new Dictionary<string, float>();
        public void AnimateIn(string id) { placeT[id] = 0f; }
        Vector3 rocketCenter;
        Dictionary<string, Vector3> asmCenter = new Dictionary<string, Vector3>();
        Dictionary<string, int> asmIndex = new Dictionary<string, int>();

        struct Pal { public Color c; public float metal, smooth; public Pal(Color c, float m, float s) { this.c = c; metal = m; smooth = s; } }
        static readonly Dictionary<string, Pal> Palette = new Dictionary<string, Pal>
        {
            ["PaintWhite"] = new Pal(new Color(0.86f, 0.87f, 0.89f), 0.05f, 0.55f),
            ["PaintBlack"] = new Pal(new Color(0.03f, 0.03f, 0.035f), 0.15f, 0.5f),
            ["BrushedAluminium"] = new Pal(new Color(0.66f, 0.68f, 0.71f), 1f, 0.6f),
            ["StainlessSteel"] = new Pal(new Color(0.55f, 0.56f, 0.58f), 1f, 0.7f),
            ["DarkSteel"] = new Pal(new Color(0.14f, 0.145f, 0.16f), 0.95f, 0.55f),
            ["InsulationFoam"] = new Pal(new Color(0.78f, 0.4f, 0.1f), 0f, 0.15f),
            ["CopperChamber"] = new Pal(new Color(0.72f, 0.38f, 0.2f), 1f, 0.7f),
            ["NozzleAlloy"] = new Pal(new Color(0.22f, 0.21f, 0.21f), 0.9f, 0.6f),
            ["CarbonComposite"] = new Pal(new Color(0.03f, 0.032f, 0.036f), 0.3f, 0.65f),
            ["GoldFoil"] = new Pal(new Color(0.85f, 0.62f, 0.18f), 1f, 0.75f),
            ["SolarCell"] = new Pal(new Color(0.03f, 0.07f, 0.25f), 0.4f, 0.8f),
            ["PCBGreen"] = new Pal(new Color(0.02f, 0.28f, 0.12f), 0.2f, 0.5f),
            ["Chip"] = new Pal(new Color(0.03f, 0.03f, 0.035f), 0.1f, 0.5f),
            ["BatteryBlue"] = new Pal(new Color(0.08f, 0.2f, 0.45f), 0.3f, 0.55f),
            ["HarnessBlack"] = new Pal(new Color(0.01f, 0.01f, 0.012f), 0f, 0.4f),
            ["SafetyOrange"] = new Pal(new Color(0.9f, 0.38f, 0.05f), 0.1f, 0.5f),
            ["Radome"] = new Pal(new Color(0.8f, 0.82f, 0.85f), 0f, 0.7f),
            ["CopperWire"] = new Pal(new Color(0.8f, 0.45f, 0.25f), 1f, 0.7f),
            ["CryoPipe"] = new Pal(new Color(0.75f, 0.8f, 0.85f), 1f, 0.8f),
            ["FuelPipe"] = new Pal(new Color(0.55f, 0.45f, 0.25f), 1f, 0.7f),
        };

        public void Build(Transform root, PartCatalog catalog)
        {
            rocketRoot = root;
            var all = root.GetComponentsInChildren<Transform>(true);
            var byName = new Dictionary<string, Transform>();
            foreach (var t in all) byName[CleanName(t.name)] = t;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var missing = new List<string>();
            foreach (var def in catalog.Data.parts)
            {
                if (!byName.TryGetValue(def.id, out var tf)) { missing.Add(def.id); continue; }
                var pv = new PartView { def = def, tf = tf, renderers = tf.GetComponentsInChildren<Renderer>(), homeLocalPos = tf.localPosition };
                foreach (var r in pv.renderers)
                {
                    var src = r.sharedMaterials; var inst = new Material[src.Length];
                    for (int i = 0; i < src.Length; i++)
                    {
                        string n = src[i] != null ? CleanName(src[i].name) : "";
                        Palette.TryGetValue(n, out var pal); if (pal.c == default) pal = new Pal(Color.gray, 0.3f, 0.5f);
                        var m = new Material(shader) { name = n };
                        m.SetColor("_BaseColor", pal.c); m.SetFloat("_Metallic", pal.metal); m.SetFloat("_Smoothness", pal.smooth);
                        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
                        inst[i] = m; pv.mats.Add(m); pv.baseColors.Add(pal.c);
                    }
                    r.sharedMaterials = inst;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var mc = r.gameObject.GetComponent<MeshCollider>();
                        if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh; ByCollider[mc] = pv;
                    }
                }
                Bounds b = default; bool first = true;
                foreach (var r in pv.renderers) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
                pv.bounds = b; Parts[def.id] = pv;
            }
            if (missing.Count > 0) Debug.LogWarning("[Explorer] parts missing in model: " + string.Join(", ", missing));

            // explode geometry (assembly centres)
            Bounds whole = default; bool f = true;
            foreach (var p in Parts.Values) { if (f) { whole = p.bounds; f = false; } else whole.Encapsulate(p.bounds); }
            rocketCenter = whole.center;
            var sums = new Dictionary<string, (Vector3 s, int n)>();
            foreach (var p in Parts.Values) { sums.TryGetValue(p.def.assembly, out var a); sums[p.def.assembly] = (a.s + p.bounds.center, a.n + 1); }
            int idx = 0;
            foreach (var a in catalog.Data.assemblies) { if (sums.TryGetValue(a.id, out var v)) asmCenter[a.id] = v.s / v.n; asmIndex[a.id] = idx++; }
        }

        public Bounds WholeBounds()
        {
            Bounds b = default; bool f = true;
            foreach (var p in Parts.Values) { if (f) { b = p.bounds; f = false; } else b.Encapsulate(p.bounds); }
            return b;
        }

        static string CleanName(string n) { int i = n.IndexOf(" (Instance)"); if (i > 0) n = n.Substring(0, i); int j = n.IndexOf('.'); return j > 0 && n.Length > 3 && char.IsDigit(n[n.Length - 1]) && n[j + 1] != 'E' ? n.Substring(0, j) : n; }

        // ---------------------------------------------------------------- state application
        public void ApplyAll(string selectedId, HashSet<string> related, string hoverId, bool isolate, HashSet<string> hiddenSubsystems, bool xray, Color accent)
        {
            foreach (var kv in Parts)
            {
                var p = kv.Value; bool sel = kv.Key == selectedId; bool rel = related != null && related.Contains(kv.Key); bool hov = kv.Key == hoverId;
                bool hiddenSub = hiddenSubsystems.Contains(p.def.subsystem) || (visibleOnly != null && !visibleOnly.Contains(kv.Key));
                p.hidden = hiddenSub;
                float ghostTarget = 0f;
                bool reveal = xray || (selectedId != null && !IsShell(selectedId));   // selecting an internal part auto-reveals it
                if (reveal && !sel && !rel) ghostTarget = Mathf.Max(ghostTarget, IsShell(p.def.id) ? 1f : 0f);
                if (isolate && selectedId != null && !sel && !rel) ghostTarget = 1f;
                p.ghost = ghostTarget;
                Color em = Color.black;
                if (sel) em = accent * 0.55f; else if (rel) em = accent * 0.18f; else if (hov) em = accent * 0.28f;
                p.emission = em;
                foreach (var r in p.renderers) r.enabled = !p.hidden;
                for (int i = 0; i < p.mats.Count; i++) SetMat(p.mats[i], p.baseColors[i], p.ghost, em);
            }
        }

        public static bool IsShell(string id) => id.EndsWith("_Skin") || id.StartsWith("PL_Fairing") || id == "IS_InterstageShell";

        static void SetMat(Material m, Color baseC, float ghost, Color emission)
        {
            if (ghost > 0.01f)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; m.SetOverrideTag("RenderType", "Transparent");
                var c = baseC; c.a = Mathf.Lerp(1f, 0.09f, ghost); m.SetColor("_BaseColor", c);
            }
            else
            {
                m.SetFloat("_Surface", 0); m.SetFloat("_SrcBlend", (float)BlendMode.One); m.SetFloat("_DstBlend", (float)BlendMode.Zero); m.SetFloat("_ZWrite", 1);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = -1; m.SetOverrideTag("RenderType", "Opaque");
                m.SetColor("_BaseColor", baseC);
            }
            m.SetColor("_EmissionColor", emission);
        }

        // ---------------------------------------------------------------- exploded view
        void LateUpdate()
        {
            foreach (var p in Parts.Values)
            {
                Vector3 off = Vector3.zero;
                if (explode > 0.0001f)
                {
                    Vector3 ac = asmCenter.TryGetValue(p.def.assembly, out var c) ? c : rocketCenter;
                    int ai = asmIndex.TryGetValue(p.def.assembly, out var ix) ? ix : 0;
                    Vector3 radial = p.bounds.center - ac; radial.y *= 0.6f;
                    Vector3 axial = Vector3.up * (ac.y - rocketCenter.y) * 0.9f;                  // spread assemblies along the axis
                    Vector3 outward = new Vector3(p.bounds.center.x - 0, 0, p.bounds.center.z - 0);
                    if (outward.sqrMagnitude < 0.01f) outward = Vector3.zero;
                    off = (axial + radial * 1.4f + outward.normalized * (IsShell(p.def.id) ? 4f : 1.2f)) * explode * explodeSpread;
                }
                if (placeT.TryGetValue(p.def.id, out var t) && t < 1f)
                {
                    t = Mathf.Min(1f, t + Time.deltaTime * 1.5f); placeT[p.def.id] = t; float e = 1f - (1f - t) * (1f - t) * (1f - t);
                    Vector3 outward = new Vector3(p.bounds.center.x, 0, p.bounds.center.z); outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.zero;
                    off += (Vector3.up * 7f + outward * 4f) * (1f - e);
                }
                p.tf.position = HomeWorld(p) + off;
            }
        }

        Vector3 HomeWorld(PartView p) => p.tf.parent != null ? p.tf.parent.TransformPoint(p.homeLocalPos) : p.homeLocalPos;
    }
}
