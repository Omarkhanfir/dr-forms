using UnityEngine;
namespace DrForms.Prototype
{
    [RequireComponent(typeof(CharacterController), typeof(PrototypeTarget))]
    public sealed class PrototypeEnemy : MonoBehaviour
    {
        [SerializeField] private PrototypePlayerHealth player;
        [SerializeField, Min(0)] private float speed = 2.5f;
        [SerializeField, Min(1)] private int contactDamage = 20;
        [SerializeField, Min(.05f)] private float contactCooldown = 1;
        [SerializeField, Min(.1f)] private float contactDistance = 1.1f;
        private CharacterController controller;
        private PrototypeTarget health;
        private PrototypeControls controls;
        private float verticalSpeed,nextContact,nextCast,windupUntil;
        private Vector3 lockedAim;
        private LineRenderer warning;
        public bool IsRanged {get;private set;}
        public bool Telegraphing => windupUntil>0;
        public Vector3 LockedAim => lockedAim;
        public int RangedShots {get;private set;}
        public int ContactHits {get;private set;}
        public float LastContactTime {get;private set;}
        public float PreviousContactTime {get;private set;}
        public int ContactDamage=>contactDamage;
        public float ContactCooldown=>contactCooldown;
        public float ContactDistance=>contactDistance;
        public float Speed=>speed;
        private void Awake()
        {
            controller=GetComponent<CharacterController>();health=GetComponent<PrototypeTarget>();
            if(player==null)player=FindAnyObjectByType<PrototypePlayerHealth>();
            if(player!=null)controls=player.GetComponent<PrototypeControls>();
        }
        public void ConfigureForWave(bool ranged)
        {
            health=GetComponent<PrototypeTarget>();
            IsRanged=ranged;speed=ranged?1.1f:Mathf.Min(speed,1.8f);
            contactDamage=Mathf.Min(contactDamage,10);contactCooldown=Mathf.Max(contactCooldown,1.5f);
            nextCast=Time.time+3.5f;
            health.SetDisplayName(ranged?"Flower mage":"Mossling");
            if(!ranged)return;
            var obj=new GameObject("Amber Aim Warning");obj.transform.SetParent(transform,false);
            warning=obj.AddComponent<LineRenderer>();warning.useWorldSpace=true;warning.positionCount=2;
            warning.sharedMaterial=PrototypeCharacterVisual.Paint(8);warning.startWidth=.16f;warning.endWidth=.34f;
            warning.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;warning.receiveShadows=false;warning.enabled=false;
        }
        private void Update()
        {
            if(player==null || player.IsDefeated || health.Health<=0 || controls==null || !controls.CanPlay){if(warning!=null && health.Health<=0)warning.enabled=false;return;}
            var delta=player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
            Vector3 horizontal=Vector3.zero;
            float stop=IsRanged?4.8f:contactDistance*.9f;
            if(distance>stop && Time.deltaTime>0 && !Telegraphing)
                horizontal=delta.normalized*Mathf.Min(speed,(distance-stop)/Time.deltaTime);
            if(controller.isGrounded && verticalSpeed<0)verticalSpeed=-2;
            verticalSpeed+=Physics.gravity.y*Time.deltaTime;
            controller.Move((horizontal+Vector3.up*verticalSpeed)*Time.deltaTime);
            if(IsRanged)Cast(distance);
            delta=player.transform.position-transform.position;delta.y=0;
            if(delta.sqrMagnitude>contactDistance*contactDistance || Time.time<nextContact || player.GraceActive)return;
            nextContact=Time.time+contactCooldown;PreviousContactTime=LastContactTime;LastContactTime=Time.time;ContactHits++;
            player.TakeDamage(contactDamage);
        }
        private void Cast(float distance)
        {
            if(Telegraphing){
                if(Time.time<windupUntil)return;
                warning.enabled=false;windupUntil=0;nextCast=Time.time+4.5f;
                PrototypeEnemyBolt.Spawn(transform.position,lockedAim,player);RangedShots++;return;
            }
            if(Time.time<nextCast || distance>7.5f || player.GraceActive)return;
            lockedAim=player.transform.position;windupUntil=Time.time+1.3f;
            warning.SetPosition(0,new Vector3(transform.position.x,.08f,transform.position.z));
            var end=transform.position+(lockedAim-transform.position).normalized*9;
            warning.SetPosition(1,new Vector3(end.x,.08f,end.z));warning.enabled=true;
        }
    }
}
