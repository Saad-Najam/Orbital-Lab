using System;
using System.Collections.Generic;

namespace OrbitalLab.Simulation
{
    public struct TelemetrySample
    {
        public double t, altitude, downrange, speed, verticalSpeed, horizontalSpeed;
        public double mass, thrust, drag, acceleration, density, dynamicPressure, mach;
        public double pitchDeg, propellantRemaining;
        public int stage;
        public string phase;
    }

    /// <summary>
    /// Planar (2D) ascent simulator over a spherical, non-rotating Earth, inertial frame.
    /// State: position (x = downrange tangent, y = radial from Earth centre), velocity, mass.
    /// Integrator: classical RK4 with fixed step. Forces: thrust (Isp interpolated by ambient pressure),
    /// drag (US Std Atmosphere 1976), point-mass gravity mu/r^2. Guidance: vertical rise, then a
    /// parametrised gravity-turn kick, then prograde following; pitch-over finishes with a horizontal hold.
    /// EDUCATIONAL MODEL: no winds, no Earth rotation, no lift, constant Cd, instantaneous staging with
    /// a short coast. Not a flight-qualified simulator.
    /// </summary>
    public class AscentSimulator
    {
        public const double Mu = 3.986004418e14;      // m^3/s^2
        public const double EarthR = 6371000.0;       // m, mean radius
        public const double P0 = 101325.0;

        public RocketConfig config;
        public double targetAltitude = 200e3;   // single-burn insertion altitude; higher orbits need a transfer burn (not modelled yet)
        public double kickStartAltitude = 1000;   // m, begin pitch-over
        public double kickAngleDeg = 6.0;         // initial pitch-over angle (from vertical)
        public double dt = 0.05;
        public double stagingCoast = 2.0;
        public double maxTime = 1500;

        // state
        double x, y, vx, vy, mass, t;
        int stage;
        double[] propLeft;
        double coastUntil = -1;
        bool throttleCut;
        enum Mode { Ascent, Coast, Circularize }
        Mode mode = Mode.Ascent;
        int stepCount;
        public int logEvery = 10;   // log one sample per logEvery steps (0.5 s at dt=0.05)
        public double Altitude => Math.Sqrt(x * x + y * y) - EarthR;

        public readonly List<TelemetrySample> Log = new List<TelemetrySample>();
        public bool InOrbit { get; private set; }
        public double ApoapsisAlt, PeriapsisAlt;
        public string Outcome = "";

        public AscentSimulator(RocketConfig cfg) { config = cfg; Reset(); }

        public void Reset()
        {
            x = 0; y = EarthR; vx = 0; vy = 0; t = 0; stage = 0; coastUntil = -1; throttleCut = false; mode = Mode.Ascent; stepCount = 0;
            propLeft = new double[config.stages.Count];
            for (int i = 0; i < propLeft.Length; i++) propLeft[i] = config.stages[i].propellantMass;
            mass = config.TotalMass;
            Log.Clear(); InOrbit = false; Outcome = "";
        }

        double PitchFromVertical()
        {
            double alt = Altitude;
            double r = Math.Sqrt(x * x + y * y);
            double ux = x / r, uy = y / r;                // local vertical
            double vr = vx * ux + vy * uy;                // radial speed
            double vt = vx * uy - vy * ux;               // tangential (downrange) speed
            double speed = Math.Sqrt(vx * vx + vy * vy);
            if (mode == Mode.Circularize)
            {   // burn prograde-horizontal, trimming radial speed to zero
                double trim = Math.Min(Math.Max(Math.Atan2(-vr, 80.0), -0.3), 0.3);
                return Math.PI / 2 - trim;
            }
            double kick = kickAngleDeg * Math.PI / 180.0;
            if (alt < kickStartAltitude) return 0;                         // vertical rise
            double flightPath = Math.Atan2(vt, Math.Max(vr, 1e-3));        // velocity angle from vertical
            // Programmed pitch-over through the dense atmosphere: kick, then a smooth ramp with altitude.
            // (Zero-lift gravity turn is what the flight-path angle would give; the ramp is steeper so the
            // vehicle trades altitude for horizontal speed earlier, as real ascents do.)
            if (alt < 60e3)
            {
                double f = (alt - kickStartAltitude) / (60e3 - kickStartAltitude);
                double programmed = kick + (80.0 * Math.PI / 180.0 - kick) * Math.Pow(Math.Min(1.0, Math.Max(0.0, f)), 0.6);
                return Math.Min(Math.PI / 2, Math.Max(programmed, Math.Min(flightPath, programmed + 0.0)));
            }
            // Exo-atmospheric: steer elevation to hold a commanded climb rate that tapers to zero at target altitude
            double vrCmd = Math.Min(Math.Max((targetAltitude - alt) * 0.003, 0), 250);
            double elevation = Math.Atan2(vrCmd - vr, 60.0);               // radians above local horizontal
            elevation = Math.Min(Math.Max(elevation, 0.0), 1.2);
            return Math.PI / 2 - elevation;
        }

