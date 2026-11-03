using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class BoatGraphicsTests
    {
        [UnityTest]public IEnumerator BoatsPreserveSkipperSteeringWakeAndTowAcrossModes()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.Worlds.VisitWorld(1);g.enabled=false;
            foreach(var ride in new[]{Ride.Motorboat,Ride.Speedboat})
            {
                g.SelectRide(ride);g.State.Place(new Point(70,-1.05f,80));g.RefreshPlayer();yield return null;var m=g.CurrentVehicle;var seat=m.Seat;var grips=(Vector3[])m.Grips.Clone();var tow=m.Root.GetComponent<TowRig>();var wake=m.Root.GetComponent<BoatWake>();Assert.That(wake,Is.Not.Null);
                g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;int low=Visible(m.Root);string originalNames=Names(m.Root);Assert.That(m.Root.GetComponent<GraphicsBoat>().ShowingHigh,Is.False,"Initial boat low state");
                foreach(bool high in new[]{true,false,true})
                {
                    g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Assert.That(m.Root.GetComponent<GraphicsBoat>().ShowingHigh,Is.EqualTo(high));Assert.That(m.Seat,Is.EqualTo(seat));Assert.That(m.Grips,Is.EqualTo(grips));Assert.That(m.Root.GetComponent<BoatWake>(),Is.SameAs(wake));
                    g.Explorer.FitRide(m,false);Assert.That(g.Explorer.HandError,Is.LessThan(.1f));Assert.That(g.Explorer.FootError,Is.LessThan(.1f));if(!high)Assert.That(Visible(m.Root),Is.EqualTo(low),ride+" restored low: "+originalNames+" NOW "+Names(m.Root));else Assert.That(Visible(m.Root),Is.LessThan(35));
                    if(tow!=null){Assert.That(tow.Towing,Is.True);g.Marine.Use();Assert.That(tow.Towing,Is.False);g.Marine.Use();Assert.That(tow.Towing,Is.True);}
                }
                for(int n=0;n<250;n++)g.State.Step(.02f,new DriveInput{Forward=1});Assert.That(g.State.Position.Z,Is.GreaterThan(100));for(int n=0;n<100;n++)g.State.Step(.02f,new DriveInput{Brake=true});Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f));g.SelectRide(Ride.Walk);
            }
            LogAssert.NoUnexpectedReceived();
        }
        static int Visible(Transform t){int n=0;foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)n++;return n;}
        static string Names(Transform t){string names="";foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)names+=r.name+";";return names;}
    }
}
