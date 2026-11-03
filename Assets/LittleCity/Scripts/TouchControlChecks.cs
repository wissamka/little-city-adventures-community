using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LittleCity
{
    // Runs only from tests or the opt-in device validation request. Uses real HUD
    // raycasts and pointer handlers, but synthesizes fingers (not hardware input).
    public static class TouchControlChecks
    {
        static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Touch validation: " + message); }

        static Vector2 Position(Component control, Vector2 direction)
        {
            var rect = (RectTransform)control.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center + direction * Mathf.Min(rect.rect.width, rect.rect.height) * .34f));
        }

        static PointerEventData Press(Component control, int finger, Vector2 direction = default)
        {
            Canvas.ForceUpdateCanvases();
            var data = new PointerEventData(EventSystem.current) { pointerId = finger, position = Position(control, direction), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            Check(hits.Count > 0 && (hits[0].gameObject == control.gameObject || hits[0].gameObject.transform.IsChildOf(control.transform)), control.name + " is obscured by " + (hits.Count > 0 ? hits[0].gameObject.name : "no raycast target"));
            data.pointerPressRaycast = data.pointerCurrentRaycast = hits[0];
            ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerDownHandler);
            return data;
        }

        static void Drag(TouchStick stick, PointerEventData data, Vector2 direction)
        {
            Vector2 next = Position(stick, direction); data.delta = next - data.position; data.position = next;
            ExecuteEvents.Execute(stick.gameObject, data, ExecuteEvents.dragHandler);
        }
        static void Release(Component control, PointerEventData data)
        { ExecuteEvents.Execute(control.gameObject, data, ExecuteEvents.pointerUpHandler); }
        static void Click(Button button)
        {
            Check(button.interactable, button.name + " is disabled");
            var data=Press(button,404);Release(button,data);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static void Tick(CityGame game, int frames)
        { for (int i = 0; i < frames; i++) game.ProcessControls(.02f); }
        static float Distance(Point a, Point b) => Vector3.Distance(new Vector3(a.X,a.Y,a.Z),new Vector3(b.X,b.Y,b.Z));

        public static IEnumerator Run(CityGame game, List<string> report)
        {
            var hud = game.Hud; var state = game.State;
            var obstacles = new List<Obstacle>(state.Obstacles); bool enabled = game.enabled;
            int parkedCount=game.Parking.Rides.Count;
            bool touch = hud.TouchControlsVisible; var keyboard = game.HeldKey;
            var physical=PointerPresence.IsHeld;PointerPresence.IsHeld=_=>true;
            game.enabled = false; game.HeldKey = _ => false;
            void Prepare(Ride ride, int style = 0)
            {
                hud.ClearInput(); hud.ShowControls(false); game.Parking.ReleaseForChange(); state.ArriveOnFoot(0);
                bool boat = ride == Ride.Motorboat || ride == Ride.Speedboat || ride==Ride.Yacht;
                state.HarborWater = boat; state.PondEnabled = false;
                Point spawn = ride==Ride.Yacht?new Point(-80,-1.05f,300):boat ? new Point(-80,0,100) : ride == Ride.Airplane ? Simulation.RunwayStart : new Point(0,0,-60);
                if (ride == Ride.Walk) state.Place(spawn); else Check(state.EnterVehicle(ride,style,spawn,0), "enter " + ride);
                game.RefreshPlayer(); hud.BeginExploring(); hud.SetTouchControls(true); state.Obstacles.Clear(); hud.Refresh();
            }
            void ClearTestVehicles()
            {
                game.Parking.ReleaseForChange();
                for(int i=game.Parking.Rides.Count-1;i>=parkedCount;i--)
                {
                    var ride=game.Parking.Rides[i];state.Obstacles.Remove(ride.Collision);
                    UnityEngine.Object.Destroy(ride.Model.Root.gameObject);game.Parking.Rides.RemoveAt(i);
                }
            }
            void LiftHelicopter()
            {
                if(state.Mode!=Ride.Helicopter)return;
                var up=Press(hud.Rise,505);Tick(game,150);Release(hud.Rise,up);Tick(game,30);
                Check(state.Airborne&&state.Position.Y>10,"helicopter UP takes off vertically before flight steering");
            }
            try
            {
                Prepare(Ride.Walk); yield return null;
                var finger = Press(hud.Stick, 101);
                Check(hud.Stick.Value.sqrMagnitude < .001f, "joystick center must be neutral");
                foreach (var direction in new[] {Vector2.left,Vector2.right,Vector2.up,Vector2.down})
                {
                    Drag(hud.Stick, finger, direction);
                    Check(Vector2.Dot(hud.Stick.Value, direction) > .9f, "joystick direction " + direction);
                }
                hud.Stick.OnPointerUp(new PointerEventData(EventSystem.current) {pointerId=999});
                Check(hud.Stick.Value.y < -.9f, "another finger cannot release the joystick");
                Release(hud.Stick, finger); Check(hud.Stick.Value == Vector2.zero, "joystick release");
                report.Add("Joystick: neutral center, four directions, ownership and release passed");

                foreach (Ride ride in Enum.GetValues(typeof(Ride)))
                for (int style = 0; style < (ride == Ride.RaceCar ? 4 : 1); style++)
                {
                    foreach (int turn in new[] {-1,1})
                    {
                        Prepare(ride, style); yield return null;
                        LiftHelicopter();Point before = state.Position; float camera = game.CameraYaw;
                        var stick = Press(hud.Stick, 101, new Vector2(turn,0));
                        var go = Press(hud.Go, 202);
                        Tick(game, 60);
                        Check(state.Speed > 1, ride + " GO must accelerate");
                        Check(Mathf.DeltaAngle(0,state.Yaw*Mathf.Rad2Deg)*turn > 8, ride + " turn " + turn);
                        Check(Distance(before,state.Position) > .25f && Distance(before,state.Position) < 15, ride + " continuous bounded movement");
                        if (ride == Ride.Walk) Check(Mathf.Abs(Mathf.DeltaAngle(camera,game.CameraYaw)) < .01f, "walking must not rotate the camera automatically");
                        Check(hud.Look.Consume() == Vector2.zero, "joystick and GO must not drag the camera");
                        Check(!hud.MenuOpen && !state.Indoors && state.WorldIndex == 0, "steering must not open menus or travel");
                        Release(hud.Go,go); Release(hud.Stick,stick);
                        Check(!hud.Go.Held && hud.Stick.Value == Vector2.zero,"simultaneous touches release independently");
                        if (ride != Ride.Walk)
                        {
                            float speed = state.Speed; var stop = Press(hud.Stop,303); Tick(game,100); Release(hud.Stop,stop);
                            Check(state.Speed < speed && Mathf.Abs(state.Speed)<.2f,ride+" STOP brakes to rest");
                            if(ride==Ride.Helicopter){Check(!state.ExitVehicle(),"helicopter cannot exit while hovering");var heliDown=Press(hud.Descend,505);Tick(game,250);Release(hud.Descend,heliDown);Check(!state.Airborne,"helicopter DOWN lands before exit");}
                            Check(state.ExitVehicle(),ride+" permits exit after stopping");
                        }
                    }
                    if (ride != Ride.Walk && ride != Ride.Airplane)
                    {
                        Prepare(ride,style); yield return null;LiftHelicopter();var reverse = Press(hud.Stick,101,Vector2.down); Tick(game,60);
                        Check(state.Speed < -.5f,ride+" reverse"); Release(hud.Stick,reverse);
                    }
                    if(ride!=Ride.Walk && ride!=Ride.Yacht)
                    {
                        Prepare(ride,style);hud.SetVehicleContext(game.Parking.Context,true);yield return null;
                        Click(hud.VehicleButton);Check(state.Mode==Ride.Walk,ride+" touch exit button");
                        Check(game.Parking.Nearest!=null,ride+" remains nearby for re-entry");
                        hud.SetVehicleContext(game.Parking.Context,true);yield return null;
                        Click(hud.VehicleButton);Check(state.Mode==ride && state.CarStyle==style,ride+" touch entry button");
                        ClearTestVehicles();
                    }
                    report.Add(ride + (ride==Ride.RaceCar?" style "+style:"") + ": left/right + GO, touch release, bounded motion, brake/exit and applicable reverse passed");
                }

                Prepare(Ride.Walk); yield return null;
                var jump = Press(hud.Jump,202); Tick(game,5); Release(hud.Jump,jump);
                Check(state.Position.Y > .2f,"walking JUMP");
                Prepare(Ride.Walk); yield return null; var walking = Press(hud.Stick,101,Vector2.up); Tick(game,40);
                Point start = state.Position; game.ProcessControls(1f);
                Check(Distance(start,state.Position) < (state.IsRider?.58f:.45f),"a one-second rendering hitch must stay within 0.1 seconds of the selected character's walking speed");
                hud.ShowExplore(true); Check(hud.Stick.Value == Vector2.zero,"opening menu cancels movement");
                hud.BeginExploring(); hud.ShowControls(true); hud.Refresh(); yield return null;
                float yaw = game.CameraYaw; var turnCamera = Press(hud.LookLeft,202); Tick(game,20); Release(hud.LookLeft,turnCamera);
                Check(Mathf.DeltaAngle(yaw,game.CameraYaw)<-10,"camera Left hold");
                yaw = game.CameraYaw; turnCamera = Press(hud.LookRight,202); Tick(game,20); Release(hud.LookRight,turnCamera);
                Check(Mathf.DeltaAngle(yaw,game.CameraYaw)>10,"camera Right hold");
                float pitch=game.CameraPitch;var up=Press(hud.LookUp,202);Tick(game,10);Release(hud.LookUp,up);
                Check(game.CameraPitch>pitch,"camera Up hold");
                var down=Press(hud.LookDown,202);Tick(game,10);Release(hud.LookDown,down);
                Check(Mathf.Abs(game.CameraPitch-pitch)<.1f,"camera Down hold");
                float zoom=game.ZoomFactor;var zin=Press(hud.ZoomIn,202);Tick(game,10);Release(hud.ZoomIn,zin);
                Check(game.ZoomFactor<zoom,"zoom in");var zout=Press(hud.ZoomOut,202);Tick(game,10);Release(hud.ZoomOut,zout);
                Check(Mathf.Abs(game.ZoomFactor-zoom)<.01f,"zoom out");
                hud.ShowControls(false);hud.Refresh(); yield return null;
                walking=Press(hud.Stick,101,Vector2.up);
                var look=Press(hud.Look,202);look.delta=new Vector2(100,0);ExecuteEvents.Execute(hud.Look.gameObject,look,ExecuteEvents.dragHandler);
                yaw=game.CameraYaw;Tick(game,1);
                Check(Mathf.DeltaAngle(yaw,game.CameraYaw)>10 && hud.Stick.Value.y>.9f,"two-finger walking and camera drag");
                Release(hud.Look,look);Release(hud.Stick,walking);
                var held=Press(hud.Go,202);hud.Go.OnPointerExit(held);Check(!hud.Go.Held,"leaving GO cancels the hold");
                walking=Press(hud.Stick,101,Vector2.up);hud.SetTouchControls(false);
                Check(hud.Stick.Value==Vector2.zero && !hud.Go.Held,"hiding touch controls cancels input");
                report.Add("Walking jump, slow-frame bound, stable camera, two-finger look/move, camera buttons/zoom, menu/hide/exit cancellation passed");

                Prepare(Ride.Airplane);yield return null;var power=Press(hud.Go,202);Tick(game,140);
                var rise=Press(hud.Rise,303);Tick(game,70);Release(hud.Rise,rise);
                Check(state.Airborne && state.Position.Y>3,"plane UP takeoff");
                float altitude=state.Position.Y;var descend=Press(hud.Descend,303);Tick(game,15);Release(hud.Descend,descend);
                Check(state.Position.Y<altitude,"plane DOWN descent");Release(hud.Go,power);
                Check(!state.ExitVehicle(),"airborne exit is blocked");
                Prepare(Ride.RaceCar);yield return null;power=Press(hud.Go,202);Tick(game,50);var drift=Press(hud.Drift,303);Tick(game,10);
                Check(state.Drifting,"car DRIFT");Release(hud.Drift,drift);Release(hud.Go,power);
                report.Add("Plane takeoff/descent and airborne exit guard; car drift passed");
                foreach(var trick in new[]{BikeTrick.Wheelie,BikeTrick.Endo,BikeTrick.Spin180})
                {
                    Prepare(Ride.Bicycle);hud.ShowControls(true);hud.Refresh();yield return null;
                    power=Press(hud.Go,202);Tick(game,100);Release(hud.Go,power);
                    string name=trick==BikeTrick.Wheelie?"Z Wheelie button":trick==BikeTrick.Endo?"X Endo button":"V 180 button";
                    Button button=null;foreach(var b in hud.GetComponentsInChildren<Button>())if(b.name==name)button=b;
                    Check(button!=null,"bicycle trick control "+name);if(trick==BikeTrick.Wheelie)
                    {
                        power=Press(hud.Go,202);var wheelie=Press(hud.Wheelie,303);Tick(game,200);
                        Check(state.Trick==BikeTrick.Wheelie && Mathf.Abs(state.TrickProgress-.5f)<.01f,"wheelie remains raised during four-second touch hold");
                        Release(hud.Wheelie,wheelie);Tick(game,20);
                        Check(state.Trick==BikeTrick.None,"wheelie lowers on touch release");Release(hud.Go,power);
                    }
                    else { Click(button);Check(state.Trick==trick,"bicycle "+trick); }
                }
                foreach(var button in hud.GetComponentsInChildren<Button>())if(button.name=="B Stand button")Click(button);
                Check(state.Standing,"bicycle standing control");
                report.Add("Touch entry/exit for all six vehicle types and all car styles; bicycle wheelie/endo/180/standing controls passed");
            }
            finally
            {
                hud.ClearInput();ClearTestVehicles();state.ArriveOnFoot(0);state.Obstacles.Clear();state.Obstacles.AddRange(obstacles);
                game.HeldKey=keyboard;game.ResetHome();hud.SetTouchControls(touch);game.enabled=enabled;
                PointerPresence.IsHeld=physical;
            }

        }
    }
}
