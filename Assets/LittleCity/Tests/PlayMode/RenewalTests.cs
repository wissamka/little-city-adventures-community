using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class RenewalTests
    {
        [UnityTest]public IEnumerator RaceCarsDriveOffRampsLandBrakeAndRetryWithoutJumpButton()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.Worlds.VisitWorld(4);g.enabled=false;
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                foreach(bool nos in new[]{false,true})foreach(int style in new[]{0,1,2})
                {
                    var s=g.State;s.SwitchRide(Ride.Walk);Assert.That(s.EnterVehicle(Ride.RaceCar,style,new Point(SkatePark.MegaX,0,SkatePark.MegaStart-36),0),Is.True);s.NitroInstalled=nos;s.NitroCharge=1;s.CancelNitro();s.Step(.02f,new DriveInput());g.RefreshPlayer();int before=s.ParkLandings;float peak=0;bool airborne=false,landed=false,climbed=false;
                    for(int i=0;i<900;i++)
                    {
                        s.Step(1f/60,new DriveInput{Forward=1,Nitro=nos&&s.Position.Z>SkatePark.MegaStart-8});peak=Mathf.Max(peak,s.Position.Y);if(s.Position.Y>10&&s.Position.Z<SkatePark.MegaLip)climbed=true;
                        if(s.Airborne&&s.Position.Z>SkatePark.MegaLip&&s.Position.Z<SkatePark.MegaLanding)airborne=true;
                        if(s.ParkLandings>before&&s.Position.Z>=SkatePark.MegaLanding){landed=true;break;}
                    }
                    Assert.That(climbed&&airborne&&landed,Is.True,"Car "+style+" NOS="+nos+" actual ramp route at z="+s.Position.Z+" peak="+peak);
                    Assert.That(s.Position.Z,Is.LessThanOrEqualTo(SkatePark.MegaLanding+42.1f),"Lands on the ramp, including with NOS");
                    Assert.That(s.Position.Y,Is.EqualTo(s.GroundHeight(s.Position)).Within(.03f));Assert.That(s.NeedsRepair,Is.False);Assert.That(s.IsJumpRide,Is.False,"Space still brakes car; no bunny-hop binding");
                    for(int n=0;n<120;n++)s.Step(1f/60,new DriveInput{Brake=true});Assert.That(s.Speed,Is.EqualTo(0).Within(.01f));s.RetryPark();Assert.That(s.Airborne,Is.False);Assert.That(s.Speed,Is.Zero);
                }
            }
            // A vertical side of the launch is not an entrance to the ramp.
            g.State.SwitchRide(Ride.Walk);g.State.EnterVehicle(Ride.RaceCar,0,new Point(SkatePark.MegaX-10,0,SkatePark.MegaLip-3),Mathf.PI/2);
            for(int n=0;n<180;n++)g.State.Step(1f/60,new DriveInput{Forward=1});Assert.That(g.State.Position.Y,Is.LessThan(.1f));Assert.That(g.State.Position.X,Is.LessThan(SkatePark.MegaX-6.5f));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]public IEnumerator CharacterHairPreservesIdentityFitAndPortraits()
        {
            int saved=PlayerPrefs.GetInt("LittleCity.Character",0);
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            foreach(bool high in new[]{false,true})
            {
                g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                foreach(int index in new[]{1,2})
                {
                    g.SelectCharacter(index);Assert.That(Simulation.CharacterNames[index],Is.EqualTo(index==1?"Maya":"Leo"));var head=g.Explorer.Head;
                    if(index==1){Assert.That(head.Find("Bob haircut"),Is.Null);Assert.That(g.Explorer.LongHair,Is.Not.Null);Assert.That(g.Explorer.LongHair.childCount,Is.EqualTo(12));}
                    else{int n=0;foreach(Transform child in head)if(child.name=="Leo fringe over eyes")n++;Assert.That(n,Is.EqualTo(7));}
                    foreach(var ride in new[]{Ride.Bicycle,Ride.Motorcycle,Ride.DirtBike,Ride.RaceCar,Ride.Walk})
                    {
                        g.State.Place(new Point(-300,0,-300));g.SelectRide(ride);yield return null;
                        if(ride!=Ride.Walk)
                        {
                            g.Explorer.Pose(0,0,true,true);g.Explorer.FitRide(g.CurrentVehicle,false);
                            var baseline=Models.Character(g.Explorer.Root.parent,0);baseline.Root.localScale=g.Explorer.Root.localScale;baseline.Pose(0,0,true,true);baseline.FitRide(g.CurrentVehicle,false);
                            Assert.That(g.Explorer.HandError,Is.LessThanOrEqualTo(baseline.HandError+.001f),ride+" hand fit preserved");Assert.That(g.Explorer.FootError,Is.LessThanOrEqualTo(baseline.FootError+.001f),ride+" foot fit preserved");Object.Destroy(baseline.Root.gameObject);
                        }
                    }
                }
            }
            g.SelectCharacter(saved);LogAssert.NoUnexpectedReceived();
        }
    }
}
