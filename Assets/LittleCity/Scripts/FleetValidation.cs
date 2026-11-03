using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class FleetValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-fleet")>=0)new GameObject("Fleet validation").AddComponent<FleetValidation>();}
        CityGame g;string folder;
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--fleet-output")+1];Directory.CreateDirectory(folder);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;int saved=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0),night=PlayerPrefs.GetInt("LittleCity.Night",0);
            g.Hud.BeginExploring();g.Worlds.VisitWorld(0);g.SelectRide(Ride.Walk);g.State.EnterVehicle(Ride.RaceCar,3,new Point(-32,0,-15),0);g.RefreshPlayer();g.HeldKey=k=>false;if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.OrbitCamera(120,0);
            float elapsed=0;
            foreach(bool high in new[]{false,true,false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.7f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsOffroad>().ShowingHigh==high,"Actual safari variant switch");
                yield return Capture(high?"safari-high":"safari-low");
                if(high){elapsed=0;for(int n=0;n<180;n++){yield return null;elapsed+=Time.unscaledDeltaTime;}}
            }
            g.State.Place(new Point(-96,0,-92));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.5f);g.HeldKey=k=>k==KeyCode.W;float start=g.State.DistanceTravelled;
            yield return new WaitForSecondsRealtime(5);float distance=g.State.DistanceTravelled-start;Require(distance>35,"Safari actual live driving");yield return Capture("safari-driving");g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(g.State.Speed)<.01f,"Safari braking");g.HeldKey=k=>false;
            g.SelectRide(Ride.Walk);g.State.Place(new Point(-29,0,-16));g.RefreshPlayer();g.OrbitCamera(60,-8);yield return new WaitForSecondsRealtime(1);yield return Capture("broadleaf-canopy-high");
            g.Graphics.SetHigh(saved==1);while(g.Graphics.Busy)yield return null;if(CityAtmosphere.Current.Night!=(night==1))CityAtmosphere.Current.ToggleNight();
            File.WriteAllText(Path.Combine(folder,"fleet-runtime-results.txt"),"Standalone Mac safari graphics: Low/High repeated switching, "+distance.ToString("F1")+" m actual live driving and Space braking passed. High stationary FPS="+(180/elapsed).ToString("F1")+" at "+Screen.width+"x"+Screen.height+". Actual safari and foliage screenshots captured. Injected keyboard input, not physical hardware or iPad verification. Preferences restored.\n");Debug.Log("FLEET_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Require(bool ok,string message){if(ok)return;Debug.LogError("FLEET_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
