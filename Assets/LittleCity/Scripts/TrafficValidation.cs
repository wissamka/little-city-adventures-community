using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity {
 public sealed class TrafficValidation:MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-traffic")>=0)new GameObject("Traffic runtime validation").AddComponent<TrafficValidation>();}
  CityGame game;TaxiFleet fleet;string folder;bool tracking;
  IEnumerator Start() {
   Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int a=Array.IndexOf(args,"--traffic-output");folder=args[a+1];Directory.CreateDirectory(folder);
   game=FindFirstObjectByType<CityGame>();while(!game.Ready||game.Graphics.Busy)yield return null;
   int previous=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);
   game.Hud.BeginExploring();game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(2);while(game.Worlds.Busy)yield return null;
   game.SelectRide(Ride.Walk);game.State.Place(new Point(172,0,-125));game.RefreshPlayer();game.enabled=false;game.Hud.Refresh();fleet=FindFirstObjectByType<TaxiFleet>();Require(fleet!=null&&fleet.Cabs.Count==4,"Bounded New York taxi fleet");tracking=true;
   game.Graphics.SetHigh(false);while(game.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(3);game.Hud.Refresh();yield return Capture("new-york-taxis-low");
   var cab=fleet.Cabs[0];float before=cab.Travelled;var heading=cab.Forward;
   game.Graphics.SetHigh(true);while(game.Graphics.Busy)yield return null;
   float end=Time.realtimeSinceStartup+23;while(Time.realtimeSinceStartup<end){game.Hud.Refresh();yield return null;}
   Require(cab.Travelled-before>100,"Actual Update taxis advance along streets");Require(Vector3.Dot(heading,cab.Forward)<.6f,"Actual route corner changes heading");
   foreach(var c in fleet.Cabs)Require(c.Model.Root.GetComponent<GraphicsCar>().ConceptVisible,"Every taxi participates in High Def");yield return Capture("new-york-taxis-high-turn");
   float distance=cab.Travelled-before;tracking=false;game.Graphics.SetHigh(previous==1);while(game.Graphics.Busy)yield return null;
   File.WriteAllText(Path.Combine(folder,"traffic-runtime-results.txt"),"Actual standalone Mac: four New York taxis run their own Update AI, distance="+distance.ToString("F1")+"m in23seconds, corner heading change verified; Low/High taxi geometry and roof signs captured. Camera automatically tracks a taxi in the real game world. No injected driving input for traffic. Player/queue/full-loop checks in PlayMode tests.\n");Debug.Log("TRAFFIC_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
  }
  void LateUpdate(){if(!tracking||fleet==null)return;var cab=fleet.Cabs[0];var p=cab.Model.Root.position;var side=new Vector3(cab.Forward.z,0,-cab.Forward.x);Camera.main.transform.position=p+cab.Forward*7+side*5+Vector3.up*3.3f;Camera.main.transform.LookAt(p+Vector3.up*.9f);}
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
  void Require(bool value,string message){if(value)return;Debug.LogError("TRAFFIC_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
