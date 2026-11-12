using System.Collections.Generic;
using UnityEngine;
using OrbitalLab.Player;
using OrbitalLab.Simulation;

namespace OrbitalLab.Interaction
{
    /// <summary>
    /// Rocket Engineering Workstation. Opens a full-screen engineering panel (IMGUI), lets the user edit
    /// the vehicle, runs the AscentSimulator and plots telemetry. All numbers are from the educational model
    /// documented in AscentSimulator (simulated, not real telemetry).
    /// </summary>
    public class RocketWorkstation : Interactable
    {
        bool open;
        RocketConfig cfg = RocketConfig.DefaultTwoStage();
        float payload = 5000, s1Prop = 395700, s2Prop = 92670, kick = 6f, targetKm = 200f;
        AscentSimulator sim;
        Texture2D chartAlt, chartSpeed, chartAcc;
        string summary = "Press RUN SIMULATION.";
        GUIStyle h1, h2, body, mono;
        Color panelBg = new Color(0.04f, 0.07f, 0.1f, 0.97f);
        Color accent = new Color(0.25f, 0.8f, 1f);

        void Reset() { prompt = "Access Rocket Engineering Workstation"; }
        void Awake() { if (string.IsNullOrEmpty(prompt) || prompt == "Interact") prompt = "Access Rocket Engineering Workstation"; }

        public override void Interact() { open = true; FirstPersonController.InputLocked = true; FirstPersonController.SetCursor(false); }
        void Close() { open = false; FirstPersonController.InputLocked = false; FirstPersonController.SetCursor(true); }

        void RunSim()
        {
            cfg.payloadMass = payload;
            cfg.stages[0].propellantMass = s1Prop;
            cfg.stages[1].propellantMass = s2Prop;
            sim = new AscentSimulator(cfg) { kickAngleDeg = kick, targetAltitude = targetKm * 1000.0 };
            sim.RunToCompletion();
            var log = sim.Log;
            double maxQ = 0, maxG = 0, tq = 0;
            foreach (var s in log) { if (s.dynamicPressure > maxQ) { maxQ = s.dynamicPressure; tq = s.t; } if (s.acceleration > maxG) maxG = s.acceleration; }
            double twr = cfg.stages[0].thrustSeaLevel / (cfg.TotalMass * Atmosphere1976.G0);
            summary =
                $"RESULT: {sim.Outcome}\n" +
                $"Orbit  peri {sim.PeriapsisAlt / 1000:F1} km  x  apo {sim.ApoapsisAlt / 1000:F1} km\n" +
                $"Flight time {log[log.Count - 1].t:F0} s   Final mass {log[log.Count - 1].mass:F0} kg\n" +
                $"Max-Q {maxQ / 1000:F1} kPa @ T+{tq:F0} s    Peak accel {maxG / 9.80665:F1} g\n" +
                $"Liftoff TWR {twr:F2}   Ideal dv (Tsiolkovsky, vacuum) {cfg.IdealDeltaV():F0} m/s";
            chartAlt = Plot(log, s => s.altitude / 1000.0, new Color(0.3f, 0.85f, 1f));
            chartSpeed = Plot(log, s => s.speed, new Color(1f, 0.75f, 0.25f));
            chartAcc = Plot(log, s => s.acceleration / 9.80665, new Color(0.55f, 1f, 0.55f));
        }

        static Texture2D Plot(List<TelemetrySample> log, System.Func<TelemetrySample, double> f, Color line)
        {
            const int W = 560, H = 200;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H]; var bg = new Color32(6, 14, 20, 255); var grid = new Color32(22, 40, 52, 255);
            for (int i = 0; i < px.Length; i++) px[i] = bg;
            for (int gx = 0; gx <= 10; gx++) for (int y = 0; y < H; y++) px[y * W + Mathf.Min(W - 1, gx * (W - 1) / 10)] = grid;
            for (int gy = 0; gy <= 4; gy++) for (int x = 0; x < W; x++) px[Mathf.Min(H - 1, gy * (H - 1) / 4) * W + x] = grid;
            if (log.Count > 1)
            {
                double tmax = log[log.Count - 1].t, vmax = 1e-9, vmin = 0;
                foreach (var s in log) { double v = f(s); if (v > vmax) vmax = v; if (v < vmin) vmin = v; }
                int px0 = 0, py0 = 0; bool first = true;
                foreach (var s in log)
                {
                    int x = (int)(s.t / tmax * (W - 1));
                    int y = (int)((f(s) - vmin) / (vmax - vmin) * (H - 5)) + 2;
                    if (!first) Line(px, W, H, px0, py0, x, y, line);
                    px0 = x; py0 = y; first = false;
                }
            }
            tex.SetPixels32(px); tex.Apply(); return tex;
        }

