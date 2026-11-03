using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace LittleCity
{
    public sealed class InputReleaseValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-input-events")>=0)new GameObject("Native input release validation").AddComponent<InputReleaseValidation>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--input-output");string folder=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
            var g=FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics==null || g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.Hud.SetTouchControls(true);
            var keys=new HashSet<KeyCode>();var fingers=new HashSet<int>();var raw=PointerPresence.IsHeld;PointerPresence.IsHeld=id=>fingers.Contains(id);g.HeldKey=k=>keys.Contains(k);
            var lines=new List<string>{"Actual standalone game Update/EventSystem press-release validation. Keyboard and pointer presence are injected test sources; this is not physical iPad touch validation."};
            foreach(var key in new[]{KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow})
            {
                g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();yield return null;keys.Add(key);yield return new WaitForSecondsRealtime(.45f);Require(g.State.Speed>3,key+" moves");keys.Clear();yield return null;yield return null;var stopped=g.State.Position;yield return new WaitForSecondsRealtime(.3f);Require(Distance(stopped,g.State.Position)<.001f && g.State.Speed==0,key+" release stops");lines.Add(key+": live press moves; release holds position and speed=0");
            }
            foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {
                g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();yield return null;var r=(RectTransform)g.Hud.Stick.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center+direction*Mathf.Min(r.rect.width,r.rect.height)*.36f));var e=new PointerEventData(EventSystem.current){pointerId=101,position=p};fingers.Add(101);
                ExecuteEvents.Execute(g.Hud.Stick.gameObject,e,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.45f);Require(g.State.Speed>3,"Pointer direction moves");
                // Fault injection: release physical owner but deliberately omit PointerUp.
                fingers.Clear();yield return null;yield return null;var stopped=g.State.Position;yield return new WaitForSecondsRealtime(.3f);Require(Distance(stopped,g.State.Position)<.001f && g.State.Speed==0 && g.Hud.Stick.Value==Vector2.zero,"Lost pointer Up stops movement");lines.Add("Stick "+direction+": actual EventSystem down moves; omitted Up + owner release stops and centers");
            }
            g.Hud.ShowControls(true);yield return null;
            foreach(var button in new[]{g.Hud.LookLeft,g.Hud.LookRight})
            {
                var e=new PointerEventData(EventSystem.current){pointerId=202};fingers.Add(202);float start=g.CameraYaw;ExecuteEvents.Execute(button.gameObject,e,ExecuteEvents.pointerDownHandler);yield return new WaitForSecondsRealtime(.3f);Require(Mathf.Abs(Mathf.DeltaAngle(start,g.CameraYaw))>10,"Camera button turns");fingers.Clear();yield return null;yield return null;float stopped=g.CameraYaw;yield return new WaitForSecondsRealtime(.3f);Require(Mathf.Abs(Mathf.DeltaAngle(stopped,g.CameraYaw))<.01f,"Camera release stops command");lines.Add(button.name+": live hold turns, lost Up release stops camera command");
            }
            g.Hud.ShowControls(false);g.Hud.Refresh();yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,"controls-released.png"),screenshot.EncodeToPNG());Destroy(screenshot);
            PointerPresence.IsHeld=raw;g.HeldKey=Input.GetKey;File.WriteAllLines(Path.Combine(folder,"input-runtime-results.txt"),lines);Debug.Log("INPUT_RELEASE_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        static float Distance(Point a,Point b)=>new Vector3(a.X-b.X,a.Y-b.Y,a.Z-b.Z).magnitude;
        void Require(bool ok,string message){if(ok)return;Debug.LogError("INPUT_RELEASE_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
