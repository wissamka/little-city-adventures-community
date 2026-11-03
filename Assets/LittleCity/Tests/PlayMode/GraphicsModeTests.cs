using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class GraphicsModeTests
    {
        [UnityTest]
        public IEnumerator LowHighSwitchBackPreservesGameAndSportsCar()
        {
            int saved=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",0);
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;yield return null;
            while(g.Graphics.Busy)yield return null;
            g.Hud.BeginExploring();g.SelectRide(Ride.RaceCar);g.State.Place(new Point(-32,0,-15));g.RefreshPlayer();g.OrbitCamera(22,4);g.enabled=false;g.People.enabled=false;
            var state=g.State;var before=state.Position;float yaw=state.Yaw;int character=state.Character,world=state.WorldIndex;var car=g.CurrentVehicle;
            var low=Capture("sports-car-low");
            Assert.That(g.Graphics.Available,Is.True,"High Def shaders supported");g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;yield return null;
            Assert.That(g.Graphics.High,Is.True);Assert.That(PlayerPrefs.GetInt("LittleCity.GraphicsModeV2"),Is.EqualTo(1));
            Assert.That(car.Root.GetComponent<GraphicsCar>().ConceptVisible,Is.True,"Actual imported concept geometry enabled");
            Assert.That(car.Root.GetComponent<GraphicsCar>().ShowingHigh,Is.True);Assert.That(car.Root.GetComponent<GraphicsCar>().DetailVertices,Is.GreaterThan(5000),"Substantial new car geometry");
            foreach(var renderer in car.Root.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled)Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True,renderer.name);
            var high=Capture("sports-car-high");Assert.That(Difference(low,high),Is.GreaterThan(.003f),"Actual visible mode difference");
            Assert.That(state.Position.X,Is.EqualTo(before.X));Assert.That(state.Position.Y,Is.EqualTo(before.Y));Assert.That(state.Position.Z,Is.EqualTo(before.Z));Assert.That(state.Yaw,Is.EqualTo(yaw));Assert.That(state.Character,Is.EqualTo(character));Assert.That(state.WorldIndex,Is.EqualTo(world));Assert.That(state.Mode,Is.EqualTo(Ride.RaceCar));
            g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;yield return null;
            var back=Capture("sports-car-low-restored");Assert.That(Difference(low,back),Is.LessThan(.02f),"Former High Def baseline returns");Assert.That(PlayerPrefs.GetInt("LittleCity.GraphicsModeV2"),Is.EqualTo(0));
            g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
            state.Place(new Point(-300,0,-300));for(int i=0;i<150;i++)g.ProcessControls(.02f);g.HeldKey=k=>k==KeyCode.W;for(int i=0;i<150;i++)g.ProcessControls(.02f);Assert.That(state.ActualSpeed,Is.GreaterThan(10));
            g.HeldKey=k=>k==KeyCode.Space;for(int i=0;i<150;i++)g.ProcessControls(.02f);Assert.That(Mathf.Abs(state.Speed),Is.LessThan(.01f));
            g.Worlds.VisitWorld(4);yield return new WaitForSecondsRealtime(.6f);Assert.That(g.Graphics.High,Is.True);Assert.That(state.WorldIndex,Is.EqualTo(4));g.SelectRide(Ride.MountainBike);state.Place(new Point(-133,0,35));g.RefreshPlayer();Capture("halfpipe-high");
            g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;Assert.That(g.CurrentVehicle.Root.GetComponentsInChildren<MeshRenderer>()[0].sharedMaterial.shader.name,Is.EqualTo("LittleCity/HighDefinition"));
            PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",saved);PlayerPrefs.Save();LogAssert.NoUnexpectedReceived();
        }
        static Color32[] Capture(string name)
        {
            Directory.CreateDirectory("Documentation/Validation");var c=Camera.main;var old=c.targetTexture;var rt=new RenderTexture(1366,900,24){antiAliasing=4};rt.Create();c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1366,900,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1366,900),0,0);t.Apply();var pixels=t.GetPixels32();File.WriteAllBytes("Documentation/Validation/"+name+".png",t.EncodeToPNG());RenderTexture.active=prev;c.targetTexture=old;Object.Destroy(t);rt.Release();Object.Destroy(rt);return pixels;
        }
        static float Difference(Color32[] a,Color32[] b){double sum=0;for(int i=0;i<a.Length;i+=13)sum+=Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b);return (float)(sum/(a.Length/13*3*255.0));}
    }
}
