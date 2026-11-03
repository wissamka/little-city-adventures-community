using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace LittleCity
{
 public sealed class DriverSeatValidation:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-driver-fit")>=0)new GameObject("Driver seating validation").AddComponent<DriverSeatValidation>();}
  CityGame g;string folder;bool done;float began;int oldGraphics;readonly List<string> report=new List<string>();
  void Update(){if(!done&&Time.realtimeSinceStartup-began>240)Need(false,"Timeout");}
  IEnumerator Start()
  {
   began=Time.realtimeSinceStartup;oldGraphics=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);Application.logMessageReceived+=(m,t,type)=>{if(type==LogType.Exception&&!done){done=true;Restore();Debug.LogError("DRIVER_FIT_FAILED "+m);Application.Quit(2);}};
   var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--driver-output")+1];Directory.CreateDirectory(folder);g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.HeldKey=k=>false;g.enabled=false;
   foreach(int character in new[]{4,5,0,1,2,3})foreach(int style in new[]{0,1,2})foreach(bool high in new[]{false,true})
   {
    g.ResetHome();g.State.SelectCharacter(character);float height=g.State.WalkingScale;g.State.ChooseCar(style);g.SelectRide(Ride.RaceCar);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();yield return null;
    var car=g.CurrentVehicle;var person=g.Explorer;person.FitRide(car,false);string tag=Simulation.CharacterNames[character]+"-style-"+style+(high?"-high":"-low");
    Need(person.HandError<.045f&&person.FootError<.045f,tag+" actual wheel and pedal contacts");Need(Vector3.Distance(person.Root.TransformPoint(new Vector3(0,1.08f,0)),car.Root.TransformPoint(car.Seat))<.005f,tag+" pelvis rests on seat");
    if(high)Need(Mathf.Abs(car.Seat.x+.005f)<.01f,tag+" uses actual center driving position");
    
    {
     float headTop=float.MinValue;foreach(var f in person.Head.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices)headTop=Mathf.Max(headTop,car.Root.InverseTransformPoint(f.transform.TransformPoint(v)).y);
     Need(headTop<(high?1.40f:1.52f),tag+" head and hair clear the roof");
    }
    
    if(character==4||character==5||character==0&&style==0)
    {
     Camera.main.transform.position=car.Root.TransformPoint(new Vector3(0,2.25f,3.3f));Camera.main.transform.LookAt(car.Root.TransformPoint(car.Seat+Vector3.up*.28f));yield return Capture(tag+"-front");
     Camera.main.transform.position=car.Root.TransformPoint(new Vector3(.70f,1.30f,.90f));Camera.main.transform.LookAt(car.Root.TransformPoint(car.Seat+new Vector3(0,.20f,.22f)));yield return Capture(tag+"-interior-side");
    }
    g.State.Place(new Point(-300,0,-300));for(int i=0;i<60;i++)g.State.Step(.02f,new DriveInput{Forward=1});for(int i=0;i<100;i++)g.State.Step(.02f,new DriveInput{Brake=true});g.RefreshPlayer();Need(g.Parking.EnterOrExit(),tag+" exits after drive and brake");Need(g.State.WalkingScale==height,tag+" on-foot height preserved");Need(g.Parking.EnterOrExit(),tag+" reboards");
   }
   Restore();report.Add("Standalone Mac offscreen renders with intact car geometry and real skinned bodies; interior-side camera placed inside passenger side to expose seating contacts. No physical iPad claim.");File.WriteAllLines(Path.Combine(folder,"driver-fit-results.txt"),report);done=true;Debug.Log("DRIVER_FIT_PASSED");Application.Quit(0);
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
  void Restore(){PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",oldGraphics);PlayerPrefs.Save();}
  void Need(bool ok,string message){if(ok){report.Add("PASS "+message);return;}Restore();done=true;Debug.LogError("DRIVER_FIT_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
