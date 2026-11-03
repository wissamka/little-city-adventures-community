using UnityEngine;
namespace LittleCity
{
    public sealed class YachtVoyages
    {
        readonly CityGame game; readonly Transform marina;
        public int YachtIndex {get;private set;}=-1;
        public int DockedIsland {get;private set;}=-2;
        public bool AtSea=>YachtIndex>=0;
        public bool InCabin {get;private set;}
        Point saved;float heading;VehicleModel moored;Obstacle mooredCollision;
        static readonly string[] names={"Aurora","Sea Star","Sunbeam"};
        public static Point HomeBerth(int i)=>new Point(i==0?-65:i==1?-130:-5,-1.05f,i==0?115:116);
        public YachtVoyages(CityGame g,Transform world){game=g;marina=world;}
        public string Context
        {
            get
            {
                if(game.State.WorldIndex!=1)return "";
                if(game.State.Mode==Ride.Yacht)return Mathf.Abs(game.State.Speed)>.8f?"Sail with WASD · Space / STOP slows down":NearDock()!=-2?"F • Dock and explore":"F • Explore the yacht at anchor";
                if(DockedIsland!=-2&&game.State.Mode==Ride.Walk&&!game.State.Indoors&&Distance(game.State.Position,BoardPoint())<5)return "F • Return to "+names[YachtIndex];
                return "";
            }
        }
        static float Distance(Point a,Point b)=>Vector2.Distance(new Vector2(a.X,a.Z),new Vector2(b.X,b.Z));
        Point BoardPoint()=>DockedIsland<0?new Point(HomeBerth(YachtIndex).X,0,YachtIndex==0?60:65):IslandLayout.Dock(DockedIsland);
        public int NearDock()
        {
            for(int i=0;i<3;i++)if(Distance(game.State.Position,IslandLayout.Berth(i))<24)return i;
            return Distance(game.State.Position,HomeBerth(YachtIndex<0?0:YachtIndex))<24?-1:-2;
        }
        public void Begin(int index)
        {
            bool resume=YachtIndex==index;var position=resume?saved:HomeBerth(index);float yaw=resume?heading:0;
            game.Worlds.Yachts.Leave();InCabin=false;RemoveMoored();YachtIndex=index;DockedIsland=-2;HideOriginal(index);
            if(game.State.Mode!=Ride.Walk)game.State.SwitchRide(Ride.Walk);
            game.State.EnterVehicle(Ride.Yacht,index,position,yaw);game.RefreshPlayer();
            game.State.Say("Captain of "+names[index]+"! W / GO sails, A/D steers, Space / STOP brakes. Follow the island beacons; stop inside a mooring ring and press F.");
        }
        void HideOriginal(int index)
        {
            var r=marina.Find(names[index].ToUpperInvariant()+" superyacht");if(r!=null)r.gameObject.SetActive(false);
            float x=HomeBerth(index).X;foreach(var collider in marina.GetComponentsInChildren<BoxCollider>(true))if(Mathf.Abs(collider.transform.position.x-x)<.1f&&collider.size.z>60&&collider.size.x>20)collider.enabled=false;game.State.Obstacles.RemoveAll(o=>Mathf.Abs(o.X-x)<.1f&&o.HalfX>=10&&o.HalfZ>25);
        }
        public bool Use()
        {
            if(game.State.WorldIndex!=1)return false;
            if(DockedIsland!=-2&&game.State.Mode==Ride.Walk&&!game.State.Indoors&&Distance(game.State.Position,BoardPoint())<5){Begin(YachtIndex);return true;}
            if(game.State.Mode!=Ride.Yacht)return false;
            if(Mathf.Abs(game.State.Speed)>.8f){game.State.Say("Slow to a stop before leaving the helm.");return true;}
            saved=game.State.Position;heading=game.State.Yaw;int dock=NearDock();
            if(dock==-2)
            {
                game.State.SwitchRide(Ride.Walk);InCabin=true;game.Worlds.Yachts.Board(YachtIndex);game.State.Say("Anchored safely. Explore both decks; return to the helm to keep sailing.");return true;
            }
            DockedIsland=dock;saved=dock<0?HomeBerth(YachtIndex):IslandLayout.Berth(dock);heading=dock<0?0:Mathf.PI;
            game.State.SwitchRide(Ride.Walk);game.State.Place(BoardPoint(),0);game.RefreshPlayer();
            moored=Models.Vehicle(game.transform,Ride.Yacht,YachtIndex);moored.Root.position=new Vector3(saved.X,saved.Y,saved.Z);moored.Root.rotation=Quaternion.Euler(0,heading*Mathf.Rad2Deg,0);
            mooredCollision=new Obstacle(saved.X,saved.Z,11,48,15);game.State.Obstacles.Add(mooredCollision);
            game.State.Say(dock<0?"Welcome back to Palm Cove. Your yacht is moored here.":"Welcome to "+IslandLayout.Names[dock]+"! Your yacht will wait at this dock.");return true;
        }
        public bool LeaveCabin(){if(!InCabin)return false;Begin(YachtIndex);return true;}
        public void Reset()
        {
            RemoveMoored();InCabin=false;DockedIsland=-2;YachtIndex=-1;
            foreach(var collider in marina.GetComponentsInChildren<BoxCollider>(true))if(collider.size.z>60&&collider.size.x>20)collider.enabled=true;
            foreach(string name in names){var r=marina.Find(name.ToUpperInvariant()+" superyacht");if(r!=null)r.gameObject.SetActive(true);}
        }
        void RemoveMoored(){game.State.Obstacles.Remove(mooredCollision);if(moored!=null){moored.Root.gameObject.SetActive(false);Object.Destroy(moored.Root.gameObject);moored=null;}}
        public void Tick()
        {
            if(moored!=null)moored.Root.gameObject.SetActive(game.State.WorldIndex==1&&!game.State.Indoors);
            if(game.State.Mode==Ride.Yacht){saved=game.State.Position;heading=game.State.Yaw;}
        }
        public static void BuildVehicle(VehicleModel boat)
        {
            int i=Mathf.Clamp(boat.CarStyle,0,2);YachtDesign.Exterior(boat.Root,Vector3.zero,names[i].ToUpperInvariant(),104,i==0?Art.Blue:i==1?Art.Teal:Art.Yellow);
            // The captain occupies the full-height bridge, with a real console and foot support.
            boat.Seat=new Vector3(0,9.55f,15);boat.Grips[0]=new Vector3(-.24f,9.93f,15.58f);boat.Grips[1]=new Vector3(.24f,9.93f,15.58f);
            Art.Box("Bridge helm seat",boat.Root,new Vector3(0,9.38f,15),new Vector3(1.2f,.2f,1.2f),YachtDesign.Linen,.10f);
            Art.Box("Bridge helm console",boat.Root,new Vector3(0,9.47f,16),new Vector3(1.9f,.8f,.45f),YachtDesign.Ink,.08f);
            foreach(int side in new[]{-1,1})boat.Pedals.Add(Art.Group("Captain foot rest",boat.Root,new Vector3(side*.16f,9.03f,15.55f)));
            boat.Root.gameObject.AddComponent<YachtWake>();
        }
    }
    public sealed class YachtWake:MonoBehaviour
    {
        Transform[] foam=new Transform[16];Vector3 previous;
        void Start(){previous=transform.position;for(int i=0;i<foam.Length;i++)foam[i]=Art.Ball("Yacht wake",transform,Vector3.zero,new Vector3(1,.10f,5),Art.White).transform;}
        void LateUpdate(){float speed=Vector3.Distance(previous,transform.position)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;for(int i=0;i<foam.Length;i++){foam[i].gameObject.SetActive(speed>.2f);float d=i/2*5;foam[i].position=transform.TransformPoint(new Vector3((i%2==0?-1:1)*(7+d*.22f),.06f,-53-d));foam[i].rotation=transform.rotation;}}
    }
}
