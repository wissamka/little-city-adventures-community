using System;
using System.Globalization;

namespace LittleCity
{
    public enum WorldTheme { Jungle, NewYork, Prehistoric, Asgard }
    public sealed class RouteDestination
    {
        public readonly long Number;
        public readonly uint Seed;
        public readonly WorldTheme Theme;
        public readonly string Name;
        public RouteDestination(long number,uint seed,WorldTheme theme,string name){Number=number;Seed=seed;Theme=theme;Name=name;}
    }
    // Stateless, versioned generation: revisiting a route number recreates the same place.
    public sealed class WorldRoute
    {
        public const uint DefaultSeed=20260904;
        public static readonly string[] ThemeNames={"Jungle","New York","Prehistoric","Asgard"};
        static readonly string[][] Titles={new[]{"Emerald Canopy","Orchid Valley","Misty Falls","Parrot Grove"},new[]{"Skyline Gardens","Harbor Lights","Uptown Squares","Riverside City"},new[]{"Fern Valley","Dinosaur Lagoon","Amber Highlands","Giant Footprints"},new[]{"Golden Halls","Rainbow Heights","Starfall Peaks","Cloud Citadel"}};
        public uint Seed { get; private set; }
        public long LastVisit { get; private set; }
        public WorldRoute(uint seed=DefaultSeed){Seed=seed;}
        public RouteDestination Describe(long number)
        {
            if(number<1)throw new ArgumentOutOfRangeException(nameof(number));
            ulong value=unchecked((ulong)number+(ulong)Seed*0x9E3779B97F4A7C15UL);
            value=unchecked((value^(value>>30))*0xBF58476D1CE4E5B9UL);
            value=unchecked((value^(value>>27))*0x94D049BB133111EBUL);value^=value>>31;
            uint s=(uint)(value^(value>>32));var theme=(WorldTheme)((number-1)%4);
            return new RouteDestination(number,s,theme,ThemeNames[(int)theme]+" · "+Titles[(int)theme][(s>>7)%4]);
        }
        public void Visit(long number){if(number>0)LastVisit=number;}
        public string Save()=>"1:"+Seed.ToString(CultureInfo.InvariantCulture)+":"+LastVisit.ToString(CultureInfo.InvariantCulture);
        public static WorldRoute Restore(string value)
        {
            string[] p=(value??"").Split(':');
            if(p.Length!=3 || p[0]!="1" || !uint.TryParse(p[1],NumberStyles.None,CultureInfo.InvariantCulture,out uint seed) || !long.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out long number) || number<0)return new WorldRoute();
            var route=new WorldRoute(seed);route.LastVisit=number;return route;
        }
        public static bool CrossedGate(Point from,Point to,bool forward)
        {
            float z=forward?165:-170;
            if(forward ? from.Z>=z || to.Z<z : from.Z<=z || to.Z>z)return false;
            float t=(z-from.Z)/(to.Z-from.Z),x=from.X+(to.X-from.X)*t,y=from.Y+(to.Y-from.Y)*t;
            return (x-172)*(x-172)+(y-40)*(y-40)<22*22 && y>=18 && y<=62;
        }
    }
}
