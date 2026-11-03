using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace LittleCity
{
 public sealed class CozyValidation:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-cozy")>=0)new GameObject("Cozy validation").AddComponent<CozyValidation>();}
  bool livingGlass;CityGame g;string folder;float began;bool done;readonly List<string> report=new List<string>();
  readonly Dictionary<string,int> prefs=new Dictionary<string,int>();
  void Update(){if(!done && Time.realtimeSinceStartup-began>240)Need(false,"Validation timeout");}
  IEnumerator Start()
  {
   began=Time.realtimeSinceStartup;foreach(var key in new[]{GraphicsQuality.CozyPreference,GraphicsQuality.GlassPreference,"LittleCity.Character","LittleCity.GraphicsModeV2","LittleCity.Night"})prefs[key]=PlayerPrefs.GetInt(key,0);
   Application.logMessageReceived+=OnLog;var args=Environment.GetCommandLineArgs();livingGlass=Array.IndexOf(args,"--living-glass-check")>=0;folder=args[Array.IndexOf(args,"--cozy-output")+1];Directory.CreateDirectory(folder);
   g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;g.People.enabled=false;g.HeldKey=k=>false;
   if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
   g.Worlds.VisitWorld(0);g.SelectCharacter(5);g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();View(new Vector3(-32,12,-65),new Vector3(-32,2,-20));yield return Pair("city");
   g.State.Place(new Point(13,0,-42.2f));g.Interact();Need(g.Buildings.Inside,"Enter furnished house");var home=g.Buildings.Active.Plan;
   View(new Vector3(home.X+2,2.5f,home.Z+4),new Vector3(home.X-3,1,home.Z-2));yield return Pair("home");g.Buildings.Leave();
   g.State.Place(new Point(-32,0,-32));g.SelectRide(Ride.RaceCar);g.RefreshPlayer();var car=g.CurrentVehicle.Root.position;View(car+new Vector3(4,2.8f,4),car+Vector3.up*.7f);yield return Pair("car");
   var initial=g.State.Position;CozyOn();for(int i=0;i<120;i++)g.State.Step(1f/60,new DriveInput{Forward=1});Need(g.State.Speed>1,"Cozy car accelerates");for(int i=0;i<120;i++)g.State.Step(1f/60,new DriveInput{Brake=true});Need(g.State.Speed<.1f,"Cozy car brakes");
   g.Worlds.VisitMarina(0);View(new Vector3(-145,53,50),new Vector3(-65,5,116));yield return Pair("marina");
   g.Worlds.Yachts.Board(0);g.State.Place(new Point(0,0,-17));g.RefreshPlayer();View(new Vector3(-.5f,2.4f,-24),new Vector3(-4,1,-19));yield return Pair("yacht-lounge");g.Worlds.Yachts.Leave();
   g.Worlds.Voyages.Begin(0);
   for(int i=0;i<3;i++)
   {
    g.State.Place(IslandLayout.Berth(i));g.Worlds.Voyages.Tick();g.Interact();Need(g.Worlds.Voyages.DockedIsland==i,"Dock at "+IslandLayout.Names[i]);
    var c=IslandLayout.Centers[i];View(new Vector3(c.X+62,43,c.Z-75),new Vector3(c.X,3,c.Z));yield return Pair("island-"+i);
    g.State.Place(IslandLayout.Dock(i));g.Interact();Need(g.State.Mode==Ride.Yacht,"Reboard after cozy island visit "+i);
   }
   g.Worlds.VisitWorld(2);View(new Vector3(-30,22,-40),new Vector3(0,3,0));yield return Pair("mountains");
   g.Worlds.VisitWorld(4);View(new Vector3(-105,18,4),new Vector3(-133,2,35));yield return Pair("skatepark");
   g.Worlds.VisitWorld(0);g.State.Place(new Point(-32,0,-32));g.SelectRide(Ride.Walk);g.RefreshPlayer();View(new Vector3(-32,12,-65),new Vector3(-32,2,-20));CityAtmosphere.Current.ToggleNight();yield return Pair("night");CityAtmosphere.Current.ToggleNight();
   g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;yield return Pair("city-low-detail");
   CozyOn();g.Hud.ShowControls(true);g.Hud.Refresh();yield return null;Capture("cozy-controls",true);
   Need(PlayerPrefs.GetInt(GraphicsQuality.CozyPreference)==1,"Cozy choice persisted");if(livingGlass)Need(g.Graphics.LivingGlass && PlayerPrefs.GetInt(GraphicsQuality.GlassPreference)==1,"Third Living Glass mode persisted across worlds and detail changes");Need(!g.Graphics.High,"Style independent of Low/High detail");
   Restore();File.WriteAllLines(Path.Combine(folder,"cozy-results.txt"),report);done=true;Debug.Log("COZY_RUNTIME_PASSED");Application.Quit(0);
  }
  void CozyOn(){if(livingGlass)g.Graphics.SetLivingGlass(true);else g.Graphics.SetCozy(true);}
  IEnumerator Pair(string name)
  {
   yield return new WaitForSecondsRealtime(.75f);
   CozyOn();yield return new WaitForSecondsRealtime(.4f);g.Graphics.SetCozy(false);yield return new WaitForSecondsRealtime(.4f);var original=Capture(name+"-original");
   CozyOn();yield return new WaitForSecondsRealtime(.4f);var cozy=Capture(name+"-cozy");
   Need(Difference(original,cozy)>.008f,name+" visibly changes (mean RGB difference "+Difference(original,cozy).ToString("F4")+")");
   g.Graphics.SetCozy(false);yield return new WaitForSecondsRealtime(.4f);var restored=Capture(name+"-restored");
   Need(Difference(original,restored)<.004f,name+" original look restored (difference "+Difference(original,restored).ToString("F4")+")");
   Need(g.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).All(m=>m.shader.isSupported),name+" supported materials");
   CozyOn();
  }
  void View(Vector3 p,Vector3 target){Camera.main.transform.position=p;Camera.main.transform.LookAt(target);}
  Color32[] Capture(string name,bool ui=false)
  {
   var camera=Camera.main;var previous=camera.targetTexture;var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32){antiAliasing=4};rt.Create();camera.targetTexture=rt;
   var canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;if(!ui)canvas.enabled=false;}Canvas.ForceUpdateCanvases();camera.Render();
   var before=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());var pixels=t.GetPixels32();RenderTexture.active=before;camera.targetTexture=previous;
   foreach(var canvas in canvases){canvas.enabled=true;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}Destroy(t);rt.Release();Destroy(rt);return pixels;
  }
  static float Difference(Color32[] a,Color32[] b){double n=0;for(int i=0;i<a.Length;i++)n+=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);return (float)(n/(a.Length*765));}
  void OnLog(string m,string stack,LogType type){if(type==LogType.Exception && !done){done=true;Restore();Debug.LogError("COZY_RUNTIME_FAILED "+m);Application.Quit(2);}}
  void Restore(){foreach(var p in prefs)PlayerPrefs.SetInt(p.Key,p.Value);PlayerPrefs.Save();}
  void Need(bool ok,string message){if(ok){report.Add("PASS "+message);return;}done=true;Restore();Debug.LogError("COZY_RUNTIME_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
