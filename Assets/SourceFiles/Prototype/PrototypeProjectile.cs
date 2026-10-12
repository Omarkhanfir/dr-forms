using UnityEngine;

namespace DrForms.Prototype
{
    public sealed class PrototypeProjectile : MonoBehaviour
    {
        private PrototypeCombat owner;
        private PrototypeTarget target;
        private int damage;
        private float speed;
        private float expires;
        private bool finished;
        public void Initialize(PrototypeCombat source, PrototypeTarget destination, int hitDamage, float travelSpeed)
        {
            owner = source;
            target = destination;
            damage = hitDamage;
            speed = travelSpeed;
            expires = Time.time + 10;
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = GetComponent<Renderer>().sharedMaterial;
            trail.time = 0.12f;
            trail.minVertexDistance = 0.15f;
            trail.startWidth = 0.18f;
            trail.endWidth = 0;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.generateLightingData = true;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
        }
        private void Update()
        {
            if (finished) return;
            if (target == null || target.Health <= 0 || Time.time >= expires)
            {
                Finish();
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude > 0.01f) return;
            // Complete before damage: deferred destruction cannot apply a second hit.
            finished = true;
            target.TakeDamage(damage);
            if (owner != null) owner.HitApplied();
            Destroy(gameObject);
        }
        private void Finish() { finished = true; Destroy(gameObject); }
        private void OnDestroy() { if (owner != null) owner.ProjectileFinished(); }
    }
}
