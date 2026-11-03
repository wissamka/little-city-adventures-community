using System;
namespace LittleCity
{
    public static class YachtStairs
    {
        public const float X=6, Start=-28, End=-20, Height=5.4f, HalfWidth=1.55f;
        public static float FloorAt(Point p)
        {
            if(Math.Abs(p.X-X)<=HalfWidth && p.Z>=Start && p.Z<=End)
                return Height*(p.Z-Start)/(End-Start);
            return p.Y>Height/2?Height:0;
        }
    }
}
