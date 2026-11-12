using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using OrbitalLab.Explorer.Build;
using OrbitalLab.Simulation;

namespace OrbitalLab.Explorer
{
    /// <summary>
    /// Build mode: start from an empty platform, place parts in an order that follows real structural/plumbing/electrical logic,
    /// get plain-language design reviews, and fly the rocket you built in the (educational, simplified) ascent simulation.
    /// </summary>
    public partial class ExplorerApp
    {
        bool buildMode; Design design = new Design(); int stepIndex;
        VisualElement leftPanel, buildLeft; List<VisualElement> exploreLeftChildren = new List<VisualElement>();
        Label exploreBtn, buildBtn; VisualElement cardList; Label stepIntro, buildCount; string lastNotice = "";
        // undo / redo of placement actions
        class Action { public List<string> parts; public bool added; }
        readonly Stack<Action> undo = new Stack<Action>(), redo = new Stack<Action>();
        // simulation result
        AscentSimulator lastSim; Texture2D chA, chB, chC; string simExplain = "";

        void SetMode(bool build)
        {
            if (build == buildMode) return;
            buildMode = build; SetPill(exploreBtn, !build); SetPill(buildBtn, build);
            selectedId = null; related.Clear(); explodeGoal = 0;
            foreach (var c in exploreLeftChildren) c.style.display = build ? DisplayStyle.None : DisplayStyle.Flex;
            if (build)
            {
                if (buildLeft == null) BuildBuildPanel();
                buildLeft.style.display = DisplayStyle.Flex; visuals.visibleOnly = design.placed; orbit.FrameAll(visuals.WholeBounds());
            }
            else { if (buildLeft != null) buildLeft.style.display = DisplayStyle.None; visuals.visibleOnly = null; }
            foreach (var r in treeRows.Values) Selected(r, false);
            Refresh(); BuildInfo(null);
        }

        // ============================================================ left panel
        void BuildBuildPanel()
        {
            buildLeft = new VisualElement(); buildLeft.style.flexGrow = 1; buildLeft.style.flexDirection = FlexDirection.Column; leftPanel.Add(buildLeft);
            Text(buildLeft, "BUILD YOUR ROCKET", 12, ACCENT, true).style.marginBottom = 4;
            buildCount = Text(buildLeft, "", 12, MUTED); buildCount.style.marginBottom = 8;
            var steps = new VisualElement(); steps.style.flexDirection = FlexDirection.Row; steps.style.flexWrap = Wrap.Wrap; steps.style.marginBottom = 8; buildLeft.Add(steps);
            for (int i = 0; i < BuildRules.Steps.Count; i++)
            {
                int idx = i; var b = Pill(steps, (i + 1).ToString(), () => { stepIndex = idx; RefreshBuildPanel(); }); b.name = "step" + i; b.tooltip = BuildRules.Steps[i].title;
            }
            stepIntro = Text(buildLeft, "", 13, TXT); stepIntro.style.marginBottom = 8;
            var tools = new VisualElement(); tools.style.flexDirection = FlexDirection.Row; tools.style.marginBottom = 8; buildLeft.Add(tools);
            Pill(tools, "Undo", Undo); Pill(tools, "Redo", Redo); Pill(tools, "Clear", () => { design.placed.Clear(); undo.Clear(); redo.Clear(); lastNotice = "Platform cleared."; AfterChange(); });
            var sv = new ScrollView(ScrollViewMode.Vertical); sv.style.flexGrow = 1; buildLeft.Add(sv); cardList = sv;
            RefreshBuildPanel();
        }

