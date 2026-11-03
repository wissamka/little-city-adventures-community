using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LittleCity
{
    public sealed class RiderValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-rider")>=0)new GameObject("Rider preview validation").AddComponent<RiderValidation>();}
        IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--rider-output");string folder=i>=0?args[i+1]:Application.persistentDataPath;Directory.CreateDirectory(folder);
            int saved=PlayerPrefs.GetInt("LittleCity.Character",0),quality=PlayerPrefs.GetInt("LittleCity.GraphicsHigh",0);
            var g=FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics==null || g.Graphics.Busy)yield return null;
            g.SelectCharacter(3);g.Hud.ShowCharacters();yield return new WaitForSecondsRealtime(.4f);yield return Capture(folder,"rider-roster");g.Hud.BeginExploring();g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.enabled=false;
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;yield return new WaitForSecondsRealtime(.7f);
                Camera.main.transform.position=new Vector3(-29.6f,1.95f,-11);Camera.main.transform.LookAt(new Vector3(-32,1.2f,-15));yield return Capture(folder,high?"rider-high":"rider-low");
                if(!high){Camera.main.transform.position=new Vector3(-31.5f,1.95f,-13.4f);Camera.main.transform.LookAt(new Vector3(-32,1.80f,-15));yield return Capture(folder,"rider-portrait");}
            }
            g.State.Place(new Point(-96,0,-92));g.RefreshPlayer();g.HeldKey=k=>k==KeyCode.W||k==KeyCode.LeftShift;g.enabled=true;yield return new WaitForSecondsRealtime(2);
            float speed=g.State.Speed;if(speed<9.9f){Debug.LogError("Rider run speed validation failed: "+speed);Application.Quit(2);yield break;}
            g.HeldKey=k=>false;yield return new WaitForSecondsRealtime(1);if(g.State.Speed>.01f){Debug.LogError("Rider stop validation failed");Application.Quit(2);yield break;}
            g.Graphics.SetHigh(quality==1);while(g.Graphics.Busy)yield return null;PlayerPrefs.SetInt("LittleCity.Character",saved);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(folder,"rider-runtime-results.txt"),"Actual standalone Mac: six-character roster and Low/High screenshots; Rider live running speed="+speed.ToString("F1")+" m/s; stops after release. Prior character and graphics preferences restored. iPad not installed/tested.\n");Debug.Log("RIDER_RUNTIME_VALIDATION_PASSED");Application.Quit(0);
        }
        IEnumerator Capture(string folder,string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());Destroy(t);}
    }
}
