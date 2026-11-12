using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitalLab.Explorer
{
    [Serializable] public class SubsystemDef { public string id, name, color, blurb; public Color32 Color => ParseColor(color);
        public static Color32 ParseColor(string hex) { return ColorUtility.TryParseHtmlString(hex, out var c) ? (Color32)c : new Color32(200, 200, 200, 255); } }
    [Serializable] public class AssemblyDef { public string id, name; }
    [Serializable] public class InterfaceDef { public string type, text; }
    [Serializable] public class QuizDef { public string question; public string[] options; public int answer; public string explanation; public bool Exists => !string.IsNullOrEmpty(question); }

    [Serializable]
    public class PartDef
    {
        public string id, name, subsystem, assembly, summary, what, why, how, source, note;
        public string[] facts;
        public InterfaceDef[] interfaces;
        public string[] connects;
        public QuizDef quiz;
    }

    [Serializable]
    public class PartCatalogData
    {
        public SubsystemDef[] subsystems;
        public AssemblyDef[] assemblies;
        public PartDef[] parts;
        public string note;
    }

    /// <summary>Loads the learning content (Resources/ExplorerData/parts.json).</summary>
    public class PartCatalog
    {
        public PartCatalogData Data;
        public Dictionary<string, PartDef> ById = new Dictionary<string, PartDef>();
        public Dictionary<string, SubsystemDef> SubById = new Dictionary<string, SubsystemDef>();

        public static PartCatalog Load()
        {
            var ta = Resources.Load<TextAsset>("ExplorerData/parts");
            if (ta == null) { Debug.LogError("[Explorer] parts.json missing in Resources/ExplorerData"); return new PartCatalog { Data = new PartCatalogData { parts = new PartDef[0], subsystems = new SubsystemDef[0], assemblies = new AssemblyDef[0] } }; }
            var c = new PartCatalog { Data = JsonUtility.FromJson<PartCatalogData>(ta.text) };
            foreach (var p in c.Data.parts) c.ById[p.id] = p;
            foreach (var s in c.Data.subsystems) c.SubById[s.id] = s;
            return c;
        }

        public string SourceLabel(string tag)
        {
            switch (tag) { case "V": return "Verified: NASA Glenn Research Center"; case "T": return "Standard textbook principle"; default: return "Educational simplification"; }
        }
    }
}
