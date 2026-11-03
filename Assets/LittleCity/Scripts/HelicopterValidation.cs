using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace LittleCity
{
    public sealed class HelicopterValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-helicopter")>=0)new GameObject("Helicopter native validation").AddComponent<HelicopterValidation>();}
        CityGame g;string folder;float started;bool saved,finished;int mode,night,character;float volume;readonly List<string> report=new List<string>();
        void Update(){if(!finished&&Time.realtimeSinceStartup-started>180)Require(false,"Helicopter validation timeout");}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--helicopter-output")+1];Directory.CreateDirectory(folder);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            mode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);night=PlayerPrefs.GetInt("LittleCity.Night",0);character=PlayerPrefs.GetInt("LittleCity.Character",0);volume=GameSounds.Volume;saved=true;PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);
            g.SendMessage("OnApplicationFocus",true);g.Sounds.SendMessage("OnApplicationFocus",true);g.HeldKey=k=>false;g.Hud.BeginExploring();g.Worlds.VisitWorld(0);if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.SelectCharacter(3);g.SelectRide(Ride.Helicopter);
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;g.RecenterCamera();g.OrbitCamera(135,-2);yield return new WaitForSecondsRealtime(.7f);Require(g.CurrentVehicle.Root.GetComponent<HelicopterRig>().ShowingHigh==high,"Helicopter graphics switch");yield return Capture(high?"helicopter-high":"helicopter-low");
            }
            g.Hud.SetTouchControls(true);g.Hud.ShowControls(true);yield return new WaitForSecondsRealtime(.3f);yield return Capture("helicopter-controls");g.Hud.ShowControls(false);g.Hud.SetTouchControls(false);
            float elapsed=0;for(int i=0;i<150;i++){yield return null;elapsed+=Time.unscaledDeltaTime;}
            var rig=g.CurrentVehicle.Root.GetComponent<HelicopterRig>();Quaternion main=rig.MainRotor.localRotation,tail=rig.TailRotor.localRotation;yield return new WaitForSecondsRealtime(.137f);Require(Quaternion.Angle(main,rig.MainRotor.localRotation)>5&&Quaternion.Angle(tail,rig.TailRotor.localRotation)>5,"Both rotors animate");
            g.RecenterCamera();Point pad=g.State.Position;g.HeldKey=k=>k==KeyCode.E;yield return new WaitForSecondsRealtime(6);Require(g.State.Position.Y>35,"Vertical takeoff with E");Require(Distance(pad,g.State.Position)<.1f,"No runway or horizontal speed needed");yield return Capture("helicopter-vertical-takeoff");Require(!g.Parking.EnterOrExit(),"Cannot exit in flight");
            g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(1);Point hover=g.State.Position;yield return new WaitForSecondsRealtime(1.3f);Require(Mathf.Abs(g.State.Position.Y-hover.Y)<.02f&&Distance(hover,g.State.Position)<.02f,"Stable neutral hover");Require(g.Sounds.EnginePlaying&&g.Sounds.EngineSound=="Helicopter rotor","Rotor engine sound");
            g.HeldKey=k=>k==KeyCode.D;float yaw=g.State.Yaw;yield return new WaitForSecondsRealtime(1);Require(Mathf.Abs(Mathf.DeltaAngle(yaw*Mathf.Rad2Deg,g.State.Yaw*Mathf.Rad2Deg))>40,"Yaw while hovering");g.HeldKey=k=>k==KeyCode.W||k==KeyCode.D;float start=g.State.DistanceTravelled;yield return new WaitForSecondsRealtime(3.5f);float travel=g.State.DistanceTravelled-start;Require(travel>20,"Directional flight");yield return Capture("helicopter-turning-flight");
            g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(g.State.Speed)<.01f&&Mathf.Abs(g.State.VerticalSpeed)<.01f,"Space hover brake");g.HeldKey=k=>false;yield return null;
            foreach(bool high in new[]{false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Require(g.State.Mode==Ride.Helicopter&&g.State.Airborne,"Graphics switching preserves flight");}
            yield return ReturnToPad(pad);g.HeldKey=k=>k==KeyCode.Q;float limit=Time.realtimeSinceStartup+15;while(g.State.Airborne){Require(Time.realtimeSinceStartup<limit,"Q descent to helipad");yield return null;}g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.3f);Require(Distance(pad,g.State.Position)<4,"Returned to original landing pad using flight controls");yield return Capture("helicopter-landed");
            g.Hud.SetTouchControls(true);PointerPresence.IsHeld=id=>id==800;var pointer=new PointerEventData(EventSystem.current){pointerId=800};g.Hud.Rise.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(4);Require(g.State.Position.Y>15,"Visible touch UP takes off");g.Hud.Rise.OnPointerUp(pointer);g.Hud.Stop.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(1);Require(Mathf.Abs(g.State.VerticalSpeed)<.01f,"Touch STOP hovers");yield return Capture("helicopter-touch-hover");g.Hud.Stop.OnPointerUp(pointer);g.Hud.Descend.OnPointerDown(pointer);limit=Time.realtimeSinceStartup+10;while(g.State.Airborne){Require(Time.realtimeSinceStartup<limit,"Touch DOWN lands");yield return null;}g.Hud.Descend.OnPointerUp(pointer);PointerPresence.IsHeld=PointerPresence.ReadPhysical;g.Hud.SetTouchControls(false);
            PlayerPrefs.SetFloat("LittleCity.SoundVolume",0);yield return new WaitForSecondsRealtime(.2f);Require(!g.Sounds.EnginePlaying,"Mute stops rotor audio");PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);yield return new WaitForSecondsRealtime(.3f);g.Hud.ShowExplore(true);yield return new WaitForSecondsRealtime(.2f);Require(!g.Sounds.EnginePlaying,"Menu stops rotor audio");g.Hud.BeginExploring();yield return null;
            Require(g.Parking.EnterOrExit(),"G exits landed helicopter");Require(g.State.Mode==Ride.Walk,"On foot after landing");Require(g.Parking.EnterOrExit(),"G reboards parked helicopter");Require(g.State.Mode==Ride.Helicopter,"Same helicopter available");
            g.State.Place(new Point(172,32,-125));g.RefreshPlayer();g.Worlds.Travel(1);Require(g.State.WorldIndex==1&&g.State.Mode==Ride.Helicopter,"World travel preserves helicopter");g.Worlds.VisitWorld(0);Require(g.State.Mode==Ride.Walk&&!g.State.Indoors,"Return to city adventure");
            report.Add("Actual standalone Mac with injected keyboard/touch events and initial focus; not physical hardware or iPad verification.");report.Add("Vertical takeoff, neutral hover, stationary yaw, "+travel.ToString("F1")+"m directional flight, Space hover brake, flight back to original pad, Q landing and G exit/reboarding passed.");report.Add("Both animated rotors, Low/High switching on pad and in flight, visible touch UP/STOP/DOWN, rotor sound/mute/menu, and helicopter world travel/return passed.");report.Add("High stationary FPS "+(150/elapsed).ToString("F1")+" at "+Screen.width+"x"+Screen.height+". Actual screenshots captured. Preferences restored.");Restore();finished=true;File.WriteAllLines(Path.Combine(folder,"helicopter-runtime-results.txt"),report);Debug.Log("HELICOPTER_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator ReturnToPad(Point pad)
        {
            float limit=Time.realtimeSinceStartup+25;
            while(Distance(pad,g.State.Position)>2.7f)
            {
                Require(Time.realtimeSinceStartup<limit,"Return flight timeout");float dx=pad.X-g.State.Position.X,dz=pad.Z-g.State.Position.Z,distance=Mathf.Sqrt(dx*dx+dz*dz),error=Mathf.DeltaAngle(g.State.Yaw*Mathf.Rad2Deg,Mathf.Atan2(dx,dz)*Mathf.Rad2Deg);
                bool forward=Mathf.Abs(error)<12&&distance>1.5f+g.State.Speed*g.State.Speed/22;
                g.HeldKey=k=>k==KeyCode.D&&error>2||k==KeyCode.A&&error< -2||k==KeyCode.W&&forward||k==KeyCode.Space&&!forward;yield return null;
            }
            g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);g.HeldKey=k=>false;yield return null;
        }
        static float Distance(Point a,Point b)=>new Vector2(a.X-b.X,a.Z-b.Z).magnitude;
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Restore(){if(!saved)return;PointerPresence.IsHeld=PointerPresence.ReadPhysical;PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",mode);PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.SetFloat("LittleCity.SoundVolume",volume);PlayerPrefs.Save();saved=false;}
        void Require(bool ok,string message){if(ok)return;finished=true;Restore();Debug.LogError("HELICOPTER_RUNTIME_VALIDATION_FAILED "+message+" position="+(g==null?"loading":g.State.Position.X+","+g.State.Position.Y+","+g.State.Position.Z));Application.Quit(2);throw new Exception(message);}
    }
}
