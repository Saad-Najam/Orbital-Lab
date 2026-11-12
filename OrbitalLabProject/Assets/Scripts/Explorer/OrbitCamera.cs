using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitalLab.Explorer
{
    /// <summary>
    /// Orbit / pan / zoom camera for inspecting a model. LMB drag = orbit, RMB or MMB drag = pan, wheel = zoom, F = frame all.
    /// Smooth-damped. Input is ignored when <see cref="BlockInput"/> returns true (pointer over UI).
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Camera cam;
        public Vector3 target = new Vector3(0, 24, 0);
        public float distance = 70f, yaw = 35f, pitch = 12f;
        public float minDistance = 0.6f, maxDistance = 140f;
        public float orbitSpeed = 0.22f, panSpeed = 0.0016f, zoomSpeed = 0.1f, smoothTime = 0.12f;
        public bool reducedMotion;
        public System.Func<bool> BlockInput;
        public bool IsDragging { get; private set; }
        public float DragDistance { get; private set; }

        Vector3 tgtGoal; float distGoal, yawGoal, pitchGoal; Vector3 tv; float dv, yv, pv;
        Vector2 downPos; bool down;

        bool goalsSet;
        void Start() { if (!goalsSet) { tgtGoal = target; distGoal = distance; yawGoal = yaw; pitchGoal = pitch; } }
        public void SnapToGoals() { target = tgtGoal; distance = distGoal; yaw = yawGoal; pitch = pitchGoal; }

        public void FocusOn(Bounds b, float padding = 2.4f)
        {
            goalsSet = true; tgtGoal = b.center; distGoal = Mathf.Clamp(b.extents.magnitude * padding, minDistance, maxDistance);
        }
        public void FrameAll(Bounds b) { goalsSet = true; tgtGoal = b.center; distGoal = Mathf.Clamp(b.extents.magnitude * 2.9f, minDistance, maxDistance); yawGoal = 35; pitchGoal = 12; }

        void Update()
        {
            var mouse = Mouse.current; var kb = Keyboard.current;
            if (mouse == null) return;
            bool blocked = BlockInput != null && BlockInput();
            Vector2 mp = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame && !blocked) { down = true; downPos = mp; DragDistance = 0; }
            if (mouse.leftButton.wasReleasedThisFrame) { down = false; IsDragging = false; }
            Vector2 d = mouse.delta.ReadValue();
            if (down && mouse.leftButton.isPressed)
            {
                DragDistance = Mathf.Max(DragDistance, (mp - downPos).magnitude);
                if (DragDistance > 4f) { IsDragging = true; yawGoal += d.x * orbitSpeed; pitchGoal = Mathf.Clamp(pitchGoal - d.y * orbitSpeed, -80f, 85f); }
            }
            if (!blocked && (mouse.rightButton.isPressed || mouse.middleButton.isPressed))
            {
                var right = transform.right; var up = transform.up; float s = panSpeed * distance;
                tgtGoal += (-right * d.x - up * d.y) * s;
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (!blocked && Mathf.Abs(scroll) > 0.01f) distGoal = Mathf.Clamp(distGoal * (1f - Mathf.Sign(scroll) * zoomSpeed), minDistance, maxDistance);
            if (kb != null && !blocked && kb.fKey.wasPressedThisFrame) FrameHint?.Invoke();

            float st = reducedMotion ? 0.001f : smoothTime;
            target = Vector3.SmoothDamp(target, tgtGoal, ref tv, st);
            distance = Mathf.SmoothDamp(distance, distGoal, ref dv, st);
            yaw = Mathf.SmoothDampAngle(yaw, yawGoal, ref yv, st);
            pitch = Mathf.SmoothDamp(pitch, pitchGoal, ref pv, st);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            transform.position = target - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
        public System.Action FrameHint;
        public void SetView(float y, float p, float d, Vector3 t) { goalsSet = true; yawGoal = y; pitchGoal = p; distGoal = d; tgtGoal = t; }
    }
}
