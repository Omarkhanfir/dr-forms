using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace DrForms.Prototype {
 // Imported rig and real skeletal clips; never moves the gameplay controller.
 public sealed class CourtyardActor : MonoBehaviour {
  private Transform rig,weapon,shield;
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
  private int hits,shots,contacts;
  private float hitUntil,castUntil,deathAt=-1,nextDust;
  public string CurrentAnimation => playing;
  public float ClipTime => motion!=null&&playing!=null&&motion[playing]!=null?motion[playing].time:0;
  public int SkinnedParts => renderers!=null?System.Array.FindAll(renderers,r=>r is SkinnedMeshRenderer).Length:0;
  private void Start() {
   player=GetComponent<PrototypePlayerHealth>();target=GetComponent<PrototypeTarget>();enemy=GetComponent<PrototypeEnemy>();combat=GetComponent<PrototypeCombat>();
   rig=new GameObject("Authored character rig").transform;rig.SetParent(transform,false);rig.localPosition=Vector3.down;
   string name=player!=null?"Hero":enemy!=null&&enemy.MageAppearance?"Acolyte":"Guardian";
   var source=Resources.Load<GameObject>("Courtyard/"+name);
   if(source==null){Debug.LogError("Missing character asset: "+name);return;}
   var model=Instantiate(source,rig,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
   var bodies=model.GetComponentsInChildren<Renderer>();
   // Imported skinned bounds include FBX's centimetre armature scale. Measure deformed vertices instead.
   var bounds=Measure(bodies);
   model.transform.localScale*=(player!=null?2.35f:2.15f)/Mathf.Max(.1f,bounds.size.y);
   bounds=Measure(bodies);model.transform.position+=Vector3.up*(rig.position.y-bounds.min.y);
   var material=CourtyardArt.CharacterMaterial(player!=null);
   foreach(var body in bodies){body.sharedMaterial=material;body.shadowCastingMode=ShadowCastingMode.On;body.receiveShadows=true;}
   motion=model.GetComponent<Animation>();if(motion==null)motion=model.AddComponent<Animation>();
   motion.playAutomatically=false;motion.Stop();motion.cullingType=AnimationCullingType.AlwaysAnimate;
   foreach(var clip in Resources.LoadAll<AnimationClip>("Courtyard/"+name)) {
    if(clip.name.StartsWith("__preview__"))continue;
    foreach(var key in new[]{"Idle","Running_A","Walking_A","Spellcast_Shoot","Spellcasting","Hit_A","Death_A","1H_Melee_Attack_Slice_Horizontal"})
     if(clip.name.EndsWith(key)){clips[key]=clip;motion.AddClip(clip,key);}
   }
   var hand=System.Array.Find(model.GetComponentsInChildren<Transform>(),t=>t.name=="handslot.r");
   var tool=Resources.Load<GameObject>("Courtyard/"+(player!=null?"Wand":enemy!=null&&enemy.MageAppearance?"EnemyStaff":"EnemyBlade"));
   if(hand!=null&&tool!=null){weapon=Instantiate(tool,hand,false).transform;weapon.localPosition=Vector3.zero;weapon.localRotation=Quaternion.identity;
    foreach(var body in weapon.GetComponentsInChildren<Renderer>()){body.sharedMaterial=material;body.shadowCastingMode=ShadowCastingMode.On;}}
   renderers=model.GetComponentsInChildren<Renderer>();
   PrototypePresentation.Contact(transform,new Vector3(0,-.98f,0),player!=null?1.1f:1.2f);
   if(player!=null){
    shield=new GameObject("Recovery ward").transform;shield.SetParent(transform,false);shield.localPosition=new Vector3(0,-.96f,0);
    var line=shield.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=48;line.widthMultiplier=.028f;line.sharedMaterial=CourtyardArt.GlowMaterial;
    for(int i=0;i<48;i++)line.SetPosition(i,new Vector3(Mathf.Cos(i*Mathf.PI/24)*.65f,0,Mathf.Sin(i*Mathf.PI/24)*.65f));
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
   motion[name].speed=name=="Spellcast_Shoot"?2.7f:name=="Running_A"?1.15f:1;
   motion.CrossFade(name,.10f);
  }
  private void LateUpdate(){
   if(rig==null||motion==null)return;
   bool dead=player!=null?player.IsDefeated:target!=null&&target.Health<=0;
   if(dead){
    if(deathAt<0){deathAt=Time.unscaledTime;Play("Death_A",false);}
    float t=Mathf.Clamp01((Time.unscaledTime-deathAt)/.45f);
    if(clips.ContainsKey("Death_A")){motion["Death_A"].time=t*clips["Death_A"].length;motion.Sample();}
    if(shield!=null)shield.gameObject.SetActive(false);
    if(player==null&&t>=1)Destroy(gameObject);return;
   }
   var delta=transform.position-previous;delta.y=0;previous=transform.position;
   if(Time.timeScale<=0)return;
   bool moving=delta.sqrMagnitude>.00001f;
   if(combat!=null&&Time.time<castUntil)rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(combat.AimDirection),Time.deltaTime*20);
   else if(moving)rig.rotation=Quaternion.Slerp(rig.rotation,Quaternion.LookRotation(delta),Time.deltaTime*14);
   int damage=player!=null?player.DamageApplications:target!=null?target.DamageApplications:0;
   if(damage!=hits){hits=damage;hitUntil=Time.time+.22f;playing=null;Play("Hit_A",false);}
   if(combat!=null&&shots!=combat.ShotsFired){shots=combat.ShotsFired;castUntil=Time.time+.34f;playing=null;Play("Spellcast_Shoot",false);}
   if(enemy!=null&&contacts!=enemy.ContactHits){contacts=enemy.ContactHits;castUntil=Time.time+.35f;playing=null;Play("1H_Melee_Attack_Slice_Horizontal",false);}
   if(Time.time>=hitUntil&&Time.time>=castUntil)Play(enemy!=null&&enemy.Telegraphing?"Spellcasting":moving?"Running_A":"Idle",true);
   tint.SetColor("_BaseColor",Time.time<hitUntil?new Color(1.65f,1.4f,.95f):Color.white);
   foreach(var body in renderers)body.SetPropertyBlock(tint);
   if(moving&&Time.time>=nextDust){nextDust=Time.time+.24f;PrototypeCombatEffect.Spawn(transform.position+new Vector3(0,-.92f,-.12f),PrototypeCharacterVisual.Paint(10),new Color(.65f,.66f,.49f),.12f,.3f,"Footstep Dust");}
   if(shield!=null)shield.gameObject.SetActive(player.GraceActive);
  }
 }
}
