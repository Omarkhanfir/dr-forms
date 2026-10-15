using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DrForms.Prototype
{
    // Presentation owns one clock and samples the complete skeletal pose once,
    // after controller movement and before combat reads the releasing hand.
    [DefaultExecutionOrder(-10)]
    public sealed class CourtyardActor : MonoBehaviour
    {
        private Transform rig, model, weapon, shield, hand, torso;
        private Animation motion;
        private readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
        private readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        private readonly MaterialPropertyBlock flash=new MaterialPropertyBlock();
        private Renderer[] renderers;
        private Color[][] baseColors;
        private PrototypePlayerHealth player;
        private PrototypeTarget target;
        private PrototypeEnemy enemy;
        private PrototypeCombat combat;
        private Vector3 previous;
        private string playing, fading;
        private float fadeUntil, castUntil, castAt, hitAt=-10, deathAt=-1;
        private float previousSpeed, stoppingUntil, turn, turnWeight, aim, meleeWeight, unitScale=1;
        private int hits,contacts,rangedShots;
        private bool wasFlashing;

        public Vector3 ReleasePosition => hand!=null?hand.position:transform.position;
        public string CurrentAnimation => player!=null&&Time.time<castUntil?"Spellcast_Shoot":playing;
        public string LocomotionAnimation => playing;
        public float LocomotionTime => StateTime(playing);
        public float ClipTime => StateTime(CurrentAnimation);
        public int SkinnedParts => renderers!=null?System.Array.FindAll(renderers,r=>r is SkinnedMeshRenderer).Length:0;
        private float StateTime(string name)=>motion!=null&&name!=null&&motion[name]!=null?motion[name].time:0;
        [System.Serializable] public struct JointFrame
        {
            public Vector3 leftHip,rightHip,leftKnee,rightKnee,leftFoot,rightFoot,body,leftArm,rightArm,releaseHand;
            public float authoredUnitScale;
        }
        private Vector3 Angles(string name)=>bones.TryGetValue(name,out var bone)?bone.localEulerAngles:Vector3.zero;
        public JointFrame Joints => new JointFrame {
            leftHip=Angles("Leg.L"),rightHip=Angles("Leg.R"),leftKnee=Angles("Shin.L"),rightKnee=Angles("Shin.R"),
            leftFoot=Angles("Foot.L"),rightFoot=Angles("Foot.R"),body=Angles("Body"),leftArm=Angles("Arm.L"),rightArm=Angles("Arm.R"),
            releaseHand=ReleasePosition,authoredUnitScale=unitScale
        };

        public void BeginThrow()
        {
            castAt=Time.time;castUntil=castAt+.25f;
        }
        public void CancelThrow(){castUntil=0;ReleaseFlask();}
        public void ReleaseFlask(){if(weapon!=null)weapon.gameObject.SetActive(false);}

        private void Start()
        {
            player=GetComponent<PrototypePlayerHealth>();target=GetComponent<PrototypeTarget>();
            enemy=GetComponent<PrototypeEnemy>();combat=GetComponent<PrototypeCombat>();
            rig=new GameObject("Authored character rig").transform;rig.SetParent(transform,false);rig.localPosition=Vector3.down;
            string asset=player!=null?"Hero":enemy!=null&&enemy.IsRanged?"Acolyte":"Guardian";
            var source=Resources.Load<GameObject>("Courtyard/"+asset);
            if(source==null){Debug.LogError("Missing character asset: "+asset);enabled=false;return;}
            model=Instantiate(source,rig,false).transform;
            model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;
            // Disable automatic playback, including any imported child component.
            // Animation.Sample below is the only writer of authored bone transforms.
            foreach(var animation in model.GetComponentsInChildren<Animation>()){
                animation.playAutomatically=false;animation.Stop();animation.enabled=false;
            }
            motion=model.GetComponent<Animation>();if(motion==null)motion=model.gameObject.AddComponent<Animation>();
            motion.enabled=false;motion.cullingType=AnimationCullingType.AlwaysAnimate;
            foreach(var clip in Resources.LoadAll<AnimationClip>("Courtyard/"+asset)){
                if(clip.name.StartsWith("__preview__"))continue;
                foreach(var key in new[]{"Idle","Running_A","Walking_A","Stopping","Turning","Spellcast_Shoot","Spellcasting","Hit_A","Death_A","1H_Melee_Attack_Slice_Horizontal"})
                    if(clip.name.EndsWith(key)){clips[key]=clip;motion.AddClip(clip,key);motion[key].enabled=false;motion[key].weight=0;}
            }
            if(clips.TryGetValue("Idle",out var idle))idle.SampleAnimation(model.gameObject,0);
            var bodies=model.GetComponentsInChildren<Renderer>();var bounds=Measure(bodies);
            model.localScale*=(player!=null?2.65f:2.35f)/Mathf.Max(.1f,bounds.size.y);
            bounds=Measure(bodies);model.position+=Vector3.up*(rig.position.y-bounds.min.y);
            foreach(var bone in model.GetComponentsInChildren<Transform>())bones[bone.name]=bone;
            bones.TryGetValue("Body",out torso);bones.TryGetValue("handslot.r",out hand);
            if(bones.TryGetValue("Leg.L",out var hip)&&bones.TryGetValue("Shin.L",out var knee)&&bones.TryGetValue("Foot.L",out var ankle))
                unitScale=(Vector3.Distance(hip.position,knee.position)+Vector3.Distance(knee.position,ankle.position))/(enemy!=null&&enemy.IsRanged?.68f:.88f);
            foreach(var body in bodies){
                var slots=body.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=CourtyardArt.EnvironmentMaterial(slots[i].name);
                body.sharedMaterials=slots;body.shadowCastingMode=ShadowCastingMode.On;body.receiveShadows=true;
            }
            if(player!=null&&hand!=null){
                weapon=PrototypeCharacterVisual.Part(hand,"Held chemical flask",PrimitiveType.Sphere,Vector3.zero,new Vector3(.22f/hand.lossyScale.x,.22f/hand.lossyScale.y,.22f/hand.lossyScale.z),5);
                PrototypeCharacterVisual.Part(weapon,"Flask neck",PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.28f,.45f,.28f),7);
            }
            renderers=model.GetComponentsInChildren<Renderer>();baseColors=new Color[renderers.Length][];
            for(int i=0;i<renderers.Length;i++){
                var materials=renderers[i].sharedMaterials;baseColors[i]=new Color[materials.Length];
                for(int m=0;m<materials.Length;m++)baseColors[i][m]=materials[m].GetColor("_BaseColor");
            }
            PrototypePresentation.Contact(transform,new Vector3(0,-.98f,0),player!=null?1.1f:1.2f);
            if(player!=null){
                shield=new GameObject("Recovery ward").transform;shield.SetParent(transform,false);shield.localPosition=new Vector3(0,-.96f,0);
                var line=shield.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.widthMultiplier=.028f;line.sharedMaterial=CourtyardArt.GlowMaterial;
                for(int i=0;i<48;i++)line.SetPosition(i,new Vector3(Mathf.Cos(i*Mathf.PI/24)*.65f,0,Mathf.Sin(i*Mathf.PI/24)*.65f));
                Configure("Spellcast_Shoot",1,false,new[]{"Arm.R","Coat.R"},true);
            }
            Configure("Turning",3,true,new[]{"Body","Head","Satchel"},false);
            Configure("Hit_A",2,true,new[]{"Body","Head"},false);
            if(enemy!=null&&!enemy.IsRanged)Configure("1H_Melee_Attack_Slice_Horizontal",1,false,new[]{"Body"},true);
            previous=transform.position;Play("Idle",true);
        }
        private void Configure(string name,int layer,bool additive,string[] names,bool recursive)
        {
            if(motion[name]==null)return;var state=motion[name];state.layer=layer;
            state.blendMode=additive?AnimationBlendMode.Additive:AnimationBlendMode.Blend;
            foreach(var nameOfBone in names)if(bones.TryGetValue(nameOfBone,out var bone))state.AddMixingTransform(bone,recursive);
        }
        private static Bounds Measure(Renderer[] bodies)
        {
            var result=new Bounds();bool first=true;
            foreach(var body in bodies){
                if(body is SkinnedMeshRenderer skin){
                    var baked=new Mesh();skin.BakeMesh(baked);var local=baked.bounds;
                    local.Expand(1.2f/Mathf.Max(.001f,skin.transform.lossyScale.x));skin.localBounds=local;
                    foreach(var vertex in baked.vertices){var p=skin.transform.TransformPoint(vertex);if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}
                    Destroy(baked);
                }else{if(first){result=body.bounds;first=false;}else result.Encapsulate(body.bounds);}
            }
            return result;
        }
        private void Play(string name,bool loop)
        {
            if(playing==name||!clips.ContainsKey(name))return;
            if(fading!=null&&motion[fading]!=null)motion[fading].enabled=false;
            float phase=playing!=null&&IsGait(playing)?Mathf.Repeat(motion[playing].normalizedTime,1):0;
            fading=playing;fadeUntil=Time.time+.12f;playing=name;
            motion[name].wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;
            motion[name].time=IsGait(name)?phase*clips[name].length:0;
        }
        private static bool IsGait(string name)=>name=="Walking_A"||name=="Running_A";
        private void SampleState(string name,float weight,float time)
        {
            if(name==null||motion[name]==null)return;
            var state=motion[name];state.enabled=weight>.0001f;state.weight=weight;state.time=time;
        }
        private void AdvanceBase(string name,float weight,float distance)
        {
            if(name==null||motion[name]==null)return;
            float advance=Time.deltaTime;
            if(IsGait(name)){
                float stride=name=="Running_A"?.48f:.32f;
                if(enemy!=null&&enemy.IsRanged)stride=(name=="Running_A"?.34f:.23f)*.7f;
                advance=distance/Mathf.Max(.01f,4*stride*unitScale)*clips[name].length;
            }
            SampleState(name,weight,motion[name].time+advance);
        }
        private void LateUpdate()
        {
            if(motion==null)return;
            bool dead=player!=null?player.IsDefeated:target!=null&&target.Health<=0;
            if(dead){
                if(deathAt<0){deathAt=Time.unscaledTime;motion.Stop();playing="Death_A";fading=null;castUntil=0;ReleaseFlask();}
                if(clips.ContainsKey("Death_A")){SampleState("Death_A",1,Mathf.Clamp01((Time.unscaledTime-deathAt)/.45f)*clips["Death_A"].length);motion.Sample();}
                if(shield!=null)shield.gameObject.SetActive(false);
                if(player==null&&Time.unscaledTime-deathAt>=.45f)Destroy(gameObject);return;
            }
            var delta=transform.position-previous;delta.y=0;previous=transform.position;
            if(Time.timeScale<=0)return;
            float distance=delta.magnitude,speed=distance/Mathf.Max(.001f,Time.deltaTime);bool moving=speed>.05f;
            Vector3 facing=moving?delta:player!=null&&Time.time<castUntil?combat.AimDirection:enemy!=null&&enemy.Telegraphing?enemy.LockedAim-transform.position:rig.forward;
            facing.y=0;
            float angle=facing.sqrMagnitude>.0001f?Vector3.SignedAngle(rig.forward,facing,Vector3.up):0;
            turn=Mathf.Lerp(turn,Mathf.Clamp(angle/90,-1,1),1-Mathf.Exp(-Time.deltaTime*12));
            if(facing.sqrMagnitude>.0001f)rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(facing),1-Mathf.Exp(-Time.deltaTime*18));
            int damage=player!=null?player.DamageApplications:target!=null?target.DamageApplications:0;
            if(damage!=hits){hits=damage;if(Time.time-hitAt>=.18f)hitAt=Time.time;}
            if(enemy!=null&&contacts!=enemy.ContactHits){contacts=enemy.ContactHits;if(!enemy.IsRanged){castAt=Time.time;castUntil=Time.time+.18f;}}
            if(enemy!=null&&rangedShots!=enemy.RangedShots){rangedShots=enemy.RangedShots;castAt=Time.time;castUntil=Time.time+.4f;}
            bool rangedCast=enemy!=null&&enemy.IsRanged&&(enemy.Telegraphing||Time.time<castUntil);
            if(rangedCast)Play("Spellcasting",false);
            else if(moving){stoppingUntil=0;Play(speed<(playing=="Running_A"?2.15f:2.65f)?"Walking_A":"Running_A",true);}
            else if(previousSpeed>.4f){Play("Stopping",false);stoppingUntil=Time.time+.3f;}
            else if(Time.time>=stoppingUntil)Play("Idle",true);
            previousSpeed=speed;
            float from=Mathf.Clamp01((fadeUntil-Time.time)/.12f);
            AdvanceBase(fading,from,distance);AdvanceBase(playing,1-from,distance);
            if(from<=0&&fading!=null){motion[fading].enabled=false;fading=null;}
            if(rangedCast){
                float phase=enemy.Telegraphing?.68f*enemy.CastPreparation:Mathf.Lerp(.68f,1,Mathf.Clamp01((Time.time-castAt)/.4f));
                SampleState("Spellcasting",1,phase*clips["Spellcasting"].length);
            }
            if(player!=null&&clips.ContainsKey("Spellcast_Shoot")){
                float elapsed=Time.time-castAt;
                float weight=Time.time<castUntil?Mathf.Min(1,elapsed/.025f,(castUntil-Time.time)/.04f):0;
                SampleState("Spellcast_Shoot",Mathf.Max(0,weight),Mathf.Clamp(elapsed,0,.25f));
            }
            if(enemy!=null&&!enemy.IsRanged&&clips.ContainsKey("1H_Melee_Attack_Slice_Horizontal")){
                bool active=Time.time<castUntil||enemy.AttackAnticipating;
                meleeWeight=Mathf.MoveTowards(meleeWeight,active?1:0,Time.deltaTime/.05f);
                float phase=Time.time<castUntil?Mathf.Lerp(.55f,1,Mathf.Clamp01((Time.time-castAt)/.18f)):.5f*enemy.ContactPreparation;
                SampleState("1H_Melee_Attack_Slice_Horizontal",meleeWeight,phase*clips["1H_Melee_Attack_Slice_Horizontal"].length);
            }
            float hitAge=Time.time-hitAt;
            if(clips.ContainsKey("Hit_A"))SampleState("Hit_A",hitAge<.18f?.8f:0,Mathf.Clamp01(hitAge/.18f)*clips["Hit_A"].length);
            turnWeight=Mathf.MoveTowards(turnWeight,moving?Mathf.Abs(turn)*.5f:0,Time.deltaTime/.08f);
            if(clips.ContainsKey("Turning"))SampleState("Turning",turnWeight,clips["Turning"].length*(turn>=0?.25f:.75f));
            // One authored evaluation, then the independent upper-body aim. No
            // repeated CrossFade/Blend calls or whole-model wobble/position offsets.
            motion.Sample();
            float desiredAim=player!=null&&Time.time<castUntil?Vector3.SignedAngle(rig.forward,combat.AimDirection,Vector3.up):0;
            aim=Mathf.LerpAngle(aim,desiredAim,1-Mathf.Exp(-Time.deltaTime*32));
            if(torso!=null)torso.rotation=Quaternion.AngleAxis(aim,Vector3.up)*torso.rotation;
            if(weapon!=null)weapon.gameObject.SetActive(combat!=null&&combat.ThrowPending);
            bool flashing=hitAge<.12f;
            if(flashing||wasFlashing)for(int i=0;i<renderers.Length;i++)for(int m=0;m<baseColors[i].Length;m++){
                flash.SetColor("_BaseColor",Color.Lerp(baseColors[i][m],new Color(1,.96f,.82f),.5f));
                renderers[i].SetPropertyBlock(flashing?flash:null,m);
            }
            wasFlashing=flashing;
            if(shield!=null)shield.gameObject.SetActive(player.GraceActive);
        }
    }
}
