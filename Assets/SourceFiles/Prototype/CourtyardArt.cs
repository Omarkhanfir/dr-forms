using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace DrForms.Prototype {
 public static class CourtyardArt {
  private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
  private static Sprite panel,touch,answer,answerFocus;
  public static Sprite AnswerPanel {get{if(answer==null)answer=Sprite(Resources.Load<Texture2D>("Courtyard/AnswerPanel"),new Vector4(18,18,18,18));return answer;}}
  public static Sprite AnswerFocus {get{if(answerFocus==null)answerFocus=Sprite(Resources.Load<Texture2D>("Courtyard/AnswerFocus"),new Vector4(18,18,18,18));return answerFocus;}}
  private static UniversalRenderPipelineAsset shadowBudget;
  private static RenderPipelineAsset previousQuality;
  public static int ShadowResolution => shadowBudget != null ? shadowBudget.mainLightShadowmapResolution : 0;
  public static Sprite Panel {get{if(panel==null)panel=Sprite(Resources.Load<Texture2D>("Courtyard/Panel"),new Vector4(20,20,20,20));return panel;}}
  public static Sprite Touch {get{if(touch==null)touch=Sprite(Resources.Load<Texture2D>("Courtyard/TouchRing"),Vector4.zero);return touch;}}
  private static Sprite Sprite(Texture2D t,Vector4 border)=>UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
  public static Material GlowMaterial {
   get{if(materials.TryGetValue("GlowVFX",out var m)&&m!=null)return m;
    m=new Material(Shader.Find("DrForms/Courtyard Glow"));m.SetTexture("_MainTex",Resources.Load<Texture2D>("Courtyard/Spark"));m.SetColor("_BaseColor",new Color(.6f,1,.88f));materials["GlowVFX"]=m;return m;}
  }
  public static Material CharacterMaterial(bool hero){
   string key=hero?"Hero":"Enemy";if(materials.TryGetValue(key,out var m)&&m!=null)return m;
   m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetTexture("_BaseMap",Resources.Load<Texture2D>("Courtyard/"+(hero?"HeroAtlas":"EnemyAtlas")));
   m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.24f);m.SetFloat("_Metallic",hero?.25f:.1f);m.enableInstancing=true;materials[key]=m;return m;
  }
  public static Material EnvironmentMaterial(string key){
   key=key.Split('.')[0];
   if(materials.TryGetValue(key,out var m)&&m!=null)return m;
   var colors=new Dictionary<string,Color>{{"Ivory",new Color(.94f,.88f,.70f)},{"Teal",new Color(.035f,.24f,.25f)},{"Skin",new Color(.72f,.43f,.24f)},{"Hair",new Color(.9f,.92f,.83f)},{"Green",new Color(.62f,.88f,.16f)},{"Violet",new Color(.50f,.23f,.70f)},{"Glass",new Color(.30f,.75f,.76f)},{"Black",new Color(.025f,.07f,.09f)},{"Stone",new Color(.46f,.52f,.48f)},{"StoneLight",new Color(.64f,.66f,.55f)},{"StoneDark",new Color(.30f,.38f,.36f)},
    {"Tile",new Color(.42f,.53f,.53f)},{"TileLight",new Color(.45f,.56f,.55f)},{"TileDark",new Color(.39f,.50f,.50f)},
    {"Wood",new Color(.06f,.26f,.27f)},{"Gold",new Color(.63f,.45f,.19f)},{"Iron",new Color(.10f,.17f,.19f)},
    {"Leaf",new Color(.15f,.32f,.19f)},{"LeafLight",new Color(.31f,.44f,.20f)},{"Trunk",new Color(.20f,.17f,.12f)},
    {"Cloth",new Color(.13f,.34f,.37f)},{"Glow",new Color(.30f,.85f,.74f)},{"Flame",new Color(1,.60f,.12f)}};
   m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",colors.TryGetValue(key,out var color)?color:Color.gray);m.SetFloat("_Smoothness",.12f);m.enableInstancing=true;
   if(key.StartsWith("Stone")||key.StartsWith("Tile")){m.SetTexture("_BaseMap",Resources.Load<Texture2D>("Courtyard/StoneGrain"));}
   if(key=="Glass")m.SetFloat("_Smoothness",.72f);
   if(key=="Gold"||key=="Iron"){m.SetFloat("_Metallic",.65f);m.SetFloat("_Smoothness",.4f);}
   if(key=="Glow"||key=="Flame"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.5f);}
   materials[key]=m;return m;
  }
  private static void BudgetShadows(){
   if(shadowBudget!=null)return;
   var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;if(source==null)return;
   previousQuality=QualitySettings.renderPipeline;shadowBudget=Object.Instantiate(source);shadowBudget.name="Courtyard bounded shadows";
   shadowBudget.mainLightShadowmapResolution=1024;shadowBudget.shadowCascadeCount=1;shadowBudget.shadowDistance=28;
   QualitySettings.renderPipeline=shadowBudget;
  }
  public static void BuildRoom(Transform route,Transform door){
   BudgetShadows();
   foreach(var renderer in route.GetComponentsInChildren<MeshRenderer>())
    if(renderer.name!="Laboratory exit status")renderer.enabled=false;
   for(int index=0;index<3;index++){
    var room=Object.Instantiate(Resources.Load<GameObject>("Courtyard/Room"),route,false);room.name="Dr.forms laboratory room "+(index+1);
    room.transform.localPosition=Vector3.forward*PrototypeWaves.RoomCenter(index);room.transform.localRotation=Quaternion.Euler(0,180,0);
    Dress(room);
    // Visuals have no colliders; room boundaries and encounters remain authoritative.
    foreach(var collision in room.GetComponentsInChildren<Collider>())Object.Destroy(collision);
   }
   var gate=Object.Instantiate(Resources.Load<GameObject>("Courtyard/Gate"),door,false);gate.transform.localPosition=Vector3.zero;gate.transform.localRotation=Quaternion.Euler(0,180,0);gate.transform.localScale=new Vector3(1/door.localScale.x,1/door.localScale.y,1/door.localScale.z);Dress(gate);
   // Visuals have no colliders; the existing room/door/controller remain authoritative.
   var light=RenderSettings.sun; if(light==null)light=GameObject.Find("Arena Light")?.GetComponent<Light>();
   if(light!=null){light.color=new Color(1,.96f,.86f);light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(60,-32,0);light.shadows=LightShadows.Soft;light.shadowStrength=.38f;light.shadowBias=.04f;light.shadowNormalBias=.25f;}
   RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.54f,.62f,.66f);RenderSettings.ambientEquatorColor=new Color(.40f,.49f,.49f);RenderSettings.ambientGroundColor=new Color(.25f,.31f,.30f);
  }
  public static void DressGate(Transform door){
   door.GetComponent<Renderer>().enabled=false;
   var gate=Object.Instantiate(Resources.Load<GameObject>("Courtyard/Gate"),door,false);gate.transform.localRotation=Quaternion.Euler(0,180,0);gate.transform.localScale=new Vector3(1/door.localScale.x,1/door.localScale.y,1/door.localScale.z);Dress(gate);
  }
  private static void Dress(GameObject root){foreach(var renderer in root.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=EnvironmentMaterial(renderer.gameObject.name);renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  private static void Reset(){if(shadowBudget!=null){QualitySettings.renderPipeline=previousQuality;Object.Destroy(shadowBudget);}shadowBudget=null;previousQuality=null;foreach(var m in materials.Values)if(m!=null)Object.Destroy(m);materials.Clear();if(panel!=null)Object.Destroy(panel);if(touch!=null)Object.Destroy(touch);if(answer!=null)Object.Destroy(answer);if(answerFocus!=null)Object.Destroy(answerFocus);panel=null;touch=null;answer=null;answerFocus=null;}
 }
}