        void RefreshBuildPanel()
        {
            if (buildLeft == null) return;
            var step = BuildRules.Steps[stepIndex];
            for (int i = 0; i < BuildRules.Steps.Count; i++) { var b = buildLeft.Q<Label>("step" + i); if (b != null) SetPill(b, i == stepIndex); }
            stepIntro.text = step.title + "\n" + step.intro;
            buildCount.text = $"Placed {design.placed.Count} / {catalog.Data.parts.Length} parts";
            cardList.Clear();
            foreach (var id in step.parts)
            {
                if (!catalog.ById.TryGetValue(id, out var def)) continue; var pid = id;
                bool placed = design.placed.Contains(id); string why = placed ? null : BuildRules.CanPlace(design.placed, id);
                var card = new VisualElement(); card.style.backgroundColor = CARD; Radius(card, 8); Pad(card, 8, 10, 8, 10); card.style.marginBottom = 6;
                Border(card, 1, placed ? new Color(0.2f, 0.8f, 0.5f) : LINE);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; card.Add(row);
                var dot = new VisualElement(); dot.style.width = 9; dot.style.height = 9; Radius(dot, 5); dot.style.backgroundColor = (Color)catalog.SubById[def.subsystem].Color; dot.style.marginRight = 8; row.Add(dot);
                var name = Text(row, def.name, 13, why != null && !placed ? MUTED : TXT, true); name.style.flexGrow = 1;
                double m = BuildRules.Mass.TryGetValue(id, out var mm) ? mm : (id.StartsWith("S1_Engine_") ? 470 : 0);
                if (m > 0) Text(row, $"{m:F0} kg", 11, MUTED).style.marginRight = 8;
                var btn = new Label(placed ? "Remove" : "Place"); btn.style.fontSize = 12; Pad(btn, 4, 10, 4, 10); Radius(btn, 10);
                btn.style.color = why != null && !placed ? MUTED : Color.black; btn.style.backgroundColor = placed ? new Color(0.9f, 0.45f, 0.4f) : (why != null ? CARD : ACCENT); Border(btn, 1, LINE);
                btn.RegisterCallback<ClickEvent>(_ => { if (placed) Remove(pid); else Place(pid); });
                row.Add(btn);
                if (why != null && !placed) { var w = Text(card, why, 11, C32(255, 190, 120, 255)); w.style.marginTop = 4; }
                else Text(card, def.summary, 11, MUTED).style.marginTop = 3;
                card.RegisterCallback<PointerEnterEvent>(_ => { hoverId = pid; Refresh(); });
                card.RegisterCallback<PointerLeaveEvent>(_ => { if (hoverId == pid) hoverId = null; Refresh(); });
                cardList.Add(card);
            }
        }

        // ============================================================ actions
        void Place(string id)
        {
            var why = BuildRules.CanPlace(design.placed, id);
            if (why != null) { lastNotice = "Cannot place " + catalog.ById[id].name + ". " + why; BuildInfo(null); return; }
            design.placed.Add(id); undo.Push(new Action { parts = new List<string> { id }, added = true }); redo.Clear();
            visuals.AnimateIn(id); lastNotice = "Placed " + catalog.ById[id].name + ".";
            if (visuals.Parts.TryGetValue(id, out var pv)) orbit.FocusOn(pv.bounds, 3.2f);
            AfterChange();
        }
        void Remove(string id)
        {
            var removed = BuildRules.Dependents(design.placed, id); removed.Add(id);
            foreach (var r in removed) design.placed.Remove(r);
            undo.Push(new Action { parts = removed.ToList(), added = false }); redo.Clear();
            lastNotice = removed.Count > 1 ? $"Removed {catalog.ById[id].name} and {removed.Count - 1} part(s) that were mounted on it." : "Removed " + catalog.ById[id].name + ".";
            AfterChange();
        }
        void Undo()
        {
            if (undo.Count == 0) return; var a = undo.Pop(); redo.Push(a);
            foreach (var p in a.parts) { if (a.added) design.placed.Remove(p); else { design.placed.Add(p); visuals.AnimateIn(p); } }
            lastNotice = "Undid last action."; AfterChange();
        }
        void Redo()
        {
            if (redo.Count == 0) return; var a = redo.Pop(); undo.Push(a);
            foreach (var p in a.parts) { if (a.added) { design.placed.Add(p); visuals.AnimateIn(p); } else design.placed.Remove(p); }
            lastNotice = "Redid action."; AfterChange();
        }
        void AfterChange() { lastSim = null; RefreshBuildPanel(); Refresh(); if (selectedId != null && !design.placed.Contains(selectedId)) Deselect(); else if (selectedId == null) BuildInfo(null); }

