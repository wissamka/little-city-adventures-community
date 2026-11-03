using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
 public sealed class CarDamageTests
 {
  static void Advance(Simulation s,float seconds,float forward=1){for(int i=0;i<seconds*60;i++)s.Step(1f/60,new DriveInput{Forward=forward});}
  static void Crash(Simulation s){s.Place(new Point(0,0,-32),Mathf.PI/2);Advance(s,4);}
  static Simulation TestCar(){var s=new Simulation();s.SwitchRide(Ride.RaceCar);s.Obstacles.Add(new Obstacle(30,-32,.2f,4,5));return s;}
  [Test]public void TenSeparateCrashesBreakCarWhileHeldWallContactAndSmallBumpsDoNot()
  {
   var s=TestCar();Crash(s);Assert.That(s.CarCrashes,Is.EqualTo(1));Assert.That(s.NeedsRepair,Is.False);
   Advance(s,20);Assert.That(s.CarCrashes,Is.EqualTo(1),"Repeated contact at the same wall is one impact");
   for(int count=2;count<=10;count++){Crash(s);Assert.That(s.CarCrashes,Is.EqualTo(count));Assert.That(s.NeedsRepair,Is.EqualTo(count==10));}
   var p=s.Position;Advance(s,3);Assert.That(s.Speed,Is.EqualTo(0).Within(.01f));Assert.That(s.Position.X,Is.EqualTo(p.X).Within(.2f));
   s.Repair();Assert.That(s.CarCrashes,Is.Zero);Assert.That(s.NeedsRepair,Is.False);s.Place(new Point(0,0,-32),Mathf.PI/2);Advance(s,1);Assert.That(s.Speed,Is.GreaterThan(1));
   s=TestCar();s.Place(new Point(27.8f,0,-32),Mathf.PI/2);Advance(s,3,.15f);Assert.That(s.CarCrashes,Is.Zero,"Slow parking bumps are harmless");
   s=TestCar();s.CarBreakdownImpacts=3;for(int i=0;i<3;i++)Crash(s);Assert.That(s.NeedsRepair,Is.True,"Threshold is tunable");
  }
  [UnityTest]public IEnumerator ActualBodyDentsAcrossStylesAndGraphicsKeepWheelMeshesAndPersistWhenParked()
  {
   int saved=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
   foreach(int style in new[]{0,1,2})foreach(bool high in new[]{false,true})
   {
    g.ResetHome();g.State.ChooseCar(style);g.SelectRide(Ride.RaceCar);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
    g.State.Obstacles.Clear();g.State.TrafficObstruction=null;g.State.Obstacles.Add(new Obstacle(30,-32,.2f,4,5));
    var wheelMeshes=g.CurrentVehicle.Root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.Contains("tire")||f.name.Contains("Tire")||f.transform.parent.name.StartsWith("Wheel")).Where(f=>f.sharedMesh.isReadable).ToDictionary(f=>f,f=>f.sharedMesh.vertices);
    Crash(g.State);g.RefreshPlayer();var damage=g.CurrentVehicle.Root.GetComponent<CarDamageVisual>();Assert.That(damage.DentCount,Is.EqualTo(1));Assert.That(damage.MaximumDeformation,Is.GreaterThan(.025f),"Actual panel vertices dent "+style+" "+high);float first=damage.MaximumDeformation;
    // RefreshPlayer can replace the actor; inspect wheel vertices on this same model during another dent.
    wheelMeshes=g.CurrentVehicle.Root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.name.Contains("tire")||f.name.Contains("Tire")||f.transform.parent.name.StartsWith("Wheel")).Where(f=>f.sharedMesh.isReadable).ToDictionary(f=>f,f=>f.sharedMesh.vertices);
    Crash(g.State);damage.Set(g.State.CarDents);Assert.That(damage.MaximumDeformation,Is.GreaterThan(first));
    foreach(var pair in wheelMeshes)Assert.That(pair.Key.sharedMesh.vertices,Is.EqualTo(pair.Value),"Wheel mesh stays intact");
    g.State.Place(new Point(0,0,-32),Mathf.PI/2);Assert.That(g.Parking.EnterOrExit(),Is.True);Assert.That(g.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(g.Parking.EnterOrExit(),Is.True);Assert.That(g.State.CarCrashes,Is.EqualTo(2));
    g.RepairCar();Assert.That(g.CurrentVehicle.Root.GetComponent<CarDamageVisual>().DentCount,Is.Zero);Assert.That(g.State.CarCrashes,Is.Zero);
   }
   PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",saved);PlayerPrefs.Save();
  }
 }
}
