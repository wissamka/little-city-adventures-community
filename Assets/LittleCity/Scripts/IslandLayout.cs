using System;
namespace LittleCity
{
    // Shared navigation and ground geometry; these coordinates also drive the visible islands.
    public static class IslandLayout
    {
        public static readonly string[] Names={"Playground Island","Mario World","Nordstrom Rack Island"};
        public static readonly Point[] Centers={new Point(-430,0,460),new Point(0,0,810),new Point(450,0,470)};
        public const float Radius=82;
        public static int At(Point p,float extra=0){for(int i=0;i<Centers.Length;i++){float x=p.X-Centers[i].X,z=p.Z-Centers[i].Z;if(x*x+z*z<(Radius+extra)*(Radius+extra))return i;}return -1;}
        public static Point Dock(int i)=>Centers[i]+new Point(0,0,-Radius-28);
        public static Point Berth(int i)=>Dock(i)+new Point(-22,-1.05f,-45);
        public static bool OnDock(Point p){foreach(var c in Centers)if(Math.Abs(p.X-c.X)<4&&p.Z>c.Z-Radius-42&&p.Z<c.Z-Radius+8)return true;return false;}
        public static float Ground(Point p)
        {
            if(OnDock(p))return 0;
            int i=At(p);if(i<0)return -1.05f;
            var c=Centers[i];float x=p.X-c.X,z=p.Z-c.Z;
            // A walkable stepped platform course, separate from the beach and shop.
            if(i==1&&Math.Abs(x+25)<3&&z>-30&&z<24)return Math.Min(4.2f,(float)Math.Floor((z+30)/6)*.45f);
            return 0;
        }
        public static bool HullInWater(Point p,float yaw)
        {
            float s=(float)Math.Sin(yaw),c=(float)Math.Cos(yaw);
            for(int iz=-2;iz<=2;iz++)for(int ix=-1;ix<=1;ix++)
            {
                var q=p+new Point(ix*11*c+iz*24*s,0,-ix*11*s+iz*24*c);
                if(q.Z<52 || q.Z<210&&q.X>119 || At(q)>=0 || OnDock(q)||Math.Abs(q.X)>820||q.Z>1030)return false;
            }
            return true;
        }
    }
}
