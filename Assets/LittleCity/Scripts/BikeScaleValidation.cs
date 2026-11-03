using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace LittleCity
{
    public sealed class BikeScaleValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-bike-scale")>=0)new GameObject("Bike and height validation").AddComponent<BikeScaleValidation>();}
        IEnumerator Start()
        {
            var g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--bike-scale-output");
            yield return Capture(g,args[i+1]);Debug.Log("BIKE_SCALE_RUNTIME_PASSED");Application.Quit(0);
        }
        public static IEnumerator Capture(CityGame g,string folder)
        {
            Directory.CreateDirectory(folder);bool high=g.Graphics.High,cozy=g.Graphics.Cozy,glass=g.Graphics.LivingGlass,fog=RenderSettings.fog;g.enabled=false;
            var studio=new GameObject("Scale comparison studio");studio.transform.position=new Vector3(1000,0,0);
            var cam=new GameObject("Scale camera").AddComponent<Camera>();cam.enabled=false;cam.cullingMask=1<<9;cam.orthographic=true;cam.orthographicSize=3.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Art.Hex("EFE7D8");cam.farClipPlane=40;
            cam.transform.position=new Vector3(1006,4.3f,13);cam.transform.LookAt(new Vector3(1000,2,0));
            var lamp=new GameObject("Scale studio light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.cullingMask=1<<9;lamp.intensity=1.15f;lamp.transform.rotation=Quaternion.Euler(38,210,0);
            Art.Box("Floor",studio.transform,new Vector3(0,-.07f,0),new Vector3(20,.10f,12),Art.Hex("D2C6B3"),0);
            var sky=Models.Character(studio.transform,0);sky.Root.localPosition=new Vector3(-2,0,0);sky.Root.localScale=Vector3.one*.66f;
            var tallExplorer=Models.Character(studio.transform,3);tallExplorer.Root.localPosition=new Vector3(1,0,0);tallExplorer.Root.localScale=Vector3.one*1.65f;
            foreach(Transform t in studio.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
            RenderSettings.fog=false;
            for(int style=0;style<3;style++)
            {
                sky.Gummy.SetLivingGlass(style==2);tallExplorer.Gummy.SetLivingGlass(style==2);sky.Gummy.SetStyle(style>0);tallExplorer.Gummy.SetStyle(style>0);sky.Pose(0,0,false,false);tallExplorer.Pose(0,0,false,false);sky.Gummy.Draw(0);tallExplorer.Gummy.Draw(0);
                Save(cam,folder,"rider-height-mode-"+style);
            }
            sky.Root.gameObject.SetActive(false);tallExplorer.Root.gameObject.SetActive(false);
            var normal=Models.Vehicle(studio.transform,Ride.Bicycle);normal.Root.localPosition=new Vector3(-2,0,0);
            var mini=Models.Vehicle(studio.transform,Ride.MiniBike);mini.Root.localPosition=new Vector3(1,0,0);
            var rider=Models.Character(studio.transform,3);rider.Root.localScale=Vector3.one*.7f;rider.Pose(0,0,true,true);rider.FitRide(mini,false);rider.Gummy.SetStyle(cozy);rider.Gummy.SetLivingGlass(glass);rider.Gummy.Draw(0);
            cam.orthographicSize=2.25f;cam.transform.position=new Vector3(1006,3,9);cam.transform.LookAt(new Vector3(1000,1,0));
            foreach(bool detailed in new[]{false,true})
            {
                g.Graphics.SetHigh(detailed);while(g.Graphics.Busy)yield return null;
                normal.Root.GetComponent<GraphicsBike>().SetHigh(detailed);mini.Root.GetComponent<GraphicsBike>().SetHigh(detailed);
                foreach(Transform t in studio.GetComponentsInChildren<Transform>())t.gameObject.layer=9;
                rider.Root.gameObject.SetActive(false);Save(cam,folder,detailed?"mini-bmx-high":"mini-bmx-low");
                rider.Root.gameObject.SetActive(true);rider.Pose(0,0,true,true);rider.FitRide(mini,false);rider.Gummy.Draw(0);Save(cam,folder,detailed?"mini-bmx-rider-high":"mini-bmx-rider-low");
            }
            Destroy(studio);Destroy(cam.gameObject);Destroy(lamp.gameObject);RenderSettings.fog=fog;
            g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
            if(glass)g.Graphics.SetLivingGlass(true);else g.Graphics.SetCozy(cozy);
        }
        static void Save(Camera cam,string folder,string name)
        {
            var rt=new RenderTexture(1440,1000,24){antiAliasing=4};rt.Create();var prior=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var image=new Texture2D(1440,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
            RenderTexture.active=prior;cam.targetTexture=null;rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
