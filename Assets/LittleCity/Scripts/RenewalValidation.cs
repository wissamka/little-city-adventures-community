using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class RenewalValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-renewal")>=0)new GameObject("Renewal validation").AddComponent<RenewalValidation>();}
        CityGame g;GameAudioCapture audio;string folder,stage="startup";readonly List<string> report=new List<string>();float started,volume;int character,graphics,night;bool prefs;
        void Update(){if(Time.realtimeSinceStartup-started>350)Fail("Timed out at "+stage);}
        void Restore(){if(!prefs)return;PlayerPrefs.SetFloat("LittleCity.SoundVolume",volume);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",graphics);PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.Save();prefs=false;}
        void Fail(string message){if(g!=null&&g.Ready)message+=" [focus="+Application.isFocused+", enabled="+g.enabled+", menu="+g.Hud.MenuOpen+", mode="+g.State.Mode+", x="+g.State.Position.X+", y="+g.State.Position.Y+", z="+g.State.Position.Z+", speed="+g.State.Speed+"]";report.Add("FAIL "+message);if(folder!=null)File.WriteAllLines(Path.Combine(folder,"renewal-runtime-results.txt"),report);Restore();Debug.LogError("RENEWAL_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
        void Check(bool ok,string message){if(!ok)Fail(message);report.Add("PASS "+message);}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--renewal-output")+1];Directory.CreateDirectory(folder);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            report.Add("Native window focused="+Application.isFocused+". This opt-in automated run injects initial focus alongside keyboard input; production focus behavior is unchanged.");
            g.SendMessage("OnApplicationFocus",true);g.Sounds.SendMessage("OnApplicationFocus",true);
            volume=GameSounds.Volume;character=PlayerPrefs.GetInt("LittleCity.Character",0);graphics=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);night=PlayerPrefs.GetInt("LittleCity.Night",0);prefs=true;
            g.Hud.BeginExploring();if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();g.HeldKey=k=>false;
            stage="yachts";yield return Yachts();stage="characters";yield return Characters();stage="car jumps";yield return CarJumps();stage="audio";yield return Sounds();
            g.HeldKey=k=>false;g.Sounds.Silence();Restore();report.Add("Actual standalone Mac rendering and mixed audio; injected keyboard and initial focus state, not physical keys or iPad verification.");File.WriteAllLines(Path.Combine(folder,"renewal-runtime-results.txt"),report);Debug.Log("RENEWAL_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Yachts()
        {
            g.Worlds.VisitWorld(1);g.SelectRide(Ride.Walk);
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                yield return View(high?"yacht-exterior-high":"yacht-exterior-low",new Vector3(-113,30,43),new Vector3(-65,5,103));
            }
            foreach(float dock in new[]{-65f,-130f,-5f})
            {
                g.State.Place(new Point(dock,0,dock==-65?60:65));g.RefreshPlayer();g.Interact();Check(g.Worlds.Yachts.Aboard,"Board yacht at "+dock);
                if(dock==-65)
                {
                    yield return View("yacht-lounge",new Vector3(1,2.65f,-25),new Vector3(-3,1.4f,-14));
                    yield return View("yacht-cabin",new Vector3(0,2.65f,15),new Vector3(-4.7f,1,21));
                    yield return View("yacht-sundeck",new Vector3(0,7.1f,-18),new Vector3(-2,5.1f,8));
                }
                g.State.Place(new Point(6,0,-28.7f));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>k==KeyCode.W;
                yield return new WaitForSecondsRealtime(1.2f);if(dock==-65)yield return Capture("yacht-stairs-walking");yield return new WaitForSecondsRealtime(1.7f);g.HeldKey=k=>false;
                Check(g.State.Position.Y>4.35f&&g.Worlds.Yachts.Deck==1,"Actual walking up yacht stairs "+dock);
                g.State.Place(new Point(6,4.4f,-19.3f));g.RefreshPlayer();yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>k==KeyCode.S;yield return new WaitForSecondsRealtime(2.9f);g.HeldKey=k=>false;
                Check(g.State.Position.Y<.05f&&g.Worlds.Yachts.Deck==0,"Actual walking down yacht stairs "+dock);
                YachtSpot sofa=null,bed=null;foreach(var spot in g.Worlds.Yachts.Spots){if(spot.Deck==0&&spot.Pose==RestPose.Sitting&&sofa==null)sofa=spot;if(spot.Pose==RestPose.Sleeping)bed=spot;}
                foreach(var spot in new[]{sofa,bed}){g.State.Place(new Point(spot.Near.x,spot.Near.y,spot.Near.z));g.RefreshPlayer();g.Interact();Check(g.State.Resting==spot.Pose,"Yacht furniture interaction "+spot.Label);g.Interact();}
                g.State.Place(new Point(0,0,-27));g.RefreshPlayer();g.Interact();Check(!g.Worlds.Yachts.Aboard&&!g.State.Indoors,"Leave yacht "+dock);
            }
        }
        IEnumerator Characters()
        {
            g.Worlds.VisitWorld(0);g.Hud.ShowCharacters();yield return new WaitForSecondsRealtime(.3f);yield return Capture("character-roster");g.Hud.BeginExploring();
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                foreach(int index in new[]{1,2})
                {
                    g.SelectRide(Ride.Walk);g.SelectCharacter(index);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();string name=Simulation.CharacterNames[index].ToLowerInvariant()+"-"+(high?"high":"low");Vector3 p=new Vector3(-300,0,-300);
                    yield return View(name+"-front",p+new Vector3(1.3f,1.85f,3),p+new Vector3(0,1,0));
                    if(index==1)yield return View(name+"-long-hair",p+new Vector3(-1.7f,1.85f,-3),p+new Vector3(0,1,0));
                    foreach(var ride in new[]{Ride.Bicycle,Ride.Motorcycle,Ride.RaceCar})
                    {
                        g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();yield return View(name+"-"+ride.ToString().ToLowerInvariant(),p+new Vector3(3,2.3f,3.5f),p+new Vector3(0,1.1f,0));
                    }
                    g.SelectRide(Ride.Walk);
                }
            }
        }
        IEnumerator CarJumps()
        {
            g.Worlds.VisitWorld(4);g.SelectCharacter(0);
            foreach(bool high in new[]{false,true})foreach(bool nos in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;g.State.SwitchRide(Ride.Walk);Check(g.State.EnterVehicle(Ride.RaceCar,0,new Point(SkatePark.MegaX,0,SkatePark.MegaStart-36),0),"Enter actual car for ramp run");g.State.NitroInstalled=nos;g.State.NitroCharge=1;g.State.CancelNitro();g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.4f);g.OrbitCamera(35,0);
                int before=g.State.ParkLandings;float begin=Time.realtimeSinceStartup,peak=0;bool air=false,land=false;string label="car-"+(high?"high":"low")+(nos?"-nos":"-ordinary");
                g.HeldKey=k=>k==KeyCode.W||nos&&k==KeyCode.LeftAlt&&g.State.Position.Z>SkatePark.MegaStart-8;
                while(Time.realtimeSinceStartup-begin<12)
                {
                    yield return null;var s=g.State;peak=Mathf.Max(peak,s.Position.Y);
                    if(!air&&s.Airborne&&s.Position.Z>SkatePark.MegaLip+8&&s.Position.Z<SkatePark.MegaLanding){air=true;yield return Capture(label+"-airborne");}
                    if(s.ParkLandings>before&&s.Position.Z>=SkatePark.MegaLanding){land=true;yield return Capture(label+"-landed");break;}
                }
                Check(air&&land,label+" real ramp climb/air/landing peak="+peak.ToString("F2")+" landing z="+g.State.Position.Z.ToString("F2"));
                Check(g.State.Position.Z<=SkatePark.MegaLanding+42.7f,label+" reaches solid landing within its end");g.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Check(Mathf.Abs(g.State.Speed)<.01f,label+" Space braking");g.HeldKey=k=>false;g.ResetHome();Check(g.State.Grounded&&g.State.Speed==0,label+" checkpoint retry");
            }
        }
        IEnumerator View(string name,Vector3 position,Vector3 target)
        {yield return new WaitForSecondsRealtime(.2f);g.enabled=false;Camera.main.transform.position=position;Camera.main.transform.LookAt(target);yield return Capture(name);g.enabled=true;}
        IEnumerator Capture(string name)
        {yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
        IEnumerator Segment(string name,float seconds,bool audible)
        {audio.Begin();yield return new WaitForSecondsRealtime(seconds);double rms=audio.Finish(Path.Combine(folder,name+".wav"));report.Add(name+" RMS="+rms.ToString("F6")+" pitch="+g.Sounds.EnginePitch.ToString("F3"));Check(audible?rms>.001:rms<.0001,name+" mixed audio "+(audible?"audible":"silent"));}
        IEnumerator Sounds()
        {
            g.Worlds.VisitWorld(0);PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);audio=Camera.main.gameObject.AddComponent<GameAudioCapture>();
            foreach(var ride in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike,Ride.Motorcycle,Ride.DirtBike,Ride.Scooter})
            {
                stage="audio "+ride;string name=ride.ToString().ToLowerInvariant();bool motor=ride==Ride.Motorcycle||ride==Ride.DirtBike;
                g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.5f);yield return Segment(name+"-idle",.6f,motor);float idlePitch=g.Sounds.EnginePitch;
                g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(.4f);yield return Segment(name+"-accelerating",3,true);float fastPitch=g.Sounds.EnginePitch;
                Check(g.Sounds.EngineSound==(motor?"Motorcycle engine":ride==Ride.Scooter?"Electric scooter whine":"Bicycle freewheel clicks"),name+" correct distinct sound");
                g.HeldKey=k=>false;yield return Segment(name+"-coasting",1,true);Check(g.Sounds.EnginePitch<fastPitch+.01f,name+" pitch responds to deceleration");
                if(motor||ride==Ride.Scooter)Check(fastPitch>idlePitch+.15f,name+" speed raises pitch");
                g.HeldKey=k=>k==KeyCode.Space||k==KeyCode.LeftControl;yield return new WaitForSecondsRealtime(2);yield return Segment(name+"-stopped",.6f,motor);
                g.HeldKey=k=>false;PlayerPrefs.SetFloat("LittleCity.SoundVolume",0);yield return new WaitForSecondsRealtime(.2f);yield return Segment(name+"-muted",.5f,false);PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);
                g.Hud.ShowExplore(true);yield return new WaitForSecondsRealtime(.2f);yield return Segment(name+"-menu",.5f,false);g.Hud.BeginExploring();
                g.SelectRide(Ride.Walk);yield return new WaitForSecondsRealtime(.3f);yield return Segment(name+"-exited",.5f,false);
            }
            g.SelectRide(Ride.Motorcycle);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();yield return new WaitForSecondsRealtime(.3f);g.Sounds.SendMessage("OnApplicationFocus",false);yield return Segment("focus-lost",.6f,false);g.Sounds.SendMessage("OnApplicationFocus",true);
            g.Worlds.VisitWorld(1);g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.4f);yield return Segment("world-reset",.6f,false);
            g.Worlds.VisitWorld(0);g.SelectRide(Ride.Walk);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>k==KeyCode.W;yield return Segment("original-walking-shoes",2,true);
            g.HeldKey=k=>false;g.SelectRide(Ride.RaceCar);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(.5f);yield return Segment("original-race-engine",2,true);Check(g.Sounds.EngineSound=="Race engine","Race car keeps its own sound");
            g.HeldKey=k=>false;g.SelectRide(Ride.Airplane);g.State.FinishFlightArrival(false);g.RefreshPlayer();yield return new WaitForSecondsRealtime(.35f);g.HeldKey=k=>k==KeyCode.W;yield return Segment("original-plane-flight",2,true);Check(g.Sounds.EngineSound=="Propeller flight","Plane retains propeller sound");
        }
    }
}
