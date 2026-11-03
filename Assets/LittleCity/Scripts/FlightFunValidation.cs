using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class FlightFunValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-flight-fun")>=0)new GameObject("Flight and backflip validation").AddComponent<FlightFunValidation>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--flight-output");string folder=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
            var g=FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics==null || g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.OrbitCamera(40,5);yield return new WaitForSecondsRealtime(.5f);
            g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(.05f);g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.08f);g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(.05f);g.HeldKey=k=>false;
            Require(g.Flip.Active,"Real game update recognizes double Space");var origin=g.State.Position;float cameraYaw=Camera.main.transform.eulerAngles.y;
            yield return new WaitForSecondsRealtime(.22f);yield return Capture(folder,"backflip-first-turn");yield return new WaitForSecondsRealtime(.6f);yield return Capture(folder,"backflip-second-turn");
            while(g.Flip.Active)yield return null;yield return null;Require(g.Flip.Completed==1 && Mathf.Abs(g.Flip.Angle-1080)<.01f,"Exactly three rotations completed");Require(Mathf.Abs(g.State.Position.X-origin.X)<.01f && Mathf.Abs(g.State.Position.Z-origin.Z)<.01f && g.State.Grounded,"Safe ground landing");Require(Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y,cameraYaw))<.2f,"Camera heading remains steady while model flips");Require(Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.z,0))<.1f,"Camera does not tumble");yield return Capture(folder,"backflip-landed");
            g.enabled=false;g.SelectRide(Ride.Airplane);for(int n=0;n<4000;n++)g.State.Step(.02f,new DriveInput{Forward=1,Rise=true,Steering=.4f});Require(Mathf.Abs(g.State.Position.Y-420)<.01f,"420 m ceiling reached");g.RefreshPlayer();g.OrbitCamera(25,22);g.SendMessage("LateUpdate");g.Hud.Refresh();yield return Capture(folder,"airplane-420m");
            g.State.FinishFlightArrival(false);for(int n=0;n<350;n++)g.State.Step(.02f,new DriveInput{Descend=true,Brake=true});Require(g.State.Position.Y<.02f && g.State.Speed<.01f,"Runway descent and stop");g.RefreshPlayer();yield return Capture(folder,"airplane-landed");
            File.WriteAllText(Path.Combine(folder,"flight-runtime-results.txt"),"Actual standalone Mac: live Update double-Space recognized; first ordinary jump continues into exactly 1080 degrees / three rotations, safe ground landing, steady heading and non-tumbling camera. Input source is injected, not physical hardware. Production airplane simulation reached420m and runway descent/stop passed; not a realtime performance benchmark.\n");Debug.Log("FLIGHT_FUN_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        void Require(bool ok,string message){if(ok)return;Debug.LogError("FLIGHT_FUN_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
        IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
    }
}
