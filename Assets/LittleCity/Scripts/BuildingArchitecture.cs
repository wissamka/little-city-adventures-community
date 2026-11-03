using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class ExplorableBuilding : MonoBehaviour
    {
        public BuildingPlan Plan;
        public readonly List<Obstacle> Interior=new List<Obstacle>();
        public readonly List<Collider> CameraFloors=new List<Collider>();
        public Transform Door;
    }
    public static class BuildingArchitecture
    {
        static readonly Color[] Palettes={Art.Hex("64BBA9"),Art.Hex("7AAFD1"),Art.Hex("E6AD79"),Art.Hex("A394C5"),Art.Hex("DCAEAC"),Art.Hex("B9C878")};
        public static ExplorableBuilding Build(Transform parent,Vector3 p,float width,float depth,int floors,float story,float yaw,Color color,int decor,string name)
        {
            var root=Art.Group(name,parent,p);root.localRotation=Quaternion.Euler(0,yaw,0);var building=root.gameObject.AddComponent<ExplorableBuilding>();
            ContactShadows.Create(root,new Vector3(0,.075f,0),new Vector2(width*1.22f,depth*1.22f));
            var plan=new BuildingPlan{X=p.x,Z=p.z,Yaw=yaw*Mathf.Deg2Rad,Width=width,Depth=depth,Floors=floors,Story=story,Decor=Mathf.Abs(decor),Name=name};building.Plan=plan;
            for(int level=0;level<=floors;level++)
            {
                float y=level*story;
                if(level==0)Box(root,"Ground floor",new Vector3(0,-.04f,0),new Vector3(width,.2f,depth),Art.Hex("E7CCA2"));
                else
                {
                    FloorSurface(root,building,y,level==floors?Art.Hex("D2DCCC"):Art.Hex("E7CCA2"));
                }
                if(floors>=3)ElevatorLanding(building,level);
                if(level==floors)
                {
                    foreach(int side in new[]{-1,1}){Box(root,"Roof safety parapet",new Vector3(side*(width/2-.1f),y+.55f,0),new Vector3(.2f,1.1f,depth),Art.White);Box(root,"Roof safety parapet",new Vector3(0,y+.55f,side*(depth/2-.1f)),new Vector3(width,1.1f,.2f),Art.White);}
                    Art.Disk("Rooftop picnic table",root,new Vector3(-width*.25f,y+.9f,0),1.1f,.15f,Palettes[plan.Decor%6]);Art.Tube("Picnic table leg",root,new Vector3(-width*.25f,y,0),new Vector3(-width*.25f,y+.85f,0),.15f,Art.White);
                    continue;
                }
                Wall(root,building,level,true,1,color);Wall(root,building,level,true,-1,color);Wall(root,building,level,false,1,color);Wall(root,building,level,false,-1,color);
                int steps=Mathf.CeilToInt(story/.19f);float run=(plan.StairEnd-plan.StairStart)/steps;
                for(int step=0;step<steps;step++)Box(root,"Walkable stair tread",new Vector3(plan.StairX,y+(step+.5f)*story/steps,plan.StairStart+(step+.5f)*run),new Vector3(3.1f,story/steps,run+.02f),Art.White);
                var stairBarrier=new GameObject("Stair camera barrier",typeof(BoxCollider));stairBarrier.transform.SetParent(root,false);stairBarrier.transform.localPosition=new Vector3(plan.StairX,y+story/2-.08f,(plan.StairStart+plan.StairEnd)/2);stairBarrier.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(story,plan.StairEnd-plan.StairStart)*Mathf.Rad2Deg,0,0);stairBarrier.layer=8;
                var stairCollider=stairBarrier.GetComponent<BoxCollider>();stairCollider.size=new Vector3(3.1f,.35f,Mathf.Sqrt(story*story+Mathf.Pow(plan.StairEnd-plan.StairStart,2)));stairCollider.enabled=false;building.CameraFloors.Add(stairCollider);
                foreach(int side in new[]{-1,1})Art.Tube("Stair handrail",root,new Vector3(plan.StairX+side*1.57f,y+1,plan.StairStart),new Vector3(plan.StairX+side*1.57f,y+story+1,plan.StairEnd),.055f,Palettes[plan.Decor%6]);
                Furniture(building,level);
                AdventureWorlds.Caption("FLOOR "+(level+1)+"  •  STAIRS →",root,new Vector3(-.7f,y+2.2f,depth/2-.3f),180,3);
            }
            var hinge=Art.Group("Opening building door",root,new Vector3(-1.05f,0,depth/2+.12f));hinge.gameObject.AddComponent<MovingScenery>();
            Box(hinge,"Door frame",new Vector3(1.05f,1.5f,0),new Vector3(2.1f,3,.12f),Palettes[plan.Decor%6]);
            Art.Ball("Door handle",hinge,new Vector3(1.75f,1.4f,.13f),Vector3.one*.15f,Art.Yellow);building.Door=hinge;
            foreach(int side in new[]{-1,1})IllustratedScenery.WindowBox(root,side*width*.31f,.85f,depth/2+.38f);
            AdventureWorlds.Caption(name+" · F TO ENTER",root,new Vector3(0,3.25f,depth/2+.25f),0,Mathf.Min(6,width*.8f));
            return building;
        }
        static void Box(Transform r,string name,Vector3 p,Vector3 size,Color color)=>Art.Box(name,r,p,size,color,0);
        static void Wall(Transform r,ExplorableBuilding b,int level,bool front,int side,Color color)
        {
            var p=b.Plan;float length=front?p.Width:p.Depth,edge=(front?p.Depth:p.Width)/2,y=level*p.Story;
            int cells=Mathf.Max(2,Mathf.FloorToInt(length/4));float cell=length/cells;
            for(int n=0;n<cells;n++)
            {
                float along=-length/2+(n+.5f)*cell;
                bool entrance=front && side==1 && level==0 && Mathf.Abs(along)<cell*.55f;
                if(entrance)continue;
                Vector3 pos=front?new Vector3(along,y,side*edge):new Vector3(side*edge,y,along);
                Vector3 span=front?new Vector3(cell,0,.22f):new Vector3(.22f,0,cell);
                Box(r,"Wall below real window",pos+Vector3.up*.45f,new Vector3(span.x,.9f,span.z),color);
                Box(r,"Wall above real window",pos+Vector3.up*(p.Story-.4f),new Vector3(span.x,.8f,span.z),color);
                foreach(int e in new[]{-1,1})Box(r,"Window side pillar",pos+(front?Vector3.right:Vector3.forward)*(e*(cell/2-.14f))+Vector3.up*(p.Story/2),front?new Vector3(.60f,p.Story,.28f):new Vector3(.28f,p.Story,.60f),color);
                float low=.97f,high=p.Story-.85f,half=cell/2-.45f;
                Vector3 a=pos+(front?Vector3.left:Vector3.back)*half+Vector3.up*low,c=pos+(front?Vector3.right:Vector3.forward)*half+Vector3.up*high;
                Art.GlassPane("Transparent window with real interior",r,a,new Vector3(c.x,a.y,c.z),c,new Vector3(a.x,c.y,a.z));
                Box(r,"Dark timber window mullion",pos+Vector3.up*((low+high)/2),front?new Vector3(.055f,high-low,.26f):new Vector3(.26f,high-low,.055f),Art.Hex("52676A"));
                Box(r,"Dark timber window transom",pos+Vector3.up*(low+(high-low)*.64f),front?new Vector3(half*2,.055f,.26f):new Vector3(.26f,.055f,half*2),Art.Hex("52676A"));
                Box(r,"Window sill",pos+Vector3.up*.88f,front?new Vector3(cell,.12f,.42f):new Vector3(.42f,.12f,cell),Art.White);
            }
            // Indoor collision includes solid glass; the front door opening remains walkable.
            if(level==0)
            {
                if(front && side==1){foreach(int edgeSide in new[]{-1,1})AddSolid(b,new Vector3(edgeSide*(p.Width/4+1.2f),p.Height/2,edge),new Vector3(p.Width/2-2.4f,p.Height,.3f));}
                else AddSolid(b,front?new Vector3(0,p.Height/2,side*edge):new Vector3(side*edge,p.Height/2,0),front?new Vector3(length,p.Height,.3f):new Vector3(.3f,p.Height,length));
            }
        }
        static void Furniture(ExplorableBuilding b,int floor)
        {
            var p=b.Plan;var r=b.transform;float y=floor*p.Story;Color accent=Palettes[(p.Decor+floor)%6];int kind=(p.Decor+floor)%3;
            float x=-p.Width*.25f,z=kind==1?-p.Depth*.24f:0;
            Box(r,"Interior rug",new Vector3(x,y+.08f,z),new Vector3(p.Width*.4f,.035f,p.Depth*.42f),accent);
            if(kind==0)
            {
                Box(r,"Sofa seat",new Vector3(x,y+.45f,z-2),new Vector3(3.4f,.9f,1.4f),accent);Box(r,"Sofa back",new Vector3(x,y+1.1f,z-2.5f),new Vector3(3.4f,1.1f,.3f),accent);
                Box(r,"Coffee table",new Vector3(x,y+.52f,z+.2f),new Vector3(2.2f,1.04f,1.2f),Art.White);AddSolid(b,new Vector3(x,y+.55f,z),new Vector3(2.2f,1.1f,1.2f));
                Box(r,"Colorful book",new Vector3(x,y+1.12f,z+.2f),new Vector3(.5f,.12f,.4f),Art.Red);
            }
            else if(kind==1)
            {
                Box(r,"Bed frame",new Vector3(x,y+.35f,z),new Vector3(2.8f,.7f,4),Art.White);Box(r,"Soft duvet",new Vector3(x,y+.83f,z),new Vector3(2.65f,.26f,3.7f),accent);Box(r,"Pillow",new Vector3(x,y+1.03f,z+1.2f),new Vector3(2.2f,.3f,.75f),Art.White);AddSolid(b,new Vector3(x,y+.6f,z),new Vector3(2.8f,1.2f,4));
                Box(r,"Bedside cabinet",new Vector3(x+2.3f,y+.65f,z+1.1f),new Vector3(1,1.3f,1),accent);
            }
            else
            {
                Box(r,"Dining table",new Vector3(x,y+.9f,z),new Vector3(3.5f,.2f,2),Art.White);foreach(int side in new[]{-1,1}){Box(r,"Dining chair",new Vector3(x+side*2.1f,y+.5f,z),new Vector3(.8f,1,.9f),accent);Art.Disk("Dinner plate",r,new Vector3(x+side*.85f,y+1.03f,z),.36f,.03f,Art.Yellow);}AddSolid(b,new Vector3(x,y+.55f,z),new Vector3(3.5f,1.1f,2));
            }
            Box(r,"Kitchen cabinets",new Vector3(-p.Width*.22f,y+.55f,-p.Depth/2+.85f),new Vector3(p.Width*.45f,1.1f,1.2f),accent);Box(r,"Kitchen countertop",new Vector3(-p.Width*.22f,y+1.15f,-p.Depth/2+.85f),new Vector3(p.Width*.45f,.15f,1.35f),Art.White);
            Box(r,"Bookcase",new Vector3(-p.Width/2+.65f,y+1.2f,(p.Floors>=3?-1:p.Depth*.23f)),new Vector3(.6f,2.4f,2),Art.White);
            for(int i=0;i<5;i++)Box(r,"Books",new Vector3(-p.Width/2+1,y+.5f+i*.35f,(p.Floors>=3?-1:p.Depth*.23f)),new Vector3(.22f,.22f,1.6f),i%2==0?accent:Art.Yellow);
            Art.Ball("Interior leafy plant",r,new Vector3(-p.Width/2+1.5f,y+1.4f,(p.Floors>=3?-p.Depth/2+2:p.Depth/2-1.5f)),new Vector3(1.3f,2,1.3f),Art.Green);
        }
        static void FloorSurface(Transform root,ExplorableBuilding building,float y,Color color)
        {
            var p=building.Plan;
            var xs=new List<float>{-p.Width/2,p.StairX-1.7f,p.Width/2};var zs=new List<float>{-p.Depth/2,p.StairStart,p.StairEnd,p.Depth/2};
            if(p.Floors>=3){xs.Add(p.LiftX-1.55f);xs.Add(p.LiftX+1.55f);zs.Add(p.LiftZ-1.55f);zs.Add(p.LiftZ+1.55f);}xs.Sort();zs.Sort();
            for(int x=0;x<xs.Count-1;x++)for(int z=0;z<zs.Count-1;z++)
            {
                float cx=(xs[x]+xs[x+1])/2,cz=(zs[z]+zs[z+1])/2;
                if(cx>p.StairX-1.7f && cz>p.StairStart && cz<p.StairEnd)continue;
                if(p.Floors>=3 && Mathf.Abs(cx-p.LiftX)<1.55f && Mathf.Abs(cz-p.LiftZ)<1.55f)continue;
                Box(root,"Floor around stairs and elevator shaft",new Vector3(cx,y-.1f,cz),new Vector3(xs[x+1]-xs[x],.2f,zs[z+1]-zs[z]),color);
                var barrier=new GameObject("Floor camera barrier",typeof(BoxCollider));barrier.transform.SetParent(root,false);barrier.transform.localPosition=new Vector3(cx,y-.1f,cz);barrier.layer=8;
                var collider=barrier.GetComponent<BoxCollider>();collider.size=new Vector3(xs[x+1]-xs[x],.2f,zs[z+1]-zs[z]);collider.enabled=false;building.CameraFloors.Add(collider);
            }
        }
        static void ElevatorLanding(ExplorableBuilding b,int floor)
        {
            var p=b.Plan;var root=b.transform;float y=floor*p.Story;
            Box(root,"Elevator back",new Vector3(p.LiftX,y+1.5f,p.LiftZ+1.6f),new Vector3(3.4f,3,.18f),Art.Hex("BCD0D0"));
            AddSolid(b,new Vector3(p.LiftX,y+1.5f,p.LiftZ+1.6f),new Vector3(3.4f,3,.18f));
            LiftCameraBarrier(root,new Vector3(p.LiftX,y+1.4f,p.LiftZ-1.6f),new Vector3(3.2f,2.8f,.1f));
            LiftCameraBarrier(root,new Vector3(p.LiftX,y+1.5f,p.LiftZ+1.6f),new Vector3(3.4f,3,.18f));
            foreach(int side in new[]{-1,1})LiftCameraBarrier(root,new Vector3(p.LiftX+side*1.65f,y+1.5f,p.LiftZ),new Vector3(.16f,3,3.2f));
            foreach(int side in new[]{-1,1}){Box(root,"Elevator side",new Vector3(p.LiftX+side*1.65f,y+1.5f,p.LiftZ),new Vector3(.16f,3,3.2f),Art.Hex("BCD0D0"));AddSolid(b,new Vector3(p.LiftX+side*1.65f,y+1.5f,p.LiftZ),new Vector3(.16f,3,3.2f));}
            Box(root,"Elevator automatic doors",new Vector3(p.LiftX,y+1.4f,p.LiftZ-1.6f),new Vector3(3.15f,2.8f,.1f),Art.Hex("87AAA9"));
            Box(root,"Elevator door seam",new Vector3(p.LiftX,y+1.4f,p.LiftZ-1.66f),new Vector3(.035f,2.8f,.02f),Art.White);
            AddSolid(b,new Vector3(p.LiftX,y+1.4f,p.LiftZ-1.6f),new Vector3(3.15f,2.8f,.1f));
            Box(root,"Elevator entry lintel",new Vector3(p.LiftX,y+2.95f,p.LiftZ-1.6f),new Vector3(3.4f,.2f,.2f),Art.Teal);
            Box(root,"Elevator call panel",new Vector3(p.LiftX+1.82f,y+1.25f,p.LiftZ-1.7f),new Vector3(.22f,.5f,.08f),Art.Teal);
            AdventureWorlds.Caption(floor==p.Floors?"LIFT · ROOF":"LIFT · "+(floor+1),root,new Vector3(p.LiftX,y+2.55f,p.LiftZ-1.73f),180,2.4f);
        }
        static void LiftCameraBarrier(Transform root,Vector3 p,Vector3 size)
        {
            var obj=new GameObject("Elevator camera barrier",typeof(BoxCollider));obj.transform.SetParent(root,false);obj.transform.localPosition=p;obj.layer=8;obj.GetComponent<BoxCollider>().size=size;
        }
        static void AddSolid(ExplorableBuilding b,Vector3 local,Vector3 size)
        {
            Point world=b.Plan.World(new Point(local.x,local.y,local.z));float c=Mathf.Abs(Mathf.Cos(b.Plan.Yaw)),s=Mathf.Abs(Mathf.Sin(b.Plan.Yaw));b.Interior.Add(new Obstacle(world.X,world.Z,(size.x*c+size.z*s)/2,(size.z*c+size.x*s)/2,world.Y+size.y/2,world.Y-size.y/2));
        }
        public static DoorSite DoorSite(ExplorableBuilding b){Point e=b.Plan.World(new Point(0,0,b.Plan.Depth/2+1.3f));return new DoorSite{Name=b.Plan.Name,Hinge=b.Door,Entrance=new Vector3(e.X,e.Y,e.Z),Building=b};}
    }
    public sealed class BuildingLife
    {
        readonly CityGame game;public ExplorableBuilding Active { get; private set; }
        public bool Inside=>Active!=null;
        public BuildingLife(CityGame owner){game=owner;}
        public string Context
        {
            get
            {
                if(!Inside)return "";if(game.State.CanRideInside)return "Ride slowly · Ctrl / STOP brakes · Leave through the open front door";if(!string.IsNullOrEmpty(game.Lift.Context))return game.Lift.Context;var p=Active.Plan;Point local=p.Local(game.State.Position);int floor=p.FloorNumber(game.State.Position);
                if(floor==0 && local.Z>p.Depth/2-2.2f && Mathf.Abs(local.X)<2.2f)return "F • Leave "+p.Name;
                return (floor==p.Floors?"ROOFTOP":"FLOOR "+(floor+1)+" / "+p.Floors)+" • Walk up the stairs on the right · Return via ground-floor door";
            }
        }
        public void Enter(ExplorableBuilding building)
        {
            foreach(var floor in building.CameraFloors)floor.enabled=true;
            if(game.State.CanRideInside)
            {
                Active=building;BikeInterior.Enter(game,building);game.State.InteriorObstacles.Clear();game.State.InteriorObstacles.AddRange(building.Interior);building.Door.localRotation=Quaternion.Euler(0,-100,0);game.RefreshPlayer();game.State.Say("Door open. Ride through slowly; Space jumps, Ctrl stops. Steer while stopped to turn.");return;
            }
            Active=building;var p=building.Plan;Point spawn=p.World(new Point(0,0,p.Depth/2-1.6f));game.State.EnterBuilding(spawn,p.X,p.Z,p.Width/2-.25f,p.Depth/2-.25f,p.Name,p.FloorAt,p.Yaw+Mathf.PI);game.State.InteriorObstacles.Clear();game.State.InteriorObstacles.AddRange(building.Interior);building.Door.localRotation=Quaternion.Euler(0,-100,0);game.RefreshPlayer();
        }
        public bool Use()
        {
            if(Inside && game.State.CanRideInside){game.State.Say("Ride through the open front door to leave. Ctrl stops; steer to turn.");return true;}
            if(!Inside)return false;if(game.Lift.Use())return true;var p=Active.Plan;Point local=p.Local(game.State.Position);
            if(p.FloorNumber(game.State.Position)==0 && local.Z>p.Depth/2-2.2f && Mathf.Abs(local.X)<2.2f)Leave();return true;
        }
        public void TickBikeDoor(){if(!Inside || !game.State.CanRideInside)return;var p=Active.Plan;var local=p.Local(game.State.Position);if(local.Y<.5f && local.Z>p.Depth/2+2.2f){foreach(var floor in Active.CameraFloors)floor.enabled=false;Active.Door.localRotation=Quaternion.identity;Active=null;game.State.ExitBikeDoor();game.Hud.Refresh();}}
        public void Leave(){if(!Inside)return;game.Lift.Cancel();foreach(var floor in Active.CameraFloors)floor.enabled=false;Active.Door.localRotation=Quaternion.identity;Active=null;game.State.InteriorBoundary=null;game.State.ExitInterior();game.RefreshPlayer();}
    }
}
