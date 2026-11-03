using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class BoardGraphicsTests
    {
        [UnityTest]public IEnumerator BoardAndScooterKeepContactsAndWheelAnimation()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
            foreach(var ride in new[]{Ride.Skateboard,Ride.Scooter})
            {
                g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();var m=g.CurrentVehicle;var seat=m.Seat;var grip=m.Grips[0];var foot=m.Pedals[0];g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;int low=Visible(m.Root);
                foreach(bool high in new[]{true,false,true}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Assert.That(m.Root.GetComponent<GraphicsBoard>().ShowingHigh,Is.EqualTo(high));Assert.That(m.Seat,Is.EqualTo(seat));Assert.That(m.Grips[0],Is.EqualTo(grip));Assert.That(m.Pedals[0],Is.SameAs(foot));g.Explorer.FitRide(m,false);Assert.That(g.Explorer.FootError,Is.LessThan(.1f));Assert.That(g.Explorer.HandError,Is.LessThan(.1f));if(!high)Assert.That(Visible(m.Root),Is.EqualTo(low));else Assert.That(Visible(m.Root),Is.LessThan(20));}
                foreach(var wheel in m.Wheels){var initial=wheel.localRotation;m.Animate(3,.02f,0);Assert.That(Quaternion.Angle(initial,wheel.localRotation),Is.GreaterThan(1));}
                for(int n=0;n<150;n++)g.State.Step(.02f,new DriveInput{Forward=1});Assert.That(g.State.Speed,Is.GreaterThan(8));for(int n=0;n<100;n++)g.State.Step(.02f,new DriveInput{Brake=true});Assert.That(g.State.Speed,Is.EqualTo(0).Within(.01f));
            }
            LogAssert.NoUnexpectedReceived();
        }
        static int Visible(Transform t){int n=0;foreach(var r in t.GetComponentsInChildren<Renderer>())if(r.enabled&&r.gameObject.activeInHierarchy)n++;return n;}
    }
}
