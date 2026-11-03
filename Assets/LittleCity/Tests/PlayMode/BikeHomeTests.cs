using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class BikeHomeTests
    {
        [UnityTest]
        public IEnumerator FourBikesUseDoorStairsBrakesWallsAndExit()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;g.Hud.BeginExploring();g.enabled=false;
            foreach(var ride in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike,Ride.DirtBike})
            {
                g.SelectRide(ride);g.State.Place(new Point(13,0,-42.2f),Mathf.PI);g.RefreshPlayer();var before=g.State.Position;g.Interact();yield return null;
                Assert.That(g.Buildings.Inside,Is.True,ride+" door entry");Assert.That(g.State.Position.X,Is.EqualTo(before.X));Assert.That(g.State.Position.Z,Is.EqualTo(before.Z),"Opening a bike door does not teleport the rider");Assert.That(g.State.Mode,Is.EqualTo(ride));
                g.HeldKey=_=>false;g.ProcessControls(.02f);g.State.Step(.3f,new DriveInput{Forward=1});Assert.That(g.State.Speed,Is.GreaterThan(.1f));g.HeldKey=k=>k==KeyCode.LeftControl;for(int i=0;i<40;i++)g.ProcessControls(.02f);Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f),ride+" actual Ctrl input stops indoors");g.HeldKey=_=>false;g.ProcessControls(.02f);
                var p=g.Buildings.Active.Plan;float corridor=p.StairX-2.5f,low=p.StairStart-.32f,high=p.StairEnd+.45f;
                Drive(g,p,new Point(0,0,high));Drive(g,p,new Point(corridor,0,high));Drive(g,p,new Point(corridor,0,low));Drive(g,p,new Point(p.StairX,0,low));Drive(g,p,new Point(p.StairX,p.Story,high));Assert.That(g.State.Position.Y,Is.EqualTo(p.Story).Within(.1f),ride+" upstairs");
                // Reverse down the same flight, with braking/restart between segments.
                Drive(g,p,new Point(p.StairX,0,low),true);Assert.That(g.State.Position.Y,Is.EqualTo(0).Within(.1f),ride+" downstairs");
                Assert.That(g.State.Blocked(p.World(new Point(p.Width/2+.2f,0,0)),.42f),Is.True,"Side wall remains solid");
                Drive(g,p,new Point(corridor,0,low));Drive(g,p,new Point(corridor,0,high));Drive(g,p,new Point(0,0,high));Drive(g,p,new Point(0,0,p.Depth/2+2.4f));Assert.That(g.State.Indoors,Is.False,ride+" actual open-door exit");Assert.That(g.State.Mode,Is.EqualTo(ride));
                g.State.Place(new Point(-300,0,-300));for(int n=0;n<150;n++)g.State.Step(.02f,new DriveInput{Forward=1});Assert.That(g.State.Speed,Is.GreaterThan(10));g.State.Place(new Point(-300,0,-300));g.SelectRide(Ride.Walk);
            }
            g.SelectRide(Ride.DirtBike);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();Assert.That(g.CurrentVehicle.Root.localScale.y,Is.GreaterThan(1));Assert.That(g.CurrentVehicle.Suspension.Count,Is.GreaterThan(0));g.Explorer.FitRide(g.CurrentVehicle,false);Assert.That(g.Explorer.HandError,Is.LessThan(.1f));Assert.That(g.Explorer.FootError,Is.LessThan(.1f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator DirtBikeFitsSmallRotatedHomesWithoutWallClipping()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;g.HeldKey=_=>false;
            foreach(float yaw in new[]{90f,180f,270f})
            {
                var b=BuildingArchitecture.Build(g.transform,new Vector3(-400,0,-400),12,14,2,3.4f,yaw,Art.Teal,0,"Rotated bike home");var p=b.Plan;
                g.SelectRide(Ride.DirtBike);g.State.Place(p.World(new Point(0,0,p.Depth/2+1.3f)),p.Yaw+Mathf.PI);g.RefreshPlayer();g.Buildings.Enter(b);
                float corridor=p.StairX-2.5f,low=p.StairStart-.32f,high=p.StairEnd+.45f;
                Drive(g,p,new Point(0,0,high));Drive(g,p,new Point(corridor,0,high));Drive(g,p,new Point(corridor,0,low));Drive(g,p,new Point(p.StairX,0,low));Drive(g,p,new Point(p.StairX,p.Story,high));Assert.That(g.State.Position.Y,Is.EqualTo(p.Story).Within(.1f));
                Drive(g,p,new Point(p.StairX,0,low),true);Assert.That(g.State.Position.Y,Is.EqualTo(0).Within(.1f));Assert.That(g.State.Blocked(p.World(new Point(p.Width/2,0,0)),.42f),Is.True,"Rotated wall remains solid");
                g.Buildings.Leave();g.SelectRide(Ride.Walk);Object.Destroy(b.gameObject);yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }
        public static void Drive(CityGame g,BuildingPlan p,Point local,bool reverse=false)
        {
            Point target=p.World(local);
            for(int n=0;n<2400;n++)
            {
                float x=target.X-g.State.Position.X,z=target.Z-g.State.Position.Z;
                if(x*x+z*z<.025f){for(int j=0;j<30;j++)g.State.Step(.02f,new DriveInput{Brake=true});return;}
                float desired=Mathf.Atan2(x,z)+(reverse?Mathf.PI:0),error=Mathf.DeltaAngle(g.State.Yaw*Mathf.Rad2Deg,desired*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                var previous=g.State.Position;g.State.Step(.02f,new DriveInput{Steering=Mathf.Clamp(error*3,-1,1),Forward=Mathf.Abs(error)<.12f?(reverse?-1:1):0,Brake=Mathf.Abs(error)>.12f});g.Buildings.TickBikeDoor();
                Assert.That(new Vector2(g.State.Position.X-previous.X,g.State.Position.Z-previous.Z).magnitude,Is.LessThan(.09f),"Continuous indoor movement without teleport");
            }
            Assert.Fail(g.State.Mode+" cannot reach "+local.X+","+local.Z+" from local "+p.Local(g.State.Position).X+","+p.Local(g.State.Position).Z+" at height "+g.State.Position.Y);
        }
    }
}
