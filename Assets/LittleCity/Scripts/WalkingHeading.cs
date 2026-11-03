using System;
namespace LittleCity
{
    // Camera chase can change the view without steering a held movement input.
    public sealed class WalkingHeading
    {
        bool held;int viewVersion;float heading;
        public void Clear(){held=false;}
        public float Resolve(float sideways,float forward,float visibleHeading,float requestedHeading,int manualViewVersion)
        {
            bool moving=Math.Abs(sideways)+Math.Abs(forward)>.08f;
            if(!held)heading=visibleHeading;
            else if(viewVersion!=manualViewVersion)heading=requestedHeading;
            held=moving;viewVersion=manualViewVersion;return heading;
        }
    }
}
