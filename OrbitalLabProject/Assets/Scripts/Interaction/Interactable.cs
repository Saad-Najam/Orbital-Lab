using UnityEngine;

namespace OrbitalLab.Interaction
{
    /// <summary>Anything the player can look at and press E on.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string prompt = "Interact";
        public float maxDistance = 3.2f;
        public abstract void Interact();
    }
}
