using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace LittleCity
{
    public sealed class AirShowValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-airshow")>=0)new GameObject("Plane maneuver validation").AddComponent<AirShowValidation>();}
        CityGame g;string folder;float maxCameraRoll,maxStep,started;Vector3 previous;bool watching;
        void Update(){if(Time.realtimeSinceStartup-started>120)Require(false,"Native aircraft validation timeout");}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--airshow-output")+1];Directory.CreateDirectory(folder);
            int mode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0),night=PlayerPrefs.GetInt("LittleCity.Night",0);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            g.SendMessage("OnApplicationFocus",true);g.Sounds.SendMessage("OnApplicationFocus",true);
            g.Hud.BeginExploring();g.Worlds.VisitWorld(0);g.SelectRide(Ride.Airplane);if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
            g.State.Place(new Point(172,0,-130));g.RefreshPlayer();g.HeldKey=k=>false;g.OrbitCamera(115,0);
            foreach(bool high in new[]{false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.7f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsPlane>().ShowingHigh==high,"Aircraft visual mode");yield return Capture(high?"plane-detail-high":"plane-detail-low");}
            // Start over the real airfield; climb using the production flight simulation.
            g.enabled=false;g.State.Place(new Point(172,150,-130));for(int i=0;i<300;i++)g.State.Step(.02f,new DriveInput{Forward=1});g.RefreshPlayer();g.RecenterCamera();g.HeldKey=k=>false;g.enabled=true;yield return new WaitForSecondsRealtime(.3f);
            g.OrbitCamera(32,10);g.Hud.ShowControls(true);yield return Capture("plane-trick-controls");g.Hud.ShowControls(false);
            previous=Position();watching=true;g.HeldKey=k=>k==KeyCode.X;yield return null;yield return null;Require(g.State.AirManeuver==AirTrick.Loop,"X starts loop in real Update");
            yield return Progress(AirTrick.Loop,.24f);yield return Capture("plane-loop-climb");yield return Progress(AirTrick.Loop,.48f);yield return Capture("plane-loop-inverted");while(g.State.AirManeuver!=AirTrick.None)yield return null;Require(g.State.AirCompletions==1,"Loop completed");
            yield return new WaitForSecondsRealtime(.25f);Require(g.State.AirManeuver==AirTrick.None,"Held X does not repeat");g.HeldKey=k=>false;yield return null;
            g.Hud.ShowControls(true);PointerPresence.IsHeld=id=>id==71;var ev=new PointerEventData(EventSystem.current){pointerId=71};g.Hud.AirRoll.OnPointerDown(ev);yield return null;yield return null;Require(g.State.AirManeuver==AirTrick.Roll,"Touch control event starts roll");g.Hud.AirRoll.OnPointerUp(ev);PointerPresence.IsHeld=PointerPresence.ReadPhysical;g.Hud.ShowControls(false);
            yield return Progress(AirTrick.Roll,.24f);yield return Capture("plane-aileron-roll");while(g.State.AirManeuver!=AirTrick.None)yield return null;
            g.HeldKey=k=>k==KeyCode.V;yield return null;yield return null;Require(g.State.AirManeuver==AirTrick.Turn,"V starts turn");yield return Progress(AirTrick.Turn,.45f);yield return Capture("plane-banked-turn");while(g.State.AirManeuver!=AirTrick.None)yield return null;Require(g.State.AirCompletions==3,"All three maneuvers completed");g.HeldKey=k=>false;yield return null;
            g.HeldKey=k=>k==KeyCode.Z;yield return new WaitForSecondsRealtime(.3f);Require(g.State.AirManeuver==AirTrick.Roll,"Fresh roll starts");g.HeldKey=k=>k==KeyCode.Space;yield return null;yield return null;Require(g.State.AirManeuver==AirTrick.None,"Space cancels");g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(2);yield return Capture("plane-trick-recovered");watching=false;
            Require(maxCameraRoll<.1f,"Camera never tumbles");Require(maxStep<5,"No position jump during live maneuvers");g.enabled=false;
            g.State.FinishFlightArrival(false);for(int i=0;i<350;i++)g.State.Step(.02f,new DriveInput{Descend=true,Brake=true});Require(g.State.Position.Y<.02f&&g.State.Speed<.01f,"Ordinary runway landing and stop preserved");
            g.RefreshPlayer();g.OrbitCamera(105,0);g.SendMessage("LateUpdate");yield return Capture("plane-detail-landed");
            g.Graphics.SetHigh(mode==1);while(g.Graphics.Busy)yield return null;PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(folder,"airshow-runtime-results.txt"),"Actual standalone Mac: keyboard X loop and V banked U-turn, injected touch event roll, all three completed, no held-key repeat, Space cancellation, upright camera (max roll "+maxCameraRoll.ToString("F3")+" degrees), maximum frame displacement "+maxStep.ToString("F2")+" m. Ordinary runway descent/stop passed. Inputs injected, not physical keyboard/iPad verification. Preferences restored.\n");Debug.Log("AIRSHOW_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        Vector3 Position(){var p=g.State.Position;return new Vector3(p.X,p.Y,p.Z);}
        IEnumerator Progress(AirTrick expected,float fraction)
        {
            float deadline=Time.realtimeSinceStartup+6;
            while(g.State.AirProgress<fraction){Require(g.State.Mode==Ride.Airplane&&g.State.AirManeuver==expected,"Expected "+expected+" but mode="+g.State.Mode+" maneuver="+g.State.AirManeuver+" world="+g.State.WorldIndex+" focus="+Application.isFocused);Require(Time.realtimeSinceStartup<deadline,"No maneuver progress for "+expected);yield return null;}
        }
        void LateUpdate(){if(!watching)return;var p=Position();maxStep=Mathf.Max(maxStep,Vector3.Distance(previous,p));previous=p;maxCameraRoll=Mathf.Max(maxCameraRoll,Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.z,0)));}
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Require(bool ok,string message){if(ok)return;Debug.LogError("AIRSHOW_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
