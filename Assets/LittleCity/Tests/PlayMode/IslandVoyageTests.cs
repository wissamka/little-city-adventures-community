using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
 public sealed class IslandVoyageTests
 {
  [UnityTest]public IEnumerator AllYachtsSailDockResumeAndAllIslandsHaveWorkingActivities()
  {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();
   for(int index=0;index<3;index++)
   {
    g.Worlds.VisitMarina(0);g.State.Place(new Point(index==0?-65:index==1?-130:-5,0,index==0?60:65));g.Interact();Assert.That(g.Worlds.Yachts.Aboard,Is.True);
    g.State.Place(new Point(6,0,-28.7f));for(int f=0;f<180;f++)g.State.Step(1f/60,new DriveInput{Forward=1});Assert.That(g.State.Position.Y,Is.EqualTo(YachtStairs.Height).Within(.02f));
    var helm=g.Worlds.Yachts.Spots.First(s=>s.Helm);g.State.Place(new Point(helm.Near.x,helm.Near.y,helm.Near.z));g.Interact();Assert.That(g.State.Mode,Is.EqualTo(Ride.Yacht));
    Point start=g.State.Position;for(int f=0;f<600;f++)g.State.Step(1f/60,new DriveInput{Forward=1});Assert.That(g.State.Position.Z-start.Z,Is.GreaterThan(60),"Yacht moves through real ocean");
    for(int f=0;f<180;f++)g.State.Step(1f/60,new DriveInput{Brake=true});Assert.That(g.State.Speed,Is.LessThan(.1f));float yaw=g.State.Yaw;
    for(int f=0;f<180;f++)g.State.Step(1f/60,new DriveInput{Forward=1,Steering=1});Assert.That(g.State.Yaw-yaw,Is.GreaterThan(.1f));
    g.State.Place(new Point(-250,-1.05f,270));g.Worlds.Voyages.Tick();g.Interact();Assert.That(g.Worlds.Voyages.InCabin,Is.True);Assert.That(g.Worlds.Yachts.Aboard,Is.True);g.State.Place(new Point(0,0,-27));g.Interact();Assert.That(g.State.Mode,Is.EqualTo(Ride.Yacht));Assert.That(g.State.Position.X,Is.EqualTo(-250).Within(.1f));
    for(int island=0;island<3;island++)
    {
     var berth=IslandLayout.Berth(island);Assert.That(IslandLayout.HullInWater(berth,0),Is.True);g.State.Place(berth);g.Worlds.Voyages.Tick();g.Interact();Assert.That(g.Worlds.Voyages.DockedIsland,Is.EqualTo(island));Assert.That(g.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(g.State.Swimming,Is.False);
     g.Interact();Assert.That(g.State.Mode,Is.EqualTo(Ride.Yacht));Assert.That(g.State.Position.X,Is.EqualTo(berth.X));
    }
    g.State.Place(YachtVoyages.HomeBerth(index));g.Worlds.Voyages.Tick();g.Interact();Assert.That(g.Worlds.Voyages.DockedIsland,Is.EqualTo(-1));Assert.That(g.State.Swimming,Is.False);
   }
   g.Worlds.VisitMarina(0);
   foreach(var activity in g.Worlds.Islands.Activities)
   {
    g.State.Place(new Point(activity.Point.x,activity.Point.y,activity.Point.z));g.RefreshPlayer();Assert.That(g.Worlds.Islands.Use(),Is.True,activity.Label);g.Worlds.Islands.Tick(.25f);
    if(activity.Moving!=null)
    {
     float hipY=g.State.Position.Y+.04f+1.08f*g.State.WalkingScale;
     Assert.That(hipY,Is.EqualTo(activity.Moving.position.y+.09f).Within(.01f),"Hips rest on the moving seat");
     if(activity.Ropes!=null)foreach(var rope in activity.Ropes)
     {
      var bottom=rope.TransformPoint(Vector3.down);Assert.That(bottom.y,Is.EqualTo(hipY).Within(.01f));Assert.That(bottom.z,Is.EqualTo(activity.Moving.position.z).Within(.01f));
     }
    }
    if(g.State.Resting!=RestPose.None)g.Worlds.Islands.Use();
    Assert.That(float.IsNaN(g.State.Position.X),Is.False);
   }
   Assert.That(g.Worlds.Islands.CompletedActivities,Is.GreaterThanOrEqualTo(6));Assert.That(g.Worlds.Islands.ShoppingMask,Is.EqualTo(7));
   g.SelectCharacter(4);Assert.That(g.State.WalkingScale,Is.GreaterThan(.9f));g.SelectCharacter(5);Assert.That(g.State.WalkingScale,Is.EqualTo(.66f));
   g.Worlds.VisitWorld(0);Assert.That(g.Worlds.Voyages.AtSea,Is.False);Assert.That(g.State.WorldIndex,Is.EqualTo(0));
  }
  [Test]public void YachtHullCannotCutThroughIslandsAndTurnsRespectTheBow()
  {
   var sim=new Simulation();sim.ArriveOnFoot(1);sim.EnterVehicle(Ride.Yacht,0,new Point(-430,-1.05f,250),0);
   for(int f=0;f<1200;f++)sim.Step(1f/60,new DriveInput{Forward=1});Assert.That(sim.Position.Z,Is.LessThan(340));Assert.That(IslandLayout.HullInWater(sim.Position,sim.Yaw),Is.True);
   foreach(var c in IslandLayout.Centers)Assert.That(IslandLayout.HullInWater(c,0),Is.False);
  }
 }
}
