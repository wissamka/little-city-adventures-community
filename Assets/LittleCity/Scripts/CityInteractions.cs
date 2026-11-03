using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class MovingScenery : MonoBehaviour { }
    public sealed class DoorSite
    {
        public string Name;
        public Transform Hinge;
        public Vector3 Entrance;
        public bool Garage;
        public ExplorableBuilding Building;
    }
    public sealed class CityInteractions
    {
        readonly CityGame game;
        Transform outdoor;
        List<DoorSite> doors;
        public void SetExterior(Transform root,List<DoorSite> sites){outdoor=root;doors=sites;}
        Transform house, garage;
        readonly List<Obstacle> furniture = new List<Obstacle>();
        bool busy;
        int visit;
        public bool Busy => busy;
        public bool InGarage { get; private set; }
        public DoorSite NearestDoor
        {
            get
            {
                Point p = game.State.Position;
                DoorSite nearest = null; float distance = 3.4f;
                foreach (var door in doors)
                {
                    float d = Vector2.Distance(new Vector2(p.X,p.Z), new Vector2(door.Entrance.x,door.Entrance.z));
                    if (d < distance) { nearest=door; distance=d; }
                }
                return nearest;
            }
        }
        public string Context
        {
            get
            {
                if(game.Buildings!=null && game.Buildings.Inside)return game.Buildings.Context;
                if (busy) return "Opening the door…";
                if (game.State.Mode==Ride.RaceCar && game.State.CarCrashes>0) return "F • Repair car";
                if (game.State.Mode != Ride.Walk && !game.State.CanRideInside) return "";
                if (game.State.Indoors) return game.State.Position.Z < -175 && Mathf.Abs(game.State.Position.X) < 3 ? "F • Open door & go outside" : "Explore inside • The exit is by the striped mat";
                var door=NearestDoor; return door == null ? "" : "F • Open & enter " + door.Name;
            }
        }
        public CityInteractions(CityGame owner, List<DoorSite> sites, Transform world)
        {
            game=owner; doors=sites; outdoor=world;
            house=Room("Explorable home", false); garage=Room("Airfield car collection", true);
            house.gameObject.SetActive(false); garage.gameObject.SetActive(false);
        }
        public IEnumerator Use()
        {
            if (busy || game.Hud.MenuOpen) yield break;
            if (game.State.Mode==Ride.RaceCar && game.State.CarCrashes>0) { game.RepairCar(); yield break; }
            if (game.State.Mode != Ride.Walk && !game.State.CanRideInside) yield break;
            if (game.State.Indoors)
            {
                if (game.State.Position.Z >= -175 || Mathf.Abs(game.State.Position.X) >= 3) yield break;
                Leave(); yield break;
            }
            DoorSite door=NearestDoor; if (door==null) yield break;
            if(door.Building!=null){game.Buildings.Enter(door.Building);yield break;}
            if(game.State.CanRideInside){game.State.Say("Ride into homes with an open front door. This garage is for walking.");yield break;}
            busy=true; int thisVisit=++visit; Vector3 original=door.Hinge.localPosition;
            for (float t=0; t<.45f; t+=Time.unscaledDeltaTime)
            {
                if(thisVisit!=visit || game.State.Mode!=Ride.Walk){door.Hinge.localPosition=original;door.Hinge.localRotation=Quaternion.identity;busy=false;yield break;}
                float q=Mathf.SmoothStep(0,1,t/.45f);
                if (door.Garage) door.Hinge.localPosition=original+Vector3.up*q*6;
                else door.Hinge.localRotation=Quaternion.Euler(0,-100*q,0);
                yield return null;
            }
            InGarage=door.Garage; game.State.EnterInterior(); outdoor.gameObject.SetActive(false); game.ShowOutdoorVehicles(false);
            game.State.InteriorObstacles.Clear();
            if (!InGarage) game.State.InteriorObstacles.AddRange(furniture);
            else for(int i=0;i<3;i++) game.State.InteriorObstacles.Add(new Obstacle(-5+i*5,-171,1.35f,2.2f,1.8f));
            house.gameObject.SetActive(!InGarage); garage.gameObject.SetActive(InGarage);
            door.Hinge.localPosition=original; door.Hinge.localRotation=Quaternion.identity;
            game.Hud.ClearInput(); game.SnapCamera(); busy=false;
        }
        public void Leave()
        {
            if(game.Buildings!=null && game.Buildings.Inside)game.Buildings.Leave();
            visit++;busy=false;
            house.gameObject.SetActive(false); garage.gameObject.SetActive(false); outdoor.gameObject.SetActive(true); game.ShowOutdoorVehicles(true);
            if(game.State.Indoors) game.State.ExitInterior();
            InGarage=false; game.Hud.ClearInput(); game.SnapCamera();
        }
        Transform Room(string name,bool isGarage)
        {
            var r=Art.Group(name,game.transform,new Vector3(0,0,-173.5f));
            Art.Box("Interior floor",r,new Vector3(0,-.10f,0),new Vector3(18,.2f,14),Art.Hex(isGarage?"ABBFC9":"DFBE83"),0);
            for(int x=-8;x<=8;x+=2) for(int z=-6;z<=6;z+=2)
                Art.Box(isGarage?"Garage tile":"Wood floorboard",r,new Vector3(x,.008f,z),new Vector3(isGarage?1.96f:.035f,.01f,1.96f),Art.Hex(isGarage?((x+z)%4==0?"DAE5E7":"BACBD1"):"C59F68"),0);
            Wall(r,new Vector3(-9,2.7f,0),new Vector3(.3f,5.4f,14),Art.Hex("D5EBE3"));
            Wall(r,new Vector3(9,2.7f,0),new Vector3(.3f,5.4f,14),Art.Hex("D5EBE3"));
            Wall(r,new Vector3(0,2.7f,6.5f),new Vector3(18,5.4f,.3f),Art.Hex("FFF0D4"));
            foreach(int side in new[]{-1,1}) Wall(r,new Vector3(side*5,1.6f,-6.5f),new Vector3(7.6f,3.2f,.3f),Art.Hex("FFF0D4"));
            Art.Box("Exit door",r,new Vector3(0,1.35f,-6.49f),new Vector3(2.35f,2.7f,.14f),Art.Teal,.14f);
            Art.Ball("Exit handle",r,new Vector3(.8f,1.2f,-6.3f),Vector3.one*.13f,Art.Yellow);
            Art.Box("Striped exit mat",r,new Vector3(0,.015f,-5.6f),new Vector3(2.4f,.02f,1.1f),Art.Blue);
            for(int i=-2;i<=2;i++)Art.Box("Mat stripe",r,new Vector3(i*.4f,.03f,-5.6f),new Vector3(.16f,.01f,1),Art.White,0);
            foreach(int side in new[]{-1,1})
            {
                Art.Box("Window recess",r,new Vector3(side*5.5f,1.9f,6.3f),new Vector3(2.9f,1.65f,.13f),Art.Blue,.12f);
                Art.Box("Sunlit window",r,new Vector3(side*5.5f,1.9f,6.20f),new Vector3(2.6f,1.4f,.04f),Art.Hex("9BDBE4"),0);
                Art.Box("Window mullion",r,new Vector3(side*5.5f,1.9f,6.13f),new Vector3(.1f,1.45f,.05f),Art.White,0);
            }
            if(isGarage)
            {
                for(int i=0;i<3;i++)
                {
                    Art.Disk("Car display pad",r,new Vector3(-5+i*5,.04f,2.5f),2.3f,.06f,Art.White);
                    var car=Models.Vehicle(r,Ride.RaceCar,i); car.Root.localPosition=new Vector3(-5+i*5,.1f,2.5f);car.Root.localRotation=Quaternion.Euler(0,180,0);
                }
                Art.Box("Mechanic tool cabinet",r,new Vector3(7.7f,.7f,5.5f),new Vector3(1.7f,1.4f,1),Art.Red);
                for(int i=0;i<4;i++)Art.Box("Tool drawer",r,new Vector3(7.7f,.3f+i*.29f,4.98f),new Vector3(1.4f,.06f,.04f),Art.White,0);
            }
            else
            {
                Furnish(r,"Sofa",new Vector3(-5,.42f,2.6f),new Vector3(4.2f,.85f,1.5f),Art.Teal);
                Art.Box("Soft sofa back",r,new Vector3(-5,1.06f,3.2f),new Vector3(4.3f,1.1f,.5f),Art.Teal,.23f);
                foreach(int side in new[]{-1,1})Art.Ball("Sofa cushion",r,new Vector3(-5+side*.9f,.91f,2.6f),new Vector3(1.4f,.3f,.98f),Art.Hex("7ED4BD"));
                Art.Box("Woven rug",r,new Vector3(-4,.02f,0),new Vector3(5.5f,.025f,3.3f),Art.Hex("E3AB6B"));
                Furnish(r,"Coffee table",new Vector3(-4,.46f,0),new Vector3(2,.9f,1),Art.White);
                Art.Box("Story book",r,new Vector3(-4,.94f,0),new Vector3(.55f,.08f,.4f),Art.Blue);
                Furnish(r,"Kitchen counter",new Vector3(5,.6f,5.3f),new Vector3(5.4f,1.2f,1.3f),Art.Hex("F4C86C"));
                Art.Box("Kitchen worktop",r,new Vector3(5,1.23f,5.3f),new Vector3(5.6f,.14f,1.45f),Art.White);
                for(int i=0;i<3;i++)Art.Ball("Fruit bowl",r,new Vector3(3.6f+i*.18f,1.42f,5.1f),Vector3.one*.23f,i==1?Art.Red:Art.Yellow);
                Furnish(r,"Dining table",new Vector3(4,.53f,.2f),new Vector3(2.5f,1.06f,1.6f),Art.Hex("B5845E"));
                foreach(int side in new[]{-1,1})Furnish(r,"Dining chair",new Vector3(4+side*1.9f,.45f,.2f),new Vector3(.8f,.9f,.8f),Art.Blue);
                Furnish(r,"Bookcase",new Vector3(-7.8f,1.1f,5.5f),new Vector3(1.5f,2.2f,.6f),Art.White);
                for(int row=0;row<3;row++)for(int col=0;col<5;col++)Art.Box("Colorful book",r,new Vector3(-8.3f+col*.24f,.4f+row*.63f,5.12f),new Vector3(.18f,.48f,.32f),col%2==0?Art.Teal:Art.Red,.02f);
                Art.Ball("Indoor leafy plant",r,new Vector3(7.5f,1.2f,-3.4f),new Vector3(1.2f,1.6f,1.2f),Art.Green);
                Furnish(r,"Plant pot",new Vector3(7.5f,.3f,-3.4f),new Vector3(.7f,.6f,.7f),Art.White);
            }
            Art.Box("Ceiling",r,new Vector3(0,5.5f,0),new Vector3(18,.2f,14),Art.White,0);
            for(int side=-1;side<=1;side+=2)
            {
                Art.Disk("Ceiling light",r,new Vector3(side*4,5.34f,0),.65f,.08f,Art.White);
                var lamp=new GameObject("Warm indoor light").AddComponent<Light>();lamp.transform.SetParent(r,false);lamp.transform.localPosition=new Vector3(side*4,3.5f,-1);lamp.type=LightType.Point;lamp.range=12;lamp.intensity=1.4f;lamp.color=Art.Hex("FFF1D4");lamp.shadows=LightShadows.None;
            }
            Art.CombineStatic(r); return r;
        }
        void Wall(Transform r,Vector3 position,Vector3 size,Color color)
        {
            var wall=Art.Box("Interior wall",r,position,size,color,.08f);wall.layer=8;
            var c=wall.AddComponent<BoxCollider>();c.size=size;
        }
        void Furnish(Transform r,string name,Vector3 p,Vector3 size,Color color)
        {
            Art.Box(name,r,p,size,color,.15f);
            furniture.Add(new Obstacle(p.x,p.z-173.5f,size.x*.5f,size.z*.5f,p.y+size.y*.5f));
        }
    }
}