        // Derivative: returns ax, ay, mdot given state (pos/vel, mass) using current stage settings
        void Deriv(double px, double py, double ux, double uy, double m, double pitch,
                   out double ax, out double ay, out double mdot, out double thrustOut, out double dragOut)
        {
            double r = Math.Sqrt(px * px + py * py);
            double alt = r - EarthR;
            // gravity
            double g = Mu / (r * r);
            double gx = -g * px / r, gy = -g * py / r;
            // thrust
            double thrust = 0; mdot = 0; thrustOut = 0;
            bool burning = stage < config.stages.Count && propLeft[stage] > 0 && coastUntil < t && !throttleCut;
            if (burning)
            {
                var s = config.stages[stage];
                var atm = Atmosphere1976.At(alt);
                double frac = Math.Min(1.0, atm.Pressure / P0);
                double isp = s.ispVacuum + (s.ispSeaLevel - s.ispVacuum) * frac;
                thrust = s.thrustVacuum + (s.thrustSeaLevel - s.thrustVacuum) * frac;
                mdot = -thrust / (isp * Atmosphere1976.G0);
                thrustOut = thrust;
            }
            // thrust direction: local vertical rotated by pitch toward downrange tangent
            double rx = px / r, ry = py / r;       // radial unit
            double tx = ry, ty = -rx;              // tangent unit (downrange +)
            double dx = Math.Cos(pitch) * rx + Math.Sin(pitch) * tx;
            double dy = Math.Cos(pitch) * ry + Math.Sin(pitch) * ty;
            // drag
            double v = Math.Sqrt(ux * ux + uy * uy);
            double dragMag = 0; dragOut = 0;
            if (v > 1e-6 && stage < config.stages.Count && alt < 200e3)
            {
                var s = config.stages[stage];
                var atm = Atmosphere1976.At(alt);
                dragMag = 0.5 * atm.Density * s.dragCoefficient * s.Area * v * v;
                dragOut = dragMag;
            }
            double dragX = v > 1e-6 ? -dragMag * ux / v : 0, dragY = v > 1e-6 ? -dragMag * uy / v : 0;
            ax = gx + (thrust * dx + dragX) / m;
            ay = gy + (thrust * dy + dragY) / m;
        }

