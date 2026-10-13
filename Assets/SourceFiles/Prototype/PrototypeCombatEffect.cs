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
        private Color color;
        private Vector3 origin, drift;
        private static Mesh sprite;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPool(){pool.Clear();active=0;}
        public static void Spawn(Vector3 position,Material material,Color color,float size,float duration,string name)
        {
            if(active>=24)return;
            PrototypeCombatEffect effect=null;
            while(pool.Count>0 && effect==null)effect=pool.Dequeue();
            if(effect==null){
                if(sprite==null){sprite=new Mesh{name="Combat sparkle quad"};sprite.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};sprite.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};sprite.colors=new[]{Color.white,Color.white,Color.white,Color.white};sprite.triangles=new[]{0,1,2,0,2,3};}
                var obj=new GameObject("Pooled spell sparkle",typeof(MeshFilter),typeof(MeshRenderer));obj.GetComponent<MeshFilter>().sharedMesh=sprite;
                effect=obj.AddComponent<PrototypeCombatEffect>();effect.body=obj.GetComponent<Renderer>();
                effect.body.shadowCastingMode=ShadowCastingMode.Off;effect.body.receiveShadows=false;
                effect.properties=new MaterialPropertyBlock();
            }
            effect.gameObject.name=name;effect.transform.position=position;effect.transform.localScale=Vector3.one*size;
            effect.body.sharedMaterial=CourtyardArt.GlowMaterial;effect.color=color;effect.origin=position;
            float angle=active*2.4f;effect.drift=new Vector3(Mathf.Cos(angle)*.22f,.35f,Mathf.Sin(angle)*.22f);
            effect.properties.SetColor("_BaseColor",color);effect.body.SetPropertyBlock(effect.properties);
            effect.started=Time.time;effect.duration=duration;effect.size=size;effect.counted=true;active++;
            effect.gameObject.SetActive(true);
        }
        private void Update()
        {
            float progress=(Time.time-started)/duration;
            if(progress>=1){gameObject.SetActive(false);pool.Enqueue(this);return;}
            if(Camera.main!=null)transform.rotation=Camera.main.transform.rotation;
            transform.position=origin+drift*progress;
            transform.localScale=Vector3.one*size*(1+progress*.5f);
            properties.SetColor("_BaseColor",new Color(color.r,color.g,color.b,color.a*(1-progress)*(1-progress)));body.SetPropertyBlock(properties);
        }
        private void OnDisable(){if(counted){active=Mathf.Max(0,active-1);counted=false;}}
    }
}
