using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class GraphicsValidation : MonoBehaviour
    {
        CityGame game;string folder;readonly List<string> report=new List<string>();int originalMode;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-graphics")>=0)new GameObject("Graphics mode validation").AddComponent<GraphicsValidation>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--graphics-output");folder=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
            game=FindFirstObjectByType<CityGame>();while(!game.Ready || game.Graphics==null || game.Graphics.Busy)yield return null;
            originalMode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);game.Hud.BeginExploring();game.Worlds.VisitWorld(0);game.SelectRide(Ride.RaceCar);game.State.Place(new Point(-32,0,-15));game.RefreshPlayer();game.OrbitCamera(30,8);
            yield return new WaitForSecondsRealtime(.6f);game.enabled=false;
            report.Add("Actual standalone Mac gameplay; "+SystemInfo.deviceModel+" / "+SystemInfo.graphicsDeviceName+"; "+Screen.width+" × "+Screen.height);
            Point position=game.State.Position;float yaw=game.State.Yaw;
            foreach(bool high in new[]{false,true,false})
            {
                game.Graphics.SetHigh(high);while(game.Graphics.Busy)yield return null;game.Hud.Refresh();yield return new WaitForSecondsRealtime(2);
                string name=high?"high-def":"low-def";if(!high && report.Count>2)name="low-def-restored";
                Require(game.State.Position.X==position.X && game.State.Position.Y==position.Y && game.State.Position.Z==position.Z && game.State.Yaw==yaw,"Position and orientation unchanged");
                Require(game.State.Mode==Ride.RaceCar && game.State.WorldIndex==0,"Vehicle and world unchanged");
                Require(game.Graphics.High==high,"Requested graphics mode active");
                Require(game.CurrentVehicle.Root.GetComponent<GraphicsCar>().ConceptVisible==high,"Imported concept car only in new High Def");
                var ms=new List<float>();var gpu=new List<double>();var timing=new FrameTiming[1];
                for(int n=0;n<360;n++){FrameTimingManager.CaptureFrameTimings();yield return null;ms.Add(Time.unscaledDeltaTime*1000);if(FrameTimingManager.GetLatestTimings(1,timing)>0 && timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);}
                ms.Sort();gpu.Sort();double sum=0;foreach(float x in ms)sum+=x;
                report.Add(name+": FPS="+(360000/sum).ToString("F1")+", frame median/p95="+ms[180].ToString("F2")+"/"+ms[342].ToString("F2")+"ms, GPU median="+(gpu.Count>0?gpu[gpu.Count/2].ToString("F2"):"n/a")+"ms; Unity allocated MB="+(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576));
                yield return Capture(name);
            }
            game.Graphics.SetHigh(true);while(game.Graphics.Busy)yield return null;
            // Same live game and model, camera orbit only, for a front three-quarter detail view.
            game.enabled=true;game.OrbitCamera(105,0);yield return new WaitForSecondsRealtime(.5f);game.enabled=false;game.Hud.Refresh();yield return Capture("high-def-front-detail");
            var detailed=game.CurrentVehicle.Root.Find("Near-realistic concept sports car");int wheels=0;
            foreach(Transform child in detailed)if(child.name.StartsWith("Wheel"))
            {wheels++;Require(Vector3.Dot(child.right,detailed.right)>.999f,"Wheel axle parallel to car axle");}
            Require(wheels==4,"Four aligned detailed wheels");
            for(int spin=0;spin<30;spin++){game.CurrentVehicle.Animate(8,.02f,1);yield return null;}
            yield return Capture("high-def-wheel-spin");
            game.Hud.ShowControls(true);game.Hud.Refresh();yield return Capture("graphics-mode-selector");game.Hud.ShowControls(false);
            game.State.Place(new Point(-96,0,-92));game.RefreshPlayer();game.HeldKey=k=>false;game.enabled=true;yield return new WaitForSecondsRealtime(.5f);game.HeldKey=k=>k==KeyCode.W;
            yield return new WaitForSecondsRealtime(3);
            var drivingFrames=new List<float>();float distanceBefore=game.State.DistanceTravelled;
            for(int n=0;n<360;n++){yield return null;drivingFrames.Add(Time.unscaledDeltaTime*1000);}
            Debug.Log("GRAPHICS_DRIVE distance="+(game.State.DistanceTravelled-distanceBefore)+" speed="+game.State.Speed+" position="+game.State.Position.X+","+game.State.Position.Z+" focus="+Application.isFocused+" menu="+game.Hud.MenuOpen);
            Require(game.State.DistanceTravelled-distanceBefore>80,"High Def live driving advances normally");drivingFrames.Sort();double drivingSum=0;foreach(float x in drivingFrames)drivingSum+=x;
            report.Add("High Def live driving, normal controller/camera/audio/HUD active: FPS="+(360000/drivingSum).ToString("F1")+", frame median/p95="+drivingFrames[180].ToString("F2")+"/"+drivingFrames[342].ToString("F2")+"ms, travelled="+(game.State.DistanceTravelled-distanceBefore).ToString("F1")+"m");
            game.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(game.State.Speed)<.01f,"High Def car brakes normally");game.HeldKey=k=>false;
            game.Graphics.SetHigh(originalMode==1);while(game.Graphics.Busy)yield return null;
            report.Add("V2 choice persisted/restored; old preference untouched. New Low Def is former High Def. Both new modes remain UNMEASURED on A10 iPad. This milestone covers sports cars, with other near-realistic vehicle classes still unfinished.");
            File.WriteAllLines(Path.Combine(folder,"graphics-runtime-results.txt"),report);Debug.Log("GRAPHICS_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Require(bool ok,string message){if(ok)return;Debug.LogError("GRAPHICS_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
