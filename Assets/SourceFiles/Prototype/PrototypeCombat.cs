using UnityEngine;

namespace DrForms.Prototype
{
    [RequireComponent(typeof(PrototypeControls))]
    public sealed class PrototypeCombat : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.1f)] private float range = 6;
        [SerializeField, Min(0)] private float cooldown = 0.25f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 10;
        private float nextAttack;
        public int ShotsFired { get; private set; }
        public int ActiveProjectiles { get; private set; }
        public int Damage => damage;
        public float CooldownRemaining => Mathf.Max(0, nextAttack - Time.time);
        public float Range => range;
        public float Cooldown => cooldown;
        public float ProjectileSpeed => projectileSpeed;
        public int HitsApplied { get; private set; }
        public float LastShotTime { get; private set; }
        public float PreviousShotTime { get; private set; }
        public bool MuzzleActive => ShotsFired > 0 && Time.time - LastShotTime < 0.09f;

        public bool TryAttack()
        {
            var health = GetComponent<PrototypePlayerHealth>();
            if (health != null && health.IsDefeated) return false;
            if (!GetComponent<PrototypeControls>().CanPlay) return false;
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
            PreviousShotTime = LastShotTime;
            LastShotTime = Time.time;
            ShotsFired++;
            PrototypeAudio.Play(0);
            ActiveProjectiles++;
            var shot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shot.name = "Prototype Projectile";
            var direction = (nearest.transform.position - transform.position).normalized;
            var muzzle = transform.position + direction * 0.65f;
            shot.transform.position = muzzle;
            shot.transform.localScale = Vector3.one * 0.25f;
            var collider = shot.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            shot.GetComponent<Renderer>().sharedMaterial = GetComponent<Renderer>().sharedMaterial;
            PrototypeCombatEffect.Spawn(muzzle, GetComponent<Renderer>().sharedMaterial,
                new Color(1, 0.95f, 0.45f), 0.45f, 0.09f, "Muzzle Flash");
            shot.AddComponent<PrototypeProjectile>().Initialize(this, nearest, damage, projectileSpeed);
            return true;
        }

        public void AddDamage(int upgrade) => damage += Mathf.Max(0, upgrade);

        internal void HitApplied() => HitsApplied++;

        internal void ProjectileFinished() => ActiveProjectiles--;
    }
}
