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
        public void Initialize(PrototypeCombat source, PrototypeTarget destination, int hitDamage, float travelSpeed)
        {
            owner = source;
            target = destination;
            damage = hitDamage;
            speed = travelSpeed;
            expires = Time.time + 10;
        }
        private void Update()
        {
            if (target == null || target.Health <= 0 || Time.time >= expires)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
            if ((transform.position - target.transform.position).sqrMagnitude > 0.01f) return;
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
        private void OnDestroy() { if (owner != null) owner.ProjectileFinished(); }
    }
}
