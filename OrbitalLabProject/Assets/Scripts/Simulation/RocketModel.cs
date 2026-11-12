using System;
using System.Collections.Generic;

namespace OrbitalLab.Simulation
{
    /// <summary>One propulsive stage. All values SI. Engine numbers are SIMULATED educational values.</summary>
    [Serializable]
    public class StageConfig
    {
        public string name = "Stage";
        public double dryMass = 25000;          // kg (structure, engines, tanks)
        public double propellantMass = 400000;  // kg
        public double thrustSeaLevel = 7600e3;  // N
        public double thrustVacuum = 8200e3;    // N
        public double ispSeaLevel = 282;        // s
        public double ispVacuum = 311;          // s
        public double diameter = 3.7;           // m (reference area for drag)
        public double dragCoefficient = 0.30;   // subsonic-supersonic average, simplified

        public double Area => Math.PI * diameter * diameter / 4.0;
    }

    [Serializable]
    public class RocketConfig
    {
        public string name = "ORBITAL-1";
        public List<StageConfig> stages = new List<StageConfig>();   // index 0 = first stage to fire
        public double payloadMass = 5000;
        public double fairingMass = 1800;

        public static RocketConfig DefaultTwoStage()
        {
            var r = new RocketConfig();
            r.stages.Add(new StageConfig { name = "Stage 1", dryMass = 25600, propellantMass = 395700,
                thrustSeaLevel = 7607e3, thrustVacuum = 8227e3, ispSeaLevel = 282, ispVacuum = 311 });
            r.stages.Add(new StageConfig { name = "Stage 2", dryMass = 4000, propellantMass = 92670,
                thrustSeaLevel = 0.9e6, thrustVacuum = 0.934e6, ispSeaLevel = 280, ispVacuum = 348 });
            return r;
        }

        /// <summary>Mass of everything above (and including) stage i, with all propellant loaded.</summary>
        public double WetMassFrom(int i)
        {
            double m = payloadMass + fairingMass;
            for (int k = i; k < stages.Count; k++) m += stages[k].dryMass + stages[k].propellantMass;
            return m;
        }

        public double TotalMass => WetMassFrom(0);

        /// <summary>Ideal (vacuum, drag-free, gravity-free) Tsiolkovsky delta-v per stage, m/s.</summary>
        public double[] IdealDeltaVPerStage()
        {
            var dv = new double[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                double m0 = WetMassFrom(i);
                double mf = m0 - stages[i].propellantMass;
                dv[i] = stages[i].ispVacuum * Atmosphere1976.G0 * Math.Log(m0 / mf);
            }
            return dv;
        }

        public double IdealDeltaV() { double s = 0; foreach (var d in IdealDeltaVPerStage()) s += d; return s; }
    }
}
