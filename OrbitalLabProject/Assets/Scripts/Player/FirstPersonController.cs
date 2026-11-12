using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitalLab.Player
{
    /// <summary>
    /// First-person controller on a CharacterController (collision + gravity), new Input System.
    /// Smooth acceleration/deceleration, smoothed look, sprint, adjustable sensitivity and speed.
    /// Jump is disabled by default: the facility is a walk-through lab, jumping is not architecturally justified.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Transform cameraPivot;
        [Header("Movement")]
        public float walkSpeed = 3.2f;
        public float sprintSpeed = 5.5f;
        public float acceleration = 14f;
        public float gravity = -9.81f;
        public bool allowJump = false;
        public float jumpHeight = 0.6f;
        [Header("Look")]
        [Range(0.02f, 0.6f)] public float sensitivity = 0.12f;  // degrees per pixel
        public float lookSmoothing = 0.045f;
        public float maxPitch = 85f;
        public bool reducedMotion = false;      // disables head bob
        [Header("Head bob")]
        public float bobAmount = 0.035f;
        public float bobFrequency = 9f;

        /// <summary>When true (workstation open) movement/look are frozen and the cursor is free.</summary>
        public static bool InputLocked;

        CharacterController cc;
        Vector3 velocity;          // horizontal velocity (smoothed)
        float verticalVelocity;
        float yaw, pitch;
        Vector2 lookVel, lookCur;
        float bobTimer;
        float pivotBaseY;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
            pivotBaseY = cameraPivot != null ? cameraPivot.localPosition.y : 1.6f;
            yaw = transform.eulerAngles.y;
        }

        void OnEnable() { SetCursor(true); }

        public static void SetCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame && !InputLocked) SetCursor(Cursor.lockState != CursorLockMode.Locked);

            bool active = !InputLocked && Cursor.lockState == CursorLockMode.Locked;

            // ---- look (smoothed) ----
            if (active && mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * sensitivity;
                lookCur = Vector2.SmoothDamp(lookCur, d, ref lookVel, lookSmoothing);
                yaw += lookCur.x;
                pitch = Mathf.Clamp(pitch - lookCur.y, -maxPitch, maxPitch);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
                if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
            }

            // ---- move ----
            Vector2 input = Vector2.zero;
            if (active)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
                input = Vector2.ClampMagnitude(input, 1f);
            }
            bool sprint = active && kb.leftShiftKey.isPressed && input.y > 0;
            float target = sprint ? sprintSpeed : walkSpeed;
            Vector3 wish = (transform.right * input.x + transform.forward * input.y) * target;
            velocity = Vector3.MoveTowards(velocity, wish, acceleration * Time.deltaTime);

            if (cc.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
            if (active && allowJump && cc.isGrounded && kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            cc.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // ---- subtle head bob (off with reduced motion) ----
            if (cameraPivot && !reducedMotion)
            {
                float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                if (cc.isGrounded && speed > 0.1f) bobTimer += Time.deltaTime * bobFrequency * (speed / walkSpeed);
                else bobTimer = Mathf.Lerp(bobTimer, 0, Time.deltaTime * 4f);
                float y = Mathf.Sin(bobTimer) * bobAmount * Mathf.Clamp01(speed / walkSpeed);
                var lp = cameraPivot.localPosition; lp.y = pivotBaseY + y; cameraPivot.localPosition = lp;
            }
        }

        /// <summary>Instant relocation (teleport accessibility option).</summary>
        public void TeleportTo(Vector3 pos, float yawDegrees)
        {
            cc.enabled = false;
            transform.position = pos; yaw = yawDegrees; transform.rotation = Quaternion.Euler(0, yaw, 0);
            velocity = Vector3.zero; verticalVelocity = 0;
            cc.enabled = true;
        }
    }
}
