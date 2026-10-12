using UnityEngine;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(CharacterController), typeof(PrototypeControls))]
    public sealed class PrototypeMovement : MonoBehaviour
    {
        [SerializeField] private Transform movementCamera;
        [SerializeField] private Collider arenaFloor;
        [SerializeField, Min(0)] private float speed = 5;
        [SerializeField, Min(0.01f)] private float stopDistance = 0.12f;
        private CharacterController controller;
        private PrototypeControls controls;
        private Camera view;
        private float verticalSpeed;
        private Vector3 destination;
        private bool hasDestination, arrived;
        private Renderer body;
        private MaterialPropertyBlock visual;
        private Color normalColor;
        private bool showingAttack;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            controls = GetComponent<PrototypeControls>();
            if (movementCamera == null && Camera.main != null) movementCamera = Camera.main.transform;
            view = movementCamera != null ? movementCamera.GetComponent<Camera>() : null;
            body = GetComponent<Renderer>();
            visual = new MaterialPropertyBlock();
            if (body != null) normalColor = body.sharedMaterial.GetColor("_BaseColor");
        }

        private void OnDisable()
        {
            hasDestination = false;
            controls?.Suspend();
            SetAttackVisual(false);
        }

        private void Update()
        {
            controls.PrepareInput();
            var horizontal = controls.TouchMode ? TouchVelocity() : PointerVelocity();
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var previous = transform.position;
            var collisions = controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            SetAttackVisual(controls.AttackActive);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (horizontal.sqrMagnitude > 0)
            {
                movingSeconds += Time.deltaTime;
                requestedDistance += horizontal.magnitude * Time.deltaTime;
                var displacement = transform.position - previous;
                displacement.y = 0;
                traveledDistance += displacement.magnitude;
            }
            if ((collisions & CollisionFlags.Sides) != 0) sideCollisionFrames++;
#endif
        }

        private Vector3 TouchVelocity()
        {
            hasDestination = false;
            var forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward.y = 0;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var input = Vector2.ClampMagnitude(controls.JoystickValue, 1);
            return (right * input.x + forward * input.y) * speed;
        }

        private Vector3 PointerVelocity()
        {
            if (!controls.ReadDesktopPointer(out var pointer, out var changed) || view == null || arenaFloor == null)
            {
                hasDestination = false;
                return Vector3.zero;
            }
            // A stationary cursor retains a fixed world target, independent of camera following.
            if (changed)
            {
                hasDestination = arenaFloor.Raycast(view.ScreenPointToRay(pointer), out var hit, 1000);
                if (hasDestination) destination = hit.point;
                var delta = destination - transform.position;
                delta.y = 0;
                if (delta.magnitude > stopDistance * 2) arrived = false;
            }
            if (!hasDestination) return Vector3.zero;
            var remaining = destination - transform.position;
            remaining.y = 0;
            var distance = remaining.magnitude;
            if (distance <= stopDistance) arrived = true;
            if (arrived || Time.deltaTime <= 0) return Vector3.zero;
            // Slow near the target and cap each step to prevent overshoot on long frames.
            var velocity = Mathf.Min(speed, distance * 8, (distance - stopDistance) / Time.deltaTime);
            return remaining.normalized * Mathf.Max(0, velocity);
        }

        private void SetAttackVisual(bool active)
        {
            if (body == null || visual == null || active == showingAttack) return;
            showingAttack = active;
            body.GetPropertyBlock(visual);
            visual.SetColor("_BaseColor", active ? new Color(1, 0.95f, 0.25f) : normalColor);
            body.SetPropertyBlock(visual);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private float movingSeconds;
        private float requestedDistance;
        private float traveledDistance;
        private int sideCollisionFrames;

        // Read-only browser verification; omitted from release builds.
        public void LogVerificationState()
        {
            var combat = GetComponent<PrototypeCombat>();
            var target = FindAnyObjectByType<PrototypeTarget>();
            Debug.Log("PROTOTYPE_STATE:" + JsonUtility.ToJson(new VerificationState
            {
                shotsFired = combat != null ? combat.ShotsFired : 0,
                projectiles = combat != null ? combat.ActiveProjectiles : 0,
                damage = combat != null ? combat.Damage : 0,
                range = combat != null ? combat.Range : 0,
                cooldown = combat != null ? combat.Cooldown : 0,
                targetHealth = target != null ? target.Health : 0,
                targetAlive = target != null,
                targetScreen = target != null ? Project(target.transform.position) : Vector3.zero,
                position = transform.position,
                destination = destination, hasDestination = hasDestination, arrived = arrived,
                camera = movementCamera != null ? movementCamera.position : Vector3.zero,
                forward = movementCamera != null ? movementCamera.forward : Vector3.forward,
                input = controls.JoystickValue, touchMode = controls.TouchMode, focused = controls.HasFocus,
                pointerInside = PrototypeBrowserInput.Inside, attackCount = controls.AttackCount, attackActive = controls.AttackActive,
                time = Time.time,
                movingSeconds = movingSeconds,
                requestedDistance = requestedDistance,
                traveledDistance = traveledDistance,
                sideCollisionFrames = sideCollisionFrames,
                screenWidth = Screen.width, screenHeight = Screen.height,
                playerFloorScreen = Project(new Vector3(transform.position.x, 0, transform.position.z)),
                eastFloorScreen = Project(new Vector3(10.7f, 0, transform.position.z)),
                joystickCenter = controls.JoystickCenter, joystickRadius = controls.JoystickScreenRadius,
                attackCenter = controls.AttackCenter, hudCenter = controls.HudCenter
            }));
        }

        private Vector3 Project(Vector3 world) => view != null ? view.WorldToScreenPoint(world) : Vector3.zero;

        [System.Serializable]
        private struct VerificationState
        {
            public int shotsFired, projectiles, damage, targetHealth;
            public float range, cooldown;
            public bool targetAlive;
            public Vector3 targetScreen;
            public Vector3 position, destination, camera, forward, playerFloorScreen, eastFloorScreen;
            public Vector2 input, joystickCenter, attackCenter, hudCenter;
            public bool touchMode, focused, pointerInside, hasDestination, arrived, attackActive;
            public float time, movingSeconds, requestedDistance, traveledDistance;
            public float joystickRadius;
            public int sideCollisionFrames, attackCount, screenWidth, screenHeight;
        }
#endif
    }
}
