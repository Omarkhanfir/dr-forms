using UnityEngine;
using UnityEngine.InputSystem;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PrototypeMovement : MonoBehaviour
    {
        [SerializeField] private Transform movementCamera;
        [SerializeField, Min(0)] private float speed = 5;
        private CharacterController controller;
        private InputAction move;
        private float verticalSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (movementCamera == null && Camera.main != null) movementCamera = Camera.main.transform;
            move = new InputAction("Prototype Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        }

        private void OnEnable() => move.Enable();
        private void OnDisable() => move.Disable();
        private void OnDestroy() => move.Dispose();

        private void Update()
        {
            var input = Application.isFocused ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1) : Vector2.zero;
            var forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            forward.y = 0;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var horizontal = (right * input.x + forward * input.y) * speed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            var previous = transform.position;
            var collisions = controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (input.sqrMagnitude > 0)
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private float movingSeconds;
        private float requestedDistance;
        private float traveledDistance;
        private int sideCollisionFrames;

        // Read-only browser verification through UnityInstance.SendMessage in development builds.
        public void LogVerificationState()
        {
            Debug.Log("PROTOTYPE_STATE:" + JsonUtility.ToJson(new VerificationState
            {
                position = transform.position,
                camera = movementCamera != null ? movementCamera.position : Vector3.zero,
                forward = movementCamera != null ? movementCamera.forward : Vector3.forward,
                input = move.ReadValue<Vector2>(),
                time = Time.time,
                movingSeconds = movingSeconds,
                requestedDistance = requestedDistance,
                traveledDistance = traveledDistance,
                sideCollisionFrames = sideCollisionFrames
            }));
        }

        [System.Serializable]
        private struct VerificationState
        {
            public Vector3 position, camera, forward;
            public Vector2 input;
            public float time, movingSeconds, requestedDistance, traveledDistance;
            public int sideCollisionFrames;
        }
#endif
    }
}
