using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class RoadCamperTests
    {
        [Test]public void LongVehiclesBrakeReverseAndKeepTheirFrontAndCornersOutOfWalls()
        {
            foreach(var ride in new[]{Ride.Van,Ride.Camper})
            {
                var s=new Simulation();s.SwitchRide(ride);s.Place(new Point(0,0,0));
                for(int i=0;i<300;i++)s.Step(.02f,new DriveInput{Forward=1});Assert.That(s.Position.Z,Is.GreaterThan(50));
                for(int i=0;i<70;i++)s.Step(.02f,new DriveInput{Brake=true,Forward=1});Assert.That(s.Speed,Is.Zero.Within(.001f));
                float z=s.Position.Z;for(int i=0;i<100;i++)s.Step(.02f,new DriveInput{Forward=-1});Assert.That(s.Position.Z,Is.LessThan(z-3));
                foreach(float angle in new[]{0f,.35f,.78f,1.2f})
                {
                    s.Place(new Point(0,0,0),angle);s.Obstacles.Clear();s.Obstacles.Add(new Obstacle(0,12,60,.5f,8));
                    for(int i=0;i<350;i++)
                    {
                        s.Step(.02f,new DriveInput{Forward=1,Steering=i>160?.5f:0});
                        float extent=Mathf.Abs(Mathf.Cos(s.Yaw))*s.RoadHalfLength+Mathf.Abs(Mathf.Sin(s.Yaw))*s.RoadRadius;
                        Assert.That(s.Position.Z+extent,Is.LessThanOrEqualTo(11.501f),ride+" nose/corner through wall");Assert.That(s.Bank,Is.Zero.Within(.001f));
                    }
                }
            }
        }
        [UnityTest]public IEnumerator AllCharactersFitAndTheSameParkedRVProvidesAUsableCabin()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            foreach(bool high in new[]{false,true})foreach(int character in new[]{0,1,2,3,4,5})
            {
                g.SelectCharacter(character);g.SelectRide(Ride.Camper);g.State.Place(new Point(0,0,-32),character*Mathf.PI/4);g.RefreshPlayer();g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                var m=g.CurrentVehicle;Assert.That(m.Wheels.Count,Is.EqualTo(4));Assert.That(m.Root.GetComponent<GraphicsCamper>().ShowingHigh,Is.EqualTo(high));
                g.Explorer.FitRide(m,false);Assert.That(g.Explorer.HandError,Is.LessThan(.08f));Assert.That(g.Explorer.FootError,Is.LessThan(.08f));
                Assert.That(g.Parking.EnterOrExit(),Is.True);Assert.That(g.Camper.Boardable,Is.Not.Null);var parked=g.Camper.Boardable;Vector3 original=parked.Model.Root.position;
                Assert.That(g.State.Blocked(g.State.Position,.5f),Is.False,"Side door reachable at parked angle");Assert.That(g.State.Blocked(new Point(original.x,original.y,original.z),.5f),Is.True,"Parked RV remains solid");
                g.Interact();Assert.That(g.Camper.Inside,Is.True);Assert.That(g.State.Indoors,Is.True);Assert.That(g.State.Mode,Is.EqualTo(Ride.Walk));
                for(int step=0;step<100&&g.State.Position.Z>-.55f;step++)g.State.Step(.02f,new DriveInput{Forward=1,CameraYaw=Mathf.PI});
                Assert.That(g.State.Position.Z,Is.LessThan(-.55f),"Can walk down the aisle");
                Assert.That(g.State.Position.X,Is.EqualTo(0).Within(.01f));g.Interact();Assert.That(g.State.Resting,Is.EqualTo(RestPose.Sitting));g.Interact();Assert.That(g.State.Resting,Is.EqualTo(RestPose.None));
                for(int i=0;i<90;i++)g.State.Step(.02f,new DriveInput{Forward=1,CameraYaw=Mathf.PI});
                Assert.That(g.State.Position.Z,Is.InRange(-1.53f,-1.15f),"Aisle reaches bed without crossing bed frame");
                g.Interact();Assert.That(g.State.Resting,Is.EqualTo(RestPose.Sleeping));Assert.That(Mathf.Abs(g.Explorer.Head.position.x),Is.LessThan(1.2f),"Head and hair remain inside bed walls");g.Interact();Assert.That(g.State.Resting,Is.EqualTo(RestPose.None));
                for(int i=0;i<35;i++)g.State.Step(.02f,new DriveInput{Forward=1,CameraYaw=0});
                Assert.That(g.State.Position.Z,Is.InRange(.77f,1.9f));g.Interact();Assert.That(g.Camper.Inside,Is.False);Assert.That(g.State.Indoors,Is.False);Assert.That(parked.Model.Root.position,Is.EqualTo(original));
                Assert.That(g.Parking.EnterOrExit(),Is.True);Assert.That(g.State.Mode,Is.EqualTo(Ride.Camper));Assert.That(g.Parking.Occupied,Is.SameAs(parked));
                g.SelectRide(Ride.Walk);
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator VanGlazingAndWheelRotationSurviveSwitchingAndRVTravelCleansUp()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            g.SelectRide(Ride.Van);var m=g.CurrentVehicle;var wheel=m.Wheels[0];var rotation=wheel.localRotation;
            foreach(bool high in new[]{true,false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;bool glass=false;
                foreach(var r in m.Root.GetComponentsInChildren<Renderer>())if(r.enabled&&r.sharedMaterial.shader.name.Contains("Glass")){Assert.That(r.sharedMaterial.color.a,Is.LessThan(.3f));Assert.That(r.sharedMaterial.GetTag("RenderType",false),Is.EqualTo("Transparent"));glass=true;}
                Assert.That(glass,Is.True,"Actual transparent glass renderer");m.Animate(8,.1f,0);Assert.That(Quaternion.Angle(rotation,wheel.localRotation),Is.GreaterThan(1));
            }
            g.SelectRide(Ride.Camper);g.State.Place(new Point(0,0,-32));g.RefreshPlayer();g.Parking.EnterOrExit();g.Interact();Assert.That(g.Camper.Inside,Is.True);
            g.Worlds.VisitWorld(1);Assert.That(g.Camper.Inside,Is.False);Assert.That(g.State.WorldIndex,Is.EqualTo(1));Assert.That(g.State.Indoors,Is.False);
            g.Worlds.VisitWorld(0);Assert.That(g.Camper.Inside,Is.False);Assert.That(g.State.Mode,Is.EqualTo(Ride.Walk));LogAssert.NoUnexpectedReceived();
        }
    }
}
