using UnityEngine;
namespace LittleCity
{
    public sealed class BuildingLift
    {
        readonly CityGame game;
        public bool Moving { get; private set; }
        Transform cabin;BuildingPlan plan;float from,to,elapsed,duration;int destination;
        public BuildingLift(CityGame owner){game=owner;}
        public bool Nearby
        {
            get
            {
                if(!game.Buildings.Inside || Moving || game.State.Resting!=RestPose.None)return false;
                var p=game.Buildings.Active.Plan;if(p.Floors<3)return false;Point local=p.Local(game.State.Position);
                return Mathf.Abs(local.X-p.LiftX)<2 && local.Z>p.LiftZ-4 && local.Z<p.LiftZ+1.5f;
            }
        }
        public string Context=>Moving?"":Nearby?"F • Elevator · choose a floor":"";
        public bool Use()
        {
            if(!Nearby)return false;var p=game.Buildings.Active.Plan;game.Hud.ShowElevator(p.Name,p.Floors,p.FloorNumber(game.State.Position));return true;
        }
        public bool Travel(int floor)
        {
            if(!Nearby)return false;plan=game.Buildings.Active.Plan;if(floor<0 || floor>plan.Floors)return false;
            destination=floor;from=plan.FloorNumber(game.State.Position)*plan.Story;to=floor*plan.Story;elapsed=0;duration=Mathf.Max(.7f,Mathf.Abs(to-from)/10);
            Moving=true;game.State.EndRest();
            cabin=Art.Group("Moving elevator cabin",game.Buildings.Active.transform,new Vector3(plan.LiftX,from,plan.LiftZ));cabin.gameObject.AddComponent<MovingScenery>();
            Art.Box("Lift cabin floor",cabin,new Vector3(0,-.08f,0),new Vector3(2.8f,.16f,2.8f),Art.Hex("B5CCCB"),0);
            Art.Box("Lift canopy",cabin,new Vector3(0,2.9f,0),new Vector3(2.8f,.12f,2.8f),Art.White,0);
            foreach(int side in new[]{-1,1})Art.GlassPane("Lift cabin glass",cabin,new Vector3(side*1.4f,0,-1.4f),new Vector3(side*1.4f,0,1.4f),new Vector3(side*1.4f,2.8f,1.4f),new Vector3(side*1.4f,2.8f,-1.4f));
            game.State.Place(plan.World(new Point(plan.LiftX,from,plan.LiftZ)),plan.Yaw+Mathf.PI);game.Hud.BeginExploring();game.RefreshPlayer();return true;
        }
        public void Tick(float dt)
        {
            if(!Moving)return;elapsed+=dt;float t=Mathf.Clamp01(elapsed/duration),y=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t));
            cabin.localPosition=new Vector3(plan.LiftX,y,plan.LiftZ);game.State.Place(plan.World(new Point(plan.LiftX,y,plan.LiftZ)),plan.Yaw+Mathf.PI);
            game.State.RoomName="Elevator · "+(destination==plan.Floors?"Roof":"Floor "+(destination+1));
            if(t>=1){Moving=false;Object.Destroy(cabin.gameObject);cabin=null;game.State.Place(plan.World(new Point(plan.LiftX,to,plan.LiftZ-2.4f)),plan.Yaw+Mathf.PI);game.State.RoomName=plan.Name;game.State.Say(destination==plan.Floors?"Rooftop":"Floor "+(destination+1));game.RefreshPlayer();}
        }
        public bool FrameCamera(Camera camera)
        {
            if(!Moving || cabin==null)return false;
            camera.transform.position=cabin.TransformPoint(new Vector3(.95f,2.65f,-1.15f));
            camera.transform.LookAt(cabin.TransformPoint(new Vector3(0,1.1f,0)));camera.fieldOfView=66;return true;
        }
        public void Cancel(){Moving=false;if(cabin!=null)Object.Destroy(cabin.gameObject);cabin=null;}
    }
}
