using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace LittleCity
{
 public sealed class CarDamageValidation:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-car-dents")>=0)new GameObject("Car damage validation").AddComponent<CarDamageValidation>();}
  CityGame g;string folder;bool done;float began;int oldGraphics;readonly List<string> report=new List<string>();
  void Update(){if(!done&&Time.realtimeSinceStartup-began>240)Need(false,"Timeout");}
  IEnumerator Start()
  {
   began=Time.realtimeSinceStartup;oldGraphics=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);Application.logMessageReceived+=(m,t,type)=>{if(type==LogType.Exception&&!done){done=true;Restore();Debug.LogError("CAR_DENTS_FAILED "+m);Application.Quit(2);}};
   var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--damage-output")+1];Directory.CreateDirectory(folder);g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.HeldKey=k=>false;g.enabled=false;
   foreach(int style in new[]{0,1,2})foreach(bool high in new[]{false,true})
   {
    g.ResetHome();g.State.ChooseCar(style);g.SelectRide(Ride.RaceCar);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
    g.State.Obstacles.Clear();g.State.TrafficObstruction=null;g.State.Obstacles.Add(new Obstacle(30,-32,.2f,4,5));
    string label="car-"+style+(high?"-high":"-low");g.State.Place(new Point(26,0,-32),Mathf.PI/2);g.RefreshPlayer();View();yield return Capture(label+"-before");
    for(int n=1;n<=10;n++)
    {
     g.State.Place(new Point(0,0,-32),Mathf.PI/2);for(int f=0;f<240;f++)g.State.Step(1f/60,new DriveInput{Forward=1});g.RefreshPlayer();Need(g.State.CarCrashes==n,label+" separate crash "+n);Need(g.State.NeedsRepair==(n==10),label+" breakdown threshold "+n);
     var damage=g.CurrentVehicle.Root.GetComponent<CarDamageVisual>();Need(damage.MaximumDeformation>.025f,label+" actual body deformation");
     if(n==1||n==5||n==9||n==10){View();yield return Capture(label+"-impact-"+n);}
    }
    float x=g.State.Position.X;for(int f=0;f<180;f++)g.State.Step(1f/60,new DriveInput{Forward=1});Need(Mathf.Abs(g.State.Speed)<.01f&&Mathf.Abs(g.State.Position.X-x)<.2f,label+" broken propulsion stopped");
    g.RepairCar();Need(g.State.CarCrashes==0&&!g.State.NeedsRepair,label+" repair resets dents and propulsion");View();yield return Capture(label+"-repaired");
   }
   Restore();report.Add("Standalone Mac offscreen renders of real deformed meshes; distinct crashes advanced through the runtime simulation. No physical iPad claim.");File.WriteAllLines(Path.Combine(folder,"car-damage-results.txt"),report);done=true;Debug.Log("CAR_DENTS_PASSED");Application.Quit(0);
  }
  void View(){var p=g.CurrentVehicle.Root.position;Camera.main.transform.position=p+new Vector3(4.4f,2.7f,3.9f);Camera.main.transform.LookAt(p+Vector3.up*.8f);}
  IEnumerator Capture(string name)
  {
   yield return new WaitForSecondsRealtime(.2f);
   var camera=Camera.main;var previous=camera.targetTexture;var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
   var canvases=FindObjectsByType<Canvas>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;}Canvas.ForceUpdateCanvases();camera.Render();
   var before=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());RenderTexture.active=before;camera.targetTexture=previous;
   foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}Destroy(t);rt.Release();Destroy(rt);
  }
  void Restore(){PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",oldGraphics);PlayerPrefs.Save();}
  void Need(bool ok,string message){if(ok){report.Add("PASS "+message);return;}Restore();done=true;Debug.LogError("CAR_DENTS_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
