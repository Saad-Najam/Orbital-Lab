using System;
using System.Linq;
using System.Collections.Generic;
using OrbitalLab.Simulation;
using OrbitalLab.Explorer.Build;
static class BuildTests {
  static int fail=0;
  static void T(string n, bool ok, string extra="") { if(!ok) fail++; Console.WriteLine($"{(ok?"PASS":"FAIL")}  {n} {extra}"); }
  static Design Full() { var d=new Design(); foreach(var s in BuildRules.Steps) foreach(var p in s.parts) { var why=BuildRules.CanPlace(d.placed,p); if(why!=null){ Console.WriteLine("ORDER PROBLEM "+p+": "+why); } d.placed.Add(p);} return d; }
  public static int Run(){
    // every part in step order is placeable in that order
    var d=new Design(); bool orderOk=true; foreach(var s in BuildRules.Steps) foreach(var p in s.parts){ if(BuildRules.CanPlace(d.placed,p)!=null) { orderOk=false; Console.WriteLine("cannot place "+p+" in step order: "+BuildRules.CanPlace(d.placed,p)); } d.placed.Add(p);} 
    T("all 56 parts placeable following the guided step order", orderOk && d.placed.Count==56, $"(count {d.placed.Count})");
    T("every required id exists", BuildRules.Requires.All(kv=>kv.Value.req.All(r=>d.placed.Contains(r))));
    var full=Full(); var f=BuildRules.Check(full);
    T("complete design has no blocking errors", !BuildRules.HasBlockingError(f), string.Join("; ", f.Where(x=>x.severity==Severity.Error).Select(x=>x.title)));
    var cfg=BuildRules.ToConfig(full); var sim=new AscentSimulator(cfg); sim.RunToCompletion();
    T("complete design reaches orbit in the simulation", sim.InOrbit, $"({sim.Outcome}, ideal dv {cfg.IdealDeltaV():F0}, mass {cfg.TotalMass:F0}, peri {sim.PeriapsisAlt/1000:F0} km)");
    // missing flight computer -> explained error
    var nofc=Full(); foreach(var x in BuildRules.Dependents(nofc.placed,"AV_FlightComputer").Concat(new[]{"AV_FlightComputer"})) nofc.placed.Remove(x);
    var f2=BuildRules.Check(nofc); T("removing flight computer raises an explained error", f2.Any(x=>x.severity==Severity.Error && x.title.Contains("flight computer")));
    // too few engines -> cannot lift off
    var few=Full(); foreach(var e in BuildRules.RingEngines) few.placed.Remove(e); few.payloadKg=20000; var f3=BuildRules.Check(few);
    var cfg3=BuildRules.ToConfig(few); Console.WriteLine($"   1 engine: TWR {cfg3.stages[0].thrustSeaLevel/(cfg3.TotalMass*9.80665):F2}");
    T("one engine and heavy payload cannot lift off", f3.Any(x=>x.title.StartsWith("Cannot lift off")));
    // dependency removal
    var dep=BuildRules.Dependents(full.placed,"S1_FuelTank"); T("removing fuel tank also removes everything stacked above it", dep.Contains("S1_Intertank") && dep.Contains("S1_Skin") && dep.Contains("PL_FairingHalf_A"));
    T("cannot place nozzle before chamber", BuildRules.CanPlace(new HashSet<string>{"S1_ThrustStructure"},"S1E_Nozzle")!=null);
    return fail;
  }
}
