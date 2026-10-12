using UnityEngine;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(PrototypeControls))]
    public sealed class PrototypeCombat : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.1f)] private float range = 6;
        [SerializeField, Min(0)] private float cooldown = 0.6f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10;
        private float nextAttack;
        public int ShotsFired { get; private set; }
        public int ActiveProjectiles { get; private set; }
        public int Damage => damage;
        public float Range => range;
        public float Cooldown => cooldown;

        public bool TryAttack()
        {
            if (Time.time < nextAttack) return false;
            PrototypeTarget nearest = null;
            var nearestDistance = range * range;
            foreach (var target in FindObjectsByType<PrototypeTarget>())
            {
                if (target.Health <= 0) continue;
                var distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance > nearestDistance) continue;
                nearestDistance = distance;
                nearest = target;
            }
            if (nearest == null) return false;
            nextAttack = Time.time + cooldown;
            ShotsFired++;
            ActiveProjectiles++;
            var shot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shot.name = "Prototype Projectile";
            shot.transform.position = transform.position;
            shot.transform.localScale = Vector3.one * 0.25f;
            var collider = shot.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            shot.GetComponent<Renderer>().sharedMaterial = GetComponent<Renderer>().sharedMaterial;
            shot.AddComponent<PrototypeProjectile>().Initialize(this, nearest, damage, projectileSpeed);
            return true;
        }

        internal void ProjectileFinished() => ActiveProjectiles--;
    }
}
