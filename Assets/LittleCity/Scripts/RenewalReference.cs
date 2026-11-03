using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    // Explicit diagnostic only: matching native views of the old and revised game.
    public sealed class RenewalReference:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--renewal-reference")>=0)new GameObject("Renewal reference").AddComponent<RenewalReference>();}
        CityGame g;string folder;float started;
        void Update(){if(Time.realtimeSinceStartup-started>150){Debug.LogError("Reference capture timed out");Application.Quit(2);}}
        IEnumerator Start()
        {
            started=Time.realtimeSinceStartup;Application.runInBackground=true;var args=Environment.GetCommandLineArgs();folder=args[Array.IndexOf(args,"--reference-output")+1];Directory.CreateDirectory(folder);
            g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;
            int saved=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);float volume=GameSounds.Volume;
            g.Hud.BeginExploring();g.Worlds.VisitWorld(1);g.SelectRide(Ride.Walk);g.HeldKey=k=>false;
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                yield return View(high?"yacht-exterior-high":"yacht-exterior-low",new Vector3(-113,30,43),new Vector3(-65,5,103));
            }
            g.State.Place(new Point(-65,0,60));g.RefreshPlayer();g.Worlds.Yachts.Use();
            yield return View("yacht-lounge",new Vector3(1,2.65f,-25),new Vector3(-3,1.4f,-14));
            yield return View("yacht-cabin",new Vector3(0,2.65f,15),new Vector3(-4.7f,1,21));
            yield return View("yacht-sundeck",new Vector3(0,7.1f,-18),new Vector3(-2,5.1f,8));
            g.Worlds.Yachts.Leave();g.Worlds.VisitWorld(0);g.SelectRide(Ride.Bicycle);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();
            PlayerPrefs.SetFloat("LittleCity.SoundVolume",.7f);var audio=Camera.main.gameObject.AddComponent<GameAudioCapture>();g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(.5f);g.HeldKey=k=>k==KeyCode.W;yield return new WaitForSecondsRealtime(2);
            audio.Begin();yield return new WaitForSecondsRealtime(3);double rms=audio.Finish(Path.Combine(folder,"bicycle-moving.wav"));
            g.HeldKey=k=>false;g.Sounds.Silence();g.Graphics.SetHigh(saved==1);while(g.Graphics.Busy)yield return null;PlayerPrefs.SetFloat("LittleCity.SoundVolume",volume);
            File.WriteAllText(Path.Combine(folder,"reference-results.txt"),"Native game reference views captured. Mixed bicycle audio RMS="+rms+".\n");Application.Quit(0);
        }
        IEnumerator View(string name,Vector3 position,Vector3 target)
        {
            yield return new WaitForSecondsRealtime(.3f);g.enabled=false;Camera.main.transform.position=position;Camera.main.transform.LookAt(target);yield return new WaitForEndOfFrame();
            var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);g.enabled=true;
        }
    }
}
