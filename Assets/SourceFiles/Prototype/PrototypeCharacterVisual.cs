using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace DrForms.Prototype
{
    // Shared matte palette and collision-free parts; the existing controller stays authoritative.
    public sealed class PrototypeCharacterVisual : MonoBehaviour
    {
        private static Mesh bevelMesh;
        private static readonly Dictionary<int, Material> palette = new Dictionary<int, Material>();
        public static Material Paint(int key)
        {
            if (palette.TryGetValue(key, out var material) && material != null) return material;
            Color[] colors = {new Color(.12f,.54f,.49f),new Color(1,.73f,.39f),new Color(.22f,.25f,.29f),
                new Color(.61f,.3f,.23f),new Color(.96f,.38f,.25f),new Color(.56f,.7f,.32f),
                new Color(.38f,.23f,.62f),new Color(.34f,.88f,.93f),new Color(.94f,.76f,.28f),
                new Color(.49f,.65f,.38f),new Color(.72f,.69f,.52f),new Color(.96f,.91f,.73f)};
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", colors[key]); material.SetFloat("_Smoothness", .08f);
            material.enableInstancing = true; palette[key] = material; return material;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPalette() { foreach(var mat in palette.Values) if(mat != null) Destroy(mat); palette.Clear(); if(bevelMesh!=null)Destroy(bevelMesh);bevelMesh=null; }
        internal static Transform Part(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, int color)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            var collision = part.GetComponent<Collider>(); collision.enabled = false; Destroy(collision);
            if(shape==PrimitiveType.Cube)part.GetComponent<MeshFilter>().sharedMesh=BeveledBox();
            var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = Paint(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            return part.transform;
        }
        private static Mesh BeveledBox()
        {
            if(bevelMesh!=null)return bevelMesh;
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector2[] ring={new Vector2(-.36f,-.5f),new Vector2(.36f,-.5f),new Vector2(.5f,-.36f),new Vector2(.5f,.36f),new Vector2(.36f,.5f),new Vector2(-.36f,.5f),new Vector2(-.5f,.36f),new Vector2(-.5f,-.36f)};
            for(int level=0;level<4;level++)for(int i=0;i<8;i++) {
                float scale=level==0||level==3?.8f:1;
                vertices.Add(new Vector3(ring[i].x*scale,level==0?-.5f:level==1?-.36f:level==2?.36f:.5f,ring[i].y*scale));
            }
            for(int level=0;level<3;level++)for(int i=0;i<8;i++){
                int a=level*8+i,b=level*8+(i+1)%8;
                triangles.AddRange(new[]{a,a+8,b,b,a+8,b+8});
            }
            for(int i=1;i<7;i++){triangles.AddRange(new[]{0,i,i+1,24,24+i+1,24+i});}
            bevelMesh=new Mesh{name="Original Beveled Form"};bevelMesh.SetVertices(vertices);bevelMesh.SetTriangles(triangles,0);bevelMesh.RecalculateNormals();return bevelMesh;
        }
        private void Start()
        {
            var body = GetComponent<Renderer>(); if (body != null) body.enabled = false;
            gameObject.AddComponent<CourtyardActor>();
        }
    }
    public sealed class PrototypeGarden : MonoBehaviour
    {
        private GameObject decorations;
        private Mesh groundMesh;
        private Material groundMaterial;
        private void Start()
        {
            decorations = new GameObject("Continuous Garden Scenery");
            var ground = GameObject.Find("Arena Ground");
            if (ground != null) ground.GetComponent<Renderer>().enabled = false;
            // A single vertex-coloured mesh extends well beyond unchanged floor/wall collision.
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var triangles = new List<int>();
            for (int z = -60; z < 60; z++) for (int x = -60; x < 60; x++) {
                if(Mathf.Abs(x*2+1)<10 && (Mathf.Abs(z*2+1)<10 || Mathf.Abs(z*2+1-24)<10 || Mathf.Abs(z*2+1-48)<10))continue;
                int first = vertices.Count;
                float shade = ((x * 17 + z * 31) & 3) * .009f;
                bool path = Mathf.Abs(x * 2 + 1) < 2 && z * 2 + 1 > -8 && z * 2 + 1 < 63;
                Color color = path ? new Color(.38f + shade, .48f + shade, .48f + shade) : new Color(.13f + shade, .22f + shade, .24f + shade);
                vertices.Add(new Vector3(x*2,.006f,z*2)); vertices.Add(new Vector3(x*2,.006f,z*2+2));
                vertices.Add(new Vector3(x*2+2,.006f,z*2+2)); vertices.Add(new Vector3(x*2+2,.006f,z*2));
                for (int i=0;i<4;i++) colors.Add(color);
                triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            groundMesh = new Mesh { name = "Laboratory approach tiles" }; groundMesh.SetVertices(vertices); groundMesh.SetColors(colors); groundMesh.SetTriangles(triangles,0); groundMesh.RecalculateNormals();
            var carpet = new GameObject("Full Viewport Grass",typeof(MeshFilter),typeof(MeshRenderer)); carpet.transform.SetParent(decorations.transform);
            carpet.GetComponent<MeshFilter>().sharedMesh = groundMesh;
            groundMaterial = new Material(Shader.Find("DrForms/Garden Ground")); carpet.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            foreach (var name in new[]{"North Wall","South Wall","East Wall","West Wall"}) {
                var wall=GameObject.Find(name); if(wall!=null) wall.GetComponent<Renderer>().enabled=false;
            }
            RenderSettings.ambientLight = new Color(.42f,.50f,.44f);
            var light = GameObject.Find("Arena Light").GetComponent<Light>(); light.intensity=1.5f; light.transform.rotation=Quaternion.Euler(55,-32,0);
            light.shadows=LightShadows.Soft; // Soft mesh contacts avoid shadow-map bandwidth and jagged wall shadows.

        }
        private void OnDestroy(){if(decorations!=null)Destroy(decorations);if(groundMesh!=null)Destroy(groundMesh);if(groundMaterial!=null)Destroy(groundMaterial);}
    }
}
