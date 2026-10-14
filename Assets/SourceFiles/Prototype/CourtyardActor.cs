using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace DrForms.Prototype {
 // Imported rig and real skeletal clips; never moves the gameplay controller.
 [DefaultExecutionOrder(-10)]
 public sealed class CourtyardActor : MonoBehaviour {
  private Transform rig,weapon,shield,hand,upperBody;
  private float upperAim,modelScale;
  public void ReleaseFlask(){if(weapon!=null)weapon.gameObject.SetActive(false);}
  public Vector3 ReleasePosition => hand!=null?hand.position:transform.position;
  public void BeginThrow(){
   castUntil=Time.time+.25f;
   if(motion==null||motion["Spellcast_Shoot"]==null)return;
   motion["Spellcast_Shoot"].time=0;motion["Spellcast_Shoot"].wrapMode=WrapMode.ClampForever;
   motion.CrossFade("Spellcast_Shoot",.025f,PlayMode.StopSameLayer);
  }
  public void CancelThrow(){castUntil=0;if(motion!=null&&motion["Spellcast_Shoot"]!=null)motion.Blend("Spellcast_Shoot",0,.06f);}
  private Animation motion;
  private readonly Dictionary<string,AnimationClip> clips=new Dictionary<string,AnimationClip>();
  private readonly MaterialPropertyBlock tint=new MaterialPropertyBlock();
  private Renderer[] renderers;
  private PrototypePlayerHealth player;
  private PrototypeTarget target;
  private PrototypeEnemy enemy;
  private PrototypeCombat combat;
  private Vector3 previous;
  private string playing;
  private int hits,contacts,rangedShots;
  private float hitUntil,castUntil,deathAt=-1,nextDust,transitionUntil,previousSpeed,turnLean;
  private Vector3 recoil;
  public string CurrentAnimation => player!=null&&Time.time<castUntil?"Spellcast_Shoot":playing;
  public string LocomotionAnimation => playing;
  public float LocomotionTime => motion!=null&&playing!=null&&motion[playing]!=null?motion[playing].time:0;
  public float ClipTime => motion!=null&&CurrentAnimation!=null&&motion[CurrentAnimation]!=null?motion[CurrentAnimation].time:0;
  public int SkinnedParts => renderers!=null?System.Array.FindAll(renderers,r=>r is SkinnedMeshRenderer).Length:0;
  private void Start() {
   player=GetComponent<PrototypePlayerHealth>();target=GetComponent<PrototypeTarget>();enemy=GetComponent<PrototypeEnemy>();combat=GetComponent<PrototypeCombat>();
   rig=new GameObject("Authored character rig").transform;rig.SetParent(transform,false);rig.localPosition=Vector3.down;
   string name=player!=null?"Hero":enemy!=null&&enemy.IsRanged?"Acolyte":"Guardian";
   var source=Resources.Load<GameObject>("Courtyard/"+name);
   if(source==null){Debug.LogError("Missing character asset: "+name);return;}
   var model=Instantiate(source,rig,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
   motion=model.GetComponent<Animation>();if(motion==null)motion=model.AddComponent<Animation>();
   motion.playAutomatically=false;motion.Stop();motion.cullingType=AnimationCullingType.AlwaysAnimate;
   foreach(var clip in Resources.LoadAll<AnimationClip>("Courtyard/"+name)) {
    if(clip.name.StartsWith("__preview__"))continue;
    foreach(var key in new[]{"Idle","Running_A","Walking_A","Stopping","Turning","Spellcast_Shoot","Spellcasting","Hit_A","Death_A","1H_Melee_Attack_Slice_Horizontal"})
     if(clip.name.EndsWith(key)){clips[key]=clip;motion.AddClip(clip,key);}
   }
   if(clips.ContainsKey("Idle"))clips["Idle"].SampleAnimation(model,0);
   var bodies=model.GetComponentsInChildren<Renderer>();
   // Establish the animated bind transform before measuring deformed vertices.
   var bounds=Measure(bodies);
   model.transform.localScale*=(player!=null?2.65f:2.35f)/Mathf.Max(.1f,bounds.size.y);
   bounds=Measure(bodies);model.transform.position+=Vector3.up*(rig.position.y-bounds.min.y);
   modelScale=model.transform.lossyScale.x;
   upperBody=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="Body");

   foreach(var body in bodies){var slots=body.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=CourtyardArt.EnvironmentMaterial(slots[i].name);body.sharedMaterials=slots;body.shadowCastingMode=ShadowCastingMode.On;body.receiveShadows=true;}
   hand=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="handslot.r");
   if(player!=null&&hand!=null){weapon=PrototypeCharacterVisual.Part(hand,"Held chemical flask",PrimitiveType.Sphere,Vector3.zero,new Vector3(.22f/hand.lossyScale.x,.22f/hand.lossyScale.y,.22f/hand.lossyScale.z),5);PrototypeCharacterVisual.Part(weapon,"Flask neck",PrimitiveType.Cylinder,new Vector3(0,.65f,0),new Vector3(.28f,.45f,.28f),7);}
   renderers=model.GetComponentsInChildren<Renderer>();
   PrototypePresentation.Contact(transform,new Vector3(0,-.98f,0),player!=null?1.1f:1.2f);
   if(player!=null){
    shield=new GameObject("Recovery ward").transform;shield.SetParent(transform,false);shield.localPosition=new Vector3(0,-.96f,0);
    var line=shield.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.widthMultiplier=.028f;line.sharedMaterial=CourtyardArt.GlowMaterial;
    for(int i=0;i<48;i++)line.SetPosition(i,new Vector3(Mathf.Cos(i*Mathf.PI/24)*.65f,0,Mathf.Sin(i*Mathf.PI/24)*.65f));
   }
   if(player!=null&&motion["Spellcast_Shoot"]!=null){motion["Spellcast_Shoot"].layer=1;foreach(var bone in model.GetComponentsInChildren<Transform>())if(bone.name=="Arm.R"||bone.name=="Coat.R")motion["Spellcast_Shoot"].AddMixingTransform(bone,true);}
   if(motion["Turning"]!=null){var turning=motion["Turning"];turning.layer=3;turning.blendMode=AnimationBlendMode.Additive;
    foreach(var bone in model.GetComponentsInChildren<Transform>())if(bone.name=="Body"||bone.name=="Head"||bone.name=="Satchel")turning.AddMixingTransform(bone,false);
   }
   if(motion["Hit_A"]!=null){
    var reaction=motion["Hit_A"];reaction.layer=2;reaction.blendMode=AnimationBlendMode.Additive;
    foreach(var bone in model.GetComponentsInChildren<Transform>())if(bone.name=="Body"||bone.name=="Head")reaction.AddMixingTransform(bone,false);
   }
   if(enemy!=null&&!enemy.IsRanged&&motion["1H_Melee_Attack_Slice_Horizontal"]!=null){
    var attack=motion["1H_Melee_Attack_Slice_Horizontal"];attack.layer=1;
    var upper=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="Body");if(upper!=null)attack.AddMixingTransform(upper,true);
   }
   previous=transform.position;Play("Idle",true);
  }
  private static Bounds Measure(Renderer[] bodies){
   Bounds result=new Bounds();bool first=true;
   foreach(var body in bodies){
    if(body is SkinnedMeshRenderer skin){
     var baked=new Mesh();skin.BakeMesh(baked);var local=baked.bounds;local.Expand(1.2f/Mathf.Max(.001f,skin.transform.lossyScale.x));skin.localBounds=local;
     foreach(var v in baked.vertices){var point=skin.transform.TransformPoint(v);if(first){result=new Bounds(point,Vector3.zero);first=false;}else result.Encapsulate(point);}
     Destroy(baked);
    }else{if(first){result=body.bounds;first=false;}else result.Encapsulate(body.bounds);}
   }
   return result;
  }
  private void Play(string name,bool loop){
   if(motion==null||playing==name||!clips.ContainsKey(name))return;
   playing=name;motion[name].wrapMode=loop?WrapMode.Loop:WrapMode.ClampForever;
   motion[name].speed=1;
   motion[name].time=0;motion.CrossFade(name,.09f,PlayMode.StopSameLayer);
  }
  private void LateUpdate(){
   if(rig==null||motion==null)return;
   bool dead=player!=null?player.IsDefeated:target!=null&&target.Health<=0;
   if(dead){
    if(deathAt<0){deathAt=Time.unscaledTime;motion.Stop();playing=null;castUntil=hitUntil=0;rig.localPosition=Vector3.down;Play("Death_A",false);}
    float t=Mathf.Clamp01((Time.unscaledTime-deathAt)/.45f);
    if(clips.ContainsKey("Death_A")){motion["Death_A"].time=t*clips["Death_A"].length;motion.Sample();}
    if(shield!=null)shield.gameObject.SetActive(false);
    if(player==null&&t>=1)Destroy(gameObject);return;
   }
   var delta=transform.position-previous;delta.y=0;previous=transform.position;
   if(Time.timeScale<=0)return;
   bool moving=delta.sqrMagnitude>.00001f;
   float speed=delta.magnitude/Mathf.Max(.001f,Time.deltaTime);
   var facing=moving?delta:combat!=null&&Time.time<castUntil?combat.AimDirection:enemy!=null&&enemy.Telegraphing?enemy.LockedAim-transform.position:rig.forward;
   facing.y=0;
   float angle=facing.sqrMagnitude>.0001f?Vector3.SignedAngle(rig.forward,facing,Vector3.up):0;
   turnLean=Mathf.Lerp(turnLean,Mathf.Clamp(angle/90,-1,1),Time.deltaTime*12);
   if(facing.sqrMagnitude>.0001f)rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(facing),1-Mathf.Exp(-Time.deltaTime*18));
   int damage=player!=null?player.DamageApplications:target!=null?target.DamageApplications:0;
   if(damage!=hits){
    hits=damage;
    // Do not restart an active reaction on rapid hits or interrupt the authoritative attack.
    if(Time.time>=hitUntil){hitUntil=Time.time+.18f;recoil=target!=null?target.HitDirection:-rig.forward;
     if(motion["Hit_A"]!=null){motion["Hit_A"].time=0;motion["Hit_A"].wrapMode=WrapMode.Once;motion.Blend("Hit_A",.65f,.02f);}
    }
   }
   if(weapon!=null)weapon.gameObject.SetActive(combat!=null&&combat.ThrowPending);
   if(enemy!=null&&contacts!=enemy.ContactHits){contacts=enemy.ContactHits;if(!enemy.IsRanged)castUntil=Time.time+.18f;}
   if(enemy!=null&&rangedShots!=enemy.RangedShots){rangedShots=enemy.RangedShots;castUntil=Time.time+.4f;playing=null;Play("Spellcasting",false);}
   if(enemy!=null&&!enemy.IsRanged&&motion["1H_Melee_Attack_Slice_Horizontal"]!=null){
    var melee=motion["1H_Melee_Attack_Slice_Horizontal"];melee.speed=0;melee.wrapMode=WrapMode.ClampForever;
    if(Time.time<castUntil||enemy.AttackAnticipating){
     melee.time=melee.clip.length*(Time.time<castUntil?Mathf.Lerp(.55f,1,1-(castUntil-Time.time)/.18f):.5f*enemy.ContactPreparation);
     motion.Blend(melee.name,1,.025f);
    }else motion.Blend(melee.name,0,.06f);
   }
   if(enemy!=null&&enemy.IsRanged&&enemy.Telegraphing){
    Play("Spellcasting",false);motion[playing].speed=0;motion[playing].time=motion[playing].length*.68f*enemy.CastPreparation;
   }else if(enemy!=null&&enemy.IsRanged&&Time.time<castUntil){
    if(enemy.IsRanged){motion[playing].speed=0;motion[playing].time=motion[playing].length*Mathf.Lerp(.68f,1,1-(castUntil-Time.time)/.4f);}
   }else{
    if(moving){transitionUntil=0;Play(speed<(playing=="Running_A"?2.15f:2.65f)?"Walking_A":"Running_A",true);}
    else if(previousSpeed>.4f){Play("Stopping",false);transitionUntil=Time.time+.3f;}
    else if(Time.time>=transitionUntil)Play("Idle",true);
   }
   previousSpeed=speed;
   if(motion["Turning"]!=null){var turn=motion["Turning"];turn.wrapMode=WrapMode.ClampForever;turn.speed=0;
    turn.time=turn.clip.length*(turnLean>=0?.25f:.75f);motion.Blend("Turning",moving?Mathf.Abs(turnLean)*.6f:0,.08f);
   }
   if(player!=null&&Time.time>=castUntil&&motion["Spellcast_Shoot"]!=null)motion.Blend("Spellcast_Shoot",0,.07f);
   // Cycle covers twice each foot's stance excursion. Match actual controller
   // displacement, including collisions and analog input, rather than nominal speed.
   float strideScale=enemy!=null&&enemy.IsRanged?.7f:1;
   if(motion["Running_A"]!=null)motion["Running_A"].speed=Mathf.Clamp(speed*motion["Running_A"].length/(4*.34f*strideScale*modelScale),.05f,3f);
   if(motion["Walking_A"]!=null)motion["Walking_A"].speed=Mathf.Clamp(speed*motion["Walking_A"].length/(4*.23f*strideScale*modelScale),.05f,3f);
   float hit=Mathf.Clamp01((hitUntil-Time.time)/.18f);
   rig.localPosition=Vector3.down;
   // Aim only the articulated torso when throwing on the move. Foot orientation
   // stays with travel; no whole-model lean or displacement substitutes for poses.
   float aim=player!=null&&Time.time<castUntil?Vector3.SignedAngle(rig.forward,combat.AimDirection,Vector3.up):0;
   upperAim=Mathf.LerpAngle(upperAim,aim,1-Mathf.Exp(-Time.deltaTime*32));
   if(upperBody!=null)upperBody.rotation=Quaternion.AngleAxis(upperAim,Vector3.up)*upperBody.rotation;
   if(Time.time>=hitUntil&&motion["Hit_A"]!=null)motion.Blend("Hit_A",0,.04f);
   tint.SetColor("_BaseColor",Time.time<hitUntil?new Color(1.65f,1.4f,.95f):Color.white);
   foreach(var body in renderers)body.SetPropertyBlock(Time.time<hitUntil?tint:null);
   if(moving&&Time.time>=nextDust){nextDust=Time.time+.24f;PrototypeCombatEffect.Spawn(transform.position+new Vector3(0,-.92f,-.12f),PrototypeCharacterVisual.Paint(10),new Color(.65f,.66f,.49f),.12f,.3f,"Footstep Dust");}
   if(shield!=null)shield.gameObject.SetActive(player.GraceActive);
  }
 }
}
