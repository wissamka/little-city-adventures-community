using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class HelicopterTests
    {
        [Test]public void VerticalTakeoffStableHoverTurnFlightBrakeAndLanding()
        {
            var s=new Simulation();s.SwitchRide(Ride.Helicopter);s.Place(new Point(0,0,0));
            for(int i=0;i<350;i++)s.Step(.02f,new DriveInput{Rise=true});Assert.That(s.Position.Y,Is.GreaterThan(40));Assert.That(s.Speed,Is.Zero);Assert.That(s.Airborne,Is.True);Assert.That(s.ExitVehicle(),Is.False);
            for(int i=0;i<80;i++)s.Step(.02f,new DriveInput());Point hover=s.Position;for(int i=0;i<300;i++)s.Step(.02f,new DriveInput());Assert.That(s.Position.X,Is.EqualTo(hover.X).Within(.001f));Assert.That(s.Position.Z,Is.EqualTo(hover.Z).Within(.001f));Assert.That(s.Position.Y,Is.EqualTo(hover.Y).Within(.001f));
            for(int i=0;i<80;i++)s.Step(.02f,new DriveInput{Steering=1});Assert.That(s.Yaw,Is.GreaterThan(1.2f));
            for(int i=0;i<180;i++)s.Step(.02f,new DriveInput{Forward=1});Assert.That(s.Position.X,Is.GreaterThan(25));
            for(int i=0;i<120;i++)s.Step(.02f,new DriveInput{Brake=true,Forward=1,Rise=true});Assert.That(s.Speed,Is.Zero.Within(.001f));Assert.That(s.VerticalSpeed,Is.Zero.Within(.001f));
            for(int i=0;i<600;i++)s.Step(.02f,new DriveInput{Descend=true});Assert.That(s.Position.Y,Is.Zero.Within(.001f));Assert.That(s.Airborne,Is.False);Assert.That(s.Speed,Is.Zero);Assert.That(s.ExitVehicle(),Is.True);
        }
        [Test]public void RotorClearanceCeilingAndWaterLandingStayBounded()
        {
            var s=new Simulation();s.SwitchRide(Ride.Helicopter);s.Place(new Point(0,419,0));for(int i=0;i<150;i++)s.Step(.02f,new DriveInput{Rise=true});Assert.That(s.Position.Y,Is.EqualTo(Simulation.Ceiling));
            s.Place(new Point(0,20,0));s.Obstacles.Add(new Obstacle(0,20,30,.5f,100));for(int i=0;i<400;i++)s.Step(.02f,new DriveInput{Forward=1});Assert.That(s.Position.Z,Is.InRange(10,13.21f));Assert.That(s.Position.Y,Is.EqualTo(20).Within(.01f));Assert.That(s.NeedsRepair,Is.False);
            s.Obstacles.Clear();s.ArriveMarina(new Point(70,0,70));s.EnterVehicle(Ride.Helicopter,0,new Point(70,20,70),0);for(int i=0;i<600;i++)s.Step(.02f,new DriveInput{Descend=true});Assert.That(s.Position.Y,Is.EqualTo(1.5f).Within(.001f));Assert.That(s.Airborne,Is.True);Assert.That(s.ExitVehicle(),Is.False);
            s.Place(new Point(143,10,-76));for(int i=0;i<250;i++)s.Step(.02f,new DriveInput{Descend=true});Assert.That(s.Position.Y,Is.Zero.Within(.001f));Assert.That(s.ExitVehicle(),Is.True);
        }
        [UnityTest]public IEnumerator AllPilotsFitAndBothRotorsAnimateAcrossGraphicsAndTravel()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            foreach(bool high in new[]{false,true})foreach(int character in new[]{0,1,2,3,4,5})
            {
                g.SelectCharacter(character);g.SelectRide(Ride.Helicopter);g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;var m=g.CurrentVehicle;var rig=m.Root.GetComponent<HelicopterRig>();Assert.That(rig.ShowingHigh,Is.EqualTo(high));g.Explorer.FitRide(m,false);Assert.That(g.Explorer.HandError,Is.LessThan(.08f));Assert.That(g.Explorer.FootError,Is.LessThan(.08f));Assert.That(g.Explorer.Head.position.y,Is.LessThan(2.8f));
                var main=rig.MainRotor.localRotation;var tail=rig.TailRotor.localRotation;m.Animate(0,.1f,1);Assert.That(Quaternion.Angle(main,rig.MainRotor.localRotation),Is.GreaterThan(15));Assert.That(Quaternion.Angle(tail,rig.TailRotor.localRotation),Is.GreaterThan(15));
                Assert.That(g.State.Blocked(g.State.Position,6.3f),Is.False,"Helipad clear for takeoff");for(int i=0;i<200;i++)g.State.Step(.02f,new DriveInput{Rise=true});Assert.That(g.State.Position.Y,Is.GreaterThan(18));Assert.That(g.State.StartAirManeuver(AirTrick.Loop),Is.False,"Plane aerobatics stay on airplane");
                g.Worlds.Travel(1);Assert.That(g.State.Mode,Is.EqualTo(Ride.Helicopter));Assert.That(g.State.WorldIndex,Is.EqualTo(1));g.Worlds.VisitWorld(0);Assert.That(g.State.Mode,Is.EqualTo(Ride.Walk));
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
