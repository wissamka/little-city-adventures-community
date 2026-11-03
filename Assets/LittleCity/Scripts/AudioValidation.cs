using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleCity
{
    public sealed class GameAudioCapture : MonoBehaviour
    {
        readonly object gate=new object();readonly List<float> samples=new List<float>();bool capture;int channels;
        public void Begin(){lock(gate){samples.Clear();capture=true;}}
        void OnAudioFilterRead(float[] data,int count){lock(gate){if(!capture)return;channels=count;if(samples.Count<AudioSettings.outputSampleRate*count*8)samples.AddRange(data);}}
        public double Finish(string path)
        {
            lock(gate)
            {
                capture=false;double square=0;float peak=0;
                using(var w=new BinaryWriter(File.Create(path)))
                {
                    int bytes=samples.Count*2;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)Math.Max(1,channels));w.Write(AudioSettings.outputSampleRate);w.Write(AudioSettings.outputSampleRate*Math.Max(1,channels)*2);w.Write((short)(Math.Max(1,channels)*2));w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);
                    foreach(float v in samples){square+=v*v;peak=Mathf.Max(peak,Mathf.Abs(v));w.Write((short)Mathf.RoundToInt(Mathf.Clamp(v,-1,1)*32767));}
                }
                if(samples.Count==0)throw new Exception("No mixed audio output captured");
                if(peak>=.98f)throw new Exception("Audio clipping");
                return Math.Sqrt(square/samples.Count);
            }
        }
    }
    public sealed class AudioValidation : MonoBehaviour
    {
        CityGame game;GameAudioCapture capture;string folder;readonly List<string> report=new List<string>();float originalVolume;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-audio")>=0)new GameObject("Audio validation").AddComponent<AudioValidation>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;game=FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;
            folder=Path.Combine(Application.dataPath,"../../AudioValidation");var args=Environment.GetCommandLineArgs();int idx=Array.IndexOf(args,"--audio-output");if(idx>=0)folder=args[idx+1];Directory.CreateDirectory(folder);
            originalVolume=GameSounds.Volume;PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);
            capture=Camera.main.gameObject.AddComponent<GameAudioCapture>();game.Hud.BeginExploring();game.State.Place(new Point(-300,0,-300));game.RefreshPlayer();
            yield return Segment("01-still",1,false);
            game.HeldKey=k=>k==KeyCode.W;int start=game.Sounds.StepsPlayed;
            yield return Segment("02-walking-shoes",3,true);Check(game.Sounds.StepsPlayed>start+5,"Footsteps follow actual movement");
            game.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.5f);yield return Segment("03-stopped",1,false);
            foreach(var bike in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike})
            {
                game.SelectRide(bike);game.State.Place(new Point(-300,0,-300));game.RefreshPlayer();game.HeldKey=k=>k==KeyCode.W;
                yield return new WaitForSecondsRealtime(.5f);yield return Segment("03-"+bike+"-pedaling",2,true);
                Check(game.Sounds.PedalingVolume>.1f && game.Sounds.CoastingVolume<.001f,"Chain engaged while pedaling "+bike);
                game.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.35f);yield return Segment("03-"+bike+"-coasting",1,true);
                Check(game.Sounds.PedalingVolume<.001f && game.Sounds.CoastingVolume>.1f,"Freehub replaces chain when coasting "+bike);
                game.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);yield return Segment("03-"+bike+"-stopped",1,false);
                Check(!game.Sounds.BicyclePlaying,"All bike layers stop "+bike);
            }
            game.SelectRide(Ride.RaceCar);game.State.Place(new Point(-300,0,-300));game.RefreshPlayer();game.HeldKey=k=>k==KeyCode.W;
            yield return new WaitForSecondsRealtime(.5f);yield return Segment("04-race-engine",3,true);Check(game.Sounds.EnginePlaying,"Single car engine loop playing");
            game.HeldKey=k=>k==KeyCode.Space;yield return new WaitForSecondsRealtime(2);yield return Segment("05-braked",1,false);
            game.HeldKey=k=>false;game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.HeldKey=k=>k==KeyCode.W;game.RefreshPlayer();
            yield return Segment("06-plane-flight",3,true);
            game.Hud.ShowExplore(true);yield return new WaitForSecondsRealtime(.3f);yield return Segment("07-menu-paused",1,false);
            game.Hud.BeginExploring();PlayerPrefs.SetFloat("LittleCity.SoundVolume",0);yield return new WaitForSecondsRealtime(.3f);yield return Segment("08-muted",1,false);
            PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);game.Worlds.VisitWorld(1);game.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.3f);yield return Segment("09-world-reset",1,false);
            PlayerPrefs.SetFloat("LittleCity.SoundVolume",originalVolume);PlayerPrefs.Save();File.WriteAllLines(Path.Combine(folder,"audio-results.txt"),report);Debug.Log("AUDIO_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Segment(string name,float seconds,bool audible)
        {
            capture.Begin();yield return new WaitForSecondsRealtime(seconds);double rms=capture.Finish(Path.Combine(folder,name+".wav"));report.Add(name+": mixed-output RMS "+rms.ToString("F6"));Check(audible?rms>.003:rms<.0001,name+" audibility/silence");
        }
        void Check(bool condition,string message){if(condition){report.Add("PASS "+message);return;}File.WriteAllLines(Path.Combine(folder,"audio-results.txt"),report);Debug.LogError("AUDIO_VALIDATION_FAILED "+message);Application.Quit(2);throw new Exception(message);}
    }
}
