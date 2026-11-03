using System;
namespace LittleCity
{
    public sealed partial class Simulation
    {
        void FlyHelicopter(float dt,float steer,float forward,DriveInput input)
        {
            bool airborne=Airborne;
            Throttle=Approach(Throttle,airborne||input.Rise?1:.2f,dt*1.2f);
            float climb=input.Rise?7:input.Descend?-6:0;
            if(Throttle<.72f)climb=0;
            if(input.Brake)climb=0;
            VerticalSpeed=Approach(VerticalSpeed,climb,dt*18);
            Speed=Approach(Speed,airborne&&!input.Brake?forward*(forward>=0?22:8):0,dt*(input.Brake||Math.Abs(forward)<.06f?11:5));
            if(airborne||Throttle>.72f)Yaw+=steer*dt*.95f;
            Pitch=Approach(Pitch,airborne?Speed*.008f:0,dt*.7f);Bank=Approach(Bank,airborne?-steer*Math.Min(.18f,Math.Abs(Speed)*.009f):0,dt*.7f);
            Point next=Position+new Point((float)Math.Sin(Yaw)*Speed,VerticalSpeed,(float)Math.Cos(Yaw)*Speed)*dt;
            float landingFloor=Math.Max(0,GroundHeight(next));
            next.Y=Clamp(next.Y,landingFloor,Ceiling);
            if(Math.Abs(next.X)>BoundsX-6.3f||Math.Abs(next.Z)>BoundsZ-6.3f||Blocked(next,6.3f))
            {
                Speed=VerticalSpeed=0;Pitch=Bank=0;
                if(impactCooldown<=0){Say("Hold your hover. Turn or climb toward open space.");impactCooldown=2;}
                return;
            }
            // Water is flyable but has no landing surface for skids.
            if(IsOcean(next)&&next.Y<1.5f){next.Y=1.5f;VerticalSpeed=Math.Max(0,VerticalSpeed);if(impactCooldown<=0){Say("Fly back to shore to land.");impactCooldown=2;}}
            Position=next;VelocityYaw=Yaw;
            if(next.Y<=landingFloor+.02f){VerticalSpeed=0;Speed=0;Pitch=Bank=0;if(airborne)Say("Landed. G to step out, or E / UP to lift off again.");}
            if(next.Y>=Ceiling)VerticalSpeed=Math.Min(0,VerticalSpeed);
        }
    }
}
