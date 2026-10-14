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
        private float nextAttack,releaseAt;
        private PrototypeTarget pendingTarget;
        public float LastWindupTime {get;private set;}
        public float ReleaseDelay {get;private set;}
        public Vector3 LastReleaseOrigin {get;private set;}
        public bool ThrowPending => pendingTarget!=null;
        public void CancelPendingThrow(){pendingTarget=null;GetComponent<CourtyardActor>()?.CancelThrow();}
        private void LateUpdate(){
            if(!ThrowPending)return;
            if(!GetComponent<PrototypeControls>().CanPlay||GetComponent<PrototypePlayerHealth>().IsDefeated){CancelPendingThrow();return;}
            if(Time.time<releaseAt)return;
            var destination=pendingTarget;pendingTarget=null;
            if(destination!=null&&destination.Health>0)Release(destination);
        }
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
            if (ThrowPending || Time.time < nextAttack || ActiveProjectiles >= 8) return false;
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
            AimDirection=(nearest.transform.position-transform.position).normalized;
            pendingTarget=nearest;LastWindupTime=Time.time;releaseAt=Time.time+.10f;
            GetComponent<CourtyardActor>()?.BeginThrow();
            return true;
        }
        private void Release(PrototypeTarget nearest)
        {
            PreviousShotTime = LastShotTime;
            LastShotTime = Time.time;
            ShotsFired++;
            PrototypeAudio.Play(0);
            ActiveProjectiles++;
            if (projectileRoot == null) projectileRoot = new GameObject("Chemical Flask Pool");
            PrototypeProjectile projectile;
            if (available.Count > 0) projectile = available.Dequeue();
            else {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = "Chemical Flask"; obj.transform.SetParent(projectileRoot.transform);
                var collision = obj.GetComponent<Collider>(); collision.enabled = false; Destroy(collision);
                obj.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PrototypeCharacterVisual.Part(obj.transform,"Glass flask neck",PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.32f,.40f,.32f),7);
                projectile = obj.AddComponent<PrototypeProjectile>(); PoolSize++;
            }
            var shot = projectile.gameObject;
            AimDirection = (nearest.transform.position - transform.position).normalized;
            var muzzle = GetComponent<CourtyardActor>()?.ReleasePosition ?? transform.position + AimDirection * .65f;
            LastReleaseOrigin=muzzle;ReleaseDelay=Time.time-LastWindupTime;
            GetComponent<CourtyardActor>()?.ReleaseFlask();
            shot.transform.position = muzzle;
            shot.transform.localScale = Vector3.one * (.34f + Mathf.Clamp(damage-25,0,30)*.004f);
            var material = CourtyardArt.EnvironmentMaterial("Green");
            shot.GetComponent<Renderer>().sharedMaterial = material;
            shot.SetActive(true);
            projectile.Initialize(this, nearest, damage, projectileSpeed);
            PrototypeCombatEffect.Spawn(muzzle, material, new Color(1,.95f,.55f), .4f, .09f, "Chemical release");
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
