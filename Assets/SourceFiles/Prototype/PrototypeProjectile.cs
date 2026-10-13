using UnityEngine;
namespace DrForms.Prototype
{
    public sealed class PrototypeProjectile : MonoBehaviour
    {
        private PrototypeCombat owner;
        private PrototypeTarget target;
        private TrailRenderer trail;
        private int damage;
        private float speed, expires,progress,distance;
        private Vector3 flight;
        private bool finished = true;
        public void Initialize(PrototypeCombat source, PrototypeTarget destination, int hitDamage, float travelSpeed)
        {
            flight=transform.position;progress=0;distance=Vector3.Distance(flight,destination.transform.position);owner=source;target=destination;damage=hitDamage;speed=travelSpeed;expires=Time.time+3;finished=false;
            if(trail==null) {
                trail=gameObject.AddComponent<TrailRenderer>();trail.time=.10f;trail.minVertexDistance=.12f;
                trail.startWidth=.09f;trail.endWidth=0;
                trail.startColor=new Color(.75f,1,.84f,1);trail.endColor=new Color(.2f,.75f,.6f,0);trail.generateLightingData=false;
                trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;
            }
            trail.sharedMaterial=CourtyardArt.GlowMaterial;trail.Clear();
        }
        private void Update()
        {
            if(finished || Time.timeScale==0) return;
            if(target==null || target.Health<=0 || Time.time>=expires){Finish();return;}
            progress=Mathf.Min(1,progress+speed*Time.deltaTime/Mathf.Max(.1f,distance));
            transform.position=Vector3.Lerp(flight,target.transform.position,progress)+Vector3.up*(Mathf.Sin(progress*Mathf.PI)*.45f);
            transform.Rotate(0,0,240*Time.deltaTime);
            if((transform.position-target.transform.position).sqrMagnitude>.01f)return;
            var destination=target;var source=owner;Finish(); // Transaction closes before damage.
            destination.TakeDamage(damage);if(source!=null)source.HitApplied();
        }
        public void Cancel()=>Finish();
        private void Finish()
        {
            if(finished)return;finished=true;target=null;
            if(owner!=null)owner.ReturnProjectile(this);else Destroy(gameObject);
        }
    }
}
