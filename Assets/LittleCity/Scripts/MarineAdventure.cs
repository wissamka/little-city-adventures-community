using UnityEngine;

namespace LittleCity
{
    public sealed class MarineAdventure
    {
        readonly CityGame game;
        public MarineAdventure(CityGame owner){game=owner;}
        public string Context
        {
            get
            {
                if(game.State.WorldIndex!=1 || game.State.Indoors)return "";
                if(game.State.Mode==Ride.Speedboat)return game.CurrentVehicle.Root.GetComponent<TowRig>().Towing?"F • Stow tube":"F • Attach tube";
                if(game.State.OnDivePlatform)return "F • Dive off the board";
                if(game.State.Mode==Ride.Walk && Vector2.Distance(new Vector2(game.State.Position.X,game.State.Position.Z),new Vector2(35,102))<4)return "F • Climb diving platform ladder";
                if(game.State.Swimming)return "SWIMMING • WASD / stick • Shore and ladders lead out";
                return "";
            }
        }
        public bool Use()
        {
            if(game.State.WorldIndex!=1 || game.State.Indoors)return false;
            if(game.State.Mode==Ride.Speedboat){var tow=game.CurrentVehicle.Root.GetComponent<TowRig>();tow.Towing=!tow.Towing;game.State.Say(tow.Towing?"Tube attached! Drive and turn to tow it across the water.":"Tube stowed. F to attach it again.");return true;}
            if(game.State.OnDivePlatform){game.State.Dive();game.SnapCamera();return true;}
            if(game.State.Mode==Ride.Walk && Vector2.Distance(new Vector2(game.State.Position.X,game.State.Position.Z),new Vector2(35,102))<4){game.State.ClimbDivingPlatform();game.SnapCamera();return true;}
            return false;
        }
        public static void BuildMarina(CityGame game,WorldPlace world)
        {
            var r=Art.Group("Marine adventures",world.Root);
            foreach(int x in new[]{22,55,90,104,-170,-185})
            {
                Art.Box("Boat boarding jetty",r,new Vector3(x,-.05f,43),new Vector3(3,.3f,12),Art.Hex("B78D60"),0);
                var boat=Models.Vehicle(r,x==22 || x==90 || x==-170?Ride.Speedboat:Ride.Motorboat);boat.Root.localPosition=new Vector3(x+3.7f,-1.05f,46);boat.Root.localRotation=Quaternion.Euler(0,0,0);
                game.Parking.Register(boat,1,boat.Mode==Ride.Speedboat?"speedboat & inflatable tube":"marina motorboat");
                AdventureWorlds.Caption(boat.Mode==Ride.Speedboat?"SPEEDBOAT + TUBE · G":"MOTORBOAT · G",r,new Vector3(x,2.2f,37),180,15);
            }
            foreach(int x in new[]{-65,-130,-5})AdventureWorlds.Solid(world,new Vector3(x,5,x==-65?115:116),new Vector3(23,16,102));
            var platform=Art.Group("Offshore diving platform",r,new Vector3(35,0,110));
            Art.Box("Diving platform deck",platform,new Vector3(0,4.2f,0),new Vector3(10,.4f,12),Art.Hex("D0AA76"),0);
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})Art.Tube("Platform ocean support",platform,new Vector3(side*4,-3,end*5),new Vector3(side*4,4.2f,end*5),.3f,Art.White);
            Art.Box("Spring diving board",platform,new Vector3(0,4.4f,7),new Vector3(2,.2f,7),Art.Blue,0);
            for(int z=-5;z<5;z+=2)foreach(int side in new[]{-1,1})Art.Tube("Platform railing",platform,new Vector3(side*4.8f,4.4f,z),new Vector3(side*4.8f,5.5f,z),.07f,Art.White);
            foreach(int side in new[]{-1,1})Art.Tube("Climbable ladder rail",platform,new Vector3(side*.75f,-1,-7),new Vector3(side*.75f,5,-7),.1f,Art.White);
            for(float y=-.7f;y<4.7f;y+=.55f)Art.Tube("Ladder step",platform,new Vector3(-.75f,y,-7),new Vector3(.75f,y,-7),.08f,Art.White);
            // Slender supports collide below the platform without blocking swimmers from the ladder.
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})AdventureWorlds.Solid(world,new Vector3(35+side*4,2,110+end*5),new Vector3(.6f,5,.6f));
            AdventureWorlds.Caption("DIVING PLATFORM · F AT LADDER",r,new Vector3(35,7,110),180,23);
            for(int i=0;i<5;i++)
            {
                var shark=Art.Group("Friendly circling reef shark",r);shark.gameObject.AddComponent<MovingScenery>();
                Color color=i%2==0?Art.Hex("72A6BE"):Art.Hex("8CBBCD");
                Art.Ball("Shark body",shark,new Vector3(0,0,0),new Vector3(1.5f,1.2f,4.6f),color);Art.Ball("Rounded nose",shark,new Vector3(0,0,1.7f),new Vector3(1.4f,1,1.7f),color);
                Art.Ball("Pale belly",shark,new Vector3(0,-.35f,.3f),new Vector3(1.2f,.5f,3.5f),Art.White);
                var fin=Art.Box("Shark dorsal fin",shark,new Vector3(0,.8f,-.5f),new Vector3(.22f,1.3f,1.2f),color,0);fin.transform.localRotation=Quaternion.Euler(-25,0,0);
                foreach(int side in new[]{-1,1}){var f=Art.Ball("Swimming side fin",shark,new Vector3(side*.9f,-.2f,0),new Vector3(1.7f,.22f,.9f),color);f.transform.localRotation=Quaternion.Euler(0,side*-28,0);Art.Ball("Shark eye",shark,new Vector3(side*.55f,.2f,2.05f),Vector3.one*.17f,Art.Ink);}
                Art.Ball("Shark tail",shark,new Vector3(0,.1f,-2.6f),new Vector3(.4f,2,1.5f),color);
                shark.gameObject.AddComponent<SharkCircle>().Initialize(new Vector3(35,-.8f,110),18+i*3,i*1.5f);
            }
        }
        public static void BuildBoat(VehicleModel boat)
        {
            bool fast=boat.Mode==Ride.Speedboat;var r=boat.Root;Color color=fast?Art.Blue:Art.Teal;
            Art.Ball("Floating boat hull",r,new Vector3(0,.3f,0),new Vector3(3.4f,1.35f,7),Art.White);
            Art.Ball("Boat color stripe",r,new Vector3(0,.48f,0),new Vector3(3.45f,.5f,6.8f),color);
            Art.Box("Boat open cockpit floor",r,new Vector3(0,.86f,-.5f),new Vector3(2.5f,.18f,3.3f),Art.Hex("CFA875"),0);
            Art.Ball("Bow cover",r,new Vector3(0,1.05f,2),new Vector3(2.8f,.5f,2.7f),color);
            foreach(int side in new[]{-1,1})Art.Box("Boat side rail",r,new Vector3(side*1.45f,1.13f,-.3f),new Vector3(.2f,.4f,4.4f),Art.White,.08f);
            Art.Box("Skipper seat",r,new Vector3(0,1.25f,-.85f),new Vector3(1.1f,.3f,1),Art.White,.14f);Art.Box("Seat back",r,new Vector3(0,1.65f,-1.32f),new Vector3(1.1f,.8f,.2f),color,.08f);
            Art.Box("Boat console",r,new Vector3(0,1.42f,.05f),new Vector3(1.5f,.85f,.54f),Art.White,.1f);
            var wheel=Art.Ring("Skipper steering wheel",r,.29f,.025f,.025f,Art.Ink);wheel.transform.localPosition=new Vector3(0,1.91f,-.30f);wheel.transform.localRotation=Quaternion.Euler(0,90,0);
            Art.Box("Outboard motor",r,new Vector3(0,.35f,-3.3f),new Vector3(.8f,1.4f,.65f),Art.Ink,.1f);
            if(!fast){Art.Box("Boat sun canopy",r,new Vector3(0,3,-.65f),new Vector3(3,.15f,3.3f),color,0);foreach(int side in new[]{-1,1})Art.Tube("Canopy post",r,new Vector3(side*1.35f,1,-1.9f),new Vector3(side*1.35f,3,-1.9f),.07f,Art.White);}
            boat.Seat=new Vector3(0,1.48f,-.82f);boat.Grips[0]=new Vector3(-.27f,1.91f,-.30f);boat.Grips[1]=new Vector3(.27f,1.91f,-.30f);
            foreach(int side in new[]{-1,1})boat.Pedals.Add(Art.Group("Skipper foot contact",r,new Vector3(side*.12f,.95f,-.32f)));
            if(fast)r.gameObject.AddComponent<TowRig>();else r.gameObject.AddComponent<BoatWake>();
        }
    }
    public class BoatWake : MonoBehaviour
    {
        protected Vector3 previous;protected float speed;Transform[] foam;
        protected virtual void Start()
        {
            previous=transform.position;foam=new Transform[12];
            for(int i=0;i<foam.Length;i++)foam[i]=Art.Ball("Boat wake foam",transform,Vector3.zero,new Vector3(.3f,.03f,1.6f),Art.White).transform;
        }
        protected virtual void LateUpdate()
        {
            float dt=Mathf.Max(.001f,Time.deltaTime);speed=Mathf.Min(25,Vector3.Distance(transform.position,previous)/dt);previous=transform.position;
            for(int i=0;i<foam.Length;i++){float distance=2+(i/2)*1.5f;foam[i].gameObject.SetActive(speed>.2f);foam[i].position=transform.TransformPoint(new Vector3((i%2==0?-1:1)*(1.3f+distance*.22f),.08f,-distance));foam[i].rotation=Quaternion.Euler(0,transform.eulerAngles.y+(i%2==0?-25:25),0);}
        }
    }
    public sealed class TowRig : BoatWake
    {
        public bool Towing=true;public Transform Tube { get; private set; }public float RopeLength=>Vector3.Distance(transform.TransformPoint(new Vector3(0,.5f,-3.3f)),Tube.position);
        LineRenderer rope;Vector3 tubePosition;bool placed;
        protected override void Start()
        {
            base.Start();var tube=Art.Ring("Towable inflatable tube",transform,1.35f,.35f,.35f,Art.Hex("F7A951"));Tube=tube.transform;Tube.localRotation=Quaternion.Euler(0,0,90);
            Art.Disk("Tube seat",Tube,Vector3.zero,.8f,.12f,Art.Blue).transform.localRotation=Quaternion.Euler(0,0,90);
            var obj=new GameObject("Speedboat tow rope");obj.transform.SetParent(transform,false);rope=obj.AddComponent<LineRenderer>();rope.useWorldSpace=true;rope.positionCount=2;rope.startWidth=rope.endWidth=.055f;rope.sharedMaterial=Art.Mat(Art.White);
        }
        protected override void LateUpdate()
        {
            base.LateUpdate();if(Tube==null)return;Tube.gameObject.SetActive(Towing);rope.enabled=Towing;if(!Towing){placed=false;return;}
            Vector3 stern=transform.TransformPoint(new Vector3(0,.5f,-3.3f)),target=stern-transform.forward*8.5f;
            if(!placed || Vector3.Distance(tubePosition,stern)>30){tubePosition=target;placed=true;}
            tubePosition=Vector3.Lerp(tubePosition,target,1-Mathf.Exp(-Time.deltaTime*1.8f));Vector3 offset=tubePosition-stern;if(offset.magnitude>8.5f)tubePosition=stern+offset.normalized*8.5f;
            tubePosition.y=-.55f+Mathf.Sin(Time.time*5)*Mathf.Min(.24f,speed*.015f);Tube.position=tubePosition;Tube.rotation=Quaternion.Euler(0,transform.eulerAngles.y,90);
            rope.SetPosition(0,stern);rope.SetPosition(1,tubePosition);
        }
    }
    public sealed class SharkCircle : MonoBehaviour
    {
        Vector3 center;float radius,phase;
        public void Initialize(Vector3 c,float r,float p){center=c;radius=r;phase=p;}
        void Update(){float a=Time.time*.13f+phase;transform.localPosition=center+new Vector3(Mathf.Sin(a)*radius,Mathf.Sin(a*3)*.12f,Mathf.Cos(a)*radius);transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+90,0);}
    }
}
