using UnityEngine;

namespace OrbitalLab.Interaction
{
    /// <summary>Forwards interaction on a decorative mesh (e.g. a screen) to the real interactable.</summary>
    public class RocketWorkstationProxy : Interactable
    {
        public RocketWorkstation target;
        void Awake() { if (target != null) prompt = target.prompt; }
        public override void Interact() { if (target != null) target.Interact(); }
    }
}
