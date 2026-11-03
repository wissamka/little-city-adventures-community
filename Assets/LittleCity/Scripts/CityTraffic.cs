using System.Collections.Generic;
using UnityEngine;
namespace LittleCity {
 public sealed class TaxiLoop {
  readonly float left,right,bottom,top,radius;
  public float Length{get;private set;}
  public TaxiLoop(float x0,float x1,float z0,float z1,float r=5.2f){left=x0;right=x1;bottom=z0;top=z1;radius=r;Length=2*(x1-x0+z1-z0-4*r)+2*Mathf.PI*r;}
  public void Sample(float distance,out Vector3 p,out Vector3 forward) {
   float d=Mathf.Repeat(distance,Length),w=right-left-2*radius,h=top-bottom-2*radius,arc=Mathf.PI*.5f*radius;
   for(int segment=0;segment<8;segment++) {
    float len=segment%2==1?arc:segment%4==0?w:h;
    if(d>len){d-=len;continue;}
    if(segment%2==0) {
     forward=segment==0?Vector3.right:segment==2?Vector3.forward:segment==4?Vector3.left:Vector3.back;
     p=segment==0?new Vector3(left+radius,0,bottom):segment==2?new Vector3(right,0,bottom+radius):segment==4?new Vector3(right-radius,0,top):new Vector3(left,0,top-radius);p+=forward*d;return;
    }
    float angle=(segment-3)*Mathf.PI*.25f+d/radius;
    Vector3 c=segment==1?new Vector3(right-radius,0,bottom+radius):segment==3?new Vector3(right-radius,0,top-radius):segment==5?new Vector3(left+radius,0,top-radius):new Vector3(left+radius,0,bottom+radius);
    p=c+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;forward=new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle));return;
   }
   p=new Vector3(left+radius,0,bottom);forward=Vector3.right;
  }
 }
 public sealed class TaxiFleet:MonoBehaviour {
  public sealed class Cab {public VehicleModel Model;public TaxiLoop Route;public float Distance,Speed,Travelled;public Vector3 Forward;}
  public readonly List<Cab> Cabs=new List<Cab>();CityGame game;WorldPlace place;
  public static TaxiFleet Build(CityGame owner,WorldPlace world) {
   var fleet=world.Root.gameObject.AddComponent<TaxiFleet>();fleet.game=owner;fleet.place=world;
   var west=new TaxiLoop(-172.5f,-77.5f,-122.5f,86.5f);var east=new TaxiLoop(52.5f,102.5f,-122.5f,137.5f);
   for(int i=0;i<4;i++) {
    var route=i<3?west:east;var model=Models.Vehicle(world.Root,Ride.RaceCar,1);model.Root.name="Driving yellow New York taxi "+(i+1);Sign(model.Root);
    var cab=new Cab{Model=model,Route=route,Distance=i<3?i*west.Length/3:30};fleet.Cabs.Add(cab);fleet.Pose(cab);
   }
   return fleet;
  }
  static void Sign(Transform parent) {
   var sign=Art.Group("TAXI roof sign",parent);Art.Box("Yellow illuminated taxi sign",sign,new Vector3(0,1.72f,-.2f),new Vector3(1.12f,.3f,.44f),Art.Yellow,.035f);
   foreach(int face in new[]{-1,1}) {
    float z=-.2f+face*.226f;
    System.Action<float,float,float,float> line=(x,y,a,b)=>Art.Tube("Taxi lettering",sign,new Vector3(-face*x,y,z),new Vector3(-face*a,b,z),.011f,Art.Hex("17232C"));
    line(-.43f,1.81f,-.25f,1.81f);line(-.34f,1.81f,-.34f,1.63f);
    line(-.18f,1.63f,-.10f,1.81f);line(-.10f,1.81f,-.02f,1.63f);line(-.15f,1.69f,-.05f,1.69f);
    line(.06f,1.63f,.22f,1.81f);line(.06f,1.81f,.22f,1.63f);line(.35f,1.63f,.35f,1.81f);
   }
  }
  void Update(){if(game!=null&&game.Ready&&!game.Hud.MenuOpen&&Application.isFocused)Step(Mathf.Min(Time.deltaTime,.05f));}
  public void Step(float dt) {
   if(game.State.WorldIndex!=3||game.Worlds.ActivePlace!=place)return;
   game.State.TrafficObstruction=BlocksPlayer;
   foreach(var cab in Cabs) {
    float look=4.5f+cab.Speed*cab.Speed/14;bool blocked=false;
    for(float d=0;d<=look;d+=1.5f){cab.Route.Sample(cab.Distance+d,out var p,out var f);if(Obstructed(cab,p,f)){blocked=true;break;}}
    cab.Speed=Mathf.MoveTowards(cab.Speed,blocked?0:6,dt*(blocked?9:2.8f));float step=cab.Speed*dt;
    cab.Route.Sample(cab.Distance+step,out var next,out var forward);
    if(Obstructed(cab,next,forward)){cab.Speed=0;step=0;}
    cab.Distance=Mathf.Repeat(cab.Distance+step,cab.Route.Length);cab.Travelled+=step;Pose(cab);cab.Model.Animate(cab.Speed,dt,cab.Speed>0?1:0);
   }
  }
  void Pose(Cab cab){cab.Route.Sample(cab.Distance,out var p,out var f);cab.Model.Root.localPosition=p;cab.Model.Root.localRotation=Quaternion.LookRotation(f);cab.Forward=f;}
  bool Obstructed(Cab cab,Vector3 p,Vector3 f) {
   var state=game.State;
   if(!state.Indoors&&state.Position.Y<2.4f&&state.IsLargeVehicle&&state.LargeVehicleOverlaps(state.Position,new Obstacle(p.x,p.z,1.5f,2.55f,2.4f,-1000,Mathf.Atan2(f.x,f.z))))return true;
   if(!state.Indoors&&state.Position.Y<2.4f&&Overlaps(p,f,new Vector3(state.Position.X,0,state.Position.Z),state.Mode==Ride.Walk?.8f:2.7f))return true;
   foreach(var other in Cabs)if(other!=cab&&Vector3.Distance(p,other.Model.Root.localPosition)<5.8f)return true;
   float hx=Mathf.Abs(f.x)*2.25f+Mathf.Abs(f.z)*1.2f,hz=Mathf.Abs(f.z)*2.25f+Mathf.Abs(f.x)*1.2f;
   foreach(var obstacle in state.Obstacles){float c=Mathf.Abs(Mathf.Cos(obstacle.Yaw)),s=Mathf.Abs(Mathf.Sin(obstacle.Yaw));if(obstacle.Bottom<1.6f&&obstacle.Height>.15f&&Mathf.Abs(p.x-obstacle.X)<hx+c*obstacle.HalfX+s*obstacle.HalfZ&&Mathf.Abs(p.z-obstacle.Z)<hz+s*obstacle.HalfX+c*obstacle.HalfZ)return true;}
   return false;
  }
  public bool BlocksPlayer(Point point,float radius) {
   if(!isActiveAndEnabled||game.State.WorldIndex!=3||game.Worlds.ActivePlace!=place||point.Y>2)return false;
   var p=new Vector3(point.X,0,point.Z);foreach(var cab in Cabs)
   {
    var center=cab.Model.Root.localPosition;
    if(game.State.IsLargeVehicle&&radius>1){if(game.State.LargeVehicleOverlaps(point,new Obstacle(center.x,center.z,1.2f,2.25f,2.4f,-1000,Mathf.Atan2(cab.Forward.x,cab.Forward.z))))return true;}
    else if(Overlaps(center,cab.Forward,p,radius))return true;
   }return false;
  }
  static bool Overlaps(Vector3 car,Vector3 f,Vector3 point,float radius){var d=point-car;float x=Mathf.Abs(Vector3.Dot(d,new Vector3(f.z,0,-f.x))),z=Mathf.Abs(Vector3.Dot(d,f));return Mathf.Pow(Mathf.Max(0,x-1.2f),2)+Mathf.Pow(Mathf.Max(0,z-2.25f),2)<radius*radius;}
  void OnDisable(){if(game!=null&&game.State.TrafficObstruction==BlocksPlayer)game.State.TrafficObstruction=null;}
 }
}
