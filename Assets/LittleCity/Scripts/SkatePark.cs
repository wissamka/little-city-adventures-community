using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public static partial class SkatePark
    {
        public const int WorldIndex=4;
        public const float LandingLength=40;
        public static float LaneX(int lane)=>(lane-1)*60;
        public static float StartZ(int stage)=>-100+stage*75;
        public static float Width(int lane)=>lane==0?12:lane==1?8:5;
        public static float Height(int lane,int stage)=>3+lane*2+stage*.5f;
        public static float Gap(int lane,int stage)=>7+lane*4+stage*2;
        public static float LandingZ(int lane,int stage)=>StartZ(stage)+14+Gap(lane,stage);
        public static float Ground(Point p)
        {
            if(PlusGround(p,out float extra))return extra;
            for(int lane=0;lane<3;lane++)for(int stage=0;stage<3;stage++)
            {
                if(Mathf.Abs(p.X-LaneX(lane))>Width(lane)*.5f)continue;
                float z=StartZ(stage),lip=z+14,landing=LandingZ(lane,stage),h=Height(lane,stage);
                if(p.Z>=z && p.Z<=lip)return h*(p.Z-z)/14;
                if(p.Z>lip && p.Z<landing)return -6;
                if(p.Z>=landing && p.Z<=landing+LandingLength)return h*.55f*(1-Mathf.Clamp01((p.Z-landing-20)/20));
            }
            float radius=Vector2.Distance(new Vector2(p.X,p.Z),new Vector2(123,55));
            if(radius<34)return 6*Mathf.Pow(radius/34,4);
            return 0;
        }
        static void Mesh(Transform root,string name,List<Vector3> vertices,List<int> triangles,Color color)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root,false);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=Art.Mat(color,2);obj.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        static void Ramp(Transform root,float x,float start,float end,float width,float a,float b,Color color)
        {
            var v=new List<Vector3>{new Vector3(x-width/2,a,start),new Vector3(x+width/2,a,start),new Vector3(x+width/2,b,end),new Vector3(x-width/2,b,end),new Vector3(x-width/2,0,start),new Vector3(x+width/2,0,start),new Vector3(x+width/2,0,end),new Vector3(x-width/2,0,end)};
            Mesh(root,"Rideable concrete launch and landing",v,new List<int>{0,2,1,0,3,2,0,4,7,0,7,3,1,2,6,1,6,5,3,7,6,3,6,2},color);
        }
        static void Sign(Transform root,string text,Vector3 p,Color color,float width)
        {
            Art.Box("Course sign board",root,p+new Vector3(0,0,.16f),new Vector3(width,2.4f,.22f),color,.05f);
            AdventureWorlds.Caption(text,root,p,180,width);
            var label=root.Find(text);if(label!=null){label.localScale*=2.5f;label.GetComponent<WorldLabel>().FarDistance=190;}
            foreach(int side in new[]{-1,1})Art.Tube("Sign support",root,new Vector3(p.x+side*width*.4f,0,p.z+.2f),new Vector3(p.x+side*width*.4f,p.y,p.z+.2f),.09f,Art.Ink);
        }
        static void DressPark(Transform root)
        {
            Color blue=Art.Hex("577F8D"),cream=Art.Hex("C8C5AB");
            foreach(int side in new[]{-1,1})
            {
                Art.Box("Park boundary wall",root,new Vector3(side*207,1.2f,0),new Vector3(.45f,2.4f,385),blue,0);
                Art.Box("Park boundary wall",root,new Vector3(0,1.2f,side*189),new Vector3(415,2.4f,.45f),blue,0);
                for(int i=0;i<10;i++)
                {
                    float z=-165+i*36,x=side*225,height=17+i%4*9;
                    Art.Box("Skatepark skyline masonry",root,new Vector3(x,height*.5f,z),new Vector3(22,height,25),i%2==0?blue:cream,.05f);
                    for(int floor=0;floor<height/6;floor++)Art.Box("Skyline window ribbon",root,new Vector3(x-side*11.04f,3+floor*6,z),new Vector3(.05f,2.8f,19),Art.Ink,0);
                }
                for(int i=0;i<5;i++)
                {
                    float z=-118+i*63;IllustratedScenery.Tree(root,new Vector3(side*193,0,z),1.2f,false);
                    Art.Box("Spectator bench seat",root,new Vector3(side*185,.55f,z),new Vector3(1.2f,.2f,5),cream,.05f);
                    Art.Box("Spectator bench back",root,new Vector3(side*185.6f,1.10f,z),new Vector3(.15f,.8f,5),blue,.05f);
                }
            }
            Art.Box("Welcome plaza paving",root,new Vector3(-10,.005f,-146),new Vector3(165,.025f,40),cream,0);
            var canopy=Art.Group("Riders clubhouse canopy",root,new Vector3(-52,0,-151));
            Art.Box("Canopy roof",canopy,new Vector3(0,4,0),new Vector3(36,.22f,10),blue,.08f);
            foreach(int x in new[]{-17,17})foreach(int z in new[]{-4,4})Art.Tube("Canopy post",canopy,new Vector3(x,0,z),new Vector3(x,4,z),.13f,cream);
            for(int i=0;i<12;i++)
            {
                float x=-140+i*25;Art.Box("Painted finish mural",root,new Vector3(x,2.2f,186),new Vector3(18,2,.12f),i%2==0?Art.Teal:Art.Yellow,0);
            }
        }
        public static WorldPlace Build(CityGame game)
        {
            var world=new WorldPlace{Root=Art.Group("Skyline Skatepark",game.transform)};var r=world.Root;
            Art.Box("Skatepark plaza",r,new Vector3(0,-.20f,0),new Vector3(440,.35f,395),Art.Hex("9BA7A8"),0);
            DressPark(r);BuildPlus(world);
            Color[] colors={Art.Hex("61A792"),Art.Hex("D6AA69"),Art.Hex("BE7687")};
            string[] names={"FLOW LINE", "BIG AIR", "SKYLINE CHALLENGE"};
            for(int lane=0;lane<3;lane++)
            {
                float x=LaneX(lane);
                Art.Box("Colored course lane",r,new Vector3(x,.001f,-2),new Vector3(Width(lane)+8,.015f,278),Color.Lerp(Art.Hex("9BA7A8"),colors[lane],.22f),0);
                Sign(r,names[lane],new Vector3(x+Width(lane)*.5f+9,3.2f,-130),colors[lane],12);
                for(int stage=0;stage<3;stage++)
                {
                    float start=StartZ(stage),lip=start+14,landing=LandingZ(lane,stage),h=Height(lane,stage),width=Width(lane);
                    Ramp(r,x,start,lip,width,0,h,colors[lane]);Ramp(r,x,landing,landing+20,width,h*.55f,h*.55f,colors[lane]);Ramp(r,x,landing+20,landing+LandingLength,width,h*.55f,0,colors[lane]);
                    Art.Box("Deep jump gap",r,new Vector3(x,.012f,(lip+landing)/2),new Vector3(width,.015f,landing-lip),Art.Ink,0);
                    Art.Box("Checkpoint stripe",r,new Vector3(x,.018f,start-12),new Vector3(width+3,.02f,.45f),Art.Yellow,0);
                    AdventureWorlds.Caption("JUMP  "+Gap(lane,stage)+" m",r,new Vector3(x-width/2-2,1.4f,start+7),90,5);
                    var ring=Art.Ring("Air target ring",r,2.3f,.09f,.10f,Art.Yellow);ring.transform.localPosition=new Vector3(x,h+3.5f,(lip+landing)/2);ring.transform.localRotation=Quaternion.Euler(0,90,0);
                    foreach(int side in new[]{-1,1})Art.Tube("Landing beacon",r,new Vector3(x+side*(width/2+.6f),0,landing),new Vector3(x+side*(width/2+.6f),h*.55f+1,landing),.10f,colors[lane]);
                }
                Art.Box("Finish line",r,new Vector3(x,.02f,123),new Vector3(Width(lane)+5,.035f,1.2f),colors[lane],0);
            }
            var bowlV=new List<Vector3>();var bowlT=new List<int>();const int rings=10,segments=48;
            for(int ring=0;ring<=rings;ring++)for(int i=0;i<=segments;i++)
            {float rad=34f*ring/rings,angle=i*Mathf.PI*2/segments;bowlV.Add(new Vector3(123+Mathf.Cos(angle)*rad,6*Mathf.Pow(rad/34,4),55+Mathf.Sin(angle)*rad));}
            for(int ring=0;ring<rings;ring++)for(int i=0;i<segments;i++)
            {int a=ring*(segments+1)+i,b=a+segments+1;bowlT.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            Mesh(r,"Rideable six-metre bowl",bowlV,bowlT,Art.Hex("889FAF"));
            AdventureWorlds.Caption("THE DEEP BOWL",r,new Vector3(123,8,92),180,8);
            Art.Box("Super bounce pad",r,new Vector3(118,.06f,-103),new Vector3(9,.12f,9),Art.Teal,.05f);
            AdventureWorlds.Caption("SUPER BOUNCE",r,new Vector3(118,2,-96),180,7);
            for(int i=0;i<5;i++)
            {
                float x=-136+i*9;Art.Box("Slalom cone base",r,new Vector3(x,.10f,-45+i%2*8),new Vector3(.8f,.2f,.8f),Art.Yellow,.03f);
                Art.Shape("Slalom cone",PrimitiveType.Cylinder,r,new Vector3(x,.6f,-45+i%2*8),new Vector3(.5f,.5f,.5f),Art.Red);
                AdventureWorlds.Solid(world,new Vector3(x,.45f,-45+i%2*8),new Vector3(.65f,.9f,.65f));
            }

            Ride[] rides={Ride.Skateboard,Ride.Scooter,Ride.MountainBike,Ride.MiniBike,Ride.Bicycle};
            for(int i=0;i<rides.Length;i++)
            {
                var model=Models.Vehicle(r,rides[i]);model.Root.localPosition=new Vector3(-64+i*6,0,-139);
                game.Parking.Register(model,WorldIndex,Simulation.RideNames[(int)rides[i]]);
                AdventureWorlds.Caption(Simulation.RideNames[(int)rides[i]],r,new Vector3(-64+i*6,1.9f,-141),180,4);
            }
            AdventureWorlds.Caption("SKYLINE SKATEPARK",r,new Vector3(0,9,-169),180,15);
            AdventureWorlds.Caption("SPACE / JUMP   •   CTRL / STOP   •   R / RETRY",r,new Vector3(0,6,-169),180,6);
            return world;
        }
    }

    public sealed class SkateParkLife
    {
        readonly CityGame game;readonly HashSet<int> cleared=new HashSet<int>();int checkpoint=-1,lastLandings;
        public int ClearedGaps=>cleared.Count;
        public SkateParkLife(CityGame owner){game=owner;}
        public void Tick()
        {
            var state=game.State;if(state.WorldIndex!=SkatePark.WorldIndex || state.Indoors)return;
            var p=state.Position;
            SkatePark.PlusCheckpoint(state);
            if(state.IsJumpRide && state.Grounded && Mathf.Abs(p.X-118)<4 && Mathf.Abs(p.Z+103)<4)state.LaunchPark(18);
            for(int lane=0;lane<3;lane++)for(int stage=0;stage<3;stage++)
            {
                if(Mathf.Abs(p.X-SkatePark.LaneX(lane))>SkatePark.Width(lane)*.5f+1)continue;
                int id=lane*3+stage;
                if(Mathf.Abs(p.Z-(SkatePark.StartZ(stage)-12))<2 && checkpoint!=id)
                {checkpoint=id;state.ParkCheckpoint=new Point(SkatePark.LaneX(lane),0,SkatePark.StartZ(stage)-19);state.Say("Checkpoint saved · "+SkatePark.Gap(lane,stage)+" m gap ahead. R / Retry brings you here.");}
                if(state.ParkLandings>lastLandings && p.Z>=SkatePark.LandingZ(lane,stage) && p.Z<=SkatePark.LandingZ(lane,stage)+SkatePark.LandingLength && cleared.Add(id))state.Say("Clean landing! "+cleared.Count+" / 9 gaps cleared.");
            }
            lastLandings=state.ParkLandings;
        }
    }
}
