using UnityEngine;
using UnityEngine.InputSystem;
using OrbitalLab.Player;

namespace OrbitalLab.Interaction
{
    /// <summary>Raycasts from the camera centre, shows a prompt, dispatches E, and offers teleport hotkeys (1-4).</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public Camera cam;
        public FirstPersonController player;
        public Transform[] teleportTargets;
        public string[] teleportNames;

        Interactable current;
        GUIStyle promptStyle, boxStyle, hudStyle;

        void Update()
        {
            current = null;
            if (FirstPersonController.InputLocked || cam == null) return;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                var it = hit.collider.GetComponentInParent<Interactable>();
                if (it != null && hit.distance <= it.maxDistance) current = it;
            }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (current != null && kb.eKey.wasPressedThisFrame) current.Interact();

            // accessibility: teleport to major facilities
            if (teleportTargets != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
                for (int i = 0; i < teleportTargets.Length && i < keys.Length; i++)
                    if (kb[keys[i]].wasPressedThisFrame && teleportTargets[i] != null)
                        player.TeleportTo(teleportTargets[i].position, teleportTargets[i].eulerAngles.y);
            }
        }

        void OnGUI()
        {
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                promptStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
                hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
                hudStyle.normal.textColor = new Color(0.7f, 0.8f, 0.9f, 0.9f);
            }
            if (FirstPersonController.InputLocked) return;
            // crosshair
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            GUI.color = new Color(1, 1, 1, 0.8f);
            GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (current != null)
            {
                var r = new Rect(cx - 260, cy + 40, 520, 40);
                GUI.color = new Color(0, 0.06f, 0.1f, 0.75f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
                GUI.Label(r, $"[E]  {current.prompt}", promptStyle);
            }
            string tp = "";
            if (teleportNames != null) for (int i = 0; i < teleportNames.Length; i++) tp += $"[{i + 1}] {teleportNames[i]}   ";
            GUI.Label(new Rect(14, Screen.height - 30, Screen.width - 28, 24),
                "WASD move  |  Shift sprint  |  Mouse look  |  Esc free cursor  |  " + tp, hudStyle);
        }
    }
}
