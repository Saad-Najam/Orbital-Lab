using System;
using OrbitalLab.Simulation;

class P {
  static int fail = 0;
  static void Check(string n, double got, double exp, double tol) {
    bool ok = Math.Abs(got-exp) <= tol; if(!ok) fail++;
    Console.WriteLine($"{(ok?"PASS":"FAIL")}  {n}: got {got:G8}, expected {exp:G8} (tol {tol})");
  }
  static void Main() {
    // US Std Atmosphere 1976 reference values
    var a0 = Atmosphere1976.At(0);      Check("rho(0)", a0.Density, 1.2250, 1e-4);
    Check("T(0)", a0.Temperature, 288.15, 1e-6); Check("P(0)", a0.Pressure, 101325, 1e-3);
    var a11 = Atmosphere1976.At(11019.1); // geopotential 11000 m
    Check("T(11km gp)", a11.Temperature, 216.65, 0.05); Check("P(11km gp)", a11.Pressure, 22632.1, 15);
    Check("rho(10000 m geometric)", Atmosphere1976.At(10000).Density, 0.41351, 2e-3);
    Check("rho(20000 m geometric)", Atmosphere1976.At(20000).Density, 0.08891, 1e-3);
    Check("P(50000 m geometric)", Atmosphere1976.At(50000).Pressure, 79.78, 1.0);
    // Tsiolkovsky
    var one = new RocketConfig{ payloadMass=0, fairingMass=0 };
    one.stages.Add(new StageConfig{ dryMass=1000, propellantMass=9000, ispVacuum=300 });
    Check("dv Tsiolkovsky", one.IdealDeltaV(), 300*9.80665*Math.Log(10), 1e-6);
    // Falcon-9-like default
    var cfg = RocketConfig.DefaultTwoStage();
    Console.WriteLine($"Default rocket: mass {cfg.TotalMass:F0} kg, ideal dv {cfg.IdealDeltaV():F0} m/s");
    // Circular orbit energy conservation: coast in orbit via simulator with zero thrust
    var sim = new AscentSimulator(cfg);
    sim.RunToCompletion();
    var last = sim.Log[sim.Log.Count-1];
    Console.WriteLine($"Outcome: {sim.Outcome}; t={last.t:F0}s alt={last.altitude/1000:F1}km v={last.speed:F0} m/s peri={sim.PeriapsisAlt/1000:F1}km apo={sim.ApoapsisAlt/1000:F1}km");
    double maxQ=0,maxG=0,tq=0; foreach(var s in sim.Log){ if(s.dynamicPressure>maxQ){maxQ=s.dynamicPressure;tq=s.t;} maxG=Math.Max(maxG,s.acceleration/9.80665);}
    Console.WriteLine($"MaxQ {maxQ/1000:F1} kPa at t={tq:F0}s; max accel {maxG:F1} g");
    // mass conservation check: initial mass - propellant burned - dry mass jettisoned
    Console.WriteLine($"Final mass {last.mass:F0} kg");
    
    double rFinal = AscentSimulator.EarthR + last.altitude;
    Check("orbit speed vs vis-viva circular speed (m/s)", last.speed, Math.Sqrt(AscentSimulator.Mu/rFinal), 60);
    Check("orbit achieved (1=yes)", sim.InOrbit?1:0, 1, 0);
    Check("periapsis within 25 km of target (km)", sim.PeriapsisAlt/1000, sim.targetAltitude/1000, 25);
    // energy conservation during coast: two-body specific energy constant
    double e0=0,e1=0; int c=0; foreach(var s in sim.Log){ if(s.phase=="COAST" && s.thrust==0){ double r=AscentSimulator.EarthR+s.altitude; double en=s.speed*s.speed/2-AscentSimulator.Mu/r; if(c==0)e0=en; e1=en; c++; } }
    Check("coast specific energy drift (J/kg)", e1, e0, 2000);
    // max-Q plausibility band for a Falcon-9-class vehicle
    Check("max-Q in plausible band (kPa)", maxQ/1000, 35, 15);
fail += BuildTests.Run(); Console.WriteLine(fail==0 ? "ALL ANALYTIC CHECKS PASSED" : $"{fail} CHECK(S) FAILED");
    Environment.Exit(fail);
  }
}
