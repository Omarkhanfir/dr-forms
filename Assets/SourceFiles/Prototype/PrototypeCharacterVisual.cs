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
        private Transform rig, leftLeg, rightLeg, leftArm, rightArm, weapon, gem, shield;
        private int lastHits;
        private float hitUntil, nextDust;
        private PrototypeCombat combat;
        private PrototypePlayerHealth player;
        private PrototypeTarget target;
        private Renderer gemRenderer;
        private Vector3 previous;
        private float phase, diedAt = -1;
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
            combat = GetComponent<PrototypeCombat>(); player = GetComponent<PrototypePlayerHealth>(); target = GetComponent<PrototypeTarget>();
            var original = GetComponent<Renderer>(); if(original != null) original.enabled = false;
            rig = new GameObject("Character Silhouette").transform; rig.SetParent(transform,false); rig.localPosition = Vector3.down;
            if (player != null) Adventurer();
            else if (GetComponent<PrototypeEnemy>() != null && GetComponent<PrototypeEnemy>().IsRanged) FlowerMage();
            else if (GetComponent<PrototypeEnemy>() != null) Mushroom();
            else TrainingTotem();
            if(gem!=null) gemRenderer=gem.GetComponent<Renderer>();
            PrototypePresentation.Contact(transform,new Vector3(0,-.98f,0),player!=null?1.25f:1.45f);
            previous = transform.position;
        }
        private void Adventurer()
        {
            Part(rig,"Tunic",PrimitiveType.Capsule,new Vector3(0,1,0),new Vector3(.66f,.48f,.5f),0);
            Part(rig,"Belt",PrimitiveType.Cube,new Vector3(0,.81f,0),new Vector3(.68f,.12f,.52f),3);
            Part(rig,"Head",PrimitiveType.Sphere,new Vector3(0,1.65f,0),Vector3.one*.65f,1);
            Part(rig,"Hair",PrimitiveType.Sphere,new Vector3(0,1.79f,-.04f),new Vector3(.57f,.35f,.56f),3);
            Part(rig,"Hat Brim",PrimitiveType.Cylinder,new Vector3(0,1.94f,0),new Vector3(.82f,.04f,.82f),4);
            Part(rig,"Hat Crown",PrimitiveType.Sphere,new Vector3(0,2.04f,0),new Vector3(.51f,.36f,.51f),4);
            Part(rig,"Satchel",PrimitiveType.Sphere,new Vector3(-.31f,.83f,-.22f),new Vector3(.3f,.37f,.22f),3);
            Part(rig,"Face L",PrimitiveType.Sphere,new Vector3(-.11f,1.66f,.31f),Vector3.one*.065f,2);
            Part(rig,"Face R",PrimitiveType.Sphere,new Vector3(.11f,1.66f,.31f),Vector3.one*.065f,2);
            Part(rig,"Scarf",PrimitiveType.Cube,new Vector3(0,1.39f,.2f),new Vector3(.65f,.15f,.16f),8);
            leftArm=Part(rig,"Left Arm",PrimitiveType.Capsule,new Vector3(-.4f,1.05f,0),new Vector3(.2f,.27f,.2f),0);
            rightArm=Part(rig,"Right Arm",PrimitiveType.Capsule,new Vector3(.4f,1.05f,.1f),new Vector3(.2f,.27f,.2f),0);
            leftLeg=Part(rig,"Left Boot",PrimitiveType.Capsule,new Vector3(-.18f,.38f,0),new Vector3(.25f,.3f,.32f),2);
            rightLeg=Part(rig,"Right Boot",PrimitiveType.Capsule,new Vector3(.18f,.38f,0),new Vector3(.25f,.3f,.32f),2);
            weapon=Part(rig,"Sunwood Wand",PrimitiveType.Cylinder,new Vector3(.54f,1.1f,.34f),new Vector3(.1f,.6f,.1f),8);
            gem=Part(weapon,"Wand Crystal",PrimitiveType.Sphere,new Vector3(0,1.15f,0),new Vector3(3, .4f,3),7);
            shield=new GameObject("Grace Halo").transform;shield.SetParent(transform,false);shield.localPosition=new Vector3(0,-.97f,0);
            var halo=shield.gameObject.AddComponent<LineRenderer>();halo.useWorldSpace=false;halo.loop=true;halo.positionCount=40;halo.widthMultiplier=.045f;halo.sharedMaterial=Paint(7);
            halo.shadowCastingMode=ShadowCastingMode.Off;halo.receiveShadows=false;
            for(int i=0;i<40;i++)halo.SetPosition(i,new Vector3(Mathf.Cos(i*Mathf.PI/20)*.7f,0,Mathf.Sin(i*Mathf.PI/20)*.7f));
        }
        private void Mushroom()
        {
            Part(rig,"Moss Body",PrimitiveType.Capsule,new Vector3(0,.75f,0),new Vector3(.65f,.49f,.65f),5);
            Part(rig,"Coral Cap",PrimitiveType.Sphere,new Vector3(0,1.48f,0),new Vector3(1.35f,.65f,1.2f),4);
            for(int i=-1;i<=1;i++) Part(rig,"Cap Spot",PrimitiveType.Sphere,new Vector3(i*.35f,1.72f,.18f),Vector3.one*.22f,11);
            Part(rig,"Eye L",PrimitiveType.Sphere,new Vector3(-.15f,1.04f,.31f),Vector3.one*.09f,2);
            Part(rig,"Eye R",PrimitiveType.Sphere,new Vector3(.15f,1.04f,.31f),Vector3.one*.09f,2);
            leftLeg=Part(rig,"Foot L",PrimitiveType.Sphere,new Vector3(-.24f,.16f,.08f),new Vector3(.34f,.24f,.43f),3);
            rightLeg=Part(rig,"Foot R",PrimitiveType.Sphere,new Vector3(.24f,.16f,.08f),new Vector3(.34f,.24f,.43f),3);
        }
        private void FlowerMage()
        {
            Part(rig,"Violet Robe",PrimitiveType.Capsule,new Vector3(0,.84f,0),new Vector3(.8f,.65f,.8f),6);
            Part(rig,"Face",PrimitiveType.Sphere,new Vector3(0,1.53f,.15f),Vector3.one*.48f,11);
            for(int i=0;i<6;i++) {
                float angle=i*Mathf.PI/3;
                var petal=Part(rig,"Crown Petal",PrimitiveType.Sphere,new Vector3(Mathf.Cos(angle)*.45f,1.7f+Mathf.Sin(angle)*.45f,0),new Vector3(.45f,.58f,.22f),6);
                petal.localRotation=Quaternion.Euler(0,0,i*60);
            }
            Part(rig,"Eye L",PrimitiveType.Sphere,new Vector3(-.1f,1.57f,.39f),Vector3.one*.07f,2);
            Part(rig,"Eye R",PrimitiveType.Sphere,new Vector3(.1f,1.57f,.39f),Vector3.one*.07f,2);
            weapon=Part(rig,"Mage Staff",PrimitiveType.Cylinder,new Vector3(.65f,1,.2f),new Vector3(.08f,.65f,.08f),3);
            Part(rig,"Staff Orb",PrimitiveType.Sphere,new Vector3(.65f,1.75f,.2f),Vector3.one*.28f,8);
        }
        private void TrainingTotem()
        {
            Part(rig,"Practice Post",PrimitiveType.Cylinder,new Vector3(0,1,0),new Vector3(.65f,1,.65f),3);
            Part(rig,"Practice Crown",PrimitiveType.Sphere,new Vector3(0,1.5f,0),new Vector3(1, .65f,1),8);
            Part(rig,"Practice Rune",PrimitiveType.Cube,new Vector3(0,1.45f,-.49f),Vector3.one*.2f,7);
        }
        private void LateUpdate()
        {
            if(rig == null) return;
            bool dead = player != null ? player.IsDefeated : target != null && target.Health<=0;
            if(dead) {
                if(diedAt<0) diedAt=Time.unscaledTime;
                float t=Mathf.Clamp01((Time.unscaledTime-diedAt)/.45f);
                rig.localRotation=Quaternion.Euler(0,rig.localEulerAngles.y,65*t);
                rig.localScale=Vector3.one*(1-.35f*t);
                if(player==null && t>=1) Destroy(gameObject);
                if(shield!=null) shield.gameObject.SetActive(false);
                return;
            }
            var delta=transform.position-previous; delta.y=0; previous=transform.position;
            if(Time.timeScale<=0) return;
            bool moving=delta.sqrMagnitude>.00001f;
            if(combat!=null && combat.MuzzleActive) rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(combat.AimDirection),Time.deltaTime*20);
            else if(moving) rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(delta),Time.deltaTime*14);
            phase+=Time.deltaTime*(moving?12:2);
            int hits=player!=null?player.DamageApplications:target!=null?target.DamageApplications:0;
            if(hits!=lastHits){lastHits=hits;hitUntil=Time.time+.2f;}
            if(moving && Time.time>=nextDust){nextDust=Time.time+.18f;PrototypeCombatEffect.Spawn(transform.position+new Vector3(0,-.92f,-.12f),Paint(10),new Color(.72f,.72f,.5f),.16f,.3f,"Footstep Dust");}
            if(leftArm!=null){leftArm.localRotation=Quaternion.Euler(moving?-Mathf.Sin(phase)*28:0,0,12);rightArm.localRotation=Quaternion.Euler(combat.MuzzleActive?-55:moving?Mathf.Sin(phase)*28:0,0,-12);}
            float recoil=combat!=null && combat.MuzzleActive ? .16f : Time.time<hitUntil ? -.12f : 0;
            rig.localPosition=Vector3.down+Vector3.up*(Mathf.Sin(phase)*(moving?.055f:.015f)+recoil);
            rig.localScale=new Vector3(1, recoil<0?.86f:1,1);
            if(leftLeg!=null) {leftLeg.localRotation=Quaternion.Euler(moving?Mathf.Sin(phase)*24:0,0,0);rightLeg.localRotation=Quaternion.Euler(moving?-Mathf.Sin(phase)*24:0,0,0);}
            if(weapon!=null) weapon.localRotation=Quaternion.Euler(recoil>0?-22:0,0,-8);
            if(gem!=null) { int level=Mathf.Clamp((combat.Damage-25)/10,0,3); gemRenderer.sharedMaterial=Paint(level>=2?8:7);gem.localScale=new Vector3(3+level*.65f,.4f+level*.12f,3+level*.65f); }
            if(shield!=null) shield.gameObject.SetActive(player.GraceActive);
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
            for (int z = -40; z < 40; z++) for (int x = -40; x < 40; x++) {
                int first = vertices.Count;
                float shade = ((x * 17 + z * 31) & 3) * .009f;
                bool path = Mathf.Abs(x * 2 + 1) < 2 && z * 2 + 1 > -8 && z * 2 + 1 < 63;
                Color color = path ? new Color(.48f + shade, .46f + shade, .28f + shade) : new Color(.28f + shade, .48f + shade, .12f + shade);
                vertices.Add(new Vector3(x*2,.006f,z*2)); vertices.Add(new Vector3(x*2,.006f,z*2+2));
                vertices.Add(new Vector3(x*2+2,.006f,z*2+2)); vertices.Add(new Vector3(x*2+2,.006f,z*2));
                for (int i=0;i<4;i++) colors.Add(color);
                triangles.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            groundMesh = new Mesh { name = "Quiet Garden Tiles" }; groundMesh.SetVertices(vertices); groundMesh.SetColors(colors); groundMesh.SetTriangles(triangles,0); groundMesh.RecalculateNormals();
            var carpet = new GameObject("Full Viewport Grass",typeof(MeshFilter),typeof(MeshRenderer)); carpet.transform.SetParent(decorations.transform);
            carpet.GetComponent<MeshFilter>().sharedMesh = groundMesh;
            groundMaterial = new Material(Shader.Find("DrForms/Garden Ground")); carpet.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            foreach (var name in new[]{"North Wall","South Wall","East Wall","West Wall"}) {
                var wall=GameObject.Find(name); if(wall!=null) wall.GetComponent<Renderer>().enabled=false;
            }
            RenderSettings.ambientLight = new Color(.64f,.69f,.60f);
            var light = GameObject.Find("Arena Light").GetComponent<Light>(); light.intensity=1.05f; light.transform.rotation=Quaternion.Euler(65,-35,0);
            light.shadows=LightShadows.None; // Soft mesh contacts avoid shadow-map bandwidth and jagged wall shadows.

        }
        private void OnDestroy(){if(decorations!=null)Destroy(decorations);if(groundMesh!=null)Destroy(groundMesh);if(groundMaterial!=null)Destroy(groundMaterial);}
    }
}
