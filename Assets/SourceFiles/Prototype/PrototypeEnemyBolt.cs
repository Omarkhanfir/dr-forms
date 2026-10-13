using UnityEngine;
namespace DrForms.Prototype
{
    // Aimed once at windup start: no homing and no collision surprises.
    public sealed class PrototypeEnemyBolt : MonoBehaviour
    {
        private static int active;
        private PrototypePlayerHealth player;
        private PrototypeControls controls;
        private Vector3 direction;
        private float expires;
        private const float Speed = 3.2f;
        public static int ActiveCount => active;
        public static void Spawn(Vector3 origin, Vector3 aim, PrototypePlayerHealth target)
        {
            if(active>=4) return;
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="Telegraphed Seed Bolt";
            obj.transform.position=origin;obj.transform.localScale=Vector3.one*.38f;
            var collision=obj.GetComponent<Collider>();collision.enabled=false;Destroy(collision);
            obj.GetComponent<Renderer>().sharedMaterial=PrototypeCharacterVisual.Paint(8);
            var bolt=obj.AddComponent<PrototypeEnemyBolt>();bolt.player=target;bolt.controls=target.GetComponent<PrototypeControls>();
            aim.y=origin.y;bolt.direction=(aim-origin).normalized;bolt.expires=Time.time+6;active++;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount()=>active=0;
        private void Update()
        {
            if(player==null){Destroy(gameObject);return;}
            if(Time.timeScale<=0 || !controls.CanPlay) return;
            if(player.IsDefeated || Time.time>=expires) {Destroy(gameObject);return;}
            Vector3 from=transform.position,to=from+direction*(Speed*Time.deltaTime);
            var p=player.transform.position; p.y=from.y;
            var segment=to-from;float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-from,segment)/segment.sqrMagnitude):0;
            if((from+segment*t-p).sqrMagnitude<.6f*.6f){player.TakeDamage(8);Destroy(gameObject);return;}
            transform.position=to;
            if(Mathf.Abs(to.x)>9.8f || Mathf.Abs(to.z)>9.8f)Destroy(gameObject);
        }
        public static void Clear(){foreach(var bolt in FindObjectsByType<PrototypeEnemyBolt>())Destroy(bolt.gameObject);}
        private void OnDestroy()=>active=Mathf.Max(0,active-1);
    }
}
