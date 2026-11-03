using System;
using UnityEngine;
namespace LittleCity
{
    public sealed partial class Simulation
    {
        public float BikeHalfLength=>Mode==Ride.DirtBike?1.58f:Mode==Ride.MountainBike?1.36f:Mode==Ride.MiniBike?.48f:1.2f;
        public bool LargeVehicleOverlaps(Point point,Obstacle obstacle)=>IsLargeVehicle&&BikeOverlap(point,Yaw,RoadRadius,RoadHalfLength,obstacle);
        public void ExitBikeDoor(){InteriorGround=null;InteriorBoundary=null;Resting=RestPose.None;FloorLevel=0;Indoors=false;Say("Outside again. Ride on!");}
        static bool BikeOverlap(Point p,float yaw,float hx,float hz,Obstacle b)
        {
            if(b.Yaw!=0){float bc=(float)Math.Cos(b.Yaw),bs=(float)Math.Sin(b.Yaw),x=p.X-b.X,z=p.Z-b.Z;p=new Point(x*bc-z*bs,p.Y,x*bs+z*bc);yaw-=b.Yaw;b.X=b.Z=0;}
            float c=(float)Math.Cos(yaw),s=(float)Math.Sin(yaw),dx=p.X-b.X,dz=p.Z-b.Z,ac=Math.Abs(c),az=Math.Abs(s);
            return Math.Abs(dx)<b.HalfX+hx*ac+hz*az && Math.Abs(dz)<b.HalfZ+hx*az+hz*ac && Math.Abs(dx*c-dz*s)<hx+b.HalfX*ac+b.HalfZ*az && Math.Abs(dx*s+dz*c)<hz+b.HalfX*az+b.HalfZ*ac;
        }
    }
    public static class BikeInterior
    {
        public static void Enter(CityGame game,ExplorableBuilding building)
        {
            var s=game.State;var p=building.Plan;Point original=s.Position;float yaw=s.Yaw;
            s.EnterBuilding(original,p.X,p.Z,p.Width+p.Depth,p.Width+p.Depth,p.Name,p.FloorAt,yaw);
            s.InteriorBoundary=(point,radius)=>
            {
                var local=p.Local(point);float a=s.Yaw-p.Yaw;
                float hx=Mathf.Abs(Mathf.Cos(a))*.42f+Mathf.Abs(Mathf.Sin(a))*s.BikeHalfLength;
                float hz=Mathf.Abs(Mathf.Sin(a))*.42f+Mathf.Abs(Mathf.Cos(a))*s.BikeHalfLength;
                if(Mathf.Abs(local.X)>p.Width/2-hx || local.Z< -p.Depth/2+hz)return false;
                if(local.Z<=p.Depth/2-hz)return true;
                return local.Y<.5f && Mathf.Abs(local.X)<1.2f-hx && local.Z<=p.Depth/2+3;
            };
        }
    }
}
