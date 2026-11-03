using System;
using UnityEngine;

namespace LittleCity
{
    public sealed class GeneratedWorldTag : MonoBehaviour { public long Number;public uint Seed;public int Theme; }
    public static class ProceduralWorlds
    {
        static readonly Color[] Greens={Art.Hex("328963"),Art.Hex("54A968"),Art.Hex("7FC86B"),Art.Hex("226A57")};
        static readonly Color[] Towers={Art.Hex("7DA9BF"),Art.Hex("91BDCD"),Art.Hex("D9C7A8"),Art.Hex("647E9E"),Art.Hex("B3CEE1")};
        public static WorldPlace Build(CityGame game,RouteDestination d)
        {
            var random=new System.Random(unchecked((int)d.Seed));
            var w=new WorldPlace{Root=Art.Group(d.Name,game.transform)};
            var tag=w.Root.gameObject.AddComponent<GeneratedWorldTag>();tag.Number=d.Number;tag.Seed=d.Seed;tag.Theme=(int)d.Theme;
            Color ground=d.Theme==WorldTheme.NewYork?Art.Hex("9FBCA7"):d.Theme==WorldTheme.Asgard?Art.Hex("B6D9C4"):Art.Hex("7DB767");
            Art.Box("Distant sea",w.Root,new Vector3(0,-2,0),new Vector3(1600,2,1600),Art.Hex("63B8CF"),0);
            Art.Box("Explorable ground",w.Root,new Vector3(0,-.25f,0),new Vector3(460,.5f,420),ground,0);
            Runway(w);
            Art.Box("Safe arrival plaza",w.Root,new Vector3(0,.02f,-28),new Vector3(44,.04f,20),Art.Hex("E1D0A0"),0);
            Art.Box("Airfield road",w.Root,new Vector3(73,.035f,-32),new Vector3(200,.06f,10),Art.Hex("D1BC93"),0);
            switch(d.Theme)
            {
                case WorldTheme.Jungle:Jungle(w,random);break;
                case WorldTheme.NewYork:NewYork(game,w,random);break;
                case WorldTheme.Prehistoric:Prehistoric(w,random);break;
                case WorldTheme.Asgard:Asgard(w,random);break;
            }
            AdventureWorlds.Cabin(w,28,-72,d.Theme==WorldTheme.Asgard?Art.Yellow:Art.Teal,"EXPLORER LODGE");
            AdventureWorlds.Caption(WorldRoute.ThemeNames[(int)d.Theme].ToUpperInvariant()+" • STOP "+d.Number,w.Root,new Vector3(0,3,-15),180,24);
            AdventureWorlds.Caption("AIRFIELD →",w.Root,new Vector3(40,2,-31),180,9);
            foreach(var mode in new[]{Ride.Airplane,Ride.Bicycle,Ride.RaceCar})
            {
                var m=Models.Vehicle(w.Root,mode,mode==Ride.RaceCar && d.Theme==WorldTheme.Prehistoric?3:random.Next(3));m.Root.localPosition=mode==Ride.Airplane?new Vector3(172,0,-130):new Vector3(mode==Ride.Bicycle?8:-9,0,-25);
                game.Parking.Register(m,3,mode==Ride.Airplane?"explorer airplane":mode==Ride.Bicycle?"green bicycle":d.Theme==WorldTheme.Prehistoric?"Dino Park safari Jeep":"explorer car");
            }
            Art.CombineStatic(w.Root);return w;
        }
        static void Runway(WorldPlace w)
        {
            Art.Box("Runway",w.Root,new Vector3(172,.05f,-3),new Vector3(22,.08f,310),Art.Hex("536C7D"),0);
            for(int z=-144;z<145;z+=12){Art.Box("Runway stripe",w.Root,new Vector3(172,.11f,z),new Vector3(.7f,.02f,5),Art.White,0);foreach(int side in new[]{-1,1})Art.Ball("Landing lights",w.Root,new Vector3(172+side*11.5f,.3f,z),Vector3.one*.4f,Art.Yellow);}
        }
        static float Next(System.Random rng,float a,float b)=>a+(float)rng.NextDouble()*(b-a);
        static bool Clear(float x,float z)=>Mathf.Abs(z+32)<12 || x>-24 && x<48 && z>-87 && z<0;
        static void Jungle(WorldPlace w,System.Random rng)
        {
            float riverX=Next(rng,-112,-70);
            Art.Box("Jungle river",w.Root,new Vector3(riverX,.02f,28),new Vector3(13,.04f,250),Art.Hex("39ADBE"),0);
            foreach(int side in new[]{-1,1})AdventureWorlds.Solid(w,new Vector3(riverX,.3f,side==1?64:-78),new Vector3(12,.6f,side==1?124:58));
            Art.Box("Jungle footbridge",w.Root,new Vector3(riverX,.08f,-32),new Vector3(23,.16f,9),Art.Hex("BE925F"),0);
            for(int i=0;i<145;i++)
            {
                float x=Next(rng,-195,125),z=Next(rng,-153,146);if(Clear(x,z)||Mathf.Abs(x-riverX)<16)continue;
                Tree(w,x,z,Next(rng,8,18),Greens[rng.Next(4)],true);
                if(i%3==0)Fern(w.Root,new Vector3(x+3,0,z+2),2.3f);
            }
            float mountainX=Next(rng,-55,65);
            Mountain(w,new Vector3(mountainX,0,140),42,54,Art.Hex("6B9690"));
            Art.Box("Tall waterfall",w.Root,new Vector3(mountainX,18,119),new Vector3(9,36,.5f),Art.Hex("A4E4E6"),0);
            for(int i=0;i<5;i++)Art.Tube("Waterfall stream",w.Root,new Vector3(mountainX-4+i*2,2,118.5f),new Vector3(mountainX-4+i*2,35,118.5f),.25f,Art.White);
            Art.Disk("Waterfall pool",w.Root,new Vector3(mountainX,.06f,109),16,.08f,Art.Hex("56BECA"));AdventureWorlds.Solid(w,new Vector3(mountainX,.4f,110),new Vector3(26,.8f,25));
            for(int i=0;i<6;i++){Vector3 p=new Vector3(-24+i*8,1,25);Art.Ball("Giant orchid petals",w.Root,p,new Vector3(2,1,2),i%2==0?Art.Hex("F78EB3"):Art.Yellow);Art.Ball("Orchid center",w.Root,p+Vector3.up*.5f,Vector3.one*.55f,Art.White);}
            for(int i=0;i<8;i++)
            {
                var perch=Art.Group("Colorful jungle parrot",w.Root,new Vector3(Next(rng,-50,100),Next(rng,8,15),Next(rng,4,80)));perch.gameObject.AddComponent<MovingScenery>();
                Art.Ball("Parrot body",perch,Vector3.zero,new Vector3(.8f,1.3f,.8f),Art.Red);Art.Ball("Parrot head",perch,new Vector3(0,.65f,.18f),Vector3.one*.7f,Art.Yellow);
                Art.Ball("Parrot wing",perch,new Vector3(.4f,0,0),new Vector3(.3f,1,.7f),Art.Blue);Art.Ball("Curved beak",perch,new Vector3(0,.6f,.58f),new Vector3(.4f,.45f,.5f),Art.Ink);
                perch.gameObject.AddComponent<WildlifeMotion>().Initialize(perch.localPosition,3,i,false);
            }
        }
        static void NewYork(CityGame game,WorldPlace w,System.Random rng)
        {
            var r=w.Root;int shift=rng.Next(-1,2)*5;
            for(int x=-170;x<=100;x+=45)Art.Box("City avenue",r,new Vector3(x,.025f,0),new Vector3(12,.05f,320),Art.Hex("6D7C8C"),0);
            for(int z=-120;z<=135;z+=51)
            {
                Art.Box("Cross street",r,new Vector3(-35,.035f,z),new Vector3(300,.06f,11),Art.Hex("6D7C8C"),0);
                for(int x=-170;x<=100;x+=45)for(int k=-2;k<=2;k++)Art.Box("Zebra crossing",r,new Vector3(x+k*1.8f,.08f,z+8),new Vector3(.9f,.02f,4),Art.White,0);
            }
            for(int ix=0;ix<6;ix++)for(int iz=0;iz<5;iz++)
            {
                float x=-148+ix*45,z=-94+iz*51;
                if(Clear(x,z)||ix>=2 && ix<=3 && iz>=2 && iz<=3 || ix==1 && iz==4)continue;
                float h=18+rng.Next(8)*5;Building(w,new Vector3(x,0,z),19+rng.Next(3)*3,h,21,Towers[rng.Next(Towers.Length)]);
            }
            Art.Box("Central park lawn",r,new Vector3(-13,.09f,57),new Vector3(74,.08f,80),Art.Hex("60B676"),0);
            Art.Box("Central park path",r,new Vector3(-13,.15f,57),new Vector3(8,.04f,80),Art.Hex("EBD6AF"),0);
            Art.Disk("Park fountain",r,new Vector3(-13,.32f,57),7,.5f,Art.White);Art.Disk("Fountain water",r,new Vector3(-13,.60f,57),6,.05f,Art.Blue);Art.Tube("Fountain jet",r,new Vector3(-13,.62f,57),new Vector3(-13,5,57),.5f,Art.Hex("A5E5EE"));AdventureWorlds.Solid(w,new Vector3(-13,.5f,57),new Vector3(14,1,14));
            for(int i=0;i<12;i++)Tree(w,-43+i%2*62,25+(i/2)*12,6,Greens[i%3],false);
            var landmark=Building(w,new Vector3(-103+shift,0,113),25,73,25,Art.Hex("CFD4CB"));
            // A compact stepped spire leaves the occupied roof and stair exit clear.
            for(int step=0;step<3;step++)Art.Box("Empire style stepped crown",r,new Vector3(-103+shift,landmark.Plan.Height+.5f+step,113),new Vector3(2.8f-step*.8f,1,2.8f-step*.8f),Art.White,0);
            Art.Tube("Skyline spire",r,new Vector3(-103+shift,landmark.Plan.Height+3,113),new Vector3(-103+shift,103,113),.5f,Art.Yellow);
            landmark.Interior.Add(new Obstacle(-103+shift,113,1.4f,1.4f,103,landmark.Plan.Height));
            Art.Box("Hudson harbor",r,new Vector3(-22,.05f,173),new Vector3(300,.08f,38),Art.Hex("60B8D1"),0);AdventureWorlds.Solid(w,new Vector3(-22,.3f,180),new Vector3(300,.6f,24));
            Art.Box("Brooklyn inspired bridge deck",r,new Vector3(-20,.14f,157),new Vector3(240,.2f,10),Art.Hex("C9B28B"),0);
            for(int side=-1;side<=1;side+=2)
            {
                float x=-20+side*75;
                foreach(int edge in new[]{-1,1})Art.Box("Bridge stone tower",r,new Vector3(x,10,157+edge*6),new Vector3(4,20,4),Art.Hex("D2BFA3"),0);
                Art.Box("Bridge tower lintel",r,new Vector3(x,19,157),new Vector3(5,3,17),Art.Hex("D2BFA3"),0);
                for(int j=-6;j<=6;j++)foreach(int edge in new[]{-1,1})Art.Tube("Suspension cable",r,new Vector3(x,18,157+edge*5),new Vector3(x+j*7,.8f,157+edge*5),.1f,Art.White);
            }
            TaxiFleet.Build(game,w);
            var statue=Art.Group("Harbor liberty statue",r,new Vector3(-154,0,138));Art.Box("Statue pedestal",statue,new Vector3(0,3,0),new Vector3(9,6,9),Art.Hex("D6C9AB"),0);Art.Ball("Liberty robe",statue,new Vector3(0,10,0),new Vector3(4,9,4),Art.Teal);Art.Ball("Liberty head",statue,new Vector3(0,15,0),Vector3.one*3,Art.Teal);Art.Tube("Raised torch arm",statue,new Vector3(1.8f,12,0),new Vector3(4,19,0),.7f,Art.Teal);Art.Ball("Torch flame",statue,new Vector3(4,21,0),new Vector3(2,3,2),Art.Yellow);
        }
        static void Prehistoric(WorldPlace w,System.Random rng)
        {
            var r=w.Root;float vx=Next(rng,-150,-80);Mountain(w,new Vector3(vx,0,148),58,60,Art.Hex("AD947B"));
            Art.Disk("Volcano crater",r,new Vector3(vx,48,148),11,1,Art.Hex("E99A64"));
            for(int i=0;i<4;i++)Art.Ball("Soft volcano clouds",r,new Vector3(vx+i*6,57+i*7,148),new Vector3(14,10,14),Art.Hex("E4D5C4"));
            for(int i=0;i<53;i++)
            {
                float x=Next(rng,-190,120),z=Next(rng,-155,135);if(Clear(x,z))continue;
                if(i%3==0)Tree(w,x,z,Next(rng,8,16),Greens[i%4],true);else Fern(r,new Vector3(x,0,z),Next(rng,2,5));
            }
            Art.Disk("Amber lagoon",r,new Vector3(77,.02f,78),29,.04f,Art.Hex("64BEC3"));AdventureWorlds.Solid(w,new Vector3(77,.3f,78),new Vector3(45,.6f,45));
            for(int i=0;i<8;i++)
            {
                bool longNeck=i%3==0;var dinosaur=Dinosaur(r,new Vector3(-85+(i%4)*47+Next(rng,-6,6),0,10+(i/4)*71),longNeck,i);
                dinosaur.gameObject.AddComponent<WildlifeMotion>().Initialize(dinosaur.localPosition,7+i%3,i,true);
            }
            for(int i=0;i<8;i++){Vector3 p=new Vector3(-5+(i%4)*3,0,-90+(i/4)*5);Art.Ball("Dinosaur egg",r,p+Vector3.up*1.2f,new Vector3(1.8f,2.4f,1.8f),Art.Hex("F6DFB8"));}
            AdventureWorlds.Caption("DINOSAUR MEADOW • GENTLE GIANTS",r,new Vector3(0,3,-8),180,28);
        }
        static Transform Dinosaur(Transform parent,Vector3 p,bool longNeck,int index)
        {
            var r=Art.Group(longNeck?"Friendly long-neck dinosaur":"Friendly horned dinosaur",parent,p);r.gameObject.AddComponent<MovingScenery>();
            Color c=index%2==0?Art.Hex("62B5A4"):Art.Hex("DFA46C");
            Art.Ball("Dinosaur body",r,new Vector3(0,3.4f,0),new Vector3(5.4f,4.4f,8),c);
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {var leg=Art.Group("Walking leg",r,new Vector3(side*1.7f,3,end*2.2f));Art.Ball("Leg",leg,new Vector3(0,-1.1f,0),new Vector3(1.5f,3.2f,1.5f),c);Art.Ball("Foot",leg,new Vector3(0,-2.6f,.3f),new Vector3(1.8f,.8f,2),Art.White);}
            Art.Tube("Tapered tail",r,new Vector3(0,3,-3),new Vector3(0,2,-9),.7f,c);Art.Ball("Tail tip",r,new Vector3(0,2,-9),new Vector3(.8f,.8f,2),c);
            Vector3 head=longNeck?new Vector3(0,10,4):new Vector3(0,4,4.4f);
            if(longNeck)Art.Tube("Long neck",r,new Vector3(0,3.5f,2.5f),head,.95f,c);
            else Art.Ball("Protective frill",r,new Vector3(0,4.3f,3.1f),new Vector3(5.2f,4,.7f),Art.Hex("F0C576"));
            Art.Ball("Smiling dinosaur head",r,head,new Vector3(3,2.4f,3.5f),c);
            foreach(int side in new[]{-1,1}){Art.Ball("White eye",r,head+new Vector3(side*1.28f,.5f,.9f),Vector3.one*.72f,Art.White);Art.Ball("Kind pupil",r,head+new Vector3(side*1.48f,.5f,1.02f),Vector3.one*.34f,Art.Ink);if(!longNeck)Art.Tube("Rounded horn",r,head+new Vector3(side*.8f,1,1),head+new Vector3(side*.8f,2,1.7f),.19f,Art.White);}
            return r;
        }
        static void Asgard(WorldPlace w,System.Random rng)
        {
            var r=w.Root;Color gold=Art.Hex("E9BC55"),stone=Art.Hex("E5DDC8");
            int width=62+rng.Next(4)*6;float z=72+rng.Next(4)*6;
            for(int i=0;i<8;i++)Mountain(w,new Vector3(-180+i*43,0,167),Next(rng,25,40),Next(rng,42,68),Art.Hex("94ACB5"));
            Art.Box("Golden hall foundation",r,new Vector3(-15,-.06f,z),new Vector3(width+10,.2f,54),stone,0);
            var hall=BuildingArchitecture.Build(r,new Vector3(-15,0,z),width,46,6,4.5f,180,gold,4,"ASGARD GOLDEN HALL");w.Doors.Add(BuildingArchitecture.DoorSite(hall));AdventureWorlds.Solid(w,new Vector3(-15,13.5f,z),new Vector3(width,27,46));
            foreach(int side in new[]{-1,1})
            {
                var roof=Art.Box("Sweeping golden roof",r,new Vector3(-15+side*width*.25f,32+width*.29f*Mathf.Sin(25*Mathf.Deg2Rad),z),new Vector3(width*.58f,2,58),Art.Hex("CF923A"),0);roof.transform.localRotation=Quaternion.Euler(0,0,side*-25);
                for(int i=0;i<4;i++){float x=-15+side*(width*.5f+9),zz=z-21+i*14;Art.Tube("Hall columns",r,new Vector3(x,0,zz),new Vector3(x,30,zz),1.4f,stone);Art.Ball("Column crown",r,new Vector3(x,30,zz),new Vector3(4,3,4),gold);}
                Building(w,new Vector3(-15+side*(width*.5f+22),0,z),12,40,18,stone);Art.Ball("Golden tower crown",r,new Vector3(-15+side*(width*.5f+22),43,z),new Vector3(16,10,20),gold);
            }
            
            
            Color[] rainbow={Art.Red,Art.Hex("F29955"),Art.Yellow,Art.Green,Art.Teal,Art.Blue,Art.Hex("B38DE0")};
            for(int i=0;i<7;i++)Art.Box("Walkable rainbow bridge",r,new Vector3(-15+(i-3)*1.45f,.09f,5),new Vector3(1.46f,.16f,110),rainbow[i],0);
            foreach(int side in new[]{-1,1})for(int i=0;i<12;i++){Art.Tube("Rainbow bridge golden posts",r,new Vector3(-15+side*6,0,-46+i*9),new Vector3(-15+side*6,1.5f,-46+i*9),.12f,gold);Art.Ball("Bridge star",r,new Vector3(-15+side*6,1.7f,-46+i*9),Vector3.one*.6f,Art.White);}
            foreach(int side in new[]{-1,1})for(int i=0;i<5;i++){var arch=Art.Ring("Rainbow sky arch",r,22+i*1.6f,.65f,.8f,rainbow[i]);arch.transform.localPosition=new Vector3(-15,4,20+side*5);arch.transform.localRotation=Quaternion.Euler(0,90,0);}
            for(int i=0;i<12;i++){float x=Next(rng,-180,110),zz=Next(rng,-140,-55);if(Clear(x,zz))continue;Tree(w,x,zz,9,Art.Hex("B5D3B3"),false);}
            AdventureWorlds.Caption("ASGARD • THE RAINBOW ROAD",r,new Vector3(-15,4,-49),180,24);
        }
        static ExplorableBuilding Building(WorldPlace w,Vector3 p,float width,float height,float depth,Color c)
        {
            int floors=Mathf.Max(1,Mathf.CeilToInt(height/4.5f));var b=BuildingArchitecture.Build(w.Root,p,width,depth,floors,4.5f,180,c,Mathf.RoundToInt(p.x*11+p.z*7),"SKYLINE HOUSE");w.Doors.Add(BuildingArchitecture.DoorSite(b));AdventureWorlds.Solid(w,p+Vector3.up*floors*2.25f,new Vector3(width,floors*4.5f,depth));return b;
        }
        static void Tree(WorldPlace w,float x,float z,float h,Color c,bool jungle)
        {
            var r=w.Root;
            IllustratedScenery.Tree(r,new Vector3(x,0,z),h/6f,!jungle,c);
            if(jungle)Art.Tube("Hanging vine",r,new Vector3(x+h*.18f,h*.8f,z),new Vector3(x+h*.18f,3,z),.12f,Greens[0]);
            AdventureWorlds.Solid(w,new Vector3(x,h*.4f,z),new Vector3(1,h*.8f,1));
        }
        static void Fern(Transform r,Vector3 p,float scale)
        {
            for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;var leaf=Art.Ball("Prehistoric fern frond",r,p+new Vector3(Mathf.Sin(a)*scale*.5f,scale*.5f,Mathf.Cos(a)*scale*.5f),new Vector3(scale*.4f,scale*.2f,scale*1.6f),Greens[i%4]);leaf.transform.localRotation=Quaternion.Euler(28,a*Mathf.Rad2Deg,0);}
        }
        static void Mountain(WorldPlace w,Vector3 p,float width,float height,Color c)
        {
            Art.Ball("Mountain foothill",w.Root,p+Vector3.up*height*.25f,new Vector3(width*1.3f,height*.7f,width),c);
            Art.Ball("Mountain summit",w.Root,p+Vector3.up*height*.60f,new Vector3(width*.65f,height*.6f,width*.65f),c);AdventureWorlds.Solid(w,p+Vector3.up*height*.3f,new Vector3(width*.8f,height*.6f,width*.7f));
        }
    }
    public sealed class WildlifeMotion : MonoBehaviour
    {
        Vector3 origin;float radius,phase;bool dinosaur;Transform[] legs;
        public void Initialize(Vector3 start,float distance,float offset,bool dino){origin=start;radius=distance;phase=offset;dinosaur=dino;var all=GetComponentsInChildren<Transform>();var list=new System.Collections.Generic.List<Transform>();foreach(var t in all)if(t.name=="Walking leg")list.Add(t);legs=list.ToArray();}
        void Update()
        {
            float a=Time.time*(dinosaur?.10f:.18f)+phase;transform.localPosition=origin+new Vector3(Mathf.Sin(a)*radius,dinosaur?0:Mathf.Sin(a*3)*.6f,Mathf.Cos(a)*radius);transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+90,0);
            for(int i=0;i<legs.Length;i++)legs[i].localRotation=Quaternion.Euler(Mathf.Sin(Time.time*2.2f+i*Mathf.PI)*16,0,0);
        }
    }
}
