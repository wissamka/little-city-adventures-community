using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class SkatePlusTests
    {
        [UnityTest]
        public IEnumerator CurvedHalfPipeMegaJumpFasterBikesAndObstacles()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;
            g.Hud.BeginExploring();g.Worlds.VisitWorld(4);g.enabled=false;var s=g.State;var report=new List<string>();
            foreach(Ride mode in new[]{Ride.Skateboard,Ride.Scooter,Ride.Bicycle,Ride.MountainBike,Ride.MiniBike})
            {
                g.SelectRide(mode);s.Place(new Point(-150,SkatePark.PipeFloor(-150,40),40),Mathf.PI/2);
                float low=100,high=0;
                for(int pass=0;pass<4;pass++)
                {
                    bool right=pass%2==0;bool reached=false;
                    for(int i=0;i<2500;i++)
                    {
                        s.Step(.02f,new DriveInput{Forward=right?.4f:-1});low=Mathf.Min(low,s.Position.Y);high=Mathf.Max(high,s.Position.Y);
                        Assert.That(Mathf.Abs(s.Position.Z-40),Is.LessThan(.1f));Assert.That(s.Position.Y,Is.GreaterThanOrEqualTo(0));
                        if(right?s.Position.X>=-117:s.Position.X<=-149){reached=true;break;}
                    }
                    Assert.That(reached,Is.True,mode+" crosses curved halfpipe pass "+pass);
                    for(int i=0;i<70;i++)s.Step(.02f,new DriveInput{Brake=true});
                    Assert.That(s.Airborne,Is.False,"Stable stop on curved surface");
                }
                Assert.That(low,Is.LessThan(.1f));Assert.That(high,Is.GreaterThan(3.5f));report.Add(mode+" four halfpipe traversals: low="+low+" high="+high);
                s.Place(new Point(SkatePark.MegaX,0,SkatePark.MegaStart-24));int prior=s.ParkLandings;bool jumped=false,landed=false;float peak=0;
                for(int i=0;i<800;i++)
                {
                    bool jump=!jumped && s.Position.Z>SkatePark.MegaLip-1.2f && s.Position.Z<SkatePark.MegaLip;if(jump)jumped=true;
                    s.Step(.02f,new DriveInput{Forward=1,Jump=jump});peak=Mathf.Max(peak,s.Position.Y);
                    if(s.ParkLandings>prior && s.Position.Z>=SkatePark.MegaLanding && s.Position.Z<=SkatePark.MegaLanding+42){landed=true;break;}
                }
                Assert.That(jumped && landed,Is.True,mode+" lands new 28 m mega gap, z="+s.Position.Z+" peak="+peak);
                report.Add(mode+" landed 28 m gap; peak="+peak.ToString("F1"));
                if(s.IsBicycle)
                {
                    s.Place(new Point(0,0,-175),Mathf.PI/2);for(int i=0;i<150;i++)s.Step(.02f,new DriveInput{Forward=1});float accelerated=s.Speed;
                    for(int i=0;i<100;i++)s.Step(.02f,new DriveInput{Forward=1});float top=s.Speed;
                    Assert.That(top,Is.EqualTo(mode==Ride.MountainBike?24:mode==Ride.MiniBike?20:16).Within(.01f));
                    float before=s.Yaw;for(int i=0;i<10;i++)s.Step(.02f,new DriveInput{Steering=.5f,Forward=1});Assert.That(Mathf.Abs(s.Yaw-before),Is.GreaterThan(.05f));
                    for(int i=0;i<100;i++)s.Step(.02f,new DriveInput{Brake=true});Assert.That(s.Speed,Is.EqualTo(0).Within(.01f));report.Add(mode+" after 3 s="+accelerated+" m/s, top="+top+" m/s, steering/brake pass");
                }
            }
            g.SelectRide(Ride.Scooter);s.Place(new Point(163,0,-50));bool collision=false;
            for(int i=0;i<180;i++){s.Step(.02f,new DriveInput{Forward=1});if(s.ImpactVersion>0)collision=true;}
            Assert.That(collision,Is.True,"Hurdle stops wheels when not jumped");
            s.Place(new Point(175,0,-50));for(int i=0;i<200;i++)s.Step(.02f,new DriveInput{Forward=1});Assert.That(s.Position.Z,Is.GreaterThan(-10),"Open hurdle bypass");
            s.Place(new Point(-133,0,-143));g.Skate.Tick();s.Place(new Point(-133,-4,-90));s.Step(.02f,new DriveInput());Assert.That(s.Position.Z,Is.EqualTo(-151).Within(.1f),"Mega retry checkpoint");
            g.SelectRide(Ride.MountainBike);s.Place(new Point(-133,0,35),Mathf.PI/2);g.RefreshPlayer();Capture("halfpipe-riding");
            s.Place(new Point(-133,12,-102));g.RefreshPlayer();Capture("mega-jump-launch");
            s.Place(new Point(133,0,-65));g.RefreshPlayer();Capture("technical-obstacle-lines");
            Directory.CreateDirectory("Documentation/Validation");File.WriteAllLines("Documentation/Validation/skate-plus-results.txt",report);LogAssert.NoUnexpectedReceived();
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory("Documentation/Screenshots");var c=Camera.main;var old=c.targetTexture;var rt=new RenderTexture(1600,1200,24){antiAliasing=2};rt.Create();c.targetTexture=rt;c.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(1600,1200,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1600,1200),0,0);t.Apply();File.WriteAllBytes("Documentation/Screenshots/"+name+".png",t.EncodeToPNG());RenderTexture.active=prev;c.targetTexture=old;Object.Destroy(t);rt.Release();Object.Destroy(rt);
        }
    }
}
