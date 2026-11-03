using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace LittleCity
{
    public sealed class CamperValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-camper")>=0)new GameObject("Camper native validation").AddComponent<CamperValidation>();}
        CityGame g;string folder;float started;bool saved,finished;int mode,night,character;readonly List<string> report=new List<string>();
        void Update(){if(!finished&&Time.realtimeSinceStartup-started>210)Require(false,"Native validation timeout");}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--camper-output")+1];Directory.CreateDirectory(folder);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            mode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);night=PlayerPrefs.GetInt("LittleCity.Night",0);character=PlayerPrefs.GetInt("LittleCity.Character",0);saved=true;
            g.SendMessage("OnApplicationFocus",true);g.Sounds.SendMessage("OnApplicationFocus",true);g.HeldKey=k=>false;g.Hud.BeginExploring();g.Worlds.VisitWorld(0);if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();
            report.Add("Actual standalone Mac with injected keyboard/touch events and initial focus. Not physical hardware or iPad testing.");
            foreach(var ride in new[]{Ride.Van,Ride.Camper})
            {
                g.SelectRide(ride);g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.OrbitCamera(120,0);
                foreach(bool high in new[]{false,true})
                {
                    g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.6f);Require(g.CurrentVehicle.Root.GetComponent<GraphicsCamper>().ShowingHigh==high,"Graphics mode");yield return Capture(ride.ToString().ToLowerInvariant()+(high?"-high":"-low"));
                }
                float elapsed=0;for(int i=0;i<150;i++){yield return null;elapsed+=Time.unscaledDeltaTime;}
                g.State.Place(new Point(-96,0,-92));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.2f);float start=g.State.DistanceTravelled;g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(5);float distance=g.State.DistanceTravelled-start;Require(distance>35,"Long vehicle live drive");yield return Capture(ride.ToString().ToLowerInvariant()+"-moving");
                g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(Mathf.Abs(g.State.Speed)<.01f,"Space brakes");g.HeldKey=k=>false;yield return null;
                float z=g.State.Position.Z;g.HeldKey=k=>k==KeyCode.S;yield return new WaitForSecondsRealtime(1.5f);Require(g.State.Position.Z<z-2,"Reverse");g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(1);g.HeldKey=k=>false;yield return null;
                g.Hud.SetTouchControls(true);PointerPresence.IsHeld=id=>id==900;var pointer=new PointerEventData(EventSystem.current){pointerId=900};g.Hud.Go.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(1.5f);Require(g.State.Speed>3,"Touch GO");yield return Capture(ride.ToString().ToLowerInvariant()+"-touch-driving");g.Hud.Go.OnPointerUp(pointer);g.Hud.Stop.OnPointerDown(pointer);yield return new WaitForSecondsRealtime(1);Require(Mathf.Abs(g.State.Speed)<.01f,"Touch STOP");g.Hud.Stop.OnPointerUp(pointer);PointerPresence.IsHeld=PointerPresence.ReadPhysical;g.Hud.SetTouchControls(false);
                report.Add(ride+": "+distance.ToString("F1")+" m live driving; keyboard Space brake, reverse and injected touch GO/STOP passed. High stationary "+(150/elapsed).ToString("F1")+" FPS at "+Screen.width+"x"+Screen.height+".");
                Require(g.Parking.EnterOrExit(),"Step out");
            }
            Require(g.Camper.Boardable!=null,"RV side door discoverable after parking");var parked=g.Camper.Boardable;Vector3 original=parked.Model.Root.position;
            foreach(bool high in new[]{false,true})foreach(int person in new[]{0,1,2,3})
            {
                g.SelectCharacter(person);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;g.Interact();Require(g.Camper.Inside,"F enters RV");yield return new WaitForSecondsRealtime(.3f);
                yield return Capture("rv-"+Simulation.CharacterNames[person].ToLowerInvariant()+(high?"-high":"-low")+"-entry");
                yield return WalkTo(-.57f);g.Interact();Require(g.State.Resting==RestPose.Sitting,"Dinette seat");yield return new WaitForSecondsRealtime(.2f);g.Interact();Require(g.State.Resting==RestPose.None,"Stand from dinette");
                yield return WalkTo(-1.35f);g.Interact();Require(g.State.Resting==RestPose.Sleeping,"Use bed");yield return new WaitForSecondsRealtime(.3f);yield return Capture("rv-"+Simulation.CharacterNames[person].ToLowerInvariant()+(high?"-high":"-low")+"-bed");g.Interact();Require(g.State.Resting==RestPose.None,"Wake from bed");
                yield return WalkTo(1.2f);g.Interact();Require(!g.Camper.Inside&&!g.State.Indoors,"F exits RV");Require(parked.Model.Root.position==original,"Parked vehicle position preserved");
                report.Add(Simulation.CharacterNames[person]+(high?" High":" Low")+": actual walking aisle, dinette sit/stand, bed sleep/wake and side door exit passed.");
            }
            Require(g.Parking.EnterOrExit(),"Board same RV driver seat");Require(g.Parking.Occupied==parked,"Same parked RV retained");g.Parking.EnterOrExit();g.Interact();Require(g.Camper.Inside,"Reenter before travel");g.Worlds.VisitWorld(1);Require(!g.Camper.Inside&&!g.State.Indoors&&g.State.WorldIndex==1,"Travel cleanup from RV");g.Worlds.VisitWorld(0);Require(!g.Camper.Inside,"Return city cleanup");
            report.Add("Reboarding same RV and travel to marina/back from its interior passed. Preferences restored.");Restore();finished=true;File.WriteAllLines(Path.Combine(folder,"camper-runtime-results.txt"),report);Debug.Log("CAMPER_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator WalkTo(float target)
        {
            g.HeldKey=k=>false;yield return null;float start=Time.realtimeSinceStartup;bool down=target<g.State.Position.Z;g.HeldKey=k=>k==(down?KeyCode.W:KeyCode.S);
            while(down?g.State.Position.Z>target:g.State.Position.Z<target){Require(Time.realtimeSinceStartup-start<4,"RV aisle movement timeout");yield return null;}
            g.HeldKey=k=>false;yield return null;Require(Mathf.Abs(g.State.Position.X)<.3f,"Player stays in clear aisle");
        }
        IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        void Restore(){if(!saved)return;PointerPresence.IsHeld=PointerPresence.ReadPhysical;PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",mode);PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.Save();saved=false;}
        void Require(bool ok,string message){if(ok)return;finished=true;Restore();Debug.LogError("CAMPER_RUNTIME_VALIDATION_FAILED "+message+" position="+(g==null?"loading":g.State.Position.X+","+g.State.Position.Z));Application.Quit(2);throw new Exception(message);}
    }
}
