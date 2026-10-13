using UnityEngine;
using System.Collections.Generic;

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
        private readonly Queue<PrototypeProjectile> available = new Queue<PrototypeProjectile>();
        private GameObject projectileRoot;
        public Vector3 AimDirection { get; private set; }
        public int PoolSize { get; private set; }
        private void OnDestroy() { if (projectileRoot != null) Destroy(projectileRoot); }
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
            if (Time.time < nextAttack || ActiveProjectiles >= 8) return false;
            PrototypeTarget nearest = null;
            var nearestDistance = range * range;
            foreach (var target in PrototypeTarget.Active)
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
            if (projectileRoot == null) projectileRoot = new GameObject("Wand Projectile Pool");
            PrototypeProjectile projectile;
            if (available.Count > 0) projectile = available.Dequeue();
            else {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = "Wand Bolt"; obj.transform.SetParent(projectileRoot.transform);
                var collision = obj.GetComponent<Collider>(); collision.enabled = false; Destroy(collision);
                obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                projectile = obj.AddComponent<PrototypeProjectile>(); PoolSize++;
            }
            var shot = projectile.gameObject;
            AimDirection = (nearest.transform.position - transform.position).normalized;
            var muzzle = transform.position + AimDirection * .65f;
            shot.transform.position = muzzle;
            shot.transform.localScale = Vector3.one * (.34f + Mathf.Clamp(damage-25,0,30)*.004f);
            var material = PrototypeCharacterVisual.Paint(damage >= 45 ? 8 : 7);
            shot.GetComponent<Renderer>().sharedMaterial = material;
            shot.SetActive(true);
            projectile.Initialize(this, nearest, damage, projectileSpeed);
            PrototypeCombatEffect.Spawn(muzzle, material, new Color(1,.95f,.55f), .4f, .09f, "Wand Spark");
            return true;
        }

        public void AddDamage(int upgrade) => damage += Mathf.Max(0, upgrade);

        internal void HitApplied() => HitsApplied++;

        internal void ReturnProjectile(PrototypeProjectile projectile)
        {
            ActiveProjectiles = Mathf.Max(0, ActiveProjectiles - 1);
            projectile.gameObject.SetActive(false); available.Enqueue(projectile);
        }
    }
}