        static void Line(Color32[] px, int W, int H, int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                if (x0 >= 0 && x0 < W && y0 >= 0 && y0 < H) { px[y0 * W + x0] = c; if (y0 + 1 < H) px[(y0 + 1) * W + x0] = c; }
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        void Update() { if (open && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) Close(); }

        void Styles()
        {
            if (h1 != null) return;
            h1 = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold }; h1.normal.textColor = accent;
            h2 = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold }; h2.normal.textColor = new Color(0.8f, 0.9f, 1f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true }; body.normal.textColor = new Color(0.78f, 0.86f, 0.92f);
            mono = new GUIStyle(body) { fontSize = 14 };
        }

        float Slider(string label, float v, float min, float max, string unit, ref float y, float x, float w)
        {
            GUI.Label(new Rect(x, y, w, 22), $"{label}: {v:F0} {unit}", body);
            v = GUI.HorizontalSlider(new Rect(x, y + 24, w, 18), v, min, max); y += 50; return v;
        }

        void OnGUI()
        {
            if (!open) return;
            Styles();
            float W = Screen.width, H = Screen.height;
            GUI.color = panelBg; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(30, 18, W, 40), "ROCKET ENGINEERING WORKSTATION", h1);
            GUI.Label(new Rect(32, 58, W - 64, 22), "Educational model: planar RK4 ascent, US Standard Atmosphere 1976, constant Cd, no winds. Simulated data - not real NASA telemetry. Independent student project.", body);

            float x = 30, y = 100, w = 380;
            GUI.Label(new Rect(x, y, w, 24), "VEHICLE & MISSION", h2); y += 30;
            payload = Slider("Payload mass", payload, 0, 20000, "kg", ref y, x, w);
            s1Prop = Slider("Stage 1 propellant", s1Prop, 150000, 450000, "kg", ref y, x, w);
            s2Prop = Slider("Stage 2 propellant", s2Prop, 20000, 120000, "kg", ref y, x, w);
            kick = Slider("Pitch kick angle", kick, 1, 12, "deg", ref y, x, w);
            targetKm = Slider("Target orbit altitude", targetKm, 150, 300, "km", ref y, x, w);
            cfg.payloadMass = payload; cfg.stages[0].propellantMass = s1Prop; cfg.stages[1].propellantMass = s2Prop;
            GUI.Label(new Rect(x, y, w, 60), $"Liftoff mass {cfg.TotalMass:F0} kg\nIdeal dv {cfg.IdealDeltaV():F0} m/s  (stage 1 {cfg.IdealDeltaVPerStage()[0]:F0} + stage 2 {cfg.IdealDeltaVPerStage()[1]:F0})", mono); y += 66;
            if (GUI.Button(new Rect(x, y, w, 44), "RUN SIMULATION")) RunSim(); y += 54;
            if (GUI.Button(new Rect(x, y, w, 34), "CLOSE  (Esc)")) Close();

            float cx = 450;
            GUI.Label(new Rect(cx, 100, W - cx - 30, 110), summary, mono);
            float cy = 215;
            DrawChart(new Rect(cx, cy, 560, 200), chartAlt, "Altitude (km) vs time");
            DrawChart(new Rect(cx + 580, cy, 560, 200), chartSpeed, "Speed (m/s) vs time");
            DrawChart(new Rect(cx, cy + 250, 560, 200), chartAcc, "Acceleration (g) vs time");
            GUI.Label(new Rect(cx + 580, cy + 250, 560, 200),
                "How to read this\n- Raise payload: delta-v falls (Tsiolkovsky), orbit may be missed.\n- Max-Q is peak dynamic pressure (0.5*rho*v^2), the structural stress point.\n- 'Suborbital' means propellant ran out before periapsis cleared the atmosphere.", body);
        }

        void DrawChart(Rect r, Texture2D t, string title)
        {
            GUI.Label(new Rect(r.x, r.y - 22, r.width, 22), title, h2);
            if (t != null) GUI.DrawTexture(r, t); else { GUI.color = new Color(0.02f, 0.05f, 0.08f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        }
    }
}
