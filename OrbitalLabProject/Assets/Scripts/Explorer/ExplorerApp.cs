using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OrbitalLab.Explorer
{
    /// <summary>
    /// Rocket Anatomy Explorer: orbit around the model, click or search any part, read what it is / why it exists / how it works,
    /// toggle subsystem layers, X-ray, isolate, explode, and test yourself with quizzes. Educational model, not a game.
    /// </summary>
    public partial class ExplorerApp : MonoBehaviour
    {
        public GameObject rocketInstance;
        PartCatalog catalog; PartVisuals visuals; OrbitCamera orbit; UIDocument doc; VisualElement root;

        // state
        string selectedId, hoverId; HashSet<string> related = new HashSet<string>();
        readonly HashSet<string> hiddenSubs = new HashSet<string>();
        readonly HashSet<string> studied = new HashSet<string>();
        bool xray, isolate, labels = true; string search = "";
        float explodeGoal;

        // ui refs
        VisualElement leftTree, infoPanel, labelLayer, tooltip; Label tooltipText, progressLabel;
        readonly Dictionary<string, VisualElement> treeRows = new Dictionary<string, VisualElement>();
        readonly Dictionary<string, Label> floatLabels = new Dictionary<string, Label>();
        Label xrayBtn, isoBtn, labelBtn;

        // palette
        static readonly Color BG = C32(10, 15, 22, 235), PANEL = C32(15, 22, 32, 238), LINE = C32(38, 52, 68, 255),
            TXT = C32(230, 238, 247, 255), MUTED = C32(143, 163, 184, 255), ACCENT = C32(77, 181, 255, 255), CARD = C32(22, 32, 45, 255);

        static Color C32(byte r, byte g, byte b, byte a) => new Color32(r, g, b, a);

        void Start()
        {
            catalog = PartCatalog.Load();
            visuals = gameObject.AddComponent<PartVisuals>(); visuals.Build(rocketInstance.transform, catalog);
            var cam = Camera.main; orbit = cam.gameObject.AddComponent<OrbitCamera>(); orbit.cam = cam;
            var wb = visuals.WholeBounds(); orbit.FrameAll(wb); orbit.SnapToGoals();
            Debug.Log($"[Explorer] model bounds size {wb.size}, parts {visuals.Parts.Count}");
            orbit.FrameHint = () => orbit.FrameAll(visuals.WholeBounds());
            BuildUI();
            orbit.BlockInput = IsPointerOverUI;
            Refresh();
        }

        // ============================================================ interaction
        void Update()
        {
            var mouse = Mouse.current; var kb = Keyboard.current; if (mouse == null) return;
            visuals.explode = Mathf.Lerp(visuals.explode, explodeGoal, 1f - Mathf.Exp(-6f * Time.deltaTime));

            bool overUI = IsPointerOverUI();
            string newHover = null;
            if (!overUI && !orbit.IsDragging)
            {
                var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                var hits = Physics.RaycastAll(ray, 500f); System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)       // first visible, non-ghosted part: ghosted shells are click-through
                    if (visuals.ByCollider.TryGetValue(hit.collider, out var pv) && !pv.hidden && pv.ghost < 0.5f) { newHover = pv.def.id; break; }
            }
            if (newHover != hoverId) { hoverId = newHover; Refresh(); }

            if (mouse.leftButton.wasReleasedThisFrame && !overUI && orbit.DragDistance <= 4f) { if (hoverId != null) Select(hoverId); else Deselect(); }
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Deselect();

            UpdateFloatingLabels(mouse.position.ReadValue());
        }

        bool IsPointerOverUI()
        {
            if (root == null || root.panel == null || Mouse.current == null) return false;
            var p = Mouse.current.position.ReadValue();
            var pos = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(p.x, Screen.height - p.y));
            var el = root.panel.Pick(pos);
            return el != null && el != root;
        }

        void Select(string id)
        {
            if (!visuals.Parts.ContainsKey(id)) return;
            selectedId = id; var def = catalog.ById[id];
            related = new HashSet<string>(def.connects ?? new string[0]); studied.Add(id);
            orbit.FocusOn(visuals.Parts[id].bounds, 2.6f);
            Refresh(); BuildInfo(def);
            if (treeRows.TryGetValue(id, out var row)) treeRows.Values.Where(r => r != row).ToList().ForEach(r => Selected(r, false));
            foreach (var kv in treeRows) Selected(kv.Value, kv.Key == id);
        }
        void Deselect() { selectedId = null; related.Clear(); foreach (var r in treeRows.Values) Selected(r, false); Refresh(); BuildInfo(null); }

        void Refresh()
        {
            if (visuals == null) return;
            Color acc = ACCENT;
            if (selectedId != null && catalog.ById.TryGetValue(selectedId, out var d) && catalog.SubById.TryGetValue(d.subsystem, out var s)) acc = s.Color;
            if (selectedId == null && hoverId != null && catalog.ById.TryGetValue(hoverId, out var d2) && catalog.SubById.TryGetValue(d2.subsystem, out var s2)) acc = s2.Color;
            visuals.ApplyAll(selectedId, related, hoverId, isolate, hiddenSubs, xray, acc);
            if (tooltip != null) { tooltip.style.display = hoverId != null ? DisplayStyle.Flex : DisplayStyle.None; if (hoverId != null) tooltipText.text = catalog.ById[hoverId].name; }
            if (progressLabel != null) progressLabel.text = $"Explored {studied.Count} / {catalog.Data.parts.Length} parts";
            SetPill(xrayBtn, xray); SetPill(isoBtn, isolate); SetPill(labelBtn, labels);
        }

        // ============================================================ UI construction
        void BuildUI()
        {
            var go = new GameObject("ExplorerUI"); go.SetActive(false);
            doc = go.AddComponent<UIDocument>();
            var ps = Resources.Load<PanelSettings>("ExplorerPanelSettings");
            if (ps == null) { ps = ScriptableObject.CreateInstance<PanelSettings>(); ps.scaleMode = PanelScaleMode.ScaleWithScreenSize; ps.referenceResolution = new Vector2Int(1920, 1080); ps.match = 0.5f; }
            doc.panelSettings = ps; go.SetActive(true);
            root = doc.rootVisualElement; root.style.position = Position.Absolute; root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            BuildTopBar(); BuildLeft(); BuildRight(); BuildBottom(); BuildFloating();
            BuildInfo(null);
        }

        VisualElement Box(VisualElement parent, Color bg, float radius = 0)
        {
            var e = new VisualElement(); e.style.backgroundColor = bg; Radius(e, radius); parent?.Add(e); return e;
        }
        static void Radius(VisualElement e, float r) { e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r; }
        static void Border(VisualElement e, float w, Color c) { e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w; e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c; }
        static void Pad(VisualElement e, float t, float r, float b, float l) { e.style.paddingTop = t; e.style.paddingRight = r; e.style.paddingBottom = b; e.style.paddingLeft = l; }
        static void Abs(VisualElement e, float? l, float? t, float? r, float? b) { e.style.position = Position.Absolute; if (l != null) e.style.left = l.Value; if (t != null) e.style.top = t.Value; if (r != null) e.style.right = r.Value; if (b != null) e.style.bottom = b.Value; }
        Label Text(VisualElement parent, string t, int size, Color c, bool bold = false)
        {
            var l = new Label(t); l.style.fontSize = size; l.style.color = c; l.style.whiteSpace = WhiteSpace.Normal; if (bold) l.style.unityFontStyleAndWeight = FontStyle.Bold; l.pickingMode = PickingMode.Ignore; parent?.Add(l); return l;
        }
        Label Pill(VisualElement parent, string t, System.Action onClick)
        {
            var l = new Label(t); l.style.fontSize = 13; l.style.color = TXT; Pad(l, 6, 12, 6, 12); Radius(l, 14); l.style.marginRight = 6; Border(l, 1, LINE);
            l.style.backgroundColor = CARD; l.RegisterCallback<ClickEvent>(_ => onClick()); parent?.Add(l); return l;
        }
        void SetPill(Label l, bool on) { if (l == null) return; l.style.backgroundColor = on ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.25f) : CARD; Border(l, 1, on ? ACCENT : LINE); }

        void BuildTopBar()
        {
            var bar = Box(root, BG); Abs(bar, 0, 0, 0, null); bar.style.height = 54; bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center; Pad(bar, 0, 18, 0, 18);
            bar.style.borderBottomWidth = 1; bar.style.borderBottomColor = LINE;
            var t = Text(bar, "ORBITAL LAB", 18, ACCENT, true); t.style.marginRight = 14;
            Text(bar, "Rocket Anatomy Explorer", 15, TXT);
            var sp = new VisualElement(); sp.style.flexGrow = 1; bar.Add(sp);
            progressLabel = Text(bar, "", 13, MUTED); progressLabel.style.marginRight = 18;
            exploreBtn = Pill(bar, "Explore", () => SetMode(false));
            buildBtn = Pill(bar, "Build a rocket", () => SetMode(true));
            Pill(bar, "Reset view", () => { Deselect(); explodeGoal = 0; orbit.FrameAll(visuals.WholeBounds()); });
            SetPill(exploreBtn, true);
        }

        void BuildLeft()
        {
            var p = Box(root, PANEL, 10); leftPanel = p; Abs(p, 14, 66, null, 74); p.style.width = 330; Border(p, 1, LINE); p.style.flexDirection = FlexDirection.Column; Pad(p, 12, 12, 12, 12);
            var search = new TextField(); search.style.marginBottom = 8; search.value = "";
            search.RegisterValueChangedCallback(e => { this.search = e.newValue.Trim().ToLowerInvariant(); FilterTree(); }); p.Add(search);
            var ph = Text(p, "Search parts (e.g. nozzle, battery, tank)", 11, MUTED); ph.style.marginBottom = 8;

            Text(p, "LAYERS  (click to show/hide)", 11, MUTED, true).style.marginBottom = 4;
            var chips = new VisualElement(); chips.style.flexDirection = FlexDirection.Row; chips.style.flexWrap = Wrap.Wrap; chips.style.marginBottom = 8; p.Add(chips);
            foreach (var s in catalog.Data.subsystems)
            {
                var sub = s; var chip = new Label(sub.name); chip.style.fontSize = 11; chip.style.color = TXT; Pad(chip, 3, 8, 3, 8); Radius(chip, 10); chip.style.marginRight = 4; chip.style.marginBottom = 4;
                chip.style.backgroundColor = new Color(sub.Color.r / 255f, sub.Color.g / 255f, sub.Color.b / 255f, 0.35f); Border(chip, 1, sub.Color);
                chip.RegisterCallback<ClickEvent>(_ => { if (!hiddenSubs.Remove(sub.id)) hiddenSubs.Add(sub.id); chip.style.opacity = hiddenSubs.Contains(sub.id) ? 0.35f : 1f; Refresh(); });
                chips.Add(chip);
            }
            Text(p, "PARTS", 11, MUTED, true).style.marginBottom = 4;
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; p.Add(sv); leftTree = sv;
            foreach (var a in catalog.Data.assemblies)
            {
                var parts = catalog.Data.parts.Where(x => x.assembly == a.id).ToList(); if (parts.Count == 0) continue;
                var head = Text(sv, a.name.ToUpperInvariant(), 11, ACCENT, true); head.style.marginTop = 8; head.style.marginBottom = 2; head.name = "head_" + a.id;
                foreach (var part in parts)
                {
                    var def = part; var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; Pad(row, 5, 6, 5, 6); Radius(row, 6);
                    var dot = new VisualElement(); dot.style.width = 9; dot.style.height = 9; Radius(dot, 5); dot.style.backgroundColor = (Color)catalog.SubById[def.subsystem].Color; dot.style.marginRight = 8; dot.pickingMode = PickingMode.Ignore; row.Add(dot);
                    Text(row, def.name, 13, TXT);
                    row.RegisterCallback<ClickEvent>(_ => Select(def.id));
                    row.RegisterCallback<PointerEnterEvent>(_ => { if (selectedId != def.id) row.style.backgroundColor = CARD; hoverId = def.id; Refresh(); });
                    row.RegisterCallback<PointerLeaveEvent>(_ => { if (selectedId != def.id) row.style.backgroundColor = Color.clear; if (hoverId == def.id) hoverId = null; Refresh(); });
                    row.userData = def; sv.Add(row); treeRows[def.id] = row;
                }
            }
            exploreLeftChildren = p.Children().ToList();
        }
        void Selected(VisualElement row, bool on) { row.style.backgroundColor = on ? new Color(ACCENT.r, ACCENT.g, ACCENT.b, 0.22f) : Color.clear; }
        void FilterTree()
        {
            foreach (var kv in treeRows) { var def = catalog.ById[kv.Key]; bool ok = search.Length == 0 || def.name.ToLowerInvariant().Contains(search) || def.summary.ToLowerInvariant().Contains(search) || catalog.SubById[def.subsystem].name.ToLowerInvariant().Contains(search); kv.Value.style.display = ok ? DisplayStyle.Flex : DisplayStyle.None; }
        }

        void BuildRight()
        {
            infoPanel = Box(root, PANEL, 10); Abs(infoPanel, null, 66, 14, 74); infoPanel.style.width = 410; Border(infoPanel, 1, LINE); Pad(infoPanel, 0, 0, 0, 0);
        }

        void BuildInfo(PartDef d)
        {
            infoPanel.Clear();
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; Pad(sv, 16, 18, 16, 18); infoPanel.Add(sv);
            if (d == null && buildMode) { BuildReview(sv); return; }
            if (d == null)
            {
                Text(sv, "Explore the rocket", 20, TXT, true).style.marginBottom = 8;
                Text(sv, "Click any part in the 3D view, or choose one from the list. You will see what it is, why the rocket needs it, how it works, and what it connects to.", 14, MUTED).style.marginBottom = 14;
                Text(sv, "HOW TO NAVIGATE", 11, ACCENT, true).style.marginBottom = 4;
                foreach (var tip in new[] { "Drag: rotate   Right-drag: move   Scroll: zoom", "Layers: show or hide whole subsystems", "X-ray: make the outer shells see-through", "Exploded view: pull the rocket apart", "Isolate: fade everything except the part you picked", "Press F to frame the whole rocket, Esc to deselect" })
                    Text(sv, "•  " + tip, 13, TXT).style.marginBottom = 3;
                Text(sv, "This is an educational model of a generic two-stage rocket. It is not a copy of any real vehicle and the engines are non-functional concept visuals.", 11, MUTED).style.marginTop = 16;
                return;
            }
            var sub = catalog.SubById[d.subsystem]; Color sc = sub.Color;
            var chip = Text(sv, sub.name.ToUpperInvariant(), 11, Color.black, true); chip.style.backgroundColor = sc; Pad(chip, 3, 9, 3, 9); Radius(chip, 9); chip.style.alignSelf = Align.FlexStart; chip.style.marginBottom = 8;
            Text(sv, d.name, 22, TXT, true).style.marginBottom = 6;
            Text(sv, d.summary, 15, C32(200, 220, 240, 255)).style.marginBottom = 12;
            Section(sv, "WHAT IT IS", d.what, sc); Section(sv, "WHY THE ROCKET NEEDS IT", d.why, sc); Section(sv, "HOW IT WORKS", d.how, sc);
            if (d.facts != null && d.facts.Length > 0) { Head(sv, "KEY IDEAS", sc); foreach (var f in d.facts) Text(sv, "•  " + f, 13, TXT).style.marginBottom = 3; }
            if (d.interfaces != null && d.interfaces.Length > 0) { Head(sv, "INTERFACES", sc); foreach (var i in d.interfaces) { var r = Text(sv, $"{i.type}: {i.text}", 13, TXT); r.style.marginBottom = 3; } }
            if (d.connects != null && d.connects.Length > 0)
            {
                Head(sv, "CONNECTED TO", sc);
                var wrap = new VisualElement(); wrap.style.flexDirection = FlexDirection.Row; wrap.style.flexWrap = Wrap.Wrap; sv.Add(wrap);
                foreach (var c in d.connects) { if (!catalog.ById.TryGetValue(c, out var cd)) continue; var id = c; var b = Pill(wrap, cd.name, () => Select(id)); b.style.marginBottom = 6; }
            }
            if (d.quiz != null && d.quiz.Exists) BuildQuiz(sv, d.quiz, sc);
            var src = Text(sv, "Source basis: " + catalog.SourceLabel(d.source), 11, MUTED); src.style.marginTop = 14;
            Text(sv, d.note, 11, MUTED).style.marginTop = 2;
        }
        void Head(VisualElement p, string t, Color c) { var h = Text(p, t, 11, c, true); h.style.marginTop = 10; h.style.marginBottom = 3; }
        void Section(VisualElement p, string title, string body, Color c) { if (string.IsNullOrEmpty(body)) return; Head(p, title, c); Text(p, body, 14, TXT).style.marginBottom = 4; }

        void BuildQuiz(VisualElement p, QuizDef q, Color c)
        {
            Head(p, "CHECK YOUR UNDERSTANDING", c);
            var card = Box(p, CARD, 8); Pad(card, 12, 12, 12, 12);
            Text(card, q.question, 14, TXT, true).style.marginBottom = 8;
            var result = Text(card, "", 13, MUTED); result.style.marginTop = 6;
            for (int i = 0; i < q.options.Length; i++)
            {
                int idx = i; var o = new Label(q.options[i]); o.style.fontSize = 13; o.style.color = TXT; o.style.whiteSpace = WhiteSpace.Normal; Pad(o, 7, 10, 7, 10); Radius(o, 6); Border(o, 1, LINE); o.style.marginBottom = 5;
                o.RegisterCallback<ClickEvent>(_ =>
                {
                    bool ok = idx == q.answer; o.style.backgroundColor = ok ? C32(0, 110, 70, 255) : C32(120, 40, 40, 255);
                    result.text = (ok ? "Correct. " : "Not quite. ") + q.explanation; result.style.color = ok ? C32(120, 230, 170, 255) : C32(255, 170, 160, 255);
                });
                card.Insert(card.childCount - 1, o);
            }
        }

        void BuildBottom()
        {
            var bar = Box(root, PANEL, 12); Abs(bar, null, null, null, 14); bar.style.left = Length.Percent(50); bar.style.translate = new Translate(Length.Percent(-50), 0); bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            Pad(bar, 10, 16, 10, 16); Border(bar, 1, LINE);
            Text(bar, "Exploded", 13, MUTED).style.marginRight = 8;
            var slider = new VisualElement(); slider.style.width = 200; slider.style.height = 22; slider.style.justifyContent = Justify.Center; slider.style.marginRight = 14;
            var track = Box(slider, LINE, 3); track.style.height = 6; track.pickingMode = PickingMode.Ignore;
            var fill = Box(track, ACCENT, 3); fill.style.height = 6; fill.style.width = Length.Percent(0); fill.pickingMode = PickingMode.Ignore;
            var knob = Box(slider, TXT, 8); knob.style.width = 16; knob.style.height = 16; knob.style.position = Position.Absolute; knob.style.top = 3; knob.style.left = 0; knob.pickingMode = PickingMode.Ignore;
            System.Action<float> setV = v => { v = Mathf.Clamp01(v); explodeGoal = v; fill.style.width = Length.Percent(v * 100); knob.style.left = v * (200 - 16); };
            bool drag = false;
            slider.RegisterCallback<PointerDownEvent>(e => { drag = true; slider.CapturePointer(e.pointerId); setV(e.localPosition.x / 200f); });
            slider.RegisterCallback<PointerMoveEvent>(e => { if (drag) setV(e.localPosition.x / 200f); });
            slider.RegisterCallback<PointerUpEvent>(e => { drag = false; slider.ReleasePointer(e.pointerId); });
            bar.Add(slider);
            xrayBtn = Pill(bar, "X-ray shells", () => { xray = !xray; Refresh(); });
            isoBtn = Pill(bar, "Isolate selection", () => { isolate = !isolate; Refresh(); });
            labelBtn = Pill(bar, "Labels", () => { labels = !labels; Refresh(); });
            Pill(bar, "Frame all", () => orbit.FrameAll(visuals.WholeBounds()));
        }

        void BuildFloating()
        {
            labelLayer = new VisualElement(); Abs(labelLayer, 0, 0, 0, 0); labelLayer.pickingMode = PickingMode.Ignore; root.Add(labelLayer);
            tooltip = Box(labelLayer, C32(8, 12, 18, 240), 6); Pad(tooltip, 5, 9, 5, 9); Border(tooltip, 1, LINE); tooltip.style.position = Position.Absolute; tooltip.pickingMode = PickingMode.Ignore; tooltip.style.display = DisplayStyle.None;
            tooltipText = Text(tooltip, "", 13, TXT);
        }

        Vector2 ToPanel(Vector2 screen) => root.panel != null ? RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y)) : Vector2.zero;

        void UpdateFloatingLabels(Vector2 mouse)
        {
            if (root.panel == null) return;
            if (tooltip != null && hoverId != null) { var tp = ToPanel(mouse); tooltip.style.left = tp.x + 16; tooltip.style.top = tp.y + 14; }
            var show = new HashSet<string>(); if (labels && selectedId != null) { show.Add(selectedId); foreach (var r in related) show.Add(r); }
            foreach (var id in show) if (!floatLabels.ContainsKey(id))
                {
                    var l = Text(labelLayer, catalog.ById[id].name, 12, TXT, true); Pad(l, 3, 8, 3, 8); Radius(l, 8); l.style.position = Position.Absolute;
                    l.style.backgroundColor = C32(8, 12, 18, 225); Border(l, 1, catalog.SubById[catalog.ById[id].subsystem].Color); l.pickingMode = PickingMode.Ignore; floatLabels[id] = l;
                }
            foreach (var kv in floatLabels)
            {
                var pv = visuals.Parts[kv.Key];
                if (!(show.Contains(kv.Key) && !pv.hidden)) { kv.Value.style.display = DisplayStyle.None; continue; }
                Vector3 shift = pv.tf.position - (pv.tf.parent != null ? pv.tf.parent.TransformPoint(pv.homeLocalPos) : pv.homeLocalPos);   // current explode offset
                var sp = Camera.main.WorldToScreenPoint(pv.bounds.center + shift);
                if (sp.z <= 0) { kv.Value.style.display = DisplayStyle.None; continue; }
                var pp = ToPanel(new Vector2(sp.x, sp.y));
                kv.Value.style.display = DisplayStyle.Flex; kv.Value.style.left = pp.x + 8; kv.Value.style.top = pp.y - 10;
            }
        }

        // ---- review / automation hooks
        public void DebugSelect(string id) { if (id == null) Deselect(); else Select(id); }
        public void DebugXray(bool on) { xray = on; Refresh(); }
        public void DebugFrameAll() { orbit.FrameAll(visuals.WholeBounds()); }
        public void DebugExplode(float v) { explodeGoal = v; }
        public void DebugCamera(float yaw, float pitch, float dist, Vector3 target) { orbit.SetView(yaw, pitch, dist, target); }
    }
}
