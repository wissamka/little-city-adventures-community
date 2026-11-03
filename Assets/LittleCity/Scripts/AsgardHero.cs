using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class MythicTarget : MonoBehaviour
    {
        public bool Broken { get; private set; }
        Transform shape;Vector3 original;float fall;
        public void Setup(){shape=transform.Find("Training target");original=shape.localPosition;}
        public void Hit(){if(Broken)return;Broken=true;fall=.65f;}
        public void Restore(){Broken=false;fall=0;shape.gameObject.SetActive(true);shape.localPosition=original;shape.localRotation=Quaternion.identity;}
        void Update(){if(fall<=0)return;fall-=Time.deltaTime;float t=1-fall/.65f;shape.localPosition=original+Vector3.up*Mathf.Sin(t*Mathf.PI)*1.5f;shape.localRotation=Quaternion.Euler(t*90,t*120,0);if(fall<=0)shape.gameObject.SetActive(false);}
    }
    public sealed class BriefEffect : MonoBehaviour
    {
        public float Remaining=.4f;public bool Expand;
        void Update(){Remaining-=Time.deltaTime;if(Expand)transform.localScale+=Vector3.one*Time.deltaTime*8;if(Remaining<=0)Destroy(gameObject);}
    }
    public sealed class AsgardHero
    {
        readonly CityGame game;readonly List<MythicTarget> targets=new List<MythicTarget>();
        public bool Active { get; private set; }
        public bool Available=>game.State.WorldIndex==3 && game.State.GeneratedTheme==3;
        public float HammerCooldown { get; private set; }public float LightningCooldown { get; private set; }public float SlamCooldown { get; private set; }
        public int BrokenCount{get{int n=0;foreach(var t in targets)if(t!=null && t.Broken)n++;return n;}}
        public int TargetCount=>targets.Count;
        public IReadOnlyList<MythicTarget> Targets=>targets;
        Transform heldHammer,flyingHammer;Vector3 throwStart,throwEnd;MythicTarget aim;float throwTime=-1,slamPose,lightningPose;bool recall;
        public bool HammerFlying=>throwTime>=0;
        public AsgardHero(CityGame owner){game=owner;}
        public string Context=>Available && !Active && !game.State.Indoors && Vector2.Distance(new Vector2(game.State.Position.X,game.State.Position.Z),new Vector2(0,-16))<4?"F • Become Thor · U transforms anywhere in Asgard":"";
        public bool Use(){if(string.IsNullOrEmpty(Context))return false;Toggle();return true;}
        public void SetWorld(Transform root,bool asgard)
        {
            CancelHammer();targets.Clear();if(!asgard){Disable();return;}
            var arena=Art.Group("Thor mythic training ground",root);arena.gameObject.AddComponent<MovingScenery>();
            Art.Disk("Thor shrine",arena,new Vector3(0,.18f,-16),2.3f,.35f,Art.Hex("DACB9B"));var display=Hammer(arena);display.localPosition=new Vector3(0,1,-16);display.localScale=Vector3.one*2;
            AdventureWorlds.Caption("THOR • U TO TRANSFORM",arena,new Vector3(0,3,-16),180,12);
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                var target=Art.Group("Breakable mythic training target",arena,new Vector3(-18+col*18,0,2+row*15));
                var shape=Art.Group("Training target",target);Art.Box("Frost target plinth",shape,new Vector3(0,.4f,0),new Vector3(2.4f,.8f,2.4f),Art.Hex("87B3C7"),0);
                Art.Box("Rune stone",shape,new Vector3(0,2,0),new Vector3(1.6f,3,1.2f),Art.Hex("AECEDC"),0);
                Art.Tube("Golden rune",shape,new Vector3(-.5f,1.5f,-.65f),new Vector3(.5f,2.7f,-.65f),.1f,Art.Yellow);Art.Tube("Golden rune",shape,new Vector3(.5f,1.5f,-.65f),new Vector3(-.5f,2.7f,-.65f),.1f,Art.Yellow);
                var component=target.gameObject.AddComponent<MythicTarget>();component.Setup();targets.Add(component);
            }
            AdventureWorlds.Caption("HAMMER • LIGHTNING • GROUND SLAM",arena,new Vector3(0,4,43),180,25);
        }
        public void Toggle()
        {
            if(!Available || game.State.Mode!=Ride.Walk || game.State.Indoors || game.State.Resting!=RestPose.None){game.State.Say("Become Thor on foot outdoors in Asgard. Reach stop 4 on the sky route.");return;}
            Active=!Active;CancelHammer();game.RefreshPlayer();game.State.Say(Active?"Thor! Y throws / recalls the hammer · O lightning · N slam. Break the nine rune targets. 0 resets them.":"Back to your explorer. U becomes Thor again.");
        }
        public void Disable(){if(!Active && !HammerFlying)return;Active=false;CancelHammer();if(game.Ready)game.RefreshPlayer();}
        public void Dress(CharacterModel person)
        {
            if(!Active)return;var armor=Art.Group("Thor armor and red cape",person.Root);
            Art.Box("Silver chest armor",armor,new Vector3(0,1.5f,.28f),new Vector3(.76f,.67f,.14f),Art.Hex("C1D0D6"),.05f);
            foreach(int side in new[]{-1,1})foreach(float y in new[]{1.32f,1.68f})Art.Ball("Thor armor disc",armor,new Vector3(side*.23f,y,.39f),new Vector3(.18f,.18f,.05f),Art.White);
            Art.Box("Flowing red cape",armor,new Vector3(0,1.22f,-.55f),new Vector3(.9f,1.38f,.08f),Art.Hex("C54743"),.03f);
            Art.Ball("Silver winged helmet",person.Head,new Vector3(0,.25f,-.04f),new Vector3(1.1f,.65f,.98f),Art.Hex("C1D0D6"));
            foreach(int side in new[]{-1,1}){var wing=Art.Ball("Helmet wing",person.Head,new Vector3(side*.62f,.35f,-.02f),new Vector3(.2f,.65f,.4f),Art.White);wing.transform.localRotation=Quaternion.Euler(0,0,side*-25);Art.Ball("Golden hair",person.Head,new Vector3(side*.48f,-.12f,-.16f),new Vector3(.28f,.9f,.48f),Art.Hex("EAC16F"));}
            Art.Ball("Thor golden beard",person.Head,new Vector3(0,-.30f,.41f),new Vector3(.60f,.38f,.18f),Art.Hex("DAA75D"));
            heldHammer=Hammer(person.Ends[3]);heldHammer.localPosition=new Vector3(0,-.06f,.08f);heldHammer.localRotation=Quaternion.Euler(0,0,-15);
        }
        static Transform Hammer(Transform parent)
        {
            var r=Art.Group("Mjolnir returning hammer",parent);Art.Tube("Leather hammer grip",r,Vector3.zero,new Vector3(0,.5f,0),.065f,Art.Hex("8E654C"));Art.Box("Broad hammer head",r,new Vector3(0,.67f,0),new Vector3(.70f,.38f,.40f),Art.Hex("CDDCE1"),.07f);Art.Box("Hammer rune",r,new Vector3(0,.67f,.21f),new Vector3(.06f,.22f,.02f),Art.Blue,0);return r;
        }
        MythicTarget Nearest(float range)
        {
            MythicTarget best=null;float distance=range;Vector3 p=Position,forward=new Vector3(Mathf.Sin(game.State.Yaw),0,Mathf.Cos(game.State.Yaw));
            foreach(var t in targets){if(t==null || t.Broken)continue;Vector3 d=t.transform.position-p;float n=d.magnitude;if(n<distance && Vector3.Dot(d.normalized,forward)>-.1f){best=t;distance=n;}}return best;
        }
        Vector3 Position=>new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z);
        bool CanAct=>Active && Available && game.State.Mode==Ride.Walk && !game.State.Indoors && !game.Hud.MenuOpen;
        public bool ThrowHammer()
        {
            if(!CanAct)return false;if(HammerFlying){recall=true;return true;}if(HammerCooldown>0)return false;
            HammerCooldown=1.2f;throwTime=0;recall=false;aim=Nearest(32);throwStart=heldHammer.position;
            throwEnd=aim!=null?aim.transform.position+Vector3.up*2:Position+new Vector3(Mathf.Sin(game.State.Yaw)*20,2,Mathf.Cos(game.State.Yaw)*20);
            flyingHammer=Hammer(game.transform);flyingHammer.position=throwStart;heldHammer.gameObject.SetActive(false);return true;
        }
        public bool Lightning()
        {
            if(!CanAct || LightningCooldown>0)return false;LightningCooldown=3.5f;lightningPose=.55f;int count=0;
            foreach(var target in targets)if(target!=null && !target.Broken && Vector3.Distance(target.transform.position,Position)<28 && count<3){Beam(target.transform.position+Vector3.up*20,target.transform.position+Vector3.up*1.5f);target.Hit();count++;}
            if(count==0)Beam(Position+Vector3.up*18,Position+Vector3.up*1);return true;
        }
        public bool Slam()
        {
            if(!CanAct || SlamCooldown>0 || !game.State.Grounded)return false;SlamCooldown=5;slamPose=.7f;
            foreach(var t in targets)if(t!=null && !t.Broken && Vector3.Distance(t.transform.position,Position)<10)t.Hit();
            var ring=Art.Ring("Slam shockwave",game.transform,1,.07f,.07f,Art.Teal);ring.transform.position=Position+Vector3.up*.2f;ring.transform.rotation=Quaternion.Euler(0,0,90);var fx=ring.AddComponent<BriefEffect>();fx.Expand=true;fx.Remaining=.75f;return true;
        }
        void Beam(Vector3 start,Vector3 end)
        {
            var obj=new GameObject("Thor lightning bolt");obj.transform.SetParent(game.transform);var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=Art.Mat(Art.Hex("BDF4FC"));line.useWorldSpace=true;line.startWidth=.20f;line.endWidth=.08f;line.positionCount=7;for(int i=0;i<7;i++)line.SetPosition(i,Vector3.Lerp(start,end,i/6f)+Vector3.right*(i%2==0?.4f:-.4f));obj.AddComponent<BriefEffect>();
        }
        public void ResetTargets(){foreach(var t in targets)if(t!=null)t.Restore();game.State.Say("Nine rune targets restored. Try a new combination of powers!");}
        void CancelHammer(){throwTime=-1;if(flyingHammer!=null)Object.Destroy(flyingHammer.gameObject);flyingHammer=null;if(heldHammer!=null)heldHammer.gameObject.SetActive(true);}
        public void Tick(float dt)
        {
            if(Active && (!Available || game.State.Mode!=Ride.Walk)){Disable();return;}
            HammerCooldown=Mathf.Max(0,HammerCooldown-dt);LightningCooldown=Mathf.Max(0,LightningCooldown-dt);SlamCooldown=Mathf.Max(0,SlamCooldown-dt);
            if(HammerFlying)
            {
                throwTime+=dt;float t=throwTime;
                if(recall && t<.38f){throwEnd=flyingHammer.position;throwTime=.38f;t=.38f;}
                if(t>=.38f && aim!=null && !recall){aim.Hit();aim=null;}
                flyingHammer.position=t<.38f?Vector3.Lerp(throwStart,throwEnd,t/.38f):Vector3.Lerp(throwEnd,heldHammer.position,(t-.38f)/.35f);flyingHammer.Rotate(0,0,dt*1100);
                if(t>=.73f)CancelHammer();
            }
            slamPose=Mathf.Max(0,slamPose-dt);lightningPose=Mathf.Max(0,lightningPose-dt);
            if(Active){game.Explorer.RaiseHammer(lightningPose>0 || HammerFlying);game.Explorer.Root.localPosition=Vector3.up*Mathf.Sin(slamPose/.7f*Mathf.PI)*.45f;}
        }
    }
}
