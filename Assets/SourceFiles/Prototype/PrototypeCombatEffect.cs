using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace DrForms.Prototype
{
    // Up to 24 reusable, unlit-by-effects bursts. No new lights or particles.
    public sealed class PrototypeCombatEffect : MonoBehaviour
    {
        private static readonly Queue<PrototypeCombatEffect> pool=new Queue<PrototypeCombatEffect>();
        private static int active;
        private float started,duration,size;
        private Renderer body;
        private MaterialPropertyBlock properties;
        private bool counted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPool(){pool.Clear();active=0;}
        public static void Spawn(Vector3 position,Material material,Color color,float size,float duration,string name)
        {
            if(active>=24)return;
            PrototypeCombatEffect effect=null;
            while(pool.Count>0 && effect==null)effect=pool.Dequeue();
            if(effect==null){
                var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var collision=obj.GetComponent<Collider>();collision.enabled=false;Destroy(collision);
                effect=obj.AddComponent<PrototypeCombatEffect>();effect.body=obj.GetComponent<Renderer>();
                effect.body.shadowCastingMode=ShadowCastingMode.Off;effect.body.receiveShadows=false;
                effect.properties=new MaterialPropertyBlock();
            }
            effect.gameObject.name=name;effect.transform.position=position;effect.transform.localScale=Vector3.one*size;
            effect.body.sharedMaterial=material;effect.properties.SetColor("_BaseColor",color);effect.body.SetPropertyBlock(effect.properties);
            effect.started=Time.time;effect.duration=duration;effect.size=size;effect.counted=true;active++;
            effect.gameObject.SetActive(true);
        }
        private void Update()
        {
            float progress=(Time.time-started)/duration;
            if(progress>=1){gameObject.SetActive(false);pool.Enqueue(this);return;}
            transform.localScale=Vector3.one*size*(1+progress)*(1-progress);
        }
        private void OnDisable(){if(counted){active=Mathf.Max(0,active-1);counted=false;}}
    }
}
