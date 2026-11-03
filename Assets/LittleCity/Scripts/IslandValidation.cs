using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace LittleCity
{
 public sealed class IslandValidation:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-islands")>=0)new GameObject("Island validation").AddComponent<IslandValidation>();}
  CityGame g;string folder;float began;bool done;readonly List<string> report=new List<string>();int oldCharacter,oldLook,oldGraphics;
  void Update(){if(!done&&Time.realtimeSinceStartup-began>300)Need(false,"Validation timeout");}
  IEnumerator Start()
  {
   began=Time.realtimeSinceStartup;Application.logMessageReceived+=(message,trace,type)=>{if(type==LogType.Exception&&!done){done=true;Restore();Debug.LogError("ISLAND_RUNTIME_FAILED "+message);Application.Quit(2);}};var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--island-output")+1];Directory.CreateDirectory(folder);g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
   oldCharacter=PlayerPrefs.GetInt("LittleCity.Character",0);oldLook=PlayerPrefs.GetInt("LittleCity.IslandScarf",0);oldGraphics=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);g.SendMessage("OnApplicationFocus",true);g.HeldKey=k=>false;g.Hud.BeginExploring();g.SelectCharacter(4);g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();g.OrbitCamera(180,-3);yield return Capture("River-tall");Need(g.State.WalkingScale>.9f,"River pronounced height setting");
   g.Hud.ShowCharacters();yield return Capture("six-characters-height-comparison");g.Hud.BeginExploring();g.Worlds.VisitMarina(0);g.RecenterCamera();g.OrbitCamera(140,7);g.enabled=false;Camera.main.transform.position=new Vector3(-145,53,50);Camera.main.transform.LookAt(new Vector3(-65,5,116));yield return Capture("larger-yachts-marina");g.enabled=true;
   for(int yacht=0;yacht<3;yacht++)
   {
    g.Worlds.VisitMarina(0);g.State.Place(new Point(yacht==0?-65:yacht==1?-130:-5,0,yacht==0?60:65));g.Interact();g.State.Place(new Point(6,0,-28.7f));for(int f=0;f<180;f++)g.State.Step(1f/60,new DriveInput{Forward=1});g.RefreshPlayer();Need(g.State.Position.Y>5.3f,"Yacht "+yacht+" taller connected stairs");yield return Capture("yacht-"+yacht+"-sun-deck");
    var helm=g.Worlds.Yachts.Spots.First(s=>s.Helm);g.State.Place(new Point(helm.Near.x,helm.Near.y,helm.Near.z));g.Interact();Need(g.State.Mode==Ride.Yacht,"Yacht "+yacht+" helm starts sailing");var p=g.State.Position;g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(5);g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);g.HeldKey=k=>false;Need(g.State.Position.Z-p.Z>15,"Yacht "+yacht+" live input moves forward");Need(g.State.Speed<.1f,"Yacht "+yacht+" STOP brakes");g.OrbitCamera(95,9);yield return Capture("yacht-"+yacht+"-sailing");
   }
   yield return Sail(new Point(-5,-1.05f,270));
   for(int i=0;i<3;i++)
   {
    yield return Sail(IslandLayout.Berth(i));g.Worlds.Voyages.Tick();g.Interact();Need(g.Worlds.Voyages.DockedIsland==i,"Continuous sea route docks at "+IslandLayout.Names[i]);yield return Capture("island-"+i+"-arrival");
    foreach(var a in g.Worlds.Islands.Activities.Where(a=>a.Island==i))
    {
     g.State.Place(new Point(a.Point.x,a.Point.y,a.Point.z));g.RefreshPlayer();Need(g.Worlds.Islands.Use(),a.Label);yield return new WaitForSecondsRealtime(.8f);g.RecenterCamera();g.OrbitCamera(115,7);yield return Capture("island-"+i+"-"+a.Kind+"-"+a.Value);if(g.State.Resting!=RestPose.None)g.Worlds.Islands.Use();
    }
    if(i==1){foreach(var t in g.Worlds.ActivePlace.Root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Collectible golden coin")){g.State.Place(new Point(t.position.x,t.position.y-.8f,t.position.z));g.Worlds.Islands.Tick(.05f);}Need(g.Worlds.Islands.Coins==8,"All eight course coins collect once");}
    var dock=IslandLayout.Dock(i);g.State.Place(dock);g.RefreshPlayer();g.Interact();Need(g.State.Mode==Ride.Yacht,"Reboard the same yacht from island "+i);
    if(i<2)yield return Sail(new Point(IslandLayout.Centers[i].X,-1.05f,260));
   }
   yield return Sail(new Point(75,-1.05f,270));yield return Sail(new Point(-5,-1.05f,250));yield return Sail(YachtVoyages.HomeBerth(2));g.Worlds.Voyages.Tick();g.Interact();Need(g.Worlds.Voyages.DockedIsland==-1,"Round trip returns to original marina berth");
   foreach(int world in new[]{0,1,2,4}){g.Worlds.VisitWorld(world);yield return new WaitForSecondsRealtime(2);Need(g.People.Count>0,"Ambient people in destination "+world);yield return Capture("visitors-world-"+world);}
   g.Worlds.VisitMarina(0);g.Worlds.Voyages.Begin(0);g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;yield return Capture("yacht-low");g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;yield return Capture("yacht-high");
   Restore();report.Add("Standalone Mac validation. Keyboard input injected for basic sailing; inter-island routes advanced continuously through the same simulation with steering/throttle, without teleporting between docks. Activity placements use test positions. No physical iPad claim.");File.WriteAllLines(Path.Combine(folder,"island-results.txt"),report);done=true;Debug.Log("ISLAND_RUNTIME_PASSED");Application.Quit(0);
  }
  IEnumerator Sail(Point target)
  {
   int frame=0;float distance=1000;
   while(frame++<5000)
   {
    var p=g.State.Position;float x=target.X-p.X,z=target.Z-p.Z;distance=Mathf.Sqrt(x*x+z*z);if(distance<17)break;
    float angle=Mathf.DeltaAngle(g.State.Yaw*Mathf.Rad2Deg,Mathf.Atan2(x,z)*Mathf.Rad2Deg);
    g.State.Step(.05f,new DriveInput{Forward=distance<65?.22f:Mathf.Abs(angle)>50?.35f:1,Steering=Mathf.Clamp(angle/20,-1,1)});
    if(frame%30==0){g.RefreshPlayer();yield return null;}
   }
   for(int j=0;j<120;j++)g.State.Step(.05f,new DriveInput{Brake=true});g.RefreshPlayer();Need(distance<17,"Sea route reaches "+target.X+", "+target.Z+" (distance "+distance+")");yield return null;
  }
  IEnumerator Capture(string name)
  {
   yield return new WaitForSecondsRealtime(.2f);
   var camera=Camera.main;var previous=camera.targetTexture;var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
   var canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;}Canvas.ForceUpdateCanvases();camera.Render();
   var before=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());RenderTexture.active=before;camera.targetTexture=previous;
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}Destroy(t);rt.Release();Destroy(rt);
  }
  void Restore(){PlayerPrefs.SetInt("LittleCity.Character",oldCharacter);PlayerPrefs.SetInt("LittleCity.IslandScarf",oldLook);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",oldGraphics);PlayerPrefs.Save();}
  void Need(bool ok,string message){if(ok){report.Add("PASS "+message);return;}Restore();done=true;Debug.LogError("ISLAND_RUNTIME_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
