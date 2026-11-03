using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity {
 public sealed class NightValidation:MonoBehaviour {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-night")>=0)new GameObject("Night drive validation").AddComponent<NightValidation>();}
  string folder;CityGame game;
  IEnumerator Start() {
   Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--night-output")+1];Directory.CreateDirectory(folder);
   int night=PlayerPrefs.GetInt("LittleCity.Night",0),glow=PlayerPrefs.GetInt("LittleCity.Underglow",0),nos=PlayerPrefs.GetInt("LittleCity.NOS",0),mode=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);float volume=GameSounds.Volume;
   game=FindFirstObjectByType<CityGame>();while(!game.Ready||game.Graphics==null||game.Graphics.Busy)yield return null;game.Hud.BeginExploring();game.Worlds.VisitWorld(0);game.SelectRide(Ride.RaceCar);game.State.Place(new Point(-32,0,-15));game.RefreshPlayer();game.OrbitCamera(135,0);yield return new WaitForSecondsRealtime(.8f);game.enabled=false;
   var atmo=CityAtmosphere.Current;if(atmo.Night)atmo.ToggleNight();game.Graphics.SetHigh(true);while(game.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.5f);game.Hud.Refresh();yield return Capture("day-drive");
   atmo.ToggleNight();while(atmo.Glow!=1)atmo.CycleGlow();if(!game.State.NitroInstalled)atmo.ToggleNitro();yield return new WaitForSecondsRealtime(.6f);game.Hud.Refresh();yield return Capture("night-underglow-high");
   game.Graphics.SetHigh(false);while(game.Graphics.Busy)yield return null;Require(atmo.Night,"Night independent of Low Def");yield return new WaitForSecondsRealtime(.5f);game.Hud.Refresh();yield return Capture("night-underglow-low");
   game.Graphics.SetHigh(true);while(game.Graphics.Busy)yield return null;game.Hud.ShowControls(true);game.Hud.Refresh();yield return Capture("night-car-controls");game.Hud.ShowControls(false);
   game.State.Place(new Point(-96,0,-92));game.RefreshPlayer();game.RecenterCamera();game.HeldKey=k=>false;game.enabled=true;yield return new WaitForSecondsRealtime(.4f);game.HeldKey=k=>k==KeyCode.W||k==KeyCode.LeftAlt;
   yield return new WaitForSecondsRealtime(1.8f);Require(game.State.NitroActive&&game.State.Speed>28,"NOS accelerates in actual Update");float speed=game.State.Speed;yield return Capture("night-nos-driving");
   PlayerPrefs.SetFloat("LittleCity.SoundVolume",0);yield return null;yield return null;Require(!game.Sounds.BoostPlaying,"Mute stops NOS whoosh");
   game.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);Require(!game.State.NitroActive&&Mathf.Abs(game.State.Speed)<.01f,"Brake cancels NOS and stops");game.HeldKey=k=>false;
   PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.SetInt("LittleCity.Underglow",glow);PlayerPrefs.SetInt("LittleCity.NOS",nos);PlayerPrefs.SetFloat("LittleCity.SoundVolume",volume);game.Graphics.SetHigh(mode==1);while(game.Graphics.Busy)yield return null;PlayerPrefs.Save();
   File.WriteAllText(Path.Combine(folder,"night-runtime-results.txt"),"Actual standalone Mac: independent day/night and Low/High modes, street/headlights and cyan underglow captured; NOS reached "+speed.ToString("F1")+"m/s under live Update with injected W/Alt, mute silenced boost, braking canceled boost and stopped. Original preferences restored. Physical iPad untested.\n");Debug.Log("NIGHT_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
  }
  IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
  void Require(bool ok,string message){if(ok)return;Debug.LogError("NIGHT_RUNTIME_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
 }
}
