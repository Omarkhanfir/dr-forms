using UnityEngine;

namespace DrForms.Prototype
{
    public sealed class PrototypeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(8, 14, -10);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;
        private Vector3 velocity;

        private void OnEnable()
        {
            velocity = Vector3.zero;
            if (target == null) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset, Vector3.up);
        }

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0) return;
            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset,
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime);
        }
    }
}
