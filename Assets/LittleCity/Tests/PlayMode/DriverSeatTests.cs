using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
 public sealed class DriverSeatTests
 {
  [UnityTest]public IEnumerator DriverUsesActualCenterCockpitWithBentKneesAndWheelPedalContacts()
  {
   int oldCharacter=PlayerPrefs.GetInt("LittleCity.Character",0),oldGraphics=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
    foreach(int character in new[]{4,5,0,1,2,3})foreach(int style in new[]{0,1,2})foreach(bool high in new[]{false,true})
    {
     g.ResetHome();g.SelectCharacter(character);float height=g.State.WalkingScale;g.State.ChooseCar(style);g.SelectRide(Ride.RaceCar);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();yield return null;
     var car=g.CurrentVehicle;var person=g.Explorer;person.FitRide(car,false);
     Assert.That(person.HandError,Is.LessThan(.045f),character+" wheel contact style "+style+" "+high);Assert.That(person.FootError,Is.LessThan(.045f),character+" pedal contact style "+style+" "+high);
     var hip=person.Root.TransformPoint(new Vector3(0,1.08f,0));Assert.That(Vector3.Distance(hip,car.Root.TransformPoint(car.Seat)),Is.LessThan(.005f));
     if(high){Assert.That(car.Seat.x,Is.EqualTo(-.005f).Within(.01f),"Verified central driving seat");Assert.That(car.Grips.Average(p=>p.x),Is.EqualTo(-.005f).Within(.01f));}
     {
      float headTop=float.MinValue;foreach(var f in person.Head.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices)headTop=Mathf.Max(headTop,car.Root.InverseTransformPoint(f.transform.TransformPoint(v)).y);
      Assert.That(headTop,Is.LessThan(high?1.40f:1.52f),"Hair and head clear the cockpit roof "+character+" "+high);
     }
     if(style==0&&(character==4||character==5||character==0))
     {
      string tag=Simulation.CharacterNames[character]+(high?"-high":"-low");Camera.main.transform.position=car.Root.TransformPoint(new Vector3(0,2.25f,3.3f));Camera.main.transform.LookAt(car.Root.TransformPoint(car.Seat+Vector3.up*.28f));yield return null;Capture(tag+"-front");
      Camera.main.transform.position=car.Root.TransformPoint(new Vector3(.70f,1.30f,.90f));Camera.main.transform.LookAt(car.Root.TransformPoint(car.Seat+new Vector3(0,.20f,.22f)));yield return null;Capture(tag+"-side");
     }
     g.State.Place(new Point(-300,0,-300));for(int f=0;f<60;f++)g.State.Step(.02f,new DriveInput{Forward=1});for(int f=0;f<100;f++)g.State.Step(.02f,new DriveInput{Brake=true});g.RefreshPlayer();Assert.That(g.Parking.EnterOrExit(),Is.True);Assert.That(g.State.WalkingScale,Is.EqualTo(height));Assert.That(g.Parking.EnterOrExit(),Is.True);g.RefreshPlayer();Assert.That(g.Explorer.HandError,Is.LessThan(.045f));
    }
   }
   finally{PlayerPrefs.SetInt("LittleCity.Character",oldCharacter);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",oldGraphics);PlayerPrefs.Save();}
  }
  static void Capture(string name)
  {
   Directory.CreateDirectory("Documentation/DriverFit");var c=Camera.main;var old=c.targetTexture;var rt=new RenderTexture(1440,1000,24);rt.Create();c.targetTexture=rt;c.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1440,1000,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1440,1000),0,0);t.Apply();File.WriteAllBytes("Documentation/DriverFit/"+name+".png",t.EncodeToPNG());RenderTexture.active=previous;c.targetTexture=old;Object.Destroy(t);rt.Release();Object.Destroy(rt);
  }
 }
}
