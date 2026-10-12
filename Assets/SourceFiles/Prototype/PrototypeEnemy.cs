using UnityEngine;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(CharacterController), typeof(PrototypeTarget))]
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerHealth player;
        [SerializeField, Min(0)] private float speed = 2.5f;
        [SerializeField, Min(1)] private int contactDamage = 20;
        [SerializeField, Min(0.05f)] private float contactCooldown = 1;
        [SerializeField, Min(0.1f)] private float contactDistance = 1.1f;
        private CharacterController controller;
        private PrototypeTarget health;
        private PrototypeControls controls;
        private float verticalSpeed, nextContact;
        public int ContactHits { get; private set; }
        public float LastContactTime { get; private set; }
        public float PreviousContactTime { get; private set; }
        public int ContactDamage => contactDamage;
        public float ContactCooldown => contactCooldown;
        public float ContactDistance => contactDistance;
        public float Speed => speed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<PrototypeTarget>();
            if (player == null) player = FindAnyObjectByType<PrototypePlayerHealth>();
            if (player != null) controls = player.GetComponent<PrototypeControls>();
        }
        private void Update()
        {
            if (player == null || player.IsDefeated || health.Health <= 0 || controls == null || !controls.CanPlay) return;
            var delta = player.transform.position - transform.position;
            delta.y = 0;
            var distance = delta.magnitude;
            var horizontal = Vector3.zero;
            if (distance > contactDistance * 0.9f && Time.deltaTime > 0)
                horizontal = delta.normalized * Mathf.Min(speed, (distance - contactDistance * 0.9f) / Time.deltaTime);
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((horizontal + Vector3.up * verticalSpeed) * Time.deltaTime);
            delta = player.transform.position - transform.position;
            delta.y = 0;
            if (delta.sqrMagnitude > contactDistance * contactDistance || Time.time < nextContact) return;
            nextContact = Time.time + contactCooldown;
            PreviousContactTime = LastContactTime;
            LastContactTime = Time.time;
            ContactHits++;
            player.TakeDamage(contactDamage);
        }
    }
}
