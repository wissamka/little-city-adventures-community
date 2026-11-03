using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class MarineGraphicsValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-marine-graphics")>=0)new GameObject("Marine graphics validation").AddComponent<MarineGraphicsValidation>();}
        CityGame g;string folder;float started;readonly System.Collections.Generic.List<string> report=new System.Collections.Generic.List<string>();
        void Update(){if(Time.realtimeSinceStartup-started>150)Require(false,"Marine validation timed out");}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--marine-output")+1];Directory.CreateDirectory(folder);g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            g.SendMessage("OnApplicationFocus",true);g.Sounds.SendMessage("OnApplicationFocus",true);report.Add("Native render with injected keyboard and initial focus state; not physical input or iPad validation.");
            int saved=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0),night=PlayerPrefs.GetInt("LittleCity.Night",0);g.Hud.BeginExploring();g.Worlds.VisitWorld(1);g.HeldKey=k=>false;if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();
            foreach(var ride in new[]{Ride.Motorboat,Ride.Speedboat})
            {
                g.SelectRide(ride);g.State.Place(new Point(70,-1.05f,80));g.RefreshPlayer();g.OrbitCamera(115,0);
                foreach(bool high in new[]{false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.7f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsBoat>().ShowingHigh==high,"Boat mode switch");yield return Capture(ride.ToString().ToLowerInvariant()+(high?"-high":"-low"));}
                float elapsed=0;for(int frame=0;frame<180;frame++){yield return null;elapsed+=Time.unscaledDeltaTime;}g.HeldKey=k=>false;yield return null;float start=g.State.DistanceTravelled;g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(5);float distance=g.State.DistanceTravelled-start;Require(distance>25,"Live boat movement");yield return Capture(ride.ToString().ToLowerInvariant()+"-moving");
                var tow=g.CurrentVehicle.Root.GetComponent<TowRig>();if(tow!=null){Require(tow.Towing&&tow.RopeLength<8.6f,"Tow line bounded while moving");g.Interact();yield return null;Require(!tow.Towing,"F interaction stows tube");g.Interact();yield return null;Require(tow.Towing,"F interaction reattaches tube");}
                g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(g.State.Speed)<.01f,"Boat stops");g.HeldKey=k=>false;report.Add(ride+": High stationary FPS="+(180/elapsed).ToString("F1")+", live distance="+distance.ToString("F1")+"m; graphics, braking and tow checks passed.");g.SelectRide(Ride.Walk);
            }
            g.Worlds.VisitWorld(0);
            foreach(var ride in new[]{Ride.Skateboard,Ride.Scooter})
            {
                g.SelectRide(ride);g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.OrbitCamera(110,0);
                foreach(bool high in new[]{false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.6f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsBoard>().ShowingHigh==high,"Board graphics mode");yield return Capture(ride.ToString().ToLowerInvariant()+(high?"-high":"-low"));}
                g.State.Place(new Point(-96,0,-92));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.4f);float start=g.State.DistanceTravelled;g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(4);Require(g.State.DistanceTravelled-start>20,"Board live movement");g.HeldKey=k=>k==KeyCode.LeftControl;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(g.State.Speed)<.01f,"Board brakes");g.HeldKey=k=>false;report.Add(ride+": graphics and live drive/brake passed.");
            }
            g.Graphics.SetHigh(saved==1);while(g.Graphics.Busy)yield return null;if(CityAtmosphere.Current.Night!=(night==1))CityAtmosphere.Current.ToggleNight();report.Add("Actual standalone Mac; injected keyboard/game interaction inputs, not physical keyboard or iPad validation. Preferences restored.");File.WriteAllLines(Path.Combine(folder,"marine-graphics-runtime-results.txt"),report);Debug.Log("MARINE_GRAPHICS_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Require(bool ok,string message){if(ok)return;Debug.LogError("MARINE_GRAPHICS_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
