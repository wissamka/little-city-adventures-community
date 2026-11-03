using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests {
 public sealed class TaxiTests {
  [UnityTest] public IEnumerator NewYorkTaxisDriveTurnsYieldResumeAndSwitchGraphics() {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;g.Hud.BeginExploring();
   g.SelectRide(Ride.Airplane);g.State.FinishFlightArrival(false);g.Worlds.BeginRoute(2);while(g.Worlds.Busy)yield return null;
   g.SelectRide(Ride.Walk);g.State.Place(new Point(172,0,-125));g.enabled=false;
   var fleet=Object.FindFirstObjectByType<TaxiFleet>();Assert.That(fleet,Is.Not.Null);fleet.enabled=false;Assert.That(fleet.Cabs.Count,Is.EqualTo(4));
   for(int i=0;i<2600;i++) {
    fleet.Step(.05f);
    foreach(var a in fleet.Cabs)foreach(var b in fleet.Cabs)if(a!=b)Assert.That(Vector3.Distance(a.Model.Root.localPosition,b.Model.Root.localPosition),Is.GreaterThan(5.5f),"No spawn/queue overlap");
   }
   foreach(var cab in fleet.Cabs)Assert.That(cab.Travelled,Is.GreaterThan(cab.Route.Length),"Completes full route, including all four turns, without static clipping or deadlock");
   var first=fleet.Cabs[0];first.Route.Sample(first.Distance+9,out var ahead,out var f);g.State.Place(new Point(ahead.x,0,ahead.z));
   for(int i=0;i<160;i++)fleet.Step(.02f);
   Assert.That(first.Speed,Is.EqualTo(0).Within(.01f),"Stops for player");Assert.That(Vector3.Distance(first.Model.Root.localPosition,ahead),Is.GreaterThan(3f),"No contact");
   float stopped=first.Distance;g.State.Place(new Point(172,0,-125));for(int i=0;i<150;i++)fleet.Step(.02f);Assert.That(Mathf.Abs(first.Distance-stopped),Is.GreaterThan(3),"Resumes after obstruction leaves");
   fleet.enabled=true;Assert.That(g.State.Blocked(new Point(first.Model.Root.position.x,0,first.Model.Root.position.z),.5f),Is.True,"Player cannot pass through taxi");fleet.enabled=false;
   foreach(var ride in new[]{Ride.Van,Ride.Camper})
   {
    fleet.enabled=true;g.State.EnterVehicle(ride,0,new Point(172,0,-125),0);
    Vector3 center=first.Model.Root.localPosition,behind=center-first.Forward*5;float yaw=Mathf.Atan2(first.Forward.x,first.Forward.z);g.State.Place(new Point(behind.x,0,behind.z),yaw);fleet.Step(0);
    Assert.That(g.State.Blocked(g.State.Position,g.State.RoadRadius),Is.True,"Long vehicle nose cannot pass through taxi");
    Vector3 clear=center-first.Forward*12;g.State.Place(new Point(clear.x,0,clear.z),yaw);Assert.That(fleet.BlocksPlayer(g.State.Position,g.State.RoadRadius),Is.False,"Clear separation allows driving");
    g.State.SwitchRide(Ride.Walk);g.State.Place(new Point(172,0,-125));fleet.enabled=false;
   }
   foreach(bool high in new[]{true,false,true}) {
    g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
    foreach(var cab in fleet.Cabs){Assert.That(cab.Model.Root.GetComponent<GraphicsCar>().ConceptVisible,Is.EqualTo(high));Assert.That(cab.Model.Root.Find("TAXI roof sign").gameObject.activeInHierarchy,Is.True);
     if(high)foreach(var r in cab.Model.Root.Find("Near-realistic concept sports car").GetComponentsInChildren<Renderer>())if(r.name.Contains("|Paint")){Assert.That(r.sharedMaterial.color.g,Is.GreaterThan(.5f),"Yellow taxi paint retained in High Def");Assert.That(r.sharedMaterial.color.b,Is.LessThan(.25f));}}
   }
   g.Worlds.VisitWorld(0);Assert.That(Object.FindFirstObjectByType<TaxiFleet>(),Is.Null,"No taxis in unrelated world");Assert.That(g.State.TrafficObstruction==null||!g.State.TrafficObstruction(new Point(ahead.x,0,ahead.z),.5f),Is.True);LogAssert.NoUnexpectedReceived();
  }
 }
}