        // ============================================================ right panel: design review + flight
        void BuildReview(VisualElement sv)
        {
            Text(sv, "Design review", 20, TXT, true).style.marginBottom = 4;
            if (!string.IsNullOrEmpty(lastNotice)) { var n = Text(sv, lastNotice, 12, ACCENT); n.style.marginBottom = 8; }
            if (design.placed.Count == 0)
            {
                Text(sv, "The platform is empty. Choose a step on the left and place the first part. Parts have to go in an order that makes engineering sense, and I will explain why whenever one can't go yet.", 14, MUTED).style.marginBottom = 10;
                Text(sv, "Click a placed part in the 3D view to read about it.", 12, MUTED);
                return;
            }
            // sliders
            Head(sv, "LOADING (hypothetical values)", ACCENT);
            MakeSlider(sv, "Stage 1 propellant", 0.3f, 1f, (float)design.stage1Fill, v => $"{v * 100:F0} %  ({BuildRules.S1Capacity * v / 1000:F0} t)", v => { design.stage1Fill = v; lastSim = null; });
            MakeSlider(sv, "Stage 2 propellant", 0.3f, 1f, (float)design.stage2Fill, v => $"{v * 100:F0} %  ({BuildRules.S2Capacity * v / 1000:F0} t)", v => { design.stage2Fill = v; lastSim = null; });
            MakeSlider(sv, "Payload (satellite) mass", 500f, 25000f, (float)design.payloadKg, v => $"{v:F0} kg", v => { design.payloadKg = v; lastSim = null; });
            var findings = BuildRules.Check(design);
            Head(sv, "CHECKS", ACCENT);
            foreach (var f in findings.OrderBy(x => x.severity))
            {
                Color c = f.severity == Severity.Error ? C32(255, 120, 110, 255) : f.severity == Severity.Warning ? C32(255, 200, 90, 255) : f.severity == Severity.Ok ? C32(110, 230, 160, 255) : C32(140, 190, 240, 255);
                string tag = f.severity == Severity.Error ? "PROBLEM" : f.severity == Severity.Warning ? "WARNING" : f.severity == Severity.Ok ? "OK" : "NOTE";
                var card = Box(sv, CARD, 6); Pad(card, 8, 10, 8, 10); card.style.marginBottom = 5; card.style.borderLeftWidth = 3; card.style.borderLeftColor = c;
                Text(card, tag + "  " + f.title, 13, c, true); Text(card, f.detail, 12, TXT).style.marginTop = 2;
            }
            bool blocked = BuildRules.HasBlockingError(findings);
            var fly = new Label(blocked ? "Fix the problems above to fly" : "Fly this rocket (simulation)"); fly.style.fontSize = 14; fly.style.unityFontStyleAndWeight = FontStyle.Bold; Pad(fly, 10, 12, 10, 12); Radius(fly, 8); fly.style.marginTop = 10; fly.style.unityTextAlign = TextAnchor.MiddleCenter;
            fly.style.color = blocked ? MUTED : Color.black; fly.style.backgroundColor = blocked ? CARD : ACCENT; Border(fly, 1, LINE);
            fly.RegisterCallback<ClickEvent>(_ => { if (!blocked) RunFlight(); else lastNotice = "A rocket with blocking problems cannot be flown. Read each PROBLEM and fix it."; BuildInfo(null); });
            sv.Add(fly);
            if (lastSim != null) ShowFlight(sv);
            Text(sv, "All numbers are hypothetical and simulated (simplified planar ascent model). Not real telemetry.", 11, MUTED).style.marginTop = 12;
        }

        void MakeSlider(VisualElement parent, string label, float min, float max, float value, System.Func<float, string> fmt, System.Action<float> onChange)
        {
            var wrap = new VisualElement(); wrap.style.marginBottom = 8; parent.Add(wrap);
            var l = Text(wrap, label + ": " + fmt(value), 12, TXT);
            var slider = new VisualElement(); slider.style.height = 20; slider.style.justifyContent = Justify.Center; wrap.Add(slider);
            var track = Box(slider, LINE, 3); track.style.height = 6; track.pickingMode = PickingMode.Ignore;
            var fill = Box(track, ACCENT, 3); fill.style.height = 6; fill.pickingMode = PickingMode.Ignore;
            var knob = Box(slider, TXT, 8); knob.style.width = 14; knob.style.height = 14; knob.style.position = Position.Absolute; knob.style.top = 3; knob.pickingMode = PickingMode.Ignore;
            float width = 340f;
            System.Action<float> setN = n => { n = Mathf.Clamp01(n); float v = Mathf.Lerp(min, max, n); l.text = label + ": " + fmt(v); fill.style.width = Length.Percent(n * 100); knob.style.left = n * (width - 14); onChange(v); };
            float n0 = Mathf.InverseLerp(min, max, value); fill.style.width = Length.Percent(n0 * 100); knob.style.left = n0 * (width - 14);
            bool drag = false;
            slider.RegisterCallback<PointerDownEvent>(e => { drag = true; slider.CapturePointer(e.pointerId); width = slider.resolvedStyle.width; setN(e.localPosition.x / width); });
            slider.RegisterCallback<PointerMoveEvent>(e => { if (drag) setN(e.localPosition.x / width); });
            slider.RegisterCallback<PointerUpEvent>(e => { drag = false; slider.ReleasePointer(e.pointerId); BuildInfo(null); });
        }

