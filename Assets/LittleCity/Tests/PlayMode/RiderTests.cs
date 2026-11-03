using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace LittleCity.Tests
{
    public sealed class RiderTests
    {
        [UnityTest]
        public IEnumerator RiderSelectionPersistenceSpeedStairsAndRideFit()
        {
            int saved=PlayerPrefs.GetInt("LittleCity.Character",0),graphics=PlayerPrefs.GetInt("LittleCity.GraphicsHigh",0);
            try
            {
                yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready || g.Graphics.Busy)yield return null;
                g.Graphics.SetHigh(false);while(g.Graphics.Busy)yield return null;
                bool clicked=false;
                foreach(var label in g.Hud.GetComponentsInChildren<TMPro.TMP_Text>(true))if(label.text=="Rider")
                {label.GetComponentInParent<Button>().onClick.Invoke();clicked=true;break;}
                Assert.That(clicked,Is.True,"Rider is a clickable roster choice");Assert.That(g.State.Character,Is.EqualTo(3));Assert.That(PlayerPrefs.GetInt("LittleCity.Character"),Is.EqualTo(3));
                Assert.That(g.Explorer.Root.localScale.y,Is.GreaterThan(.8f));
                Assert.That(g.Explorer.Head.GetComponentsInChildren<MeshFilter>(), Is.Not.Empty);
                Assert.That(g.Explorer.Ends[0].Find("Basketball high top collar"),Is.Not.Null);
                var bounds=new Bounds(g.Explorer.Root.position,Vector3.zero);foreach(var r in g.Explorer.Root.GetComponentsInChildren<Renderer>())if(r.enabled)bounds.Encapsulate(r.bounds);
                Assert.That(bounds.size.y,Is.InRange(4.0f,5.7f),"Towering Rider outdoors");
                g.Hud.BeginExploring();g.enabled=false;
                float[] speed=new float[2];
                for(int c=0;c<2;c++){var s=new Simulation();s.SelectCharacter(c==0?0:3);s.Place(new Point(-300,0,-300));for(int n=0;n<150;n++)s.Step(.02f,new DriveInput{Forward=1,Sprint=true});speed[c]=s.Speed;for(int n=0;n<40;n++)s.Step(.02f,new DriveInput());Assert.That(s.Speed,Is.EqualTo(0).Within(.001f));}
                Assert.That(speed[1],Is.EqualTo(10).Within(.01f));Assert.That(speed[1],Is.GreaterThan(speed[0]*1.35f));
                g.State.Place(new Point(13,0,-42.2f));g.Interact();yield return null;Assert.That(g.Buildings.Inside,Is.True,"Existing actual doorway interaction");
                var p=g.Buildings.Active.Plan;float corridor=p.StairX-2.4f;
                Walk(g,p,new Point(corridor,0,p.StairEnd+.65f));Walk(g,p,new Point(corridor,0,p.StairStart-.65f));Walk(g,p,new Point(p.StairX,0,p.StairStart-.65f));Walk(g,p,new Point(p.StairX,p.Story,p.StairEnd+.65f));Assert.That(g.State.Position.Y,Is.EqualTo(p.Story).Within(.08f));
                Walk(g,p,new Point(p.StairX,0,p.StairStart-.65f));Assert.That(g.State.Position.Y,Is.EqualTo(0).Within(.08f));g.Buildings.Leave();
                foreach(bool high in new[]{false,true,false})
                {
                    g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                    foreach(var ride in new[]{Ride.Bicycle,Ride.MountainBike,Ride.MiniBike,Ride.Motorcycle,Ride.RaceCar,Ride.Airplane,Ride.Walk})
                    {
                        g.State.Place(new Point(-300,0,-300));g.SelectRide(ride);yield return null;Assert.That(g.State.Mode,Is.EqualTo(ride));Assert.That(g.State.Character,Is.EqualTo(3));
                        if(ride==Ride.Bicycle){for(int n=0;n<24;n++){g.CurrentVehicle.Animate(8,.04f,0);g.Explorer.FitRide(g.CurrentVehicle,false);Assert.That(g.Explorer.HandError,Is.LessThan(.03f));Assert.That(g.Explorer.FootError,Is.LessThan(.03f));}}
                        foreach(var renderer in g.Explorer.Root.GetComponentsInChildren<Renderer>())if(renderer.enabled)Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
                    }
                }
                yield return SceneManager.LoadSceneAsync("LittleCity");g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;Assert.That(g.State.Character,Is.EqualTo(3),"Selection survives scene reload");LogAssert.NoUnexpectedReceived();
            }
            finally{PlayerPrefs.SetInt("LittleCity.Character",saved);PlayerPrefs.SetInt("LittleCity.GraphicsHigh",graphics);PlayerPrefs.Save();}
        }
        static void Walk(CityGame g,BuildingPlan p,Point local)
        {
            var target=p.World(local);for(int n=0;n<1800;n++){float x=target.X-g.State.Position.X,z=target.Z-g.State.Position.Z;if(x*x+z*z<.035f)return;g.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=Mathf.Atan2(x,z)});}Assert.Fail("Rider could not reach stair landing");
        }
    }
}