        void Step()
        {
            double pitch = PitchFromVertical();
            double m = mass;
            double k1x, k1y, md1, th, dr; // thrust/drag for telemetry from first eval
            Deriv(x, y, vx, vy, m, pitch, out k1x, out k1y, out md1, out th, out dr);
            double k2x, k2y, md2, a, b;
            Deriv(x + 0.5 * dt * vx, y + 0.5 * dt * vy, vx + 0.5 * dt * k1x, vy + 0.5 * dt * k1y, m + 0.5 * dt * md1, pitch, out k2x, out k2y, out md2, out a, out b);
            double vx2 = vx + 0.5 * dt * k1x, vy2 = vy + 0.5 * dt * k1y;
            double k3x, k3y, md3;
            Deriv(x + 0.5 * dt * vx2, y + 0.5 * dt * vy2, vx + 0.5 * dt * k2x, vy + 0.5 * dt * k2y, m + 0.5 * dt * md2, pitch, out k3x, out k3y, out md3, out a, out b);
            double vx3 = vx + 0.5 * dt * k2x, vy3 = vy + 0.5 * dt * k2y;
            double k4x, k4y, md4;
            Deriv(x + dt * vx3, y + dt * vy3, vx + dt * k3x, vy + dt * k3y, m + dt * md3, pitch, out k4x, out k4y, out md4, out a, out b);
            double vx4 = vx + dt * k3x, vy4 = vy + dt * k3y;

            double nx = x + dt / 6.0 * (vx + 2 * vx2 + 2 * vx3 + vx4);
            double ny = y + dt / 6.0 * (vy + 2 * vy2 + 2 * vy3 + vy4);
            double nvx = vx + dt / 6.0 * (k1x + 2 * k2x + 2 * k3x + k4x);
            double nvy = vy + dt / 6.0 * (k1y + 2 * k2y + 2 * k3y + k4y);
            double dm = dt / 6.0 * (md1 + 2 * md2 + 2 * md3 + md4);

            if (stage < config.stages.Count && md1 < 0)
            {
                // clamp propellant (burnout inside the step)
                double burn = -dm;
                if (burn > propLeft[stage]) { burn = propLeft[stage]; dm = -burn; }
                propLeft[stage] -= burn;
            }
            mass += dm; x = nx; y = ny; vx = nvx; vy = nvy; t += dt;

            // staging
            if (stage < config.stages.Count && propLeft[stage] <= 1e-6)
            {
                if (stage < config.stages.Count - 1) mass -= config.stages[stage].dryMass; // last stage stays attached
                stage++;
                coastUntil = t + stagingCoast;
            }

            double rr = Math.Sqrt(x * x + y * y);
            double rx = x / rr, ry = y / rr;
            double vrad = vx * rx + vy * ry, vtan = vx * ry - vy * rx;
            var atmNow = Atmosphere1976.At(rr - EarthR);
            double sp = Math.Sqrt(vx * vx + vy * vy);
            if (++stepCount % Math.Max(1, logEvery) == 0)
            Log.Add(new TelemetrySample
            {
                t = t, altitude = rr - EarthR, downrange = Math.Atan2(x, y) * EarthR, speed = sp,
                verticalSpeed = vrad, horizontalSpeed = vtan, mass = mass, thrust = th, drag = dr,
                acceleration = Math.Sqrt(k1x * k1x + k1y * k1y), density = atmNow.Density,
                dynamicPressure = 0.5 * atmNow.Density * sp * sp, mach = sp / atmNow.SpeedOfSound,
                pitchDeg = pitch * 180 / Math.PI, stage = Math.Min(stage, config.stages.Count - 1) + 1,
                propellantRemaining = stage < propLeft.Length ? propLeft[stage] : 0,
                phase = (stage >= config.stages.Count || throttleCut) ? "COAST" : (coastUntil > t ? "STAGING" : "POWERED")
            });
        }

        /// <summary>Orbital elements from current state (two-body). Returns false if hyperbolic.</summary>
        public bool OrbitalElements(out double periAlt, out double apoAlt, out double ecc)
        {
            double r = Math.Sqrt(x * x + y * y), v2 = vx * vx + vy * vy;
            double energy = v2 / 2 - Mu / r;
            double h = Math.Abs(x * vy - y * vx);
            double a = -Mu / (2 * energy);
            ecc = Math.Sqrt(Math.Max(0, 1 + 2 * energy * h * h / (Mu * Mu)));
            periAlt = a * (1 - ecc) - EarthR; apoAlt = a * (1 + ecc) - EarthR;
            return energy < 0;
        }

        /// <summary>
        /// Three-phase mission: (1) powered ascent until predicted apoapsis reaches the target altitude,
        /// (2) coast to apoapsis, (3) circularisation burn until periapsis >= target - 20 km.
        /// Ends in "Orbit achieved", "Suborbital - propellant exhausted", "Crashed" or "Timeout".
        /// </summary>
        public void RunToCompletion()
        {
            while (t < maxTime)
            {
                Step();
                if (Altitude < -1 && t > 5) { Outcome = "Crashed"; break; }
                bool bound = OrbitalElements(out double pe, out double ap, out double e);
                double rr = Math.Sqrt(x * x + y * y);
                double vr = (vx * x + vy * y) / rr;

                if (mode == Mode.Ascent && Altitude > 80e3 && bound && ap >= targetAltitude)
                { throttleCut = true; mode = Mode.Coast; }
                else if (mode == Mode.Coast && vr <= 0)
                { throttleCut = false; mode = Mode.Circularize; }
                else if (mode == Mode.Circularize && pe >= targetAltitude - 20e3)
                { throttleCut = true; InOrbit = true; Outcome = "Orbit achieved"; break; }

                if (stage >= config.stages.Count && !InOrbit)
                {
                    if (bound && pe > 120e3) { InOrbit = true; Outcome = "Orbit achieved (propellant exhausted)"; }
                    else Outcome = "Suborbital - propellant exhausted before orbit";
                    break;
                }
            }
            OrbitalElements(out PeriapsisAlt, out ApoapsisAlt, out _);
            if (Outcome == "") Outcome = "Timeout";
        }
    }
}
