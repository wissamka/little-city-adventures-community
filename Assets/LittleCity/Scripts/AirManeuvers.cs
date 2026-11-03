using System;
namespace LittleCity
{
    public enum AirTrick { None, Loop, Roll, Turn }
    public sealed partial class Simulation
    {
        public AirTrick AirManeuver {get;private set;}
        public float AirProgress {get;private set;}
        public int AirCompletions {get;private set;}
        public const float AirDuration=4;
        Point airOrigin;float airYaw,airSpeed;
        bool loopHeld,rollHeld,turnHeld;
        public void CancelAirManeuver()
        {
            if(AirManeuver==AirTrick.None)return;
            AirManeuver=AirTrick.None;AirProgress=0;Pitch=Wrap(Pitch);Bank=Wrap(Bank);
        }
        public bool StartAirManeuver(AirTrick trick)
        {
            if(trick==AirTrick.None || AirManeuver!=AirTrick.None || Mode!=Ride.Airplane || Indoors)return false;
            if(Position.Y<70 || Speed<18){Say("For aerial tricks, climb above 70 m and build speed.");return false;}
            airOrigin=Position;airYaw=Yaw;airSpeed=Speed;
            // Check the entire swept path before committing, including wing clearance.
            for(int i=0;i<=128;i++)
            {
                Point p=AirPath(trick,i/128f);
                if(p.Y<50 || p.Y>Ceiling-3.5f || Math.Abs(p.X)>BoundsX-6 || Math.Abs(p.Z)>BoundsZ-6 || Blocked(p,5))
                {Say("More open sky needed for that trick. Turn away from buildings or the edge.");return false;}
            }
            AirManeuver=trick;AirProgress=0;Pitch=Bank=0;
            Say(trick==AirTrick.Loop?"Loop! Space / STOP cancels.":trick==AirTrick.Roll?"Aileron roll! Space / STOP cancels.":"Banked U-turn! Space / STOP cancels.");return true;
        }
        Point AirPath(AirTrick trick,float t)
        {
            float pi=(float)Math.PI,forward,right=0,y=0;
            if(trick==AirTrick.Loop){float r=airSpeed*AirDuration/(2*pi),a=2*pi*t;forward=r*(float)Math.Sin(a);y=r*(1-(float)Math.Cos(a));}
            else if(trick==AirTrick.Turn){float r=airSpeed*AirDuration/pi,a=pi*t;forward=r*(float)Math.Sin(a);right=r*(1-(float)Math.Cos(a));}
            else forward=airSpeed*AirDuration*t;
            return airOrigin+new Point((float)Math.Sin(airYaw)*forward+(float)Math.Cos(airYaw)*right,y,(float)Math.Cos(airYaw)*forward-(float)Math.Sin(airYaw)*right);
        }
        bool TickAirManeuver(float dt,DriveInput input)
        {
            bool loop=input.AirLoop&&!loopHeld,roll=input.AirRoll&&!rollHeld,turn=input.AirTurn&&!turnHeld;
            loopHeld=input.AirLoop;rollHeld=input.AirRoll;turnHeld=input.AirTurn;
            if(input.Brake)CancelAirManeuver();
            else if(loop||roll||turn)StartAirManeuver(loop?AirTrick.Loop:roll?AirTrick.Roll:AirTrick.Turn);
            if(AirManeuver==AirTrick.None)return false;
            float next=Math.Min(1,AirProgress+dt/AirDuration);Point proposed=AirPath(AirManeuver,next);
            if(Blocked(proposed,5) || !InBounds(proposed)){CancelAirManeuver();Speed=Throttle=0;Say("Trick canceled: the flight path is no longer clear.");return true;}
            Point delta=proposed+Position*-1;DistanceTravelled+=(float)Math.Sqrt(delta.X*delta.X+delta.Y*delta.Y+delta.Z*delta.Z);
            Position=proposed;AirProgress=next;float pi=(float)Math.PI;
            Pitch=AirManeuver==AirTrick.Loop?-next*2*pi:0;
            Bank=AirManeuver==AirTrick.Roll?-next*2*pi:AirManeuver==AirTrick.Turn?-.75f*(float)Math.Sin(next*pi):0;
            Yaw=Wrap(airYaw+(AirManeuver==AirTrick.Turn?next*pi:0));VelocityYaw=Yaw;
            if(next>=1){AirManeuver=AirTrick.None;Pitch=Bank=0;AirCompletions++;Say("Nice flying! Your controls are back.");}
            return true;
        }
    }
}
