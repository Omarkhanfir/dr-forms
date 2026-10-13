using UnityEngine;

namespace DrForms.Prototype
{
    public sealed class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 17, -6);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
        private Vector3 velocity;

        public void ResetComposition()
        {
            velocity = Vector3.zero;
            if (target == null) return;
            transform.position = Composition() + offset;
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        }

        // Frame each room and smoothly reveal the forward route as the player advances.
        private Vector3 Composition() => new Vector3(Mathf.Clamp(target.position.x, -8, 8), target.position.y, Mathf.Clamp(target.position.z + 3, -6, 63));

        private void OnEnable() => ResetComposition();

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0) return;
            transform.position = Vector3.SmoothDamp(transform.position, Composition() + offset,
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        }
    }
}
