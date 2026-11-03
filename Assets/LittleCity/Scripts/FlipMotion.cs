using System;
namespace LittleCity
{
    public sealed partial class Simulation
    {
        public const float FlipDuration=1.8f;
        public bool FlipMotionActive {get;private set;}
        public float FlipProgress {get;private set;}
        float flipTime,flipGround,flipStart,flipVelocity,flipGravity;
        public bool BeginFlipMotion()
        {
            if(Mode!=Ride.Walk || Indoors || Swimming || Resting!=RestPose.None || Grounded || FlipMotionActive)return false;
            flipGround=GroundHeight(Position);flipStart=Position.Y;flipVelocity=VerticalSpeed;
            // Continue the first jump without changing its position or velocity.
            flipGravity=2*(flipStart-flipGround+flipVelocity*FlipDuration)/(FlipDuration*FlipDuration);
            if(flipGravity<=0)return false;
            FlipMotionActive=true;FlipProgress=flipTime=0;Speed=ActualSpeed=0;jumpHeld=true;return true;
        }
        public void CancelFlipMotion(){FlipMotionActive=false;FlipProgress=flipTime=0;}
        void IntegrateFlip(float dt)
        {
            flipTime=Math.Min(FlipDuration,flipTime+dt);FlipProgress=flipTime/FlipDuration;
            float y=flipStart+flipVelocity*flipTime-.5f*flipGravity*flipTime*flipTime;
            Position=new Point(Position.X,Math.Max(flipGround,y),Position.Z);VerticalSpeed=flipVelocity-flipGravity*flipTime;Speed=0;
            if(flipTime>=FlipDuration){Position=new Point(Position.X,flipGround,Position.Z);VerticalSpeed=0;FlipMotionActive=false;}
        }
    }
}
