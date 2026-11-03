using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class BikeHomeValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-bike-homes")>=0)new GameObject("Bike home validation").AddComponent<BikeHomeValidation>();}
        CityGame g;string folder;float maxStep;bool stairCaptured;readonly System.Collections.Generic.List<string> graphicsReport=new System.Collections.Generic.List<string>();
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--bike-output")+1];Directory.CreateDirectory(folder);
            int mode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0),night=PlayerPrefs.GetInt("LittleCity.Night",0);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.Worlds.VisitWorld(0);g.enabled=false;g.HeldKey=k=>false;if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
            foreach(var ride in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike,Ride.Motorcycle,Ride.DirtBike})
            {
                g.SelectRide(ride);g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.enabled=true;g.OrbitCamera(105,0);
                foreach(bool high in new[]{false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.8f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsBike>().ShowingHigh==high,"Bike graphics switch");yield return Capture(ride.ToString().ToLowerInvariant()+(high?"-detail-high":"-detail-low"));}
                float elapsed=0;for(int frame=0;frame<120;frame++){yield return null;elapsed+=Time.unscaledDeltaTime;}graphicsReport.Add(ride+" High Def stationary FPS="+(120/elapsed).ToString("F1")+"; vertices="+g.CurrentVehicle.Root.GetComponent<GraphicsBike>().DetailVertices);g.enabled=false;
            }
            File.WriteAllLines(Path.Combine(folder,"bike-graphics-runtime-results.txt"),graphicsReport);
            foreach(var ride in new[]{Ride.DirtBike,Ride.Bicycle,Ride.MountainBike,Ride.MiniBike})
            {
                g.SelectRide(ride);g.State.Place(new Point(13,0,-42.2f),Mathf.PI);g.RefreshPlayer();g.OrbitCamera(35,0);g.SendMessage("LateUpdate");yield return null;if(ride==Ride.DirtBike)yield return Capture("large-dirt-bike-outside");
                var original=g.State.Position;g.Interact();yield return null;Require(g.State.Indoors&&g.State.Position.X==original.X&&g.State.Position.Z==original.Z,"Door opens without moving rider");g.enabled=true;var p=g.Buildings.Active.Plan;
                float corridor=p.StairX-2.5f,low=p.StairStart-.32f,high=p.StairEnd+.45f;
                yield return Drive(p,new Point(0,0,high));if(ride==Ride.DirtBike)yield return Capture("dirt-bike-inside-home");
                yield return Drive(p,new Point(corridor,0,high));yield return Drive(p,new Point(corridor,0,low));yield return Drive(p,new Point(p.StairX,0,low));
                yield return Drive(p,new Point(p.StairX,p.Story,high));Require(Mathf.Abs(g.State.Position.Y-p.Story)<.1f,"Upstairs "+ride);yield return Capture(ride.ToString().ToLowerInvariant()+"-upstairs");
                yield return Drive(p,new Point(p.StairX,0,low),true);Require(g.State.Position.Y<.1f,"Downstairs "+ride);
                yield return Drive(p,new Point(corridor,0,low));yield return Drive(p,new Point(corridor,0,high));yield return Drive(p,new Point(0,0,high));yield return Drive(p,new Point(0,0,p.Depth/2+2.4f));Require(!g.State.Indoors,"Ride through open exit "+ride);
                g.SelectRide(Ride.Walk);
            }
            g.Graphics.SetHigh(mode==1);while(g.Graphics.Busy)yield return null;PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(folder,"bike-runtime-results.txt"),"Standalone Mac: large dirt bike, bicycle, MTB and mini bike opened actual house door without teleport, rode inside, turned onto stairs, climbed and reversed down, braked/restarted, and exited through the open door. Live game Update driven by injected keyboard inputs; not physical keyboard/iPad verification. Maximum frame displacement "+maxStep.ToString("F3")+" m. Actual screenshots rendered along the routes using the ordinary chase camera. Preferences restored.\n");Debug.Log("BIKE_HOME_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Drive(BuildingPlan p,Point local,bool reverse=false)
        {
            var target=p.World(local);
            for(int n=0;n<6000;n++)
            {
                float x=target.X-g.State.Position.X,z=target.Z-g.State.Position.Z;if(x*x+z*z<.025f){g.HeldKey=k=>k==KeyCode.LeftControl;yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>false;yield return null;yield break;}
                float desired=Mathf.Atan2(x,z)+(reverse?Mathf.PI:0),error=Mathf.DeltaAngle(g.State.Yaw*Mathf.Rad2Deg,desired*Mathf.Rad2Deg)*Mathf.Deg2Rad;var before=g.State.Position;
                g.HeldKey=k=>k==KeyCode.D&&error>.045f || k==KeyCode.A&&error<-.045f || k==(reverse?KeyCode.S:KeyCode.W)&&Mathf.Abs(error)<.12f || k==KeyCode.LeftControl&&Mathf.Abs(error)>=.12f;
                yield return null;var after=g.State.Position;maxStep=Mathf.Max(maxStep,new Vector2(after.X-before.X,after.Z-before.Z).magnitude);Require(maxStep<.4f,"Continuous indoor movement");
                if(g.State.Mode==Ride.DirtBike&&!stairCaptured&&g.State.Position.Y>p.Story*.45f&&g.State.Position.Y<p.Story*.7f){stairCaptured=true;yield return Capture("dirt-bike-on-stairs");}
            }
            var here=p.Local(g.State.Position);Require(false,"Bike route blocked: "+g.State.Mode+" to "+local.X+","+local.Z+" from "+here.X+","+here.Y+","+here.Z+" yaw="+g.State.Yaw+" speed="+g.State.Speed+" focused="+Application.isFocused);
        }
        void Frame(){g.SendMessage("UpdateActor",.2f);g.SendMessage("LateUpdate");g.Hud.Refresh();}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);if(name=="dirt-bike-on-stairs"||name.EndsWith("-upstairs")){var head=g.Explorer.Head.position;var screen=Camera.main.WorldToViewportPoint(head);Require(screen.z>Camera.main.nearClipPlane && screen.x>.02f&&screen.x<.98f&&screen.y>.02f&&screen.y<.98f,"Rider head visible in stair camera: "+name);Require(!Physics.Linecast(Camera.main.transform.position,head,1<<8,QueryTriggerInteraction.Ignore),"Stair camera has clear view of rider: "+name);}}
        void Require(bool ok,string message){if(ok)return;Debug.LogError("BIKE_HOME_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
