using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class BikeGraphicsTests
    {
        [UnityTest]public IEnumerator FiveDetailedBikesPreserveFitMotionAndLowMode()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
            g.Hud.BeginExploring();g.enabled=false;
            foreach(var ride in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike,Ride.Motorcycle,Ride.DirtBike})
            {
                g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;
                g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();var m=g.CurrentVehicle;var detail=m.Root.GetComponent<GraphicsBike>();
                Vector3 scale=m.Root.localScale,seat=m.Seat,grip=m.Grips[0];int wheelCount=m.Wheels.Count;float foot=g.Explorer.FootError;
                int lowRenderers=Visible(m.Root);Assert.That(lowRenderers,Is.GreaterThan(0));
                g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;
                Assert.That(detail.ShowingHigh,Is.True,ride.ToString());Assert.That(detail.DetailVertices,Is.InRange(10000,250000));
                Assert.That(m.Root.localScale,Is.EqualTo(scale));Assert.That(m.Seat,Is.EqualTo(seat));Assert.That(m.Grips[0],Is.EqualTo(grip));Assert.That(m.Wheels.Count,Is.EqualTo(wheelCount));
                g.Explorer.FitRide(m,false);Assert.That(g.Explorer.HandError,Is.LessThan(.1f));Assert.That(g.Explorer.FootError,Is.LessThan(.1f));
                foreach(var wheel in m.Wheels)
                {
                    var start=Bounds(wheel);Quaternion rotation=wheel.localRotation;
                    for(int n=0;n<24;n++){wheel.localRotation=rotation*Quaternion.Euler(n*15,0,0);var b=Bounds(wheel);Assert.That(b.size.x,Is.EqualTo(start.size.x).Within(.005f),"Straight axle during wheel rotation");Assert.That(b.size.y,Is.EqualTo(start.size.y).Within(.04f));Assert.That(b.size.z,Is.EqualTo(start.size.z).Within(.04f));Assert.That(Vector3.Distance(b.center,start.center),Is.LessThan(.025f));}
                    wheel.localRotation=rotation;
                }
                var before=m.Wheels[0].localRotation;m.Animate(7,.04f,1);Assert.That(Quaternion.Angle(before,m.Wheels[0].localRotation),Is.GreaterThan(5));
                foreach(var spring in m.Suspension){var shock=spring.Find("Detailed moving shock");Assert.That(shock,Is.Not.Null);m.SuspensionCompression=.5f;m.Animate(0,.02f,0);Assert.That(shock.lossyScale.y,Is.EqualTo(m.Root.lossyScale.y*.8f).Within(.001f),"High detail shock follows suspension compression");}m.SuspensionCompression=0;m.Animate(0,.02f,0);
                for(int i=0;i<100;i++)g.State.Step(.02f,new DriveInput{Forward=1});Assert.That(g.State.Speed,Is.GreaterThan(5));for(int i=0;i<100;i++)g.State.Step(.02f,new DriveInput{Brake=true});Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f));
                int highCount=Visible(m.Root);Assert.That(highCount,Is.LessThan(40),"Material batches keep draw calls bounded");
                for(int i=0;i<2;i++){g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(lowRenderers),"Original Low Def geometry restored");g.Graphics.SetHigh(true);while(g.Graphics.Busy)yield return null;Assert.That(Visible(m.Root),Is.EqualTo(highCount),"No duplicate geometry after repeated switch");}
            }
            LogAssert.NoUnexpectedReceived();
        }
        static int Visible(Transform root){int n=0;foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)n++;return n;}
        static Bounds Bounds(Transform wheel)
        {
            bool first=true;var b=new Bounds();foreach(var f in wheel.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<Renderer>().enabled)foreach(var v in f.sharedMesh.vertices){var p=wheel.parent.InverseTransformPoint(f.transform.TransformPoint(v));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}return b;
        }
    }
}
