using System;

namespace OrbitalLab.Simulation
{
    /// <summary>
    /// US Standard Atmosphere 1976, geopotential altitude 0-86 km (7 layers), exponential tail above.
    /// Source: NOAA/NASA/USAF, "U.S. Standard Atmosphere, 1976", NASA-TM-X-74335.
    /// Limitations: static mean atmosphere (no weather/seasons); above 86 km density uses a crude
    /// exponential fit and is only indicative. Educational model, SI units.
    /// </summary>
    public static class Atmosphere1976
    {
        public const double G0 = 9.80665;          // m/s^2
        public const double R = 287.05287;         // J/(kg K), specific gas constant of dry air
        public const double EarthRadius = 6356766.0; // m, geopotential radius used by the standard

        // Layer base geopotential altitude (m), base temperature (K), lapse rate (K/m), base pressure (Pa)
        static readonly double[] Hb = { 0, 11000, 20000, 32000, 47000, 51000, 71000 };
        static readonly double[] Tb = { 288.15, 216.65, 216.65, 228.65, 270.65, 270.65, 214.65 };
        static readonly double[] Lb = { -0.0065, 0.0, 0.001, 0.0028, 0.0, -0.0028, -0.002 };
        static readonly double[] Pb = new double[7];

        static Atmosphere1976()
        {
            Pb[0] = 101325.0;
            for (int i = 1; i < 7; i++)
                Pb[i] = PressureInLayer(i - 1, Hb[i]);
        }

        static double PressureInLayer(int i, double h)
        {
            double dh = h - Hb[i];
            if (Math.Abs(Lb[i]) < 1e-12)
                return Pb[i] * Math.Exp(-G0 * dh / (R * Tb[i]));
            double t = Tb[i] + Lb[i] * dh;
            return Pb[i] * Math.Pow(Tb[i] / t, G0 / (R * Lb[i]));
        }

        /// <summary>Converts geometric altitude (m) to geopotential altitude (m).</summary>
        public static double ToGeopotential(double z) => EarthRadius * z / (EarthRadius + z);

        public struct State { public double Temperature, Pressure, Density, SpeedOfSound; }

        /// <param name="geometricAltitude">metres above sea level</param>
        public static State At(double geometricAltitude)
        {
            double z = Math.Max(0, geometricAltitude);
            double h = ToGeopotential(z);
            State s;
            if (h <= 84852.0)
            {
                int i = 0;
                for (int k = 6; k >= 0; k--) if (h >= Hb[k]) { i = k; break; }
                double dh = h - Hb[i];
                s.Temperature = Tb[i] + Lb[i] * dh;
                s.Pressure = PressureInLayer(i, h);
                s.Density = s.Pressure / (R * s.Temperature);
            }
            else
            {
                // Above ~85 km: exponential continuation anchored at the 84.852 km values (indicative only).
                double t = 186.95, p0 = PressureInLayer(6, 84852.0), rho0 = p0 / (R * t);
                const double scaleHeight = 5900.0;
                s.Temperature = t;
                s.Density = rho0 * Math.Exp(-(z - 85000.0) / scaleHeight);
                s.Pressure = s.Density * R * t;
            }
            s.SpeedOfSound = Math.Sqrt(1.4 * R * s.Temperature);
            return s;
        }
    }
}
