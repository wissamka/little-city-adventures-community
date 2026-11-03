using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace LittleCity.Tests
{
    public sealed class InputReleaseTests
    {
        [UnityTest]
        public IEnumerator DirectionReleaseCancellationFocusMenusAndTravel()
        {
            var original=PointerPresence.IsHeld;var fingers=new HashSet<int>();PointerPresence.IsHeld=id=>fingers.Contains(id);
            try
            {
                yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready)yield return null;g.enabled=false;g.Hud.BeginExploring();g.Hud.SetTouchControls(true);
                var keys=new HashSet<KeyCode>();g.HeldKey=k=>keys.Contains(k);
                void Tick(int n=1){for(int i=0;i<n;i++)g.ProcessControls(.02f);}
                void Reset(){keys.Clear();fingers.Clear();g.Hud.ClearInput();g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();Tick();}
                foreach(var key in new[]{KeyCode.A,KeyCode.D,KeyCode.W,KeyCode.S,KeyCode.LeftArrow,KeyCode.RightArrow,KeyCode.UpArrow,KeyCode.DownArrow})
                {
                    Reset();keys.Add(key);Tick(25);Assert.That(g.State.Speed,Is.GreaterThan(3));keys.Clear();var pos=g.State.Position;Tick();Assert.That(Distance(pos,g.State.Position),Is.LessThan(.0001f),key+" release");Assert.That(g.State.Speed,Is.EqualTo(0));
                }
                Reset();keys.Add(KeyCode.A);keys.Add(KeyCode.D);Tick(10);Assert.That(g.State.Speed,Is.EqualTo(0));keys.Remove(KeyCode.A);Tick(20);Assert.That(g.State.Position.X,Is.GreaterThan(-32));keys.Clear();Tick();keys.Add(KeyCode.A);float x=g.State.Position.X;Tick(20);Assert.That(g.State.Position.X,Is.LessThan(x));keys.Clear();Tick();
                foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
                {
                    Reset();var e=Press(g.Hud.Stick,direction,101);fingers.Add(101);Tick(25);Assert.That(g.State.Speed,Is.GreaterThan(3));
                    g.Hud.Stick.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=202});Tick();Assert.That(g.Hud.Stick.Value.magnitude,Is.GreaterThan(.9f),"Another finger cannot release ownership");
                    // Reproduce a lost Up: hardware says released, UI never received PointerUp.
                    fingers.Remove(101);var p=g.State.Position;Tick();Assert.That(g.Hud.Stick.Value,Is.EqualTo(Vector2.zero));Assert.That(Distance(p,g.State.Position),Is.LessThan(.0001f),"Lost Up cannot latch motion");Assert.That(g.State.Speed,Is.EqualTo(0));
                    fingers.Add(101);g.Hud.Stick.OnPointerDown(e);g.Hud.Stick.OnCancel(new BaseEventData(EventSystem.current));Tick();Assert.That(g.Hud.Stick.Value,Is.EqualTo(Vector2.zero));
                    g.Hud.Stick.OnPointerDown(e);g.Hud.Stick.OnEndDrag(e);Tick();Assert.That(g.Hud.Stick.Value,Is.EqualTo(Vector2.zero));
                }
                Reset();var finger=Press(g.Hud.Stick,Vector2.right,101);fingers.Add(101);keys.Add(KeyCode.W);Tick(20);g.Hud.Stick.OnPointerUp(finger);fingers.Clear();Tick(20);Assert.That(g.State.Speed,Is.GreaterThan(3),"Remaining real keyboard input still works");keys.Clear();Tick();Assert.That(g.State.Speed,Is.EqualTo(0));
                foreach(var hold in new[]{g.Hud.Go,g.Hud.LookLeft,g.Hud.LookRight,g.Hud.Rise,g.Hud.Descend})
                {
                    Reset();fingers.Add(303);var e=new PointerEventData(EventSystem.current){pointerId=303};hold.OnPointerDown(e);Tick(10);Assert.That(hold.Held,Is.True);fingers.Clear();Tick();Assert.That(hold.Held,Is.False,"Lost release cleared for "+hold.name);
                    fingers.Add(303);hold.OnPointerDown(e);hold.OnPointerExit(e);Assert.That(hold.Held,Is.False,"Release outside hold button");
                }
                Reset();keys.Add(KeyCode.D);Tick(15);g.SendMessage("OnApplicationFocus",false);var paused=g.State.Position;Tick(10);g.SendMessage("OnApplicationFocus",true);Tick(10);Assert.That(Distance(paused,g.State.Position),Is.LessThan(.0001f),"Held key cannot resume after focus loss");keys.Clear();Tick();keys.Add(KeyCode.D);Tick(15);Assert.That(Distance(paused,g.State.Position),Is.GreaterThan(.5f));
                g.Hud.ShowExplore(true);g.Hud.BeginExploring();paused=g.State.Position;Tick(15);Assert.That(Distance(paused,g.State.Position),Is.LessThan(.0001f),"Menu closes without replaying held key");keys.Clear();Tick();
                for(int n=0;n<3;n++){g.Worlds.VisitWorld(n%2);while(g.Worlds.Busy)yield return null;Tick();Assert.That(g.Hud.Stick.Value,Is.EqualTo(Vector2.zero));Assert.That(g.State.Speed,Is.EqualTo(0));}
                g.Worlds.VisitWorld(0);while(g.Worlds.Busy)yield return null;g.SelectRide(Ride.RaceCar);g.State.Place(new Point(-32,0,-32));g.RefreshPlayer();keys.Add(KeyCode.W);keys.Add(KeyCode.D);Tick(30);keys.Remove(KeyCode.D);float yaw=g.State.Yaw;Tick(20);Assert.That(g.State.Yaw,Is.EqualTo(yaw).Within(.001f),"Car steering command stops while propulsion remains held");keys.Clear();Tick();Assert.That(g.State.Speed,Is.GreaterThan(0),"Intentional vehicle coasting preserved");
                LogAssert.NoUnexpectedReceived();
            }
            finally{PointerPresence.IsHeld=original;}
        }
        static PointerEventData Press(TouchStick stick,Vector2 direction,int id)
        {
            var r=(RectTransform)stick.transform;var position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center+direction*Mathf.Min(r.rect.width,r.rect.height)*.36f));var e=new PointerEventData(EventSystem.current){pointerId=id,position=position};stick.OnPointerDown(e);return e;
        }
        static float Distance(Point a,Point b)=>new Vector3(a.X-b.X,a.Y-b.Y,a.Z-b.Z).magnitude;
    }
}
