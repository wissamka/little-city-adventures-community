using System;
namespace LittleCity
{
    public sealed class BuildingPlan
    {
        public float X,Z,Yaw,Width,Depth,Story;
        public int Floors,Decor;
        public string Name;
        public float Height=>Floors*Story;
        public Point Local(Point p){float c=(float)Math.Cos(Yaw),s=(float)Math.Sin(Yaw),x=p.X-X,z=p.Z-Z;return new Point(x*c-z*s,p.Y,x*s+z*c);}
        public Point World(Point p){float c=(float)Math.Cos(Yaw),s=(float)Math.Sin(Yaw);return new Point(X+p.X*c+p.Z*s,p.Y,Z-p.X*s+p.Z*c);}
        public float StairStart=>-Depth/2+3;
        public float StairEnd=>Depth/2-3;
        public float LiftX=>-Width/2+2.3f;
        public float LiftZ=>Depth/2-3.2f;
        public float StairX=>Width/2-2.2f;
        public float FloorAt(Point point)
        {
            Point p=Local(point);
            if(Math.Abs(p.X-StairX)<1.6f && p.Z>=StairStart && p.Z<=StairEnd)
            {
                float t=(p.Z-StairStart)/(StairEnd-StairStart);
                int flight=Math.Max(0,Math.Min(Floors-1,(int)Math.Round((p.Y-t*Story)/Story)));
                return flight*Story+t*Story;
            }
            return Math.Max(0,Math.Min(Floors,(int)Math.Round(p.Y/Story)))*Story;
        }
        public int FloorNumber(Point p)=>Math.Max(0,Math.Min(Floors,(int)Math.Round(p.Y/Story)));
    }
}
