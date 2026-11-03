using System;
using System.Collections.Generic;
namespace LittleCity
{
    public sealed partial class Simulation
    {
        public int CarBreakdownImpacts {get;set;}=10;
        public float CarDentMinimumSpeed {get;set;}=6.5f;
        readonly List<Point> carDents=new List<Point>();
        public IReadOnlyList<Point> CarDents=>carDents;
        public int CarCrashes=>carDents.Count;
        bool crashArmed=true;Point lastCarCrash;
        void ResetCarDamage(){carDents.Clear();crashArmed=true;NeedsRepair=false;}
        public void RestoreCarDamage(IEnumerable<Point> dents)
        {
            ResetCarDamage();if(dents!=null)foreach(var d in dents){if(carDents.Count>=CarBreakdownImpacts)break;carDents.Add(d);}
            NeedsRepair=CarCrashes>=CarBreakdownImpacts;crashArmed=false;lastCarCrash=Position;
        }
        void RearmCarCrash()
        {
            float x=Position.X-lastCarCrash.X,z=Position.Z-lastCarCrash.Z;
            if(x*x+z*z>36&&impactCooldown<=0)crashArmed=true;
        }
        bool RecordCarCrash(Point delta,Point candidate,float radius)
        {
            if(Mode!=Ride.RaceCar||!Grounded||!crashArmed||NeedsRepair)return false;
            float length=Math.Max(.00001f,(float)Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z));
            // Use the blocked axis so a fast, shallow scrape is not a full-speed collision.
            bool hitX=Blocked(new Point(candidate.X,candidate.Y,Position.Z),radius);
            bool hitZ=Blocked(new Point(Position.X,candidate.Y,candidate.Z),radius);
            float nx=hitX?Math.Sign(delta.X):0,nz=hitZ?Math.Sign(delta.Z):0;
            if(!hitX&&!hitZ){nx=delta.X/length;nz=delta.Z/length;}
            float n=Math.Max(.001f,(float)Math.Sqrt(nx*nx+nz*nz));nx/=n;nz/=n;
            float normalSpeed=ImpactSpeed*Math.Abs((delta.X*nx+delta.Z*nz)/length);
            if(normalSpeed<CarDentMinimumSpeed)return false;
            float c=(float)Math.Cos(Yaw),s=(float)Math.Sin(Yaw);
            carDents.Add(new Point(nx*c-nz*s,Clamp(normalSpeed/18,.5f,1),nx*s+nz*c));
            lastCarCrash=Position;crashArmed=false;NeedsRepair=CarCrashes>=CarBreakdownImpacts;
            Say(NeedsRepair?"Car needs repair. F to fix it, G to step out, or R to return home.":"A new dent · "+CarCrashes+" / "+CarBreakdownImpacts+" bumps. F repairs your car.");return true;
        }
    }
}
