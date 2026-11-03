using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleCity.Tests
{
    public sealed class SkateParkTests
    {
        static IEnumerator Ready()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            var game=Object.FindFirstObjectByType<CityGame>();float deadline=Time.realtimeSinceStartup+180;
            while(!game.Ready && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(game.Ready,Is.True,"Expanded city startup");
        }
        static void Capture(string name)
        {
            Directory.CreateDirectory("Documentation/Screenshots");var camera=Camera.main;var old=camera.targetTexture;
            foreach(var label in Object.FindObjectsByType<WorldLabel>(FindObjectsSortMode.None))label.RefreshVisibility(camera);
            var rt=new RenderTexture(1600,1200,24){antiAliasing=2};rt.Create();camera.targetTexture=rt;camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=rt;var pixels=new Texture2D(1600,1200,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1600,1200),0,0);pixels.Apply();File.WriteAllBytes("Documentation/Screenshots/"+name+".png",pixels.EncodeToPNG());
            RenderTexture.active=prior;camera.targetTexture=old;Object.Destroy(pixels);rt.Release();Object.Destroy(rt);
        }
        [UnityTest]
        public IEnumerator SkateparkRidesJumpLandRetryAndTravel()
        {
            yield return Ready();var game=Object.FindFirstObjectByType<CityGame>();game.Hud.BeginExploring();game.Worlds.VisitWorld(4);
            Assert.That(game.State.WorldIndex,Is.EqualTo(4));Assert.That(game.Worlds.ActivePlace.Root.name,Is.EqualTo("Skyline Skatepark"));
            game.enabled=false;game.HeldKey=_=>false;var state=game.State;var report=new List<string>();
            foreach(var ride in new[]{Ride.Skateboard,Ride.Scooter,Ride.MountainBike,Ride.MiniBike})
            {
                var parked=game.Parking.Rides.Find(r=>r.World==4 && r.Model.Mode==ride);Assert.That(parked,Is.Not.Null);
                if(state.Mode!=Ride.Walk)game.SelectRide(Ride.Walk);
                Vector3 p=parked.Model.Root.position;state.Place(new Point(p.x+.9f,0,p.z));game.RefreshPlayer();
                Assert.That(game.Parking.Nearest?.Model.Mode,Is.EqualTo(ride),"Reach parked "+ride);
                Assert.That(game.Parking.EnterOrExit(),Is.True,"Mount "+ride);Assert.That(state.Mode,Is.EqualTo(ride));
                Assert.That(game.CurrentVehicle.Wheels.Count,Is.EqualTo(ride==Ride.Skateboard?4:2));
                if(ride==Ride.MountainBike)Assert.That(game.CurrentVehicle.Suspension.Count,Is.EqualTo(3));
                if(ride==Ride.MiniBike)Assert.That(game.CurrentVehicle.Root.localScale.x,Is.LessThan(.6f));
                for(int lane=0;lane<3;lane++)
                {
                    const int stage=2;float x=SkatePark.LaneX(lane),start=SkatePark.StartZ(stage),lip=start+14;
                    game.HeldKey=key=>false;state.Place(new Point(x,0,start-24),0);game.RefreshPlayer();game.ProcessControls(.02f);game.HeldKey=key=>key==KeyCode.W;
                    bool jumped=false,airborne=false,landed=false;float maxHeight=0;var trace=new List<string>();int landings=state.ParkLandings;
                    for(int frame=0;frame<600;frame++)
                    {
                        bool jump=!jumped && state.Position.Z>lip-1.25f && state.Position.Z<lip;
                        if(jump)jumped=true;
                        game.HeldKey=key=>key==KeyCode.W || key==KeyCode.Space && jump;
                        game.ProcessControls(.02f);maxHeight=Mathf.Max(maxHeight,state.Position.Y);
                        if(frame%5==0)trace.Add(frame+","+state.Position.Z+","+state.Position.Y+","+state.GroundHeight(state.Position)+","+state.VerticalSpeed+","+state.ParkLandings);
                        if(state.Airborne){airborne=true;Assert.That(state.ExitVehicle(),Is.False,"No dismount in flight");}
                        if(state.ParkLandings>landings && state.Position.Z>=SkatePark.LandingZ(lane,stage) && state.Position.Z<=SkatePark.LandingZ(lane,stage)+SkatePark.LandingLength){landed=true;break;}
                    }
                    File.WriteAllLines("Documentation/Validation/trajectory-"+ride+"-"+lane+".csv",trace);
                    Assert.That(jumped && airborne,Is.True,ride+" takes off, lane "+lane);
                    Assert.That(landed,Is.True,ride+" lands the "+SkatePark.Gap(lane,stage)+" metre gap; final z="+state.Position.Z+" y="+state.Position.Y+" peak="+maxHeight+" landings="+(state.ParkLandings-landings));
                    Assert.That(maxHeight,Is.GreaterThan(SkatePark.Height(lane,stage)+2),"Air above the launch ramp");
                    report.Add(ride+" lane "+lane+": "+SkatePark.Gap(lane,stage)+" m gap landed; peak "+maxHeight.ToString("F1")+" m; impact "+state.LastLandingSpeed.ToString("F1"));
                }
                game.HeldKey=key=>key==KeyCode.LeftControl;for(int i=0;i<150;i++)game.ProcessControls(.02f);
                Assert.That(Mathf.Abs(state.Speed),Is.LessThan(.1f),"Ctrl brake");Assert.That(game.Parking.EnterOrExit(),Is.True,"Dismount after stopping");
            }
            game.SelectRide(Ride.Skateboard);state.ParkCheckpoint=new Point(0,0,-119);state.Place(new Point(0,-4,-81));game.ProcessControls(.02f);
            Assert.That(state.Position.Z,Is.EqualTo(-119).Within(.1f),"Failed jump retries at checkpoint");
            game.Worlds.VisitWorld(1);Assert.That(state.OutdoorGround,Is.Null);game.Worlds.VisitWorld(4);game.Worlds.VisitWorld(0);
            Assert.That(state.WorldIndex,Is.EqualTo(0));Assert.That(state.OutdoorGround,Is.Null,"Skate terrain must not leak into city/yacht travel");
            Directory.CreateDirectory("Documentation/Validation");File.WriteAllLines("Documentation/Validation/skate-jump-results.txt",report);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SkateparkTouchControlsAndPlayablePreviews()
        {
            yield return Ready();var game=Object.FindFirstObjectByType<CityGame>();game.Hud.BeginExploring();game.Worlds.VisitWorld(4);game.enabled=false;game.HeldKey=_=>false;
            game.SelectRide(Ride.Scooter);game.State.Place(new Point(0,0,-129),0);game.RefreshPlayer();game.Hud.SetTouchControls(true);yield return null;
            PointerEventData Press(HoldAction control,int id)
            {
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)control.transform;var data=new PointerEventData(EventSystem.current){pointerId=id,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject,Is.EqualTo(control.gameObject));control.OnPointerDown(data);return data;
            }
            var go=Press(game.Hud.Go,1);for(int i=0;i<60;i++)game.ProcessControls(.02f);
            var jump=Press(game.Hud.Jump,2);game.ProcessControls(.02f);Assert.That(game.State.Airborne,Is.True,"Touch JUMP launches scooter");
            game.Hud.Jump.OnPointerUp(jump);game.Hud.Go.OnPointerUp(go);game.Hud.ClearInput();
            for(int i=0;i<180;i++)game.ProcessControls(.02f);Assert.That(game.State.Grounded,Is.True);
            var stop=Press(game.Hud.Stop,3);for(int i=0;i<50;i++)game.ProcessControls(.02f);game.Hud.Stop.OnPointerExit(stop);Assert.That(game.Hud.Stop.Held,Is.False);
            game.Hud.SetTouchControls(false);game.State.Place(new Point(-60,0,-145));game.RefreshPlayer();
            Camera.main.transform.position=new Vector3(20,43,-175);Camera.main.transform.LookAt(new Vector3(0,4,-30));Capture("skatepark-overview");
            Camera.main.transform.position=new Vector3(-51,5,-148);Camera.main.transform.LookAt(new Vector3(-52,1,-138));Capture("skatepark-rides");
            game.SelectRide(Ride.MountainBike);game.State.Place(new Point(60,0,26),0);bool launched=false;
            for(int i=0;i<350;i++)
            {
                bool leap=!launched && game.State.Position.Z>62.8f;if(leap)launched=true;
                game.HeldKey=k=>k==KeyCode.W || k==KeyCode.Space && leap;game.ProcessControls(.02f);
                if(launched && game.State.Position.Y>12)
                {
                    game.RefreshPlayer();var p=game.CurrentVehicle.Root.position;Camera.main.transform.position=p+new Vector3(-6,2,-7);Camera.main.transform.LookAt(p+Vector3.up*.8f);Capture("mountain-bike-big-air");break;
                }
            }
            Assert.That(File.Exists("Documentation/Screenshots/mountain-bike-big-air.png"),Is.True);
            game.Worlds.VisitWorld(0);game.State.Place(new Point(-300,0,-300));game.RefreshPlayer();var city=Object.FindFirstObjectByType<ExpandedCity>();city.RefreshVisibility();
            Assert.That(city.DistrictCount,Is.GreaterThanOrEqualTo(40));Assert.That(city.ActiveDistrictCount,Is.LessThan(16));Assert.That(city.Aircraft.Count,Is.EqualTo(5));
            city.TickAircraft(0);var before=city.Aircraft[0].position;city.TickAircraft(45);Assert.That(Vector3.Distance(before,city.Aircraft[0].position),Is.GreaterThan(20));
            Camera.main.transform.position=new Vector3(-410,100,-410);Camera.main.transform.LookAt(new Vector3(-100,30,-100));Capture("greater-city-aircraft");
            game.HeldKey=_=>false;LogAssert.NoUnexpectedReceived();
        }
    }
}
