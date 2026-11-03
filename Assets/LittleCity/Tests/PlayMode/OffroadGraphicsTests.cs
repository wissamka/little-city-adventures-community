using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class OffroadGraphicsTests
    {
        [UnityTest]public IEnumerator SafariSwitchPreservesDriverWheelsAndDriving()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            g.SelectRide(Ride.Walk);Assert.That(g.State.EnterVehicle(Ride.RaceCar,3,new Point(-300,0,-300),0),Is.True);g.RefreshPlayer();var m=g.CurrentVehicle;Vector3 seat=m.Seat,grip=m.Grips[0];var p=g.State.Position;
            g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;int low=Visible(m.Root);
            g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
            Assert.That(m.Root.GetComponent<GraphicsOffroad>().ShowingHigh,Is.True);Assert.That(m.Wheels.Count,Is.EqualTo(4));Assert.That(m.Seat,Is.EqualTo(seat));Assert.That(m.Grips[0],Is.EqualTo(grip));Assert.That(g.State.Position.X,Is.EqualTo(p.X));
            g.Explorer.FitRide(m,false);Assert.That(g.Explorer.HandError,Is.LessThan(.1f));Assert.That(g.Explorer.FootError,Is.LessThan(.1f));
            foreach(var w in m.Wheels){var start=Bounds(w);for(int n=0;n<24;n++){w.localRotation=Quaternion.Euler(n*15,0,0);var b=Bounds(w);Assert.That(Vector3.Distance(start.center,b.center),Is.LessThan(.03f));Assert.That(b.size.x,Is.EqualTo(start.size.x).Within(.005f));Assert.That(b.size.y,Is.EqualTo(start.size.y).Within(.04f));Assert.That(b.size.z,Is.EqualTo(start.size.z).Within(.04f));}}
            for(int n=0;n<200;n++)g.State.Step(.02f,new DriveInput{Forward=1});Assert.That(g.State.Speed,Is.GreaterThan(10));for(int n=0;n<100;n++)g.State.Step(.02f,new DriveInput{Brake=true});Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f));
            int high=Visible(m.Root);Assert.That(high,Is.LessThan(35));for(int n=0;n<2;n++){g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(low));g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(high));}
            Assert.That(g.State.ExitVehicle(),Is.True);LogAssert.NoUnexpectedReceived();
        }
        static int Visible(Transform t){int n=0;foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)n++;return n;}
        static Bounds Bounds(Transform w){bool first=true;var b=new Bounds();foreach(var f in w.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<Renderer>().enabled)foreach(var v in f.sharedMesh.vertices){var p=w.parent.InverseTransformPoint(f.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}return b;}
    }
}
