using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LittleCity
{
    public sealed class WorldPlace
    {
        public Transform Root;
        public readonly List<Obstacle> Collisions=new List<Obstacle>();
        public readonly List<DoorSite> Doors=new List<DoorSite>();
    }
    public sealed class AdventureWorlds
    {
        readonly CityGame game;
        readonly WorldPlace[] places=new WorldPlace[5];
        public WorldRoute Route { get; private set; }
        public long CurrentStop { get; private set; }
        public bool Busy { get; private set; }
        public WorldPlace ActivePlace => places[game.State.WorldIndex];
        public int LoadedGeneratedWorlds => places[3]==null?0:1;
        const string RouteSave="LittleCity.Endless.Route.v1";
        public string RouteCaption => CurrentStop>0 && game.State.WorldIndex==3 ? "STOP "+CurrentStop+" · NEXT: "+WorldRoute.ThemeNames[(int)(CurrentStop%4)] : "SKY ROUTE · JUNGLE → NEW YORK → DINOSAURS → ASGARD";
        public YachtLife Yachts { get; private set; }
        public YachtVoyages Voyages {get;private set;}
        public IslandActivities Islands {get;private set;}
        public AdventureWorlds(CityGame owner,CityWorld original)
        {
            game=owner;Route=Application.isBatchMode?new WorldRoute():WorldRoute.Restore(PlayerPrefs.GetString(RouteSave,""));places[0]=new WorldPlace{Root=original.Root};places[0].Collisions.AddRange(game.State.Obstacles);places[0].Doors.AddRange(original.Doors);
            places[1]=BuildDestination(1);places[2]=BuildDestination(2);
            places[1].Root.gameObject.SetActive(false);places[2].Root.gameObject.SetActive(false);
            MarineAdventure.BuildMarina(game,places[1]);Yachts=new YachtLife(game);Voyages=new YachtVoyages(game,places[1].Root);Islands=new IslandActivities(game,places[1]);
            for(int i=0;i<3;i++)AddGates(places[i].Root,i==0?"JUNGLE · START ROUTE":"JUNGLE · SKY ROUTE",false);
        }
        public void Travel(int destination)
        {
            if(Busy || !game.State.CanTravel || destination==game.State.WorldIndex)return;
            if(destination<0 || destination>4 || destination==3)return;
            if(destination==4 && places[4]==null)places[4]=SkatePark.Build(game);
            places[game.State.WorldIndex].Root.gameObject.SetActive(false);
            game.State.OutdoorGround=destination==4?SkatePark.Ground:null;game.State.TravelTo(destination);game.Hero?.Disable();var world=places[destination];world.Root.gameObject.SetActive(true);
            game.State.Obstacles.Clear();game.State.Obstacles.AddRange(world.Collisions);
            game.State.PondEnabled=destination==0;game.State.HarborWater=destination==1;
            game.Interactions.SetExterior(world.Root,world.Doors);
            RenderSettings.fogColor=Art.Hex(destination==4?"83A9B0":destination==2?"CADFEF":destination==1?"A2DCE5":"7FADB7");Camera.main.backgroundColor=RenderSettings.fogColor;
            game.Parking.QuickChange();game.RefreshPlayer();game.Hud.ShowTravel(false);
        }
        public void VisitWorld(int destination)
        {
            if(Busy || !game.Ready || destination<0 || destination>4 || destination==3)return;
            if(destination==4 && places[4]==null)places[4]=SkatePark.Build(game);
            game.ResetHome();places[game.State.WorldIndex].Root.gameObject.SetActive(false);
            game.State.OutdoorGround=destination==4?SkatePark.Ground:null;game.State.ArriveOnFoot(destination);game.Hero?.Disable();var world=places[destination];world.Root.gameObject.SetActive(true);
            game.State.Obstacles.Clear();game.State.Obstacles.AddRange(world.Collisions);game.Interactions.SetExterior(world.Root,world.Doors);
            RenderSettings.fogColor=Art.Hex(destination==4?"83A9B0":destination==2?"CADFEF":destination==1?"A2DCE5":"7FADB7");Camera.main.backgroundColor=RenderSettings.fogColor;
            game.Parking.QuickChange();game.Hud.BeginExploring();game.RefreshPlayer();
        }
        public void CheckFlight(Point before)
        {
            if(Busy || !game.State.CanTravel || game.Hud.MenuOpen)return;
            if(WorldRoute.CrossedGate(before,game.State.Position,true))BeginRoute(game.State.WorldIndex==3?CurrentStop+1:1,false);
            else if(game.State.WorldIndex==3 && WorldRoute.CrossedGate(before,game.State.Position,false))
            {
                if(CurrentStop<=1){Travel(0);game.State.FinishFlightArrival(true);game.State.Say("Back in Little City. Your route is saved. T to return to it.");game.RefreshPlayer();}
                else BeginRoute(CurrentStop-1,true);
            }
        }
        public void VisitMarina(int stop)
        {
            if(Busy || !game.Ready)return;
            game.ResetHome();places[game.State.WorldIndex].Root.gameObject.SetActive(false);
            Vector3 p=stop==0?new Vector3(-65,0,58):stop==1?new Vector3(23,0,46):new Vector3(35,-1.05f,102);
            game.State.OutdoorGround=null;game.State.ArriveMarina(new Point(p.x,p.y,p.z));var w=places[1];w.Root.gameObject.SetActive(true);game.State.Obstacles.Clear();game.State.Obstacles.AddRange(w.Collisions);game.Interactions.SetExterior(w.Root,w.Doors);
            RenderSettings.fogColor=Art.Hex("A2DCE5");Camera.main.backgroundColor=RenderSettings.fogColor;game.Parking.QuickChange();game.Hud.BeginExploring();game.RefreshPlayer();
        }
        public void ResumeRoute()
        {
            if(Busy || !game.Ready)return;
            game.ResetHome();game.State.SwitchRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Parking.QuickChange();game.RefreshPlayer();
            BeginRoute(System.Math.Max(1,Route.LastVisit),false);
        }
        public void BeginRoute(long number,bool returning=false)
        {
            if(Busy || !game.State.CanTravel || number<1)return;
            game.StartCoroutine(GenerateAndTravel(number,returning));
        }
        IEnumerator GenerateAndTravel(long number,bool returning)
        {
            Busy=true;var descriptor=Route.Describe(number);game.Hud.ShowJourney(true,"Flying to stop "+number+"\n"+descriptor.Name);yield return null;
            places[game.State.WorldIndex].Root.gameObject.SetActive(false);
            game.Parking.RemoveWorld(3);
            if(places[3]!=null){Object.Destroy(places[3].Root.gameObject);places[3]=null;}
            yield return null;
            var world=ProceduralWorlds.Build(game,descriptor);places[3]=world;
            AddGates(world.Root,"NEXT · "+WorldRoute.ThemeNames[(int)(number%4)].ToUpperInvariant(),true);game.Hero?.SetWorld(world.Root,descriptor.Theme==WorldTheme.Asgard);
            game.State.OutdoorGround=null;game.State.ArriveGenerated(descriptor.Name,(int)descriptor.Theme,returning);CurrentStop=number;
            game.State.PondEnabled=false;game.State.HarborWater=false;
            game.State.Obstacles.Clear();game.State.Obstacles.AddRange(world.Collisions);
            game.Interactions.SetExterior(world.Root,world.Doors);
            RenderSettings.fogColor=Art.Hex(descriptor.Theme==WorldTheme.Asgard?"BCD8E5":descriptor.Theme==WorldTheme.Jungle?"B1DCCA":"ADD8E8");Camera.main.backgroundColor=RenderSettings.fogColor;
            game.Parking.QuickChange();game.RefreshPlayer();Route.Visit(number);
            if(!Application.isBatchMode){PlayerPrefs.SetString(RouteSave,Route.Save());PlayerPrefs.Save();}
            game.State.Say("Stop "+number+": "+descriptor.Name+". Green gate NORTH = next; blue gate SOUTH = return. Land to explore.");
            yield return null;game.Hud.ShowJourney(false,"");Busy=false;
        }
        public static void AddGates(Transform root,string next,bool previous)
        {
            Gate(root,165,next,Art.Teal,0);
            if(previous)Gate(root,-170,"PREVIOUS STOP · RETURN",Art.Blue,180);
        }
        static void Gate(Transform root,float z,string name,Color color,float yaw)
        {
            var r=Art.Group(name,root,new Vector3(172,40,z));
            var ring=Art.Ring("Fly-through sky gate",r,23,1.05f,1.05f,color);ring.transform.localRotation=Quaternion.Euler(0,90,0);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Art.Ball("Gate beacon",r,new Vector3(Mathf.Sin(a)*23,Mathf.Cos(a)*23,0),Vector3.one*1.9f,Art.Yellow);}
            Caption(name,r,new Vector3(0,29,0),yaw+180,33);Caption("FLY THROUGH · 18–62 m",r,new Vector3(0,-27,0),yaw+180,25);
            foreach(int side in new[]{-1,1})Art.Tube("Sky gate marker",root,new Vector3(172+side*25,0,z),new Vector3(172+side*25,10,z),.5f,color);
        }
        public void SetOutdoorVisible(bool visible)
        {
            places[game.State.WorldIndex].Root.gameObject.SetActive(visible);game.ShowOutdoorVehicles(visible);
        }
        WorldPlace BuildDestination(int index)
        {
            bool tropical=index==1;var world=new WorldPlace{Root=Art.Group(Simulation.WorldNames[index],game.transform)};var r=world.Root;
            Color ground=Art.Hex(tropical?"F3D9A0":"ECF4F5");
            Art.Box("Open ocean",r,new Vector3(0,-2,300),new Vector3(2200,2,2400),Art.Hex(tropical?"269CB9":"7DB5D2"),0);
            Art.Box("Explorable island",r,new Vector3(-25,-.2f,tropical?-75:0),new Vector3(410,.4f,tropical?225:405),ground,0);
            Art.Box("Airfield peninsula",r,new Vector3(172,-.2f,-3),new Vector3(80,.4f,400),ground,0);
            Art.Box("Connected promenade",r,new Vector3(35,.02f,-32),new Vector3(340,.04f,10),Art.Hex(tropical?"D2B488":"A6BDCE"),0);
            Art.Box("Route to airfield",r,new Vector3(149,.025f,-64),new Vector3(10,.05f,72),Art.Hex(tropical?"D2B488":"A6BDCE"),0);
            Art.Box("Runway",r,new Vector3(172,.06f,-3),new Vector3(22,.08f,310),Art.Hex("637C8C"),0);
            for(int z=-144;z<145;z+=12)
            {
                Art.Box("Runway stripe",r,new Vector3(172,.11f,z),new Vector3(.5f,.02f,5),Art.White,0);
                foreach(int side in new[]{-1,1})Art.Ball("Runway edge light",r,new Vector3(172+side*11.5f,.3f,z),Vector3.one*.45f,Art.Yellow);
            }
            Caption(Simulation.WorldNames[index].ToUpperInvariant(),r,new Vector3(0,3,-21),180,12);
            var plane=Models.Vehicle(r,Ride.Airplane);plane.Root.localPosition=new Vector3(172,0,-130);game.Parking.Register(plane,index,Simulation.WorldNames[index]+" airplane");
            var bike=Models.Vehicle(r,Ride.Bicycle);bike.Root.localPosition=new Vector3(7,0,-24);game.Parking.Register(bike,index,"green bicycle");
            var car=Models.Vehicle(r,Ride.RaceCar,tropical?1:2);car.Root.localPosition=new Vector3(-8,0,-24);game.Parking.Register(car,index,tropical?"Sunshine Roadster":"Mint Mini");
            if(tropical)
            {
                for(int i=0;i<22;i++)Palm(r,-165+i*14,-8+(i%3)*14);
                for(int i=0;i<12;i++)Palm(r,-160+i*25,-110-i%2*28);
                for(int i=0;i<6;i++)
                {
                    Vector3 p=new Vector3(20+i*15,0,22);
                    Art.Tube("Beach parasol pole",r,p,p+Vector3.up*2.8f,.045f,Art.White);
                    Art.Ball("Colorful beach parasol",r,p+Vector3.up*2.8f,new Vector3(5,.6f,5),i%2==0?Art.Red:Art.Teal);
                    Art.Box("Beach lounger",r,p+new Vector3(0,.45f,1),new Vector3(1,.3f,2.5f),Art.White,.14f);
                }
                foreach(int x in new[]{-65,-130,-5})
                {
                    float end=x==-65?61:66;
                    Art.Box("Marina pier",r,new Vector3(x,.01f,(36+end)/2),new Vector3(4.4f,.12f,end-36),Art.Hex("BC9065"),0);
                    for(int z=38;z<end;z+=2)
                        foreach(int side in new[]{-1,1})Art.Tube("Pier railing",r,new Vector3(x+side*2.1f,.1f,z),new Vector3(x+side*2.1f,1.15f,z),.055f,Art.White);
                    Solid(world,new Vector3(x-2.25f,.65f,(37+end)/2),new Vector3(.12f,1.3f,end-37));
                    Solid(world,new Vector3(x+2.25f,.65f,(37+end)/2),new Vector3(.12f,1.3f,end-37));
                }
                YachtExterior(r,new Vector3(-65,0,115),"AURORA",104,Art.Blue);
                YachtExterior(r,new Vector3(-130,0,116),"SEA STAR",104,Art.Teal);
                YachtExterior(r,new Vector3(-5,0,116),"SUNBEAM",104,Art.Yellow);
                Cabin(world,-20,-70,Art.Teal,"PALM HOUSE");Cabin(world,30,-70,Art.Red,"CORAL HOUSE");
                Caption("MARINA • THREE BOARDABLE YACHTS",r,new Vector3(-65,2,35),180,15);
            }
            else
            {
                for(int i=0;i<13;i++)
                {
                    float x=-190+i*27;
                    Art.Ball("Snow capped mountain",r,new Vector3(x,15,170+i%2*20),new Vector3(64,62+i%3*13,64),Art.Hex("C2D7E2"));
                    Art.Ball("Snow summit",r,new Vector3(x,40+i%3*5,174+i%2*20),new Vector3(38,27,42),Art.White);
                }
                for(int i=0;i<42;i++)Pine(world,-175+(i%14)*23,-125+(i/14)*95);
                Cabin(world,-30,6,Art.Red,"SNOWDROP CABIN");Cabin(world,25,6,Art.Blue,"SKY CABIN");
                Cabin(world,-70,-65,Art.Teal,"PINE LODGE");Cabin(world,65,-65,Art.Hex("F5BF69"),"COCOA HOUSE");
                Art.Disk("Frozen lake",r,new Vector3(-105,.02f,80),25,.03f,Art.Hex("7EC8DF"));
                Solid(world,new Vector3(-105,.35f,80),new Vector3(40,.7f,40));
                Caption("CLOUD PEAKS • EXPLORE THE CABINS",r,new Vector3(0,2.5f,-15),180,20);
            }
            Art.CombineStatic(r);return world;
        }
        public static void Solid(WorldPlace world,Vector3 p,Vector3 size)
        {
            world.Collisions.Add(new Obstacle(p.x,p.z,size.x/2,size.z/2,p.y+size.y/2));
            var obj=new GameObject("Solid scenery",typeof(BoxCollider));obj.transform.SetParent(world.Root,false);obj.transform.localPosition=p;obj.layer=8;obj.GetComponent<BoxCollider>().size=size;
        }
        static void Palm(Transform r,float x,float z)
        {
            Vector3 p=new Vector3(x,0,z);Art.Tube("Palm trunk",r,p,p+new Vector3(.5f,5.5f,0),.22f,Art.Hex("B38A5D"));
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;var leaf=Art.Ball("Palm frond",r,p+new Vector3(Mathf.Sin(a)*1.8f,5.7f,Mathf.Cos(a)*1.8f),new Vector3(1.3f,.38f,5),Art.Hex("45AD78"));leaf.transform.localRotation=Quaternion.Euler(12,a*Mathf.Rad2Deg,0);
            }
        }
        static void Pine(WorldPlace w,float x,float z)
        {
            var r=w.Root;Art.Tube("Pine trunk",r,new Vector3(x,0,z),new Vector3(x,4,z),.23f,Art.Hex("926D53"));
            for(int i=0;i<3;i++)
            {
                Art.Ball("Pine branches",r,new Vector3(x,2.5f+i*1.4f,z),new Vector3(5-i,2.6f,5-i),Art.Hex("468C80"));
                Art.Ball("Soft snow on branches",r,new Vector3(x,3.35f+i*1.4f,z),new Vector3(4.5f-i,.8f,4.5f-i),Art.White);
            }
            Solid(w,new Vector3(x,2,z),new Vector3(.5f,4,.5f));
        }
        public static void Cabin(WorldPlace w,float x,float z,Color color,string name)
        {
            int decor=Mathf.Abs(Mathf.RoundToInt(x*7+z*11));var b=BuildingArchitecture.Build(w.Root,new Vector3(x,0,z),14,11,1,4.4f,180,color,decor,name);w.Doors.Add(BuildingArchitecture.DoorSite(b));Solid(w,new Vector3(x,2.2f,z),new Vector3(14,4.4f,11));
        }
        public static void Caption(string text,Transform r,Vector3 p,float yaw,float width)
        {
            bool gate=text.Contains("FLY THROUGH") || text.Contains("NEXT STOP") || text.Contains("PREVIOUS STOP");
            text=text.Replace(" · F TO ENTER", "").Replace(" • WELCOME ABOARD", "");
            if(text.StartsWith("SPEEDBOAT"))text="Speedboat";else if(text.StartsWith("MOTORBOAT"))text="Motorboat";else if(text.StartsWith("DIVING PLATFORM"))text="Diving platform";
            width=Mathf.Min(width,gate?18:5);
            var obj=new GameObject(text,typeof(RectTransform));
            var label=obj.AddComponent<TextMeshPro>();label.text=text;label.font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");label.fontSharedMaterial=Art.SignMaterial(label.font.material);label.fontSize=10;label.color=Art.Ink;label.fontStyle=FontStyles.Bold;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;label.rectTransform.sizeDelta=new Vector2(200,10);label.ForceMeshUpdate(true);
            Vector2 extent=label.GetPreferredValues(text);float scale=Mathf.Min(width/Mathf.Max(.1f,extent.x),(gate?.8f:.30f)/Mathf.Max(.1f,extent.y));
            obj.transform.SetParent(r,false);obj.transform.localPosition=p;obj.transform.localRotation=Quaternion.Euler(0,yaw+180,0);label.transform.localScale=Vector3.one*scale;obj.AddComponent<WorldLabel>().FarDistance=gate?280:38;
        }
        static void YachtExterior(Transform parent,Vector3 position,string name,float length,Color accent)
        {
            YachtDesign.Exterior(parent,position,name,length,accent);
        }
    }
}
