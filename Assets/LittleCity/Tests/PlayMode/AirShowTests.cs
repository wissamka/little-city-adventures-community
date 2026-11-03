using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class AirShowTests
    {
        static Simulation Cruise(float y=120)
        {
            var s=new Simulation();s.SwitchRide(Ride.Airplane);s.Obstacles.Clear();s.Place(new Point(0,y,-150));
            for(int i=0;i<300;i++)s.Step(.02f,new DriveInput{Forward=1});return s;
        }
        [Test]public void CompleteManeuversStayContinuousAndReturnControl()
        {
            foreach(AirTrick trick in new[]{AirTrick.Loop,AirTrick.Roll,AirTrick.Turn})
            {
                var s=Cruise();Point start=s.Position,previous=start;float heading=s.Yaw,speed=s.Speed,apex=start.Y,mostPitch=0,mostBank=0;
                Assert.That(s.StartAirManeuver(trick),Is.True);Assert.That(s.CanTravel,Is.False);
                for(int i=0;i<210&&s.AirManeuver!=AirTrick.None;i++)
                {
                    s.Step(.02f,new DriveInput{Forward=1,Steering=-1,Rise=true});
                    var p=s.Position;Assert.That(Vector3.Distance(new Vector3(p.X,p.Y,p.Z),new Vector3(previous.X,previous.Y,previous.Z)),Is.LessThan(.7f),"No teleport or excessive flight velocity");
                    Assert.That(p.Y,Is.InRange(70,Simulation.Ceiling));apex=Mathf.Max(apex,p.Y);mostPitch=Mathf.Max(mostPitch,Mathf.Abs(s.Pitch));mostBank=Mathf.Max(mostBank,Mathf.Abs(s.Bank));previous=p;
                }
                Assert.That(s.AirCompletions,Is.EqualTo(1));Assert.That(Mathf.Abs(s.Pitch),Is.LessThan(.01f));Assert.That(Mathf.Abs(s.Bank),Is.LessThan(.01f));Assert.That(s.Speed,Is.EqualTo(speed));Assert.That(s.CanTravel,Is.True);
                if(trick==AirTrick.Loop){Assert.That(apex-start.Y,Is.GreaterThan(39));Assert.That(mostPitch,Is.GreaterThan(6));Assert.That(s.Position.X,Is.EqualTo(start.X).Within(.01));Assert.That(s.Position.Z,Is.EqualTo(start.Z).Within(.7));}
                if(trick==AirTrick.Roll){Assert.That(mostBank,Is.GreaterThan(6));Assert.That(s.Position.Y,Is.EqualTo(start.Y).Within(.1));Assert.That(s.Position.Z-start.Z,Is.EqualTo(speed*4).Within(.7));}
                if(trick==AirTrick.Turn){Assert.That(Mathf.Abs(Mathf.DeltaAngle(s.Yaw*Mathf.Rad2Deg,heading*Mathf.Rad2Deg)),Is.EqualTo(180).Within(.5));Assert.That(mostBank,Is.GreaterThan(.7));}
                float yaw=s.Yaw;for(int i=0;i<20;i++)s.Step(.02f,new DriveInput{Steering=1});Assert.That(Mathf.Abs(s.Yaw-yaw),Is.GreaterThan(.2));
            }
        }
        [Test]public void UnsafePathsRejectedAndHeldInputsCannotRepeat()
        {
            var low=Cruise(35);Assert.That(low.StartAirManeuver(AirTrick.Loop),Is.False);
            var ceiling=Cruise(420);Assert.That(ceiling.StartAirManeuver(AirTrick.Loop),Is.False);
            var obstacle=Cruise();var p=obstacle.Position;obstacle.Obstacles.Add(new Obstacle(p.X,p.Z+15,8,8,200));Assert.That(obstacle.StartAirManeuver(AirTrick.Loop),Is.False);
            var s=Cruise();for(int i=0;i<300;i++)s.Step(.02f,new DriveInput{AirRoll=true});Assert.That(s.AirCompletions,Is.EqualTo(1));Assert.That(s.AirManeuver,Is.EqualTo(AirTrick.None));
            s.Step(.02f,new DriveInput());s.Step(.02f,new DriveInput{AirRoll=true});Assert.That(s.AirManeuver,Is.EqualTo(AirTrick.Roll));
            var before=s.Position;s.Step(.02f,new DriveInput{AirRoll=true,Brake=true});Assert.That(s.AirManeuver,Is.EqualTo(AirTrick.None));Assert.That(s.Speed,Is.LessThan(32));Assert.That(Mathf.Abs(s.Position.Z-before.Z),Is.LessThan(.7));
            for(int i=0;i<200;i++)s.Step(.02f,new DriveInput{AirRoll=true});Assert.That(s.AirCompletions,Is.EqualTo(1));
            s.SwitchRide(Ride.Walk);Assert.That(s.StartAirManeuver(AirTrick.Roll),Is.False);
        }
        [UnityTest]public IEnumerator KeyboardMenuAndFocusReleaseKeepCameraUpright()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics.Busy)yield return null;
            g.enabled=false;g.Hud.BeginExploring();g.SelectRide(Ride.Airplane);g.State.Obstacles.Clear();g.State.Place(new Point(0,160,-150));g.HeldKey=k=>k==KeyCode.W;for(int i=0;i<300;i++)g.ProcessControls(.02f);
            g.HeldKey=k=>k==KeyCode.X;g.ProcessControls(.02f);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.Loop));
            for(int i=0;i<100;i++){g.ProcessControls(.02f);g.SendMessage("UpdateActor",.02f);g.SendMessage("LateUpdate");Assert.That(Mathf.Abs(Mathf.DeltaAngle(Camera.main.transform.eulerAngles.z,0)),Is.LessThan(.1f));}
            var p=g.State.Position;g.Hud.ShowExplore(true);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.None));Assert.That(g.State.Position.Y,Is.EqualTo(p.Y));
            g.Hud.BeginExploring();for(int i=0;i<10;i++)g.ProcessControls(.02f);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.None),"Held X cannot queue another loop after menu");
            g.HeldKey=k=>false;g.ProcessControls(.02f);g.HeldKey=k=>k==KeyCode.Z;g.ProcessControls(.02f);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.Roll));g.SendMessage("OnApplicationFocus",false);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.None));g.SendMessage("OnApplicationFocus",true);g.ProcessControls(.02f);Assert.That(g.State.AirManeuver,Is.EqualTo(AirTrick.None));
            g.HeldKey=k=>false;LogAssert.NoUnexpectedReceived();
        }
    }
}
