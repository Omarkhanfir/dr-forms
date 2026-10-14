using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace DrForms.Prototype
{
    // Original mesh contacts and a bounded, reusable world-space number pool.
    public sealed class PrototypePresentation : MonoBehaviour
    {
        private static Mesh contactMesh;
        private static Material contactMaterial;
        private static readonly Queue<PrototypePresentation> numbers = new Queue<PrototypePresentation>();
        private static int active;
        private Text label;
        private Vector3 origin;
        private float started;
        private bool counted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if(contactMesh!=null) Destroy(contactMesh);
            if(contactMaterial!=null) Destroy(contactMaterial);
            contactMesh=null; contactMaterial=null; numbers.Clear(); active=0;
        }
        internal static void Contact(Transform parent, Vector3 position, float diameter)
        {
            if(contactMesh==null) {
                var v=new List<Vector3>{Vector3.zero};var c=new List<Color>{new Color(.08f,.16f,.1f,.22f)};var t=new List<int>();
                for(int i=0;i<=32;i++){float a=i*Mathf.PI/16;v.Add(new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f));c.Add(new Color(.08f,.16f,.1f,0));}
                for(int i=1;i<=32;i++)t.AddRange(new[]{0,i+1,i});
                contactMesh=new Mesh{name="Soft Radial Contact"};contactMesh.SetVertices(v);contactMesh.SetColors(c);contactMesh.SetTriangles(t,0);contactMesh.RecalculateNormals();
                contactMaterial=new Material(Shader.Find("DrForms/Soft Contact"));
            }
            var obj=new GameObject("Soft Ground Contact",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=new Vector3(diameter,1,diameter);
            obj.GetComponent<MeshFilter>().sharedMesh=contactMesh;
            var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=contactMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        internal static void Damage(Vector3 position,int amount)
        {
            if(active>=12)return;
            PrototypePresentation number=null;while(numbers.Count>0&&number==null)number=numbers.Dequeue();
            if(number==null){
                var obj=new GameObject("Floating Damage",typeof(Canvas),typeof(PrototypePresentation));
                obj.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
                number=obj.GetComponent<PrototypePresentation>();
                var text=new GameObject("Amount",typeof(RectTransform),typeof(Text));text.transform.SetParent(obj.transform,false);
                text.GetComponent<RectTransform>().sizeDelta=new Vector2(100,50);
                number.label=text.GetComponent<Text>();number.label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                number.label.fontSize=28;number.label.fontStyle=FontStyle.Bold;number.label.alignment=TextAnchor.MiddleCenter;number.label.raycastTarget=false;
                var rim=text.AddComponent<Outline>();rim.effectColor=new Color(.08f,.13f,.16f,.9f);rim.effectDistance=new Vector2(1,-1);
                obj.transform.localScale=Vector3.one*.016f;
            }
            number.origin=position;number.transform.position=position;number.label.text="-"+amount;
            number.started=Time.time;number.counted=true;active++;number.gameObject.SetActive(true);
        }
        private void Update()
        {
            if(label==null)return;
            float age=(Time.time-started)/.65f;
            if(age>=1){gameObject.SetActive(false);numbers.Enqueue(this);return;}
            if(Camera.main!=null)transform.rotation=Camera.main.transform.rotation;
            transform.position=origin+Vector3.up*age*.75f;
            label.color=new Color(1,.96f,.76f,1-age*age);
        }
        private void OnDisable(){if(counted){active=Mathf.Max(0,active-1);counted=false;}}
    }
}
