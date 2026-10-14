using UnityEngine;

namespace DrForms.Prototype
{
    public sealed class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 14, -8);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
        private Vector3 velocity;
        private float lastAspect;
        private Vector3 ViewOffset {
            get {
                var camera = GetComponent<Camera>();
                // Fit the laboratory width on portrait, while landscape retains
                // a useful combat scale. Never scale the input/UI canvas.
                float halfView = Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
                float distance = Mathf.Max(19, 9.2f / (halfView * Mathf.Max(.3f,camera.aspect)));
                return offset.normalized * distance;
            }
        }

        public void ResetComposition()
        {
            velocity = Vector3.zero;
            lastAspect = GetComponent<Camera>().aspect;
            if (target == null) return;
            transform.position = Composition() + ViewOffset;
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        }

        // Frame each room and smoothly reveal the forward route as the player advances.
        private Vector3 Composition() => new Vector3(Mathf.Clamp(target.position.x * .15f, -1.2f, 1.2f), target.position.y, Mathf.Clamp(target.position.z + 2, -6, 63));

        private void OnEnable() => ResetComposition();

        private void LateUpdate()
        {
            if (target == null) return;
            // Rotation/resizing must reframe even while a question or Pause freezes
            // scaled time. This changes presentation only, never the gameplay clock.
            if (Mathf.Abs(GetComponent<Camera>().aspect-lastAspect)>.001f) {
                ResetComposition(); return;
            }
            if (Time.deltaTime <= 0) return;
            transform.position = Vector3.SmoothDamp(transform.position, Composition() + ViewOffset,
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        }
    }
}
