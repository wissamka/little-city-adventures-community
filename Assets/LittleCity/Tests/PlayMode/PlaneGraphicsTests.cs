using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class PlaneGraphicsTests
    {
        [UnityTest]public IEnumerator AircraftGraphicsPreservePilotPropellerAndFlightState()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;g.SelectRide(Ride.Airplane);g.State.Place(new Point(172,0,-130));g.RefreshPlayer();
            var m=g.CurrentVehicle;var seat=m.Seat;var scale=m.Root.localScale;var head=g.Explorer.Head.position;var prop=m.Propeller;
            g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;int low=Visible(m.Root);
            g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;Assert.That(m.Root.GetComponent<GraphicsPlane>().ShowingHigh,Is.True);Assert.That(m.Seat,Is.EqualTo(seat));Assert.That(m.Root.localScale,Is.EqualTo(scale));Assert.That(g.Explorer.Head.position,Is.EqualTo(head));Assert.That(m.Propeller,Is.SameAs(prop));Assert.That(m.Wheels.Count,Is.EqualTo(3));
            bool glass=false;foreach(var r in m.Root.GetComponentsInChildren<Renderer>())if(r.enabled){Assert.That(r.sharedMaterial.shader.isSupported,Is.True);if(r.sharedMaterial.shader.name=="LittleCity/HighGlass")glass=true;}Assert.That(glass,Is.True,"Actual transparent cockpit glazing");
            var before=prop.localRotation;m.Animate(10,.3f,.6f);Assert.That(Quaternion.Angle(before,prop.localRotation),Is.GreaterThan(1));
            int high=Visible(m.Root);Assert.That(high,Is.LessThan(30));
            for(int n=0;n<2;n++){g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(low));g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(high));}
            for(int n=0;n<450;n++)g.State.Step(.02f,new DriveInput{Forward=1,Rise=true});Assert.That(g.State.Position.Y,Is.GreaterThan(12));Assert.That(g.State.Mode,Is.EqualTo(Ride.Airplane));LogAssert.NoUnexpectedReceived();
        }
        static int Visible(Transform t){int n=0;foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)n++;return n;}
    }
}