        void RunFlight()
        {
            var cfg = BuildRules.ToConfig(design);
            lastSim = new AscentSimulator(cfg) { targetAltitude = 200e3 }; lastSim.RunToCompletion();
            var log = lastSim.Log;
            chA = ChartTexture.Plot(log, s => s.altitude / 1000.0, new Color32(80, 200, 255, 255));
            chB = ChartTexture.Plot(log, s => s.speed, new Color32(255, 190, 70, 255));
            chC = ChartTexture.Plot(log, s => s.acceleration / 9.80665, new Color32(120, 255, 140, 255));
            double dv = cfg.IdealDeltaV();
            if (lastSim.InOrbit) simExplain = $"Success. The rocket reached orbit because its ideal delta-v ({dv:F0} m/s) was enough to cover the ~7,800 m/s of orbital speed plus gravity and drag losses.";
            else if (lastSim.Outcome.StartsWith("Crashed")) simExplain = "The rocket fell back to Earth. Thrust was not enough to gain the height and speed it needed. Check thrust-to-weight and propellant load.";
            else simExplain = $"The rocket ran out of propellant before reaching orbital speed. Its ideal delta-v was {dv:F0} m/s, which is not enough once gravity and drag losses are included (typically 9,000-10,000 m/s needed). Reduce payload, add propellant, or add engines to carry more.";
            BuildInfo(null);
        }

        void ShowFlight(VisualElement sv)
        {
            var log = lastSim.Log; double maxQ = 0, tq = 0, maxG = 0;
            foreach (var s in log) { if (s.dynamicPressure > maxQ) { maxQ = s.dynamicPressure; tq = s.t; } if (s.acceleration > maxG) maxG = s.acceleration; }
            Head(sv, "FLIGHT RESULT (simulated)", ACCENT);
            var card = Box(sv, CARD, 8); Pad(card, 10, 12, 10, 12);
            Text(card, lastSim.Outcome, 15, lastSim.InOrbit ? C32(110, 230, 160, 255) : C32(255, 150, 130, 255), true);
            Text(card, $"Orbit: periapsis {lastSim.PeriapsisAlt / 1000:F0} km x apoapsis {lastSim.ApoapsisAlt / 1000:F0} km", 12, TXT).style.marginTop = 4;
            Text(card, $"Flight time {log[log.Count - 1].t:F0} s   Max-Q {maxQ / 1000:F1} kPa at T+{tq:F0} s   Peak {maxG / 9.80665:F1} g", 12, TXT).style.marginTop = 2;
            Text(card, simExplain, 12, MUTED).style.marginTop = 6;
            foreach (var (tex, title) in new[] { (chA, "Altitude (km) vs time"), (chB, "Speed (m/s) vs time"), (chC, "Acceleration (g) vs time") })
            {
                Text(card, title, 11, MUTED, true).style.marginTop = 8;
                var img = new VisualElement(); img.style.height = 100; img.style.backgroundImage = new StyleBackground(tex); img.style.marginTop = 2; img.pickingMode = PickingMode.Ignore; card.Add(img);
            }
        }

        // ---- review / automation hooks
        public void DebugBuildMode(bool on) { SetMode(on); }
        public void DebugPlace(string id) { Place(id); }
        public void DebugPlaceStep(int step) { foreach (var id in BuildRules.Steps[step].parts) if (BuildRules.CanPlace(design.placed, id) == null) { design.placed.Add(id); visuals.AnimateIn(id); } stepIndex = Mathf.Clamp(step, 0, BuildRules.Steps.Count - 1); AfterChange(); }
        public void DebugStep(int step) { stepIndex = step; RefreshBuildPanel(); }
        public void DebugFly() { RunFlight(); }
        public void DebugRemove(string id) { Remove(id); }
    }
}
