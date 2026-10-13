using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace DrForms.Prototype
{
    // Shared matte palette and collision-free parts; the existing controller stays authoritative.
    public sealed class PrototypeCharacterVisual : MonoBehaviour
    {
        private static readonly Dictionary<int, Material> palette = new Dictionary<int, Material>();
        private Transform rig, leftLeg, rightLeg, weapon, gem, shield;
        private PrototypeCombat combat;
        private PrototypePlayerHealth player;
        private PrototypeTarget target;
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
        private static void ResetPalette() { foreach(var mat in palette.Values) if(mat != null) Destroy(mat); palette.Clear(); }
        internal static Transform Part(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, int color)
        {
            var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            var collision = part.GetComponent<Collider>(); collision.enabled = false; Destroy(collision);
            var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = Paint(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
            return part.transform;
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
            previous = transform.position;
        }
        private void Adventurer()
        {
            Part(rig,"Tunic",PrimitiveType.Capsule,new Vector3(0,1,0),new Vector3(.66f,.48f,.5f),0);
            Part(rig,"Belt",PrimitiveType.Cube,new Vector3(0,.81f,0),new Vector3(.68f,.12f,.52f),3);
            Part(rig,"Head",PrimitiveType.Sphere,new Vector3(0,1.65f,0),Vector3.one*.55f,1);
            Part(rig,"Hair",PrimitiveType.Sphere,new Vector3(0,1.79f,-.04f),new Vector3(.57f,.35f,.56f),3);
            Part(rig,"Hat Brim",PrimitiveType.Cylinder,new Vector3(0,1.94f,0),new Vector3(.82f,.04f,.82f),4);
            Part(rig,"Hat Crown",PrimitiveType.Sphere,new Vector3(0,2.04f,0),new Vector3(.51f,.36f,.51f),4);
            Part(rig,"Scarf",PrimitiveType.Cube,new Vector3(0,1.39f,.2f),new Vector3(.65f,.15f,.16f),8);
            Part(rig,"Left Arm",PrimitiveType.Capsule,new Vector3(-.4f,1.05f,0),new Vector3(.2f,.27f,.2f),0);
            Part(rig,"Right Arm",PrimitiveType.Capsule,new Vector3(.4f,1.05f,.1f),new Vector3(.2f,.27f,.2f),0);
            leftLeg=Part(rig,"Left Boot",PrimitiveType.Capsule,new Vector3(-.18f,.38f,0),new Vector3(.25f,.3f,.32f),2);
            rightLeg=Part(rig,"Right Boot",PrimitiveType.Capsule,new Vector3(.18f,.38f,0),new Vector3(.25f,.3f,.32f),2);
            weapon=Part(rig,"Sunwood Wand",PrimitiveType.Cylinder,new Vector3(.54f,1.1f,.34f),new Vector3(.1f,.6f,.1f),8);
            gem=Part(weapon,"Wand Crystal",PrimitiveType.Sphere,new Vector3(0,1.15f,0),new Vector3(3, .4f,3),7);
            shield=Part(transform,"Grace Halo",PrimitiveType.Cylinder,new Vector3(0,-.94f,0),new Vector3(1.65f,.018f,1.65f),7);
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
            if(moving) rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(delta),Time.deltaTime*14);
            phase+=Time.deltaTime*(moving?12:2);
            float recoil=combat!=null && combat.MuzzleActive ? .16f : target!=null && target.HitFlashActive ? -.12f : 0;
            rig.localPosition=Vector3.down+Vector3.up*(Mathf.Sin(phase)*(moving?.055f:.015f)+recoil);
            rig.localScale=new Vector3(1, recoil<0?.86f:1,1);
            if(leftLeg!=null) {leftLeg.localRotation=Quaternion.Euler(moving?Mathf.Sin(phase)*24:0,0,0);rightLeg.localRotation=Quaternion.Euler(moving?-Mathf.Sin(phase)*24:0,0,0);}
            if(weapon!=null) weapon.localRotation=Quaternion.Euler(recoil>0?-22:0,0,-8);
            if(gem!=null) { int level=Mathf.Clamp((combat.Damage-25)/10,0,3); gem.GetComponent<Renderer>().sharedMaterial=Paint(level>=2?8:7);gem.localScale=new Vector3(3+level*.65f,.4f+level*.12f,3+level*.65f); }
            if(shield!=null) shield.gameObject.SetActive(player.GraceActive);
        }
    }
    public sealed class PrototypeGarden : MonoBehaviour
    {
        private GameObject decorations;
        private void Start()
        {
            decorations=new GameObject("Garden Border Decorations");
            var ground=GameObject.Find("Arena Ground");if(ground!=null)ground.GetComponent<Renderer>().sharedMaterial=PrototypeCharacterVisual.Paint(9);
            foreach(var name in new[]{"North Wall","South Wall","East Wall","West Wall"}) {
                var wall=GameObject.Find(name);if(wall!=null)wall.GetComponent<Renderer>().sharedMaterial=PrototypeCharacterVisual.Paint(10);
            }
            RenderSettings.ambientLight=new Color(.65f,.72f,.64f);
            if(Camera.main!=null)Camera.main.backgroundColor=new Color(.64f,.82f,.79f);
            // All decoration lives beyond the existing walls (inside edge +/-10).
            for(int side=0;side<4;side++)for(int n=-2;n<=2;n++) {
                Vector3 pos=side<2?new Vector3(n*4,0,side==0?12:-12):new Vector3(side==2?12:-12,0,n*4);
                var tree=new GameObject("Border Topiary").transform;tree.SetParent(decorations.transform);tree.position=pos;
                PrototypeCharacterVisual.Part(tree,"Trunk",PrimitiveType.Cylinder,new Vector3(0,.9f,0),new Vector3(.32f,.9f,.32f),3);
                PrototypeCharacterVisual.Part(tree,"Crown",PrimitiveType.Sphere,new Vector3(0,2.3f,0),new Vector3(1.6f,2,1.6f),5);
                PrototypeCharacterVisual.Part(tree,"Fruit",PrimitiveType.Sphere,new Vector3(.48f,2.3f,-.55f),Vector3.one*.3f,8);
            }
            // A narrow garden rim sits on the walls and has no new collision.
            for(int side=0;side<4;side++) {
                Vector3 pos=side<2?new Vector3(0,2.04f,side==0?10.5f:-10.5f):new Vector3(side==2?10.5f:-10.5f,2.04f,0);
                PrototypeCharacterVisual.Part(decorations.transform,"Moss Wall Cap",PrimitiveType.Cube,pos,side<2?new Vector3(22,.12f,1):new Vector3(1,.12f,22),5);
            }
        }
        private void OnDestroy(){if(decorations!=null)Destroy(decorations);}
    }
}
