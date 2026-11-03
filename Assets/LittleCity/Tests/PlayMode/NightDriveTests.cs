using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests {
 public sealed class NightDriveTests {
  [Test]public void NitroIsBoundedRequiresReleaseRechargesAndBrakes() {
   var s=new Simulation();s.SwitchRide(Ride.RaceCar);s.Place(new Point(-96,0,-300));s.NitroInstalled=true;
   float peak=0;for(int i=0;i<170;i++){s.Step(.02f,new DriveInput{Forward=1,Nitro=true});peak=Mathf.Max(peak,s.Speed);}
   Assert.That(peak,Is.GreaterThan(28));Assert.That(peak,Is.LessThanOrEqualTo(33.4f));Assert.That(s.NitroActive,Is.False);Assert.That(s.NitroCharge,Is.LessThan(.02f));
   for(int i=0;i<300;i++)s.Step(.02f,new DriveInput{Forward=1,Nitro=true});Assert.That(s.NitroCharge,Is.LessThan(.02f),"Held empty NOS cannot refill/retrigger");
   for(int i=0;i<401;i++)s.Step(.02f,new DriveInput{Brake=true});Assert.That(s.NitroCharge,Is.GreaterThan(.99f));Assert.That(Mathf.Abs(s.Speed),Is.LessThan(.01f));
   s.Step(.02f,new DriveInput{Forward=1,Nitro=true});Assert.That(s.NitroActive,Is.True);s.Step(.02f,new DriveInput{Forward=1,Nitro=true,Brake=true});Assert.That(s.NitroActive,Is.False);
   s.SwitchRide(Ride.Walk);s.Step(.02f,new DriveInput{Forward=1,Nitro=true});Assert.That(s.NitroActive,Is.False);
  }
  [UnityTest]public IEnumerator NightAndGlowIndependentOfGraphicsAndMenuCancelsBoost() {
   int night=PlayerPrefs.GetInt("LittleCity.Night",0),glow=PlayerPrefs.GetInt("LittleCity.Underglow",0),nos=PlayerPrefs.GetInt("LittleCity.NOS",0);
   PlayerPrefs.SetInt("LittleCity.Night",0);PlayerPrefs.SetInt("LittleCity.Underglow",0);PlayerPrefs.SetInt("LittleCity.NOS",0);
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics==null||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.SelectRide(Ride.RaceCar);g.State.Place(new Point(-96,0,-300));g.RefreshPlayer();g.enabled=false;yield return null;
   var atmo=CityAtmosphere.Current;Assert.That(atmo,Is.Not.Null);var position=g.State.Position;atmo.ToggleNight();atmo.CycleGlow();atmo.ToggleNitro();yield return null;
   Assert.That(atmo.Night,Is.True);Assert.That(Shader.GetGlobalFloat("_CityNight"),Is.EqualTo(1));Assert.That(g.State.NitroInstalled,Is.True);
   foreach(bool high in new[]{true,false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Assert.That(atmo.Night,Is.True);Assert.That(atmo.Glow,Is.EqualTo(1));Assert.That(RenderSettings.sun.intensity,Is.LessThan(.2f));Assert.That(g.State.Position.X,Is.EqualTo(position.X));}
   var fx=g.CurrentVehicle.Root.Find("Car lights and NOS");Assert.That(fx,Is.Not.Null);Assert.That(fx.Find("Soft ground underglow").gameObject.activeSelf,Is.True);Assert.That(fx.Find("Undercar light").GetComponent<Light>().enabled,Is.True);
   g.HeldKey=k=>false;g.ProcessControls(.02f);g.HeldKey=k=>k==KeyCode.W||k==KeyCode.LeftAlt;for(int i=0;i<60;i++)g.ProcessControls(.02f);Assert.That(g.State.NitroActive,Is.True);
   g.Hud.ShowHelp();Assert.That(g.State.NitroActive,Is.False,"Menu immediately cancels boost");g.Hud.BeginExploring();for(int i=0;i<10;i++)g.ProcessControls(.02f);Assert.That(g.State.NitroActive,Is.False,"Held Alt blocked after menu");
   atmo.ToggleNight();Assert.That(Shader.GetGlobalFloat("_CityNight"),Is.EqualTo(0));Assert.That(RenderSettings.sun.intensity,Is.GreaterThan(1));
   g.HeldKey=k=>false;PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.SetInt("LittleCity.Underglow",glow);PlayerPrefs.SetInt("LittleCity.NOS",nos);PlayerPrefs.Save();LogAssert.NoUnexpectedReceived();
  }
 }
}
