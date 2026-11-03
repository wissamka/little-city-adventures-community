using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class FlightFunTests
    {
        [Test]public void HigherFlightCeilingTurnsDescentAndLanding()
        {
            var s=new Simulation();s.SwitchRide(Ride.Airplane);for(int n=0;n<4000;n++)s.Step(.02f,new DriveInput{Forward=1,Rise=true,Steering=.4f});Assert.That(s.Position.Y,Is.EqualTo(420).Within(.01));float yaw=s.Yaw;for(int n=0;n<50;n++)s.Step(.02f,new DriveInput{Steering=-1});Assert.That(Mathf.Abs(s.Yaw-yaw),Is.GreaterThan(.2f));for(int n=0;n<1000;n++)s.Step(.02f,new DriveInput{Descend=true,Steering=.4f});Assert.That(s.Position.Y,Is.InRange(230,250));s.TravelTo(1);for(int n=0;n<3000;n++)s.Step(.02f,new DriveInput{Forward=1,Rise=true});Assert.That(s.Position.Y,Is.EqualTo(420).Within(.01));Assert.That(Mathf.Abs(s.Position.Z),Is.LessThanOrEqualTo(s.BoundsZ));s.FinishFlightArrival(false);for(int n=0;n<350;n++)s.Step(.02f,new DriveInput{Descend=true,Brake=true});Assert.That(s.Position.Y,Is.EqualTo(0).Within(.02));Assert.That(s.Speed,Is.EqualTo(0).Within(.01));Assert.That(s.Position.Z,Is.GreaterThan(-123));
        }
        [UnityTest]public IEnumerator DoubleSpaceContinuesJumpThreeTurnsAllCharacters()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;g.enabled=false;g.Hud.BeginExploring();g.HeldKey=_=>false;
            void Frame(bool down){g.Flip.Input(down,.02f);g.State.Step(.02f,new DriveInput{Jump=down});}
            for(int character=0;character<Simulation.CharacterNames.Length;character++)
            {
                g.SelectCharacter(character);g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();g.Flip.Cancel();Frame(false);
                int count=g.Flip.Completed;float apex=0;for(int n=0;n<110;n++){Frame(true);apex=Mathf.Max(apex,g.State.Position.Y);}Assert.That(apex,Is.InRange(.8f,1.3f));Assert.That(g.State.Grounded,Is.True);Assert.That(g.Flip.Completed,Is.EqualTo(count),"Holding Space is one ordinary jump");
                Frame(false);Frame(true);for(int n=0;n<4;n++)Frame(false);float y=g.State.Position.Y,v=g.State.VerticalSpeed;g.Flip.Input(true,.02f);Assert.That(g.Flip.Active,Is.True);Assert.That(g.State.Position.Y,Is.EqualTo(y),"Second tap does not teleport");Assert.That(g.State.VerticalSpeed,Is.EqualTo(v),"Second tap does not relaunch");
                var origin=g.State.Position;for(int n=0;n<100;n++)Frame(false);Assert.That(g.Flip.Completed,Is.EqualTo(count+1));Assert.That(g.Flip.Angle,Is.EqualTo(1080).Within(.01));Assert.That(g.State.Grounded,Is.True);Assert.That(g.State.Position.X,Is.EqualTo(origin.X));Assert.That(g.State.Position.Z,Is.EqualTo(origin.Z));g.RefreshPlayer();Assert.That(Quaternion.Angle(g.Explorer.Root.localRotation,Quaternion.identity),Is.LessThan(.01));
                Frame(false);Frame(true);for(int n=0;n<4;n++)Frame(false);Frame(true);Assert.That(g.Flip.Active,Is.True);g.SendMessage("OnApplicationFocus",false);Assert.That(g.Flip.Active,Is.False);g.SendMessage("OnApplicationFocus",true);for(int n=0;n<100;n++)Frame(false);Assert.That(g.State.Grounded,Is.True);Assert.That(g.Flip.Completed,Is.EqualTo(count+1),"Canceled flip does not queue another");
                Frame(true);for(int n=0;n<30;n++)Frame(false);Frame(true);Assert.That(g.Flip.Active,Is.False,"Late second press is not a double tap");for(int n=0;n<100;n++)Frame(false);
            }
            g.SelectRide(Ride.RaceCar);g.State.Place(new Point(-32,0,-32));for(int n=0;n<150;n++)g.State.Step(.02f,new DriveInput{Forward=1});g.HeldKey=k=>k==KeyCode.Space;for(int n=0;n<150;n++){g.Flip.Input(n%4==0,.02f);g.ProcessControls(.02f);}Assert.That(g.Flip.Active,Is.False);Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f),"Vehicle Space remains brake");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
