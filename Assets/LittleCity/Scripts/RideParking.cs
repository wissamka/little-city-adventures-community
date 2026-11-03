using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class ParkedRide
    {
        public VehicleModel Model;
        public int World;
        public string Name;
        public Obstacle Collision;
        public List<Point> Dents=new List<Point>();
    }
    public sealed class RideParking
    {
        readonly CityGame game;
        public readonly List<ParkedRide> Rides=new List<ParkedRide>();
        public ParkedRide Occupied { get; private set; }
        bool show=true;
        public RideParking(CityGame owner){game=owner;}
        public void Register(VehicleModel model,int world,string name)
        {
            model.Root.gameObject.AddComponent<MovingScenery>();
            Rides.Add(new ParkedRide{Model=model,World=world,Name=name});
        }
        public ParkedRide Nearest
        {
            get
            {
                if(game.State.Mode!=Ride.Walk || game.State.Indoors || game.State.Resting!=RestPose.None)return null;
                Vector3 p=new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z);
                ParkedRide result=null;float best=2f;
                foreach(var ride in Rides)
                {
                    if(ride.World!=game.State.WorldIndex || ride==Occupied || !ride.Model.Root.gameObject.activeInHierarchy)continue;
                    // Board from beside the hull/body, including planes and boats
                    // whose center is unreachable behind their own collision box.
                    Vector3 center=ride.Model.Root.position;
                    float dx=Mathf.Max(0,Mathf.Abs(p.x-center.x)-ride.Collision.HalfX);
                    float dz=Mathf.Max(0,Mathf.Abs(p.z-center.z)-ride.Collision.HalfZ);
                    if(ride.Model.Mode==Ride.Van||ride.Model.Mode==Ride.Camper){var local=ride.Model.Root.InverseTransformPoint(p);dx=Mathf.Max(0,Mathf.Abs(local.x)-ride.Collision.HalfX);dz=Mathf.Max(0,Mathf.Abs(local.z)-ride.Collision.HalfZ);}
                    float distance=new Vector3(dx,p.y-center.y,dz).magnitude;
                    if(distance<best){best=distance;result=ride;}
                }
                return result;
            }
        }
        public string Context => game.State.Mode==Ride.Yacht?"":game.State.Mode!=Ride.Walk ? (game.State.Airborne?(game.State.UsesRampPhysics?"Land, then stop to step out":"E / UP climb · Q / DOWN descend"):Mathf.Abs(game.State.Speed)>.8f?"Slow down to exit with G":"G • Step out") : Nearest!=null?"G • Enter "+Nearest.Name:"";
        public void RemoveWorld(int index)
        {
            for(int i=Rides.Count-1;i>=0;i--)
            {
                var ride=Rides[i];if(ride.World!=index)continue;
                game.State.Obstacles.Remove(ride.Collision);if(Occupied==ride)Occupied=null;
                if(ride.Model.Root!=null){ride.Model.Root.gameObject.SetActive(false);Object.Destroy(ride.Model.Root.gameObject);}
                Rides.RemoveAt(i);
            }
        }
        public void Show(bool visible){show=visible;RefreshVisibility();}
        public void RefreshVisibility()
        {
            foreach(var ride in Rides)
            {
                game.State.Obstacles.Remove(ride.Collision);
                bool visible=show && ride.World==game.State.WorldIndex && ride!=Occupied;
                ride.Model.Root.gameObject.SetActive(visible);
                if(visible)
                {
                    Vector3 p=ride.Model.Root.position;
                    float hx=ride.Model.Mode==Ride.Motorboat || ride.Model.Mode==Ride.Speedboat?1.65f:ride.Model.Mode==Ride.RaceCar?1.25f:ride.Model.Mode==Ride.Airplane?3.8f:.45f;
                    float hz=ride.Model.Mode==Ride.Motorboat || ride.Model.Mode==Ride.Speedboat?3.5f:ride.Model.Mode==Ride.RaceCar?2.1f:ride.Model.Mode==Ride.Airplane?3.2f:1.2f;
                    if(Mathf.Abs(ride.Model.Root.forward.x)>.7f){float tmp=hx;hx=hz;hz=tmp;}
                    bool large=ride.Model.Mode==Ride.Van||ride.Model.Mode==Ride.Camper;
                    if(large){hx=ride.Model.Mode==Ride.Camper?1.76f:1.56f;hz=ride.Model.Mode==Ride.Camper?4.1f:3.4f;}
                    if(ride.Model.Mode==Ride.Helicopter){hx=hz=6.3f;}
                    ride.Collision=new Obstacle(p.x,p.z,hx,hz,ride.Model.Mode==Ride.Helicopter?4.0f:large?3.45f:ride.Model.Mode==Ride.Airplane?2.8f:1.7f,-1000,large?ride.Model.Root.eulerAngles.y*Mathf.Deg2Rad:0);
                    game.State.Obstacles.Add(ride.Collision);
                }
            }
        }
        public bool EnterOrExit()
        {
            if(game.State.Mode==Ride.Yacht)return game.Worlds.Voyages.Use();
            if(game.State.Mode==Ride.Walk)
            {
                var target=Nearest;if(target==null)return false;
                Vector3 p=target.Model.Root.position;
                Occupied=target;RefreshVisibility();
                bool entered=game.State.EnterVehicle(target.Model.Mode,target.Model.CarStyle,new Point(p.x,p.y,p.z),target.Model.Root.eulerAngles.y*Mathf.Deg2Rad);
                if(entered&&target.Model.Mode==Ride.RaceCar)game.State.RestoreCarDamage(target.Dents);
                game.RefreshPlayer();return entered;
            }
            if(game.State.Airborne || Mathf.Abs(game.State.Speed)>.8f){game.State.Say("Stop on the ground before stepping out.");return false;}
            if(Occupied==null)
            {
                var model=Models.Vehicle(game.transform,game.State.Mode,game.State.CarStyle);
                Register(model,game.State.WorldIndex,Simulation.RideNames[(int)game.State.Mode]);Occupied=Rides[Rides.Count-1];
            }
            Occupied.Dents.Clear();if(game.State.Mode==Ride.RaceCar)foreach(var dent in game.State.CarDents)Occupied.Dents.Add(dent);
            Occupied.Model.Root.GetComponent<CarDamageVisual>()?.Set(Occupied.Dents);
            Point position=game.State.Position;
            Occupied.Model.Root.position=new Vector3(position.X,position.Y,position.Z);
            Occupied.Model.Root.rotation=Quaternion.Euler(0,game.State.Yaw*Mathf.Rad2Deg,0);
            var parked=Occupied;Occupied=null;RefreshVisibility();
            bool exited=game.State.ExitVehicle();
            if(exited&&parked.Model.Mode==Ride.Camper)
            {
                var door=CamperLife.Door(parked);var point=new Point(door.x,door.y,door.z);
                if(!game.State.Blocked(point,.5f))game.State.Place(point,parked.Model.Root.eulerAngles.y*Mathf.Deg2Rad-Mathf.PI/2);
                game.State.Say("F at the side door explores your RV. G gets back behind the wheel.");
            }
            game.RefreshPlayer();return exited;
        }
        public void ReleaseForChange(){Occupied=null;RefreshVisibility();}
        public void QuickChange()
        {
            Occupied=null;
            if(game.State.Mode!=Ride.Walk)
                foreach(var ride in Rides)
                {
                    Vector3 p=ride.Model.Root.position;
                    if(ride.World==game.State.WorldIndex && ride.Model.Mode==game.State.Mode && Vector2.Distance(new Vector2(p.x,p.z),new Vector2(game.State.Position.X,game.State.Position.Z))<6){Occupied=ride;break;}
                }
            RefreshVisibility();
        }
    }
}
