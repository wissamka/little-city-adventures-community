using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace LittleCity.Tests
{
    public sealed class PlayableSceneTests
    {
        [UnityTest]
        public IEnumerator BicycleWheelieHoldReleaseAndCancellation()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            var game=Object.FindFirstObjectByType<CityGame>();float timeout=Time.realtimeSinceStartup+90;
            while(!game.Ready && Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(game.Ready,Is.True);game.enabled=false;
            var keys=new System.Collections.Generic.HashSet<KeyCode>();var original=game.HeldKey;
            var state=game.State;var hud=game.Hud;
            void Tick(int frames){for(int i=0;i<frames;i++)game.ProcessControls(.02f);}
            try
            {
                game.HeldKey=key=>keys.Contains(key);hud.BeginExploring();game.SelectRide(Ride.Bicycle);
                state.Obstacles.Clear();state.PondEnabled=false;keys.Add(KeyCode.W);Tick(80);
                keys.Add(KeyCode.Z);keys.Add(KeyCode.D);float yaw=state.Yaw;Tick(400);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.Wheelie),"Eight-second Z hold must exceed the old cutoff");
                Assert.That(state.TrickProgress,Is.EqualTo(.5f).Within(.01f));
                Assert.That(Mathf.Abs(state.Yaw-yaw),Is.GreaterThan(.3f),"Steering works during the wheelie");
                keys.Remove(KeyCode.Z);Tick(5);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.Wheelie),"Release lowers progressively, without snapping");
                Assert.That(state.TrickProgress,Is.GreaterThan(.5f));Tick(20);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None));
                keys.Add(KeyCode.Z);Tick(30);game.SendMessage("OnApplicationFocus",false);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Losing focus resets wheelie");
                game.SendMessage("OnApplicationFocus",true);Tick(30);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Previously held key cannot restart after focus returns");
                keys.Remove(KeyCode.W);Tick(1);keys.Add(KeyCode.W);Tick(40);
                keys.Remove(KeyCode.Z);Tick(1);keys.Add(KeyCode.Z);Tick(30);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.Wheelie));keys.Remove(KeyCode.Z);Tick(20);
                hud.SetTouchControls(true);hud.ShowControls(true);hud.Refresh();yield return null;
                keys.Remove(KeyCode.W);Tick(1);keys.Add(KeyCode.W);Tick(80);
                var finger=new PointerEventData(EventSystem.current){pointerId=303};
                hud.Wheelie.OnPointerDown(finger);Tick(250);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.Wheelie),"Five-second touch hold");
                hud.Wheelie.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=404});
                Assert.That(hud.Wheelie.Held,Is.True,"Other finger cannot release wheelie");
                hud.Wheelie.OnPointerExit(finger);Tick(20);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Pointer exit lowers wheelie");
                hud.Wheelie.OnPointerDown(finger);Tick(30);hud.Wheelie.OnCancel(new BaseEventData(EventSystem.current));Tick(20);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Touch cancel lowers wheelie");
                hud.Wheelie.OnPointerDown(finger);Tick(30);hud.ShowControls(false);Tick(20);
                Assert.That(hud.Wheelie.Held,Is.False,"Disabling control releases pointer");
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None));
                keys.Add(KeyCode.Z);Tick(30);game.SelectRide(Ride.Walk);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Leaving bicycle resets wheelie");
                Assert.That(hud.Wheelie.Held,Is.False);
                game.SelectRide(Ride.Bicycle);Tick(80);
                Assert.That(state.Trick,Is.EqualTo(BikeTrick.None),"Held key stays cancelled after changing vehicles");
                keys.Remove(KeyCode.Z);Tick(20);
                keys.Remove(KeyCode.W);Tick(1);keys.Add(KeyCode.W);Tick(80);
                foreach(var trick in new[]{BikeTrick.Endo,BikeTrick.Spin180})
                { Assert.That(state.StartTrick(trick),Is.True);Tick(90);Assert.That(state.Trick,Is.EqualTo(BikeTrick.None)); }
            }
            finally { keys.Clear();game.HeldKey=original;hud.ClearInput();game.SendMessage("OnApplicationFocus",true);game.enabled=true; }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator IllustratedGraphicsProduceGameplayPreviews()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            var game=Object.FindFirstObjectByType<CityGame>();float deadline=Time.realtimeSinceStartup+120;
            while(!game.Ready && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(game.Ready,Is.True);Assert.That(game.sunshineShader.isSupported,Is.True);
            Capture(game,"illustrated-character-selection",1600,1200);
            game.Hud.BeginExploring();game.Hud.SetTouchControls(false);game.enabled=false;
            game.State.Place(new Point(0,0,-19));game.RefreshPlayer();
            Camera.main.transform.position=new Vector3(2.7f,2.5f,-24.5f);Camera.main.transform.LookAt(new Vector3(0,1.7f,-12));
            Capture(game,"illustrated-park-walk",1600,1200);
            game.State.Place(new Point(0,0,-12));game.RefreshPlayer();
            Camera.main.transform.position=new Vector3(-2.4f,2.15f,-7.5f);Camera.main.transform.LookAt(new Vector3(0,1.2f,-12));
            Capture(game,"illustrated-explorer",1600,1200);
            Camera.main.transform.position=new Vector3(23,4.3f,-35);Camera.main.transform.LookAt(new Vector3(13,4.1f,-49));
            Capture(game,"illustrated-street",1600,1200);
            Camera.main.transform.position=new Vector3(5,2.4f,-4);Camera.main.transform.LookAt(new Vector3(-10,2.2f,13));
            Capture(game,"illustrated-orchard",1600,1200);
            long triangles=0;int renderers=0;var planes=GeometryUtility.CalculateFrustumPlanes(Camera.main);
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(planes,renderer.bounds))continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null || filter.sharedMesh==null)continue;
                triangles+=filter.sharedMesh.triangles.Length/3;renderers++;
            }
            Directory.CreateDirectory("Documentation/Validation");
            File.WriteAllText("Documentation/Validation/illustrated-render-budget.txt","Visible mesh renderers: "+renderers+"\nVisible triangles (before outline/shadow passes): "+triangles+"\nCached scenery vertices: "+IllustratedScenery.CachedVertices+"\nMetal shader supported: "+game.sunshineShader.isSupported+"\nThese are editor camera geometry counts, not A10 frame-rate measurements.\n");
            Assert.That(triangles,Is.LessThan(1400000),"Keep the park view bounded for mobile");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TouchSteeringWorksAcrossEveryRideAndCarStyle()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            var game = Object.FindFirstObjectByType<CityGame>();
            float deadline = Time.realtimeSinceStartup + 90;
            while (!game.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.Ready, Is.True);
            var controls = new System.Collections.Generic.List<string>();
            yield return TouchControlChecks.Run(game, controls);
            Assert.That(controls.Count, Is.GreaterThanOrEqualTo(13));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryRuntimePrimitiveHasAMeshAndDecorativeMaterial()
        {
            foreach (PrimitiveType type in System.Enum.GetValues(typeof(PrimitiveType)))
            {
                var raw = GameObject.CreatePrimitive(type);
                Assert.That(raw.GetComponent<Collider>(), Is.Not.Null, type + " native collider dependency");
                Object.Destroy(raw);
                var shape = Art.Shape("Primitive regression " + type, type, null, Vector3.zero, Vector3.one, Art.Blue);
                Assert.That(shape.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null, type.ToString());
                Assert.That(Vector4.Distance(shape.GetComponent<Renderer>().sharedMaterial.color, Art.Blue), Is.LessThan(.001f));
                yield return null;
                Assert.That(shape.GetComponent<Collider>(), Is.Null, "Decorative shapes must not obstruct movement");
                Object.Destroy(shape);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SceneBootsAndEveryCharacterAndRideCanBeSelected()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            CityGame game = Object.FindFirstObjectByType<CityGame>();
            Assert.That(game, Is.Not.Null);
            float timeout = Time.realtimeSinceStartup + 90;
            while (!game.Ready && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(game.Ready, Is.True, "Procedural scene did not finish loading.");
            int portraitCount = 0;
            foreach (RawImage image in game.Hud.GetComponentsInChildren<RawImage>(true))
            {
                if (image.name != "3D explorer portrait") continue;
                var texture = image.texture as RenderTexture;
                Assert.That(texture, Is.Not.Null, "Portrait must be generated before readiness");
                Assert.That(image.enabled, Is.True);
                Assert.That(texture.IsCreated(), Is.True);
                var previous = RenderTexture.active;
                var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                try
                {
                    RenderTexture.active = texture;
                    pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); pixels.Apply();
                    int colored = 0;
                    foreach (Color32 p in pixels.GetPixels32())
                        if (p.a > 200 && Mathf.Max(p.r, Mathf.Max(p.g, p.b)) - Mathf.Min(p.r, Mathf.Min(p.g, p.b)) > 45) colored++;
                    Directory.CreateDirectory("Documentation/Validation"); File.WriteAllBytes("Documentation/Validation/portrait-"+portraitCount+".png",pixels.EncodeToPNG());
                    Assert.That(colored, Is.GreaterThan(1000), "Portrait "+portraitCount+" must contain colored character pixels, not a white placeholder");
                }
                finally { RenderTexture.active = previous; Object.Destroy(pixels); }
                portraitCount++;
            }
            Assert.That(portraitCount, Is.EqualTo(Simulation.CharacterNames.Length));
            Capture(game, "01-character-choice");
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(game.State.Obstacles.Count, Is.GreaterThan(80));
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (renderer.enabled) Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name);
            foreach (int index in new[] { 0, 1, 2, 3 })
            {
                game.SelectCharacter(index); yield return null;
                Assert.That(game.State.Character, Is.EqualTo(index));
                Transform player = game.transform.Find("Player");
                Assert.That(player.Find(Simulation.CharacterNames[index]), Is.Not.Null);
            }
            Button start = null;
            foreach (Button button in game.Hud.GetComponentsInChildren<Button>())
                if (button.GetComponentInChildren<Text>().text == "Let's explore!") start = button;
            Assert.That(start, Is.Not.Null); start.onClick.Invoke(); yield return null;
            Assert.That(game.Hud.MenuOpen, Is.False);
            Capture(game, "02-walking");
            foreach (Ride mode in new[] { Ride.RaceCar, Ride.Bicycle, Ride.Motorcycle, Ride.Airplane, Ride.Walk })
            {
                game.SelectRide(mode); yield return null;
                Assert.That(game.State.Mode, Is.EqualTo(mode));
                Assert.That(game.State.Blocked(game.State.Position, mode == Ride.Airplane ? 3.5f : .5f), Is.False, mode + " spawn is obstructed");
                Capture(game, "03-" + mode);
            }
            game.ResetHome(); yield return null;
            Assert.That(game.State.Mode, Is.EqualTo(Ride.Walk));
            Assert.That(game.State.Position.Z, Is.EqualTo(-24));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TouchReleaseAndMenuClearPreventStuckInput()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            CityGame game = Object.FindFirstObjectByType<CityGame>();
            float timeout = Time.realtimeSinceStartup + 90;
            while (!game.Ready && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(game.Ready, Is.True);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 1 };
            game.Hud.Go.OnPointerDown(pointer);
            Assert.That(game.Hud.Go.Held, Is.True);
            game.Hud.Go.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 2 });
            Assert.That(game.Hud.Go.Held, Is.True, "A different finger must not release GO.");
            game.Hud.Go.OnPointerUp(pointer);
            Assert.That(game.Hud.Go.Held, Is.False);
            game.Hud.Rise.OnPointerDown(pointer); game.Hud.ClearInput();
            Assert.That(game.Hud.Rise.Held, Is.False);
            Assert.That(game.Hud.Stick.Value, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator NextVersionInteractionsPosturesAndImpactArePlayable()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");
            var game=Object.FindFirstObjectByType<CityGame>();
            float deadline=Time.realtimeSinceStartup+90;
            while(!game.Ready && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(game.Ready,Is.True);
            foreach(var button in game.Hud.GetComponentsInChildren<Button>())
                if(button.GetComponentInChildren<Text>().text=="Let's explore!"){button.onClick.Invoke();break;}
            game.SelectCharacter(0);yield return null;
            View(game,new Vector3(2.5f,1.8f,3.6f),1.0f);Capture(game,"04-rounded-explorer");
            game.SelectRide(Ride.Bicycle);yield return null;
            for(int i=0;i<32;i++)
            {
                game.CurrentVehicle.Animate(8.5f,.024f,0);game.Explorer.FitRide(game.CurrentVehicle,false);
                Assert.That(game.Explorer.FootError,Is.LessThan(.025f),"Shoes must meet pedals at phase "+i);
                Assert.That(game.Explorer.HandError,Is.LessThan(.025f),"Hands must meet both grips");
            }
            View(game,new Vector3(3.4f,1.8f,2.1f),1.1f);Capture(game,"05-bicycle-contact");
            game.State.Step(.7f,new DriveInput{Forward=1});
            foreach(var trick in new[]{BikeTrick.Wheelie,BikeTrick.Endo,BikeTrick.Spin180})
            {
                if(game.State.Speed<2)game.State.Step(.7f,new DriveInput{Forward=1});
                Assert.That(game.State.StartTrick(trick),Is.True);game.State.Step(trick==BikeTrick.Spin180?.4f:.7f,new DriveInput{Forward=1});yield return null;
                View(game,new Vector3(3.8f,1.5f,1.5f),1.1f);Capture(game,"06-"+trick);
                game.State.Step(1,new DriveInput{Forward=1});
            }
            game.State.ToggleStanding();yield return null;
            Assert.That(game.Explorer.FootError,Is.LessThan(.03f),"Standing shoes stay on the pedals");
            Assert.That(game.Explorer.HandError,Is.LessThan(.03f),"Standing hands stay on the grips");
            View(game,new Vector3(3.5f,1.9f,1.4f),1.15f);Capture(game,"07-standing-bike");
            game.SelectRide(Ride.RaceCar);yield return null;
            Assert.That(game.Explorer.Head.position.y,Is.LessThan(game.CurrentVehicle.Root.position.y+1.50f));
            Assert.That(game.Explorer.Root.gameObject.activeInHierarchy,Is.True);
            View(game,new Vector3(-2.6f,1.65f,2.7f),1.04f);Capture(game,"08-visible-car-interior");
            game.State.Step(1,new DriveInput{Forward=1});game.State.Step(1,new DriveInput{Forward=1});game.State.Step(.45f,new DriveInput{Forward=1,Steering=1,Drift=true});yield return null;
            View(game,new Vector3(-3.8f,2.4f,-4.8f),.8f);Capture(game,"09-drift");
            game.SelectRide(Ride.Walk);game.State.Place(new Point(135,0,-49.7f));yield return null;
            Assert.That(game.Interactions.NearestDoor.Garage,Is.True);
            View(game,new Vector3(-17,9,-24),4);Capture(game,"10-airfield-garage");
            yield return game.Interactions.Use();yield return null;
            Assert.That(game.State.Indoors && game.Interactions.InGarage,Is.True);
            Camera.main.transform.position=new Vector3(0,4.8f,-179);Camera.main.transform.LookAt(new Vector3(0,1,-171));Capture(game,"11-garage-collection");
            game.State.ChooseCar(2);game.Interactions.Leave();game.SelectRide(Ride.RaceCar);yield return null;
            Assert.That(game.CurrentVehicle.Root.Find("Roof/Weekend suitcase"),Is.Not.Null,"Chosen garage car must actually be the driven model");
            game.SelectRide(Ride.Walk);game.State.Place(new Point(13,0,-42.2f));yield return null;
            Assert.That(game.Interactions.NearestDoor,Is.Not.Null,"Town house must expose a usable door");
            yield return game.Interactions.Use();yield return null;
            Assert.That(game.State.Indoors && !game.Interactions.InGarage,Is.True);
            var homePlan=game.Buildings.Active.Plan;Camera.main.transform.position=new Vector3(homePlan.X+2,2.5f,homePlan.Z+4);Camera.main.transform.LookAt(new Vector3(homePlan.X-3,1,homePlan.Z-2));Capture(game,"12-explorable-home");
            game.Interactions.Leave();game.ResetHome();game.SelectRide(Ride.RaceCar);yield return null;
            game.State.Place(new Point(0,0,-32),Mathf.PI/2);game.State.Obstacles.Add(new Obstacle(30,-32,.2f,4,5));
            for(int crash=0;crash<10;crash++){game.State.Place(new Point(0,0,-32),Mathf.PI/2);for(int i=0;i<240 && !game.State.NeedsRepair;i++)game.State.Step(1f/60,new DriveInput{Forward=1});}
            yield return new WaitForSeconds(.45f);
            Assert.That(game.State.NeedsRepair,Is.True);
            Assert.That(Object.FindFirstObjectByType<CrashParts>(),Is.Not.Null,"Crash must create physically moving parts");
            View(game,new Vector3(-4.2f,3,-4.2f),.8f);Capture(game,"13-impact-parts");
            game.RepairCar();yield return null;Assert.That(game.State.NeedsRepair,Is.False);
            Assert.That(game.CurrentVehicle.Root.Find("Nose").gameObject.activeSelf,Is.True,"Replacement restores missing body parts");
            game.ResetHome();LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator WorldsYachtsFurnitureAndNearbyVehicleEntryWork()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();float until=Time.realtimeSinceStartup+120;
            while(!game.Ready && Time.realtimeSinceStartup<until)yield return null;Assert.That(game.Ready,Is.True);
            game.Hud.BeginExploring();
            game.Hud.ShowHelp();yield return null;
            string helpText="";foreach(var label in game.Hud.GetComponentsInChildren<Text>())helpText+=label.text+"\n";
            Assert.That(helpText,Does.Contain("E / UP climbs").And.Contain("Q / DOWN descends").And.Contain("G enter / exit a nearby vehicle"));
            Capture(game,"18-controls");
            foreach(var b in game.Hud.GetComponentsInChildren<Button>())if(b.GetComponentInChildren<Text>().text=="Back to exploring"){b.onClick.Invoke();break;}
            Assert.That(game.Hud.MenuOpen,Is.False);
            Assert.That(game.Parking.EnterOrExit(),Is.False,"G must not enter a distant vehicle");
            game.State.Place(new Point(128,0,-54.2f));yield return null;
            var parked=game.Parking.Nearest;Assert.That(parked,Is.Not.Null);Vector3 initial=parked.Model.Root.position;
            Assert.That(game.Parking.Context,Does.StartWith("G • Enter "));
            Button enter=null;foreach(var b in game.Hud.GetComponentsInChildren<Button>())if(b.GetComponentInChildren<Text>().text==game.Parking.Context)enter=b;
            Assert.That(enter,Is.Not.Null,"The visible nearby-vehicle button must show G and the parked vehicle name");
            Camera.main.transform.position=initial+new Vector3(-7,4,-8);Camera.main.transform.LookAt(initial+Vector3.up);
            Capture(game,"19-nearby-vehicle");enter.onClick.Invoke();yield return null;
            Assert.That(game.State.Mode,Is.EqualTo(Ride.RaceCar));Assert.That(game.Parking.Context,Is.EqualTo("G • Step out"));
            Assert.That(Vector3.Distance(initial,new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z)),Is.LessThan(.1f),"Nearby entry must start at the actual parked car");
            Assert.That(parked.Model.Root.gameObject.activeSelf,Is.False,"The entered parked car must not remain duplicated");
            game.State.Step(.3f,new DriveInput{Forward=1});Assert.That(game.Parking.EnterOrExit(),Is.False,"Exit requires stopping");game.State.Step(1,new DriveInput{Brake=true});
            Assert.That(game.Parking.EnterOrExit(),Is.True);yield return null;Assert.That(game.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(game.State.Blocked(game.State.Position,.5f),Is.False,"Exit must put the player beside the car");
            foreach(var ride in game.Parking.Rides)
            {
                if(ride.World!=0 || ride.Model.Mode!=Ride.RaceCar || ride.Model.Root.position.x<120)continue;
                Vector3 near=ride.Model.Root.position+ride.Model.Root.right*2.7f;game.State.Place(new Point(near.x,near.y,near.z));
                Assert.That(game.Parking.EnterOrExit(),Is.True);Assert.That(game.State.CarStyle,Is.EqualTo(ride.Model.CarStyle));Assert.That(game.Parking.EnterOrExit(),Is.True);
            }
            game.Zoom(100);Assert.That(game.ZoomFactor,Is.EqualTo(.55f));game.Zoom(-100);Assert.That(game.ZoomFactor,Is.EqualTo(2.2f));game.Zoom((2.2f-1)/.13f);
            game.SelectRide(Ride.Airplane);for(int i=0;i<5;i++)game.State.Step(1,new DriveInput{Forward=1,Rise=true});Assert.That(game.State.CanTravel,Is.True);
            Assert.That(game.Hud.Rise.GetComponentInChildren<Text>().text,Is.EqualTo("UP\nE"));Assert.That(game.Hud.Descend.GetComponentInChildren<Text>().text,Is.EqualTo("DOWN\nQ"));
            Assert.That(game.Parking.Context,Is.EqualTo("E / UP climb · Q / DOWN descend"));
            game.Hud.ShowTravel(true);yield return null;Capture(game,"20-flight-destinations");game.Worlds.Travel(1);yield return null;
            Assert.That(game.State.WorldIndex,Is.EqualTo(1));Assert.That(game.State.Airborne,Is.True);
            Camera.main.transform.position=new Vector3(-15,52,-25);Camera.main.transform.LookAt(new Vector3(-90,5,100));Capture(game,"21-palm-cove-yachts");
            game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);game.State.Place(new Point(-65,0,60));game.Interact();yield return null;
            Assert.That(game.Worlds.Yachts.Aboard && game.State.Indoors,Is.True);
            Camera.main.transform.position=new Vector3(1,2.5f,-26);Camera.main.transform.LookAt(new Vector3(0,1,-16));Capture(game,"22-yacht-lounge");
            YachtSpot sofa=null,bed=null,stairs=null;
            foreach(var spot in game.Worlds.Yachts.Spots){if(spot.Deck==0 && spot.Pose==RestPose.Sitting && sofa==null)sofa=spot;if(spot.Pose==RestPose.Sleeping && bed==null)bed=spot;if(spot.Deck==0 && spot.Stairs)stairs=spot;}
            Assert.That(sofa,Is.Not.Null);game.State.Place(new Point(sofa.Near.x,sofa.Near.y,sofa.Near.z));game.Interact();yield return null;
            Assert.That(game.State.Resting,Is.EqualTo(RestPose.Sitting));Assert.That(game.Explorer.Root.position.y+1.08f*game.Explorer.Root.lossyScale.y,Is.GreaterThan(1.13f),"The seated hips must be above the sofa cushion");foreach(var label in game.Hud.GetComponentsInChildren<Text>())if(label.text.EndsWith(" km/h"))Assert.That(label.text,Is.EqualTo("0 km/h"),"Furniture entry must immediately clear the displayed speed");Point seated=game.State.Position;game.State.Step(1,new DriveInput{Forward=1});Assert.That(game.State.Position.X,Is.EqualTo(seated.X));
            Camera.main.transform.position=new Vector3(sofa.Anchor.x+3,2.1f,sofa.Anchor.z+3);Camera.main.transform.LookAt(new Vector3(sofa.Anchor.x,1.2f,sofa.Anchor.z));Capture(game,"23-sitting-on-yacht");
            game.Interact();Assert.That(game.State.Resting,Is.EqualTo(RestPose.None));
            game.State.Place(new Point(bed.Near.x,bed.Near.y,bed.Near.z));game.Interact();yield return null;Assert.That(game.State.Resting,Is.EqualTo(RestPose.Sleeping));foreach(var label in game.Hud.GetComponentsInChildren<Text>())if(label.text.EndsWith(" km/h"))Assert.That(label.text,Is.EqualTo("0 km/h"),"Sleeping must show zero speed");
            Camera.main.transform.position=new Vector3(bed.Anchor.x+2.7f,2.8f,bed.Anchor.z-2);Camera.main.transform.LookAt(new Vector3(bed.Anchor.x,1.1f,bed.Anchor.z+1));Capture(game,"24-sleeping-in-cabin");
            game.Interact();Assert.That(game.State.Resting,Is.EqualTo(RestPose.None));
            game.State.Place(new Point(6,0,-28.7f));for(int i=0;i<150;i++)game.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=0});yield return null;Assert.That(game.Worlds.Yachts.Deck,Is.EqualTo(1));Assert.That(game.State.Position.Y,Is.EqualTo(YachtStairs.Height));
            Camera.main.transform.position=new Vector3(0,8,-27);Camera.main.transform.LookAt(new Vector3(0,5.3f,8));Capture(game,"25-yacht-sun-deck");
            game.Worlds.Yachts.Leave();game.State.Place(new Point(-130,0,65));game.Interact();yield return null;Assert.That(game.Worlds.Yachts.YachtIndex,Is.EqualTo(1));Assert.That(game.Worlds.Yachts.Aboard,Is.True);
            game.Worlds.Yachts.Leave();game.SelectRide(Ride.Airplane);for(int i=0;i<5;i++)game.State.Step(1,new DriveInput{Forward=1,Rise=true});game.Worlds.Travel(2);yield return null;Assert.That(game.State.WorldIndex,Is.EqualTo(2));
            Camera.main.transform.position=new Vector3(15,24,-37);Camera.main.transform.LookAt(new Vector3(0,4,40));Capture(game,"26-cloud-peaks");
            game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);game.State.Place(new Point(-30,0,-1.2f));yield return game.Interactions.Use();yield return null;Assert.That(game.State.Indoors,Is.True);game.Interactions.Leave();
            game.SelectRide(Ride.Airplane);for(int i=0;i<5;i++)game.State.Step(1,new DriveInput{Forward=1,Rise=true});game.Worlds.Travel(0);yield return null;Assert.That(game.State.WorldIndex,Is.EqualTo(0));Assert.That(game.State.PondEnabled,Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EndlessRouteCameraAndBoundedWorldsWork()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();
            while(!game.Ready)yield return null;
            game.Hud.BeginExploring();
            game.OrbitCamera(90,20);Assert.That(game.CameraYaw,Is.EqualTo(90).Within(.01f));Assert.That(game.CameraPitch,Is.EqualTo(36).Within(.01f));
            game.OrbitCamera(1000,1000);Assert.That(game.CameraPitch,Is.EqualTo(70));game.OrbitCamera(-1000,-1000);Assert.That(game.CameraPitch,Is.EqualTo(5));
            game.Zoom(100);game.RecenterCamera();Assert.That(game.ZoomFactor,Is.EqualTo(1));Assert.That(Mathf.DeltaAngle(game.CameraYaw,game.State.Yaw*Mathf.Rad2Deg),Is.EqualTo(0).Within(.01f));
            var pointer=new PointerEventData(EventSystem.current){pointerId=8};game.Hud.LookLeft.OnPointerDown(pointer);game.Hud.ZoomIn.OnPointerDown(pointer);game.Hud.ShowHelp();Assert.That(game.Hud.LookLeft.Held || game.Hud.ZoomIn.Held,Is.False,"Menus release all camera holds");
            string help="";foreach(var label in game.Hud.GetComponentsInChildren<Text>())help+=label.text;Assert.That(help,Does.Contain("J / L orbit").And.Contain("I / K look").And.Contain("P recenter").And.Contain("E / UP climbs").And.Contain("Q / DOWN descends"));Capture(game,"30-camera-and-travel-help");
            foreach(var b in game.Hud.GetComponentsInChildren<Button>())if(b.GetComponentInChildren<Text>().text=="Back to exploring"){b.onClick.Invoke();break;}
            game.SelectRide(Ride.Airplane);for(int i=0;i<5;i++)game.State.Step(1,new DriveInput{Forward=1,Rise=true});
            yield return FlyGate(game,true);Assert.That(game.Worlds.CurrentStop,Is.EqualTo(1));Assert.That(game.State.WorldIndex,Is.EqualTo(3));
            string firstLayout=Layout(game.Worlds.ActivePlace);uint firstSeed=game.Worlds.Route.Describe(1).Seed;
            int[] vertices=new int[4];
            for(int theme=0;theme<4;theme++)
            {
                if(theme>0)yield return FlyGate(game,true);
                Assert.That(game.State.GeneratedTheme,Is.EqualTo(theme));
                var root=game.Worlds.ActivePlace.Root;Assert.That(root.GetComponent<GeneratedWorldTag>().Seed,Is.EqualTo(game.Worlds.Route.Describe(theme+1).Seed));
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())vertices[theme]+=mesh.sharedMesh.vertexCount;
                Assert.That(game.State.Blocked(Simulation.Home,.5f),Is.False,"Every generated world has a clear home plaza");
                Assert.That(game.State.Blocked(Simulation.RunwayStart,3.5f),Is.False,"Every generated world has a safe landing strip");
                Camera.main.transform.position=new Vector3(110,66,-99);Camera.main.transform.LookAt(new Vector3(-28,16,55));Capture(game,"3"+(theme+1)+"-"+WorldRoute.ThemeNames[theme].Replace(" ","-"));
                if(theme==2){var animals=root.GetComponentsInChildren<WildlifeMotion>();Assert.That(animals.Length,Is.GreaterThanOrEqualTo(8));Vector3 before=animals[0].transform.position;Camera.main.transform.position=before+new Vector3(13,8,16);Camera.main.transform.LookAt(before+Vector3.up*4);Capture(game,"35-dinosaurs-close");yield return new WaitForSeconds(.3f);Assert.That(Vector3.Distance(before,animals[0].transform.position),Is.GreaterThan(.05f),"Cartoon dinosaurs actually move");}
            }
            Assert.That(new System.Collections.Generic.HashSet<int>(vertices).Count,Is.EqualTo(4),"Themes have different built geometry");
            int warmBoxes=Art.CachedBoxCount,warmMaterials=Art.CachedMaterialCount;
            for(int i=0;i<8;i++){yield return FlyGate(game,true);Assert.That(Object.FindObjectsByType<GeneratedWorldTag>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,Is.EqualTo(1),"Only one generated world is retained");Assert.That(game.Parking.Rides.Count,Is.LessThan(30),"Travel must release old parked-vehicle records");}
            Assert.That(Art.CachedBoxCount,Is.LessThanOrEqualTo(warmBoxes+2),"Geometry caches stay bounded across new seeds");Assert.That(Art.CachedMaterialCount,Is.LessThanOrEqualTo(warmMaterials+2),"Palette caches stay bounded");
            long at=game.Worlds.CurrentStop;yield return FlyGate(game,false);Assert.That(game.Worlds.CurrentStop,Is.EqualTo(at-1),"South gate returns to the previous stop");
            game.Worlds.BeginRoute(1);while(game.Worlds.Busy)yield return null;yield return null;
            Assert.That(Layout(game.Worlds.ActivePlace),Is.EqualTo(firstLayout));Assert.That(game.Worlds.ActivePlace.Root.GetComponent<GeneratedWorldTag>().Seed,Is.EqualTo(firstSeed));
            game.Worlds.BeginRoute(5);while(game.Worlds.Busy)yield return null;yield return null;Assert.That(Layout(game.Worlds.ActivePlace),Is.Not.EqualTo(firstLayout),"Later jungle seeds visibly rearrange terrain and vegetation");
            game.Worlds.BeginRoute(1);while(game.Worlds.Busy)yield return null;yield return FlyGate(game,false);Assert.That(game.State.WorldIndex,Is.EqualTo(0),"First stop returns to Little City without stranding the player");
            yield return game.ShowNewAdventure();Assert.That(game.State.WorldIndex,Is.EqualTo(3));Assert.That(game.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(game.Hud.MenuOpen,Is.False);yield return null;game.RecenterCamera();Capture(game,"36-jungle-playable-start");
            LogAssert.NoUnexpectedReceived();
        }
        static string Layout(WorldPlace place)
        {
            string result="";foreach(var o in place.Collisions)result+=o.X.ToString("F2")+","+o.Z.ToString("F2")+","+o.Height.ToString("F2")+";";return result;
        }
        static IEnumerator FlyGate(CityGame game,bool next)
        {
            game.State.FinishFlightArrival(!next);long old=game.Worlds.CurrentStop;int world=game.State.WorldIndex;
            for(int i=0;i<650 && !game.Worlds.Busy && game.State.WorldIndex==world && game.Worlds.CurrentStop==old;i++)
            {
                Point from=game.State.Position;game.State.Step(1f/30,new DriveInput{Forward=1});game.Worlds.CheckFlight(from);
            }
            Assert.That(game.Worlds.Busy || game.State.WorldIndex!=world,Is.True,"Flying along the runway must cross a working route gate: world="+world+" stop="+old+" next="+next+" mode="+game.State.Mode+" menu="+game.Hud.MenuOpen+" canTravel="+game.State.CanTravel+" position="+game.State.Position.X+","+game.State.Position.Y+","+game.State.Position.Z+" speed="+game.State.Speed+" notice="+game.State.Notice);
            float until=Time.realtimeSinceStartup+60;while(game.Worlds.Busy && Time.realtimeSinceStartup<until)yield return null;Assert.That(game.Worlds.Busy,Is.False);yield return null;
        }
        [UnityTest]
        public IEnumerator BoatsTubeSwimmingAndDivingWork()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;
            foreach(var b in game.Hud.GetComponentsInChildren<Button>())if(b.GetComponentInChildren<Text>().text=="Let's explore!"){b.onClick.Invoke();break;}
            game.SelectRide(Ride.Airplane);for(int i=0;i<5;i++)game.State.Step(1,new DriveInput{Forward=1,Rise=true});game.Worlds.Travel(1);yield return null;
            foreach(var sign in game.Worlds.ActivePlace.Root.Find("Marine adventures").GetComponentsInChildren<TMPro.TMP_Text>())
            {sign.ForceMeshUpdate();Assert.That(sign.textBounds.size.x*sign.transform.lossyScale.x,Is.LessThanOrEqualTo(24),"Marine signs must fit their intended width");Assert.That(sign.textBounds.size.y*sign.transform.lossyScale.y,Is.LessThanOrEqualTo(1.11f),"Marine signs must remain at human scale");}
            game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);game.State.Place(new Point(22.8f,0,46));yield return null;
            Assert.That(game.Parking.Nearest.Model.Mode,Is.EqualTo(Ride.Speedboat));Assert.That(game.Parking.EnterOrExit(),Is.True);yield return null;
            Assert.That(game.State.Mode,Is.EqualTo(Ride.Speedboat));var rig=game.CurrentVehicle.Root.GetComponent<TowRig>();Assert.That(rig,Is.Not.Null);Assert.That(rig.Towing,Is.True);
            game.State.Place(new Point(78,-1.05f,72));game.State.Step(1,new DriveInput{Forward=1});yield return null;
            for(int i=0;i<30;i++){game.State.Step(.06f,new DriveInput{Forward=1,Steering=.4f});yield return null;}
            Assert.That(rig.RopeLength,Is.LessThan(9));Assert.That(rig.Tube.gameObject.activeInHierarchy,Is.True);
            Camera.main.transform.position=new Vector3(104,11,98);Camera.main.transform.LookAt(new Vector3(78,0,85));Capture(game,"40-speedboat-and-tube");
            game.Interact();Assert.That(rig.Towing,Is.False);game.Interact();Assert.That(rig.Towing,Is.True);
            game.State.Step(1,new DriveInput{Brake=true});game.State.Step(1,new DriveInput{Brake=true});Assert.That(game.Parking.EnterOrExit(),Is.True);yield return null;Assert.That(game.State.Swimming,Is.True);
            game.State.Place(new Point(35,-1.05f,102));game.Interact();yield return null;Assert.That(game.State.OnDivePlatform,Is.True);Assert.That(game.State.Position.Y,Is.EqualTo(4.4f));
            Camera.main.transform.position=new Vector3(55,13,89);Camera.main.transform.LookAt(new Vector3(35,2,111));Capture(game,"41-diving-platform-and-sharks");
            var sharks=Object.FindObjectsByType<SharkCircle>(FindObjectsSortMode.None);Assert.That(sharks.Length,Is.EqualTo(5));Vector3 sharkPosition=sharks[0].transform.position;yield return new WaitForSeconds(.3f);Assert.That(Vector3.Distance(sharkPosition,sharks[0].transform.position),Is.GreaterThan(.1f));
            game.Interact();for(int i=0;i<120;i++)game.State.Step(1f/60,new DriveInput{});yield return null;Assert.That(game.State.Swimming,Is.True);Assert.That(game.State.OnDivePlatform,Is.False);
            Camera.main.transform.position=new Vector3(game.State.Position.X+3,2.6f,game.State.Position.Z+4);Camera.main.transform.LookAt(new Vector3(game.State.Position.X,-.3f,game.State.Position.Z+1));Capture(game,"42-swimming");
            game.State.Place(new Point(81,-1.05f,44));for(int i=0;i<180;i++)game.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=Mathf.PI});Assert.That(game.State.Swimming,Is.False);Assert.That(game.State.Position.Y,Is.EqualTo(0));
            game.State.Place(new Point(55.8f,0,46));yield return null;Assert.That(game.Parking.Nearest.Model.Mode,Is.EqualTo(Ride.Motorboat));Assert.That(game.Parking.EnterOrExit(),Is.True);yield return null;Assert.That(game.State.Mode,Is.EqualTo(Ride.Motorboat));
            game.ResetHome();Assert.That(game.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(game.State.Swimming,Is.False);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ForwardKeysStayStraightAfterCameraChanges()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();game.State.Obstacles.Clear();game.State.PondEnabled=false;
            var held=new System.Collections.Generic.HashSet<KeyCode>();game.HeldKey=k=>held.Contains(k);
            foreach(float yaw in new[]{0f,43f,95f,173f,-60f})foreach(float dt in new[]{1f/60,.12f})
            {
                held.Clear();game.State.Place(new Point(-135,0,-115),yaw*Mathf.Deg2Rad);game.RecenterCamera();game.ProcessControls(dt);
                float chosen=Camera.main.transform.eulerAngles.y*Mathf.Deg2Rad;Point start=game.State.Position;held.Add(KeyCode.UpArrow);
                for(int i=0;i<Mathf.CeilToInt(3/dt);i++)game.ProcessControls(dt);
                Vector2 delta=new Vector2(game.State.Position.X-start.X,game.State.Position.Z-start.Z);Vector2 direction=new Vector2(Mathf.Sin(chosen),Mathf.Cos(chosen));
                Assert.That(Mathf.Abs(delta.x*direction.y-delta.y*direction.x),Is.LessThan(.025f),"Held Up must not curve as chase camera recenters");Assert.That(Vector2.Dot(delta,direction),Is.GreaterThan(10));
            }
            held.Clear();game.State.Place(new Point(-135,0,-115));game.RecenterCamera();game.ProcessControls(.02f);held.Add(KeyCode.W);for(int i=0;i<30;i++)game.ProcessControls(.02f);
            game.OrbitCamera(90,0);game.ProcessControls(.02f);Point turn=game.State.Position;for(int i=0;i<100;i++)game.ProcessControls(.02f);Assert.That(game.State.Position.X-turn.X,Is.GreaterThan(7));Assert.That(Mathf.Abs(game.State.Position.Z-turn.Z),Is.LessThan(.03f),"Keyboard orbit deliberately chooses the new heading and then stays straight");
            held.Clear();game.ProcessControls(.02f);game.RecenterCamera();var pointer=new PointerEventData(EventSystem.current){pointerId=19};game.Hud.Go.OnPointerDown(pointer);Point touch=game.State.Position;for(int i=0;i<60;i++)game.ProcessControls(.02f);game.Hud.Go.OnPointerUp(pointer);Assert.That(game.State.Position.X-touch.X,Is.GreaterThan(6));Assert.That(Mathf.Abs(game.State.Position.Z-touch.Z),Is.LessThan(.03f),"Touch GO uses the same stable movement heading");
            held.Clear();game.Hud.ClearInput();game.State.Place(new Point(-120,0,-90),.6f);game.RefreshPlayer();game.OrbitCamera(55,0);for(int i=0;i<8;i++)yield return null;
            held.Add(KeyCode.UpArrow);Point liveStart=game.State.Position;float visibleYaw=Camera.main.transform.eulerAngles.y*Mathf.Deg2Rad;
            // A test coroutine resumes before LateUpdate: allow the first actual input frame
            // to choose the visible heading, then measure the entire remaining trajectory.
            yield return null;yield return null;Vector2 first=new Vector2(game.State.Position.X-liveStart.X,game.State.Position.Z-liveStart.Z);Assert.That(first.magnitude,Is.GreaterThan(.001f));float liveYaw=Mathf.Atan2(first.x,first.y);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(visibleYaw*Mathf.Rad2Deg,liveYaw*Mathf.Rad2Deg)),Is.LessThan(8),"First step follows the visible view, including its final smoothing frame");
            for(int i=0;i<90;i++){yield return null;Vector2 d=new Vector2(game.State.Position.X-liveStart.X,game.State.Position.Z-liveStart.Z);Assert.That(Mathf.Abs(d.x*Mathf.Cos(liveYaw)-d.y*Mathf.Sin(liveYaw)),Is.LessThan(.04f),"Actual update and camera smoothing must preserve the held forward line");}
            Assert.That(Vector2.Distance(new Vector2(liveStart.X,liveStart.Z),new Vector2(game.State.Position.X,game.State.Position.Z)),Is.GreaterThan(1));held.Clear();game.HeldKey=Input.GetKey;yield return null;View(game,new Vector3(4,3,-5),1);Capture(game,"50-straight-walking");LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator BuildingsHaveTransparentWindowsAndConnectedFloors()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();
            game.State.Place(new Point(13,0,-42.2f));yield return null;var door=game.Interactions.NearestDoor;Assert.That(door.Building,Is.Not.Null);game.Interact();yield return null;Assert.That(game.Buildings.Inside,Is.True);
            var house=game.Buildings.Active;Assert.That(house.Plan.Floors,Is.GreaterThan(1));yield return WalkAllFloors(game,house);game.Buildings.Leave();yield return null;Assert.That(game.State.Indoors,Is.False);
            int windows=0;foreach(var pane in house.GetComponentsInChildren<MeshFilter>())if(pane.name=="Transparent window with real interior"){windows++;Assert.That(pane.sharedMesh.vertexCount,Is.GreaterThanOrEqualTo(4));}Assert.That(windows,Is.GreaterThan(12));
            int glassBatches=0;foreach(var renderer in game.Worlds.ActivePlace.Root.GetComponentsInChildren<MeshRenderer>())if(renderer.name.StartsWith("Scenery ") && (renderer.sharedMaterial.shader.name=="LittleCity/Glass" || renderer.sharedMaterial.shader.name=="LittleCity/HighGlass")){glassBatches++;Assert.That(renderer.enabled,Is.True);Assert.That(renderer.sharedMaterial.GetTag("RenderType",false),Is.EqualTo("Transparent"));Assert.That(renderer.sharedMaterial.color.a,Is.InRange(.05f,.3f));}Assert.That(glassBatches,Is.GreaterThan(0),"Transparent building windows must survive scenery batching in both graphics modes");
            Camera.main.transform.position=house.transform.TransformPoint(new Vector3(-house.Plan.Width*.33f,1.65f,house.Plan.Depth/2+3));Camera.main.transform.LookAt(house.transform.TransformPoint(new Vector3(-3,1.4f,0)));Capture(game,"51-see-through-home-window");
            Camera.main.transform.position=house.transform.TransformPoint(new Vector3(-18,10,22));Camera.main.transform.LookAt(house.transform.position+Vector3.up*4);Capture(game,"60-explorable-homes");
            game.Hud.ShowHelp();yield return null;Capture(game,"61-help-controls");game.Hud.BeginExploring();
            game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(2);while(game.Worlds.Busy)yield return null;game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);
            ExplorableBuilding tallest=null;foreach(var b in game.Worlds.ActivePlace.Root.GetComponentsInChildren<ExplorableBuilding>())if(tallest==null || b.Plan.Floors>tallest.Plan.Floors)tallest=b;
            var allBuildings=game.Worlds.ActivePlace.Root.GetComponentsInChildren<ExplorableBuilding>();
            for(int i=0;i<allBuildings.Length;i++)for(int j=i+1;j<allBuildings.Length;j++){var a=allBuildings[i].Plan;var b=allBuildings[j].Plan;Assert.That(Mathf.Abs(a.X-b.X)>=(a.Width+b.Width)/2 || Mathf.Abs(a.Z-b.Z)>=(a.Depth+b.Depth)/2,Is.True,"Explorable buildings must not occupy one another's interiors");}
            Assert.That(tallest.Plan.Floors,Is.GreaterThanOrEqualTo(16));Point entry=tallest.Plan.World(new Point(0,0,tallest.Plan.Depth/2+1.3f));game.State.Place(entry);game.Buildings.Enter(tallest);yield return null;
            yield return WalkAllFloors(game,tallest);Camera.main.transform.position=tallest.transform.TransformPoint(new Vector3(tallest.Plan.Width/2-1,tallest.Plan.Height+4,0));Camera.main.transform.LookAt(tallest.transform.TransformPoint(new Vector3(3,tallest.Plan.Height+1,7)));Capture(game,"52-skyscraper-rooftop");
            yield return DescendAllFloors(game,tallest);game.Buildings.Leave();Assert.That(game.State.Indoors,Is.False);Assert.That(Vector3.Distance(new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z),new Vector3(entry.X,entry.Y,entry.Z)),Is.LessThan(.1f));LogAssert.NoUnexpectedReceived();
        }
        static IEnumerator WalkAllFloors(CityGame game,ExplorableBuilding building)
        {
            var p=building.Plan;float corridor=p.StairX-2.4f;
            for(int level=0;level<p.Floors;level++)
            {
                WalkTo(game,p,new Point(corridor,level*p.Story,p.StairEnd+.65f));WalkTo(game,p,new Point(corridor,level*p.Story,p.StairStart-.65f));WalkTo(game,p,new Point(p.StairX,level*p.Story,p.StairStart-.65f));WalkTo(game,p,new Point(p.StairX,(level+1)*p.Story,p.StairEnd+.65f));WalkTo(game,p,new Point(corridor,(level+1)*p.Story,p.StairEnd+.65f));
                Assert.That(game.State.Position.Y,Is.EqualTo((level+1)*p.Story).Within(.08f),"Every visible storey must be reachable by walking its staircase");yield return null;
                if(level==1){Camera.main.transform.position=building.transform.TransformPoint(new Vector3(p.StairX-3,(level+1)*p.Story+2,p.StairStart-1));Camera.main.transform.LookAt(building.transform.TransformPoint(new Vector3(p.StairX,(level+1)*p.Story+1,p.StairEnd)));Capture(game,"53-connected-stairs");}
            }
        }
        static IEnumerator DescendAllFloors(CityGame game,ExplorableBuilding building)
        {
            var p=building.Plan;float corridor=p.StairX-2.4f;
            for(int level=p.Floors;level>0;level--){WalkTo(game,p,new Point(p.StairX,level*p.Story,p.StairEnd+.65f));WalkTo(game,p,new Point(p.StairX,(level-1)*p.Story,p.StairStart-.65f));WalkTo(game,p,new Point(corridor,(level-1)*p.Story,p.StairStart-.65f));WalkTo(game,p,new Point(corridor,(level-1)*p.Story,p.StairEnd+.65f));Assert.That(game.State.Position.Y,Is.EqualTo((level-1)*p.Story).Within(.08f));yield return null;}
        }
        static void WalkTo(CityGame game,BuildingPlan p,Point local)
        {
            Point target=p.World(local);
            for(int i=0;i<1800;i++){float x=target.X-game.State.Position.X,z=target.Z-game.State.Position.Z;if(x*x+z*z<.035f)return;game.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=Mathf.Atan2(x,z)});}
            Assert.Fail("Could not walk to building landing "+local.X+", "+local.Y+", "+local.Z+" from "+game.State.Position.X+", "+game.State.Position.Y+", "+game.State.Position.Z);
        }
        [UnityTest]
        public IEnumerator MarinaSafariJeepAndThorArePlayable()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();
            game.Hud.ShowMarina();yield return null;Capture(game,"54-marina-guide");game.Worlds.VisitMarina(0);yield return null;Assert.That(game.State.WorldIndex,Is.EqualTo(1));Assert.That(game.Worlds.Yachts.Context,Does.Contain("Board Aurora"));game.Interact();yield return null;Assert.That(game.Worlds.Yachts.Aboard,Is.True);game.Worlds.Yachts.Leave();
            game.State.Place(new Point(-5,0,65));game.Interact();yield return null;Assert.That(game.Worlds.Yachts.YachtIndex,Is.EqualTo(2));Assert.That(game.Worlds.Yachts.Aboard,Is.True);game.Worlds.Yachts.Leave();yield return null;
            int boats=0;foreach(var ride in game.Parking.Rides)if(ride.World==1 && (ride.Model.Mode==Ride.Speedboat || ride.Model.Mode==Ride.Motorboat))boats++;Assert.That(boats,Is.EqualTo(6));Camera.main.transform.position=new Vector3(100,47,12);Camera.main.transform.LookAt(new Vector3(-60,3,95));Capture(game,"55-three-yachts-six-boats");
            game.Worlds.VisitMarina(1);yield return null;Assert.That(game.Parking.EnterOrExit(),Is.True);Assert.That(game.State.Mode,Is.EqualTo(Ride.Speedboat));game.ResetHome();
            game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(3);while(game.Worlds.Busy)yield return null;game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);game.State.Place(new Point(-11.7f,0,-25));yield return null;
            Assert.That(game.Parking.Nearest.Model.CarStyle,Is.EqualTo(3));Assert.That(game.Parking.EnterOrExit(),Is.True);yield return null;Assert.That(game.State.CarStyle,Is.EqualTo(3));Assert.That(game.CurrentVehicle.Root.Find("Rear spare tire"),Is.Not.Null);View(game,new Vector3(-4.5f,2.5f,5),1.1f);Capture(game,"56-dinosaur-safari-jeep");
            game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(4);while(game.Worlds.Busy)yield return null;game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);game.State.Place(new Point(0,0,-14));game.RefreshPlayer();game.Hero.Toggle();yield return null;Assert.That(game.Hero.Active,Is.True);Assert.That(game.Explorer.Root.Find("Thor armor and red cape"),Is.Not.Null);
            View(game,new Vector3(3,2.2f,4),1.1f);Capture(game,"57-playable-thor");Assert.That(game.Hero.ThrowHammer(),Is.True);Assert.That(game.Hero.HammerFlying,Is.True);for(int i=0;i<50;i++)game.Hero.Tick(.02f);Assert.That(game.Hero.BrokenCount,Is.GreaterThan(0));Assert.That(game.Hero.HammerFlying,Is.False);Assert.That(game.Hero.ThrowHammer(),Is.False,"Hammer cooldown prevents repeated instant throws");
            game.Hero.Tick(1);Assert.That(game.Hero.ThrowHammer(),Is.True);Assert.That(game.Hero.ThrowHammer(),Is.True,"Y recalls a flying hammer");for(int i=0;i<50;i++)game.Hero.Tick(.02f);Assert.That(game.Hero.HammerFlying,Is.False);
            game.Hero.ResetTargets();Assert.That(game.Hero.Lightning(),Is.True);Assert.That(game.Hero.Lightning(),Is.False);Assert.That(game.Hero.BrokenCount,Is.GreaterThan(0));yield return null;Camera.main.transform.position=new Vector3(24,12,-24);Camera.main.transform.LookAt(new Vector3(0,3,7));Capture(game,"58-thor-lightning-targets");
            game.Hero.ResetTargets();game.State.Place(new Point(0,0,2));Assert.That(game.Hero.Slam(),Is.True);Assert.That(game.Hero.Slam(),Is.False);Assert.That(game.Hero.BrokenCount,Is.GreaterThan(0));yield return null;Camera.main.transform.position=new Vector3(13,8,-9);Camera.main.transform.LookAt(new Vector3(0,1,2));Capture(game,"59-thor-ground-slam");game.Hero.ResetTargets();Assert.That(game.Hero.BrokenCount,Is.EqualTo(0));game.Hero.Toggle();Assert.That(game.Hero.Active,Is.False);LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator QuietHudKeepsPlayAreaClearAndCommandsAvailable()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();
            Assert.That(game.Hud.TouchControlsVisible,Is.False);Assert.That(game.Hud.ExploreOpen || game.Hud.ControlsOpen,Is.False);
            game.Worlds.VisitMarina(1);game.Parking.EnterOrExit();yield return null;game.RecenterCamera();Capture(game,"70-quiet-boat-ui");Capture(game,"77-quiet-ui-laptop",1366,768);Capture(game,"78-quiet-ui-ipad-size",1194,834);yield return null;Canvas.ForceUpdateCanvases();
            foreach(var button in game.Hud.GetComponentsInChildren<Button>())
            {
                var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);var r=new Rect(corners[0].x, corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);
                Assert.That(r.Overlaps(new Rect(Screen.width*.31f,Screen.height*.22f,Screen.width*.38f,Screen.height*.56f)),Is.False,"Playing controls must leave the central view clear: "+button.name+" "+r+" screen "+Screen.width+"x"+Screen.height);
            }
            Assert.That(game.Hud.VehicleButton.gameObject.activeInHierarchy,Is.True);Assert.That(game.Hud.InteractionButton.gameObject.activeInHierarchy,Is.True);
            game.Hud.ShowExplore(true);yield return null;Capture(game,"71-explore-drawer");Assert.That(game.Hud.MenuOpen,Is.True);
            foreach(var button in game.Hud.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text=="3\nBicycle")button.onClick.Invoke();
            Assert.That(game.Hud.MenuOpen,Is.False);Assert.That(game.State.Mode,Is.EqualTo(Ride.Bicycle),"Choosing a ride from Explore must close the menu before dispatching the ride change");
            game.Hud.ShowControls(true);yield return null;Capture(game,"72-controls-on-demand");var pointer=new PointerEventData(EventSystem.current){pointerId=44};game.Hud.LookLeft.OnPointerDown(pointer);game.ProcessControls(.3f);game.Hud.ShowControls(false);Assert.That(game.Hud.LookLeft.Held,Is.False);
            game.Hud.SetTouchControls(true);yield return null;Capture(game,"73-optional-touch-ui");game.Hud.Go.OnPointerDown(pointer);game.Hud.SetTouchControls(false);Assert.That(game.Hud.Go.Held,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator EveryYachtHasWalkableConnectedStairs()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();game.Worlds.VisitMarina(0);
            foreach(float dock in new[]{-65f,-130f,-5f})
            {
                game.State.Place(new Point(dock,0,dock==-65?60:65));game.Interact();yield return null;Assert.That(game.Worlds.Yachts.Aboard,Is.True);
                game.State.Place(new Point(6,0,-28.7f));float last=0;
                for(int i=0;i<150;i++){game.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=0});Assert.That(game.State.Position.Y-last,Is.InRange(-.001f,.08f),"Stairs must rise smoothly, not teleport");last=game.State.Position.Y;}
                yield return null;Assert.That(game.Worlds.Yachts.Deck,Is.EqualTo(1));Assert.That(game.State.Position.Y,Is.EqualTo(YachtStairs.Height).Within(.01f));
                if(dock==-65){game.State.Place(new Point(6,2.2f,-24));game.RefreshPlayer();yield return null;Camera.main.transform.position=new Vector3(6,2,-29.3f);Camera.main.transform.LookAt(new Vector3(6,3.2f,-23));Capture(game,"74-connected-yacht-stairs");}
                game.State.Place(new Point(6,YachtStairs.Height,-19.3f));last=YachtStairs.Height;
                for(int i=0;i<150;i++){game.State.Step(1f/60,new DriveInput{Forward=1,CameraYaw=Mathf.PI});Assert.That(last-game.State.Position.Y,Is.InRange(-.001f,.08f));last=game.State.Position.Y;}
                yield return null;Assert.That(game.Worlds.Yachts.Deck,Is.EqualTo(0));Assert.That(game.State.Position.Y,Is.EqualTo(0).Within(.01f));game.Worlds.Yachts.Leave();
            }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ElevatorKeypadVisitsEveryFloorAndRoof()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(2);while(game.Worlds.Busy)yield return null;game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);
            ExplorableBuilding tower=null;foreach(var b in game.Worlds.ActivePlace.Root.GetComponentsInChildren<ExplorableBuilding>())if(tower==null || b.Plan.Floors>tower.Plan.Floors)tower=b;
            game.Buildings.Enter(tower);var p=tower.Plan;WalkTo(game,p,new Point(0,0,p.LiftZ-3));WalkTo(game,p,new Point(p.LiftX,0,p.LiftZ-2.4f));yield return null;
            Assert.That(game.Lift.Nearby,Is.True);game.Interact();yield return null;Assert.That(game.Hud.ElevatorOpen,Is.True);
            foreach(string invalid in new[]{"","0","-1","1.5","abc",(p.Floors+1).ToString()}){game.Hud.FloorInput.text=invalid;game.Hud.SubmitFloor();Assert.That(game.Lift.Moving,Is.False);Assert.That(game.Hud.ElevatorOpen,Is.True);}
            game.Hud.FloorInput.text="";
            foreach(string digit in new[]{"1","2"})foreach(var button in game.Hud.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text==digit)button.onClick.Invoke();
            Assert.That(game.Hud.FloorInput.text,Is.EqualTo("12"));yield return null;Capture(game,"75-elevator-keypad");
            for(int floor=1;floor<=p.Floors;floor++)
            {
                if(!game.Hud.ElevatorOpen)game.Interact();game.Hud.FloorInput.text=floor.ToString();game.Hud.SubmitFloor();Assert.That(game.Lift.Moving,Is.True);int timeout=0;while(game.Lift.Moving && timeout++<200)game.Lift.Tick(.1f);
                Assert.That(game.Lift.Moving,Is.False);Assert.That(game.State.Position.Y,Is.EqualTo((floor-1)*p.Story).Within(.01f));Assert.That(game.Lift.Nearby,Is.True);Assert.That(game.State.Blocked(game.State.Position,.4f),Is.False,"Arrival must be outside all furniture and elevator walls");yield return null;
                game.RecenterCamera();Vector3 focus=game.Explorer.Root.parent.position+Vector3.up*1.15f;Assert.That(Vector3.Distance(Camera.main.transform.position,focus),Is.GreaterThan(2),"Landing camera must stay outside the character");Assert.That(Physics.Linecast(focus,Camera.main.transform.position,1<<8),Is.False,"Elevator walls must not hide the arrival view");
            }
            game.Interact();foreach(var button in game.Hud.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text=="Roof")button.onClick.Invoke();Assert.That(game.Lift.Moving,Is.True);while(game.Lift.Moving){game.Lift.Tick(.2f);yield return null;}Assert.That(game.State.Position.Y,Is.EqualTo(p.Height).Within(.01f));yield return null;Assert.That(game.Hud.MenuOpen,Is.False);game.RecenterCamera();yield return null;yield return null;Capture(game,"76-elevator-rooftop-arrival");
            Assert.That(game.Lift.Travel(0),Is.True);while(game.Lift.Moving)game.Lift.Tick(.2f);Assert.That(game.State.Position.Y,Is.EqualTo(0).Within(.01f));
            Assert.That(game.Lift.Travel(p.Floors),Is.True);game.Lift.Tick(3);yield return null;yield return null;Capture(game,"79-elevator-in-motion");game.ResetHome();yield return null;Assert.That(game.Lift.Moving,Is.False);Assert.That(game.State.Indoors,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ElevatorsStayAccessibleAcrossBuildingLayouts()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();int buildings=0,landings=0;
            foreach(int world in new[]{0,2,4})
            {
                if(world!=0){game.SelectRide(Ride.Airplane);game.State.FinishFlightArrival(false);game.Worlds.BeginRoute(world);while(game.Worlds.Busy)yield return null;game.State.Place(Simulation.RunwayStart);game.SelectRide(Ride.Walk);}
                foreach(var b in game.Worlds.ActivePlace.Root.GetComponentsInChildren<ExplorableBuilding>())
                {
                    var p=b.Plan;if(p.Floors<3)continue;buildings++;game.Buildings.Enter(b);WalkTo(game,p,new Point(0,0,p.LiftZ-2.4f));WalkTo(game,p,new Point(p.LiftX,0,p.LiftZ-2.4f));Assert.That(game.Lift.Nearby,Is.True,p.Name);
                    for(int floor=0;floor<=p.Floors;floor++)
                    {
                        Assert.That(game.Lift.Travel(floor),Is.True);int timeout=0;while(game.Lift.Moving && timeout++<200)game.Lift.Tick(.2f);Assert.That(game.Lift.Moving,Is.False);landings++;
                        Assert.That(game.State.Position.Y,Is.EqualTo(floor*p.Story).Within(.01f));Assert.That(game.State.Blocked(game.State.Position,.4f),Is.False,p.Name+" floor "+floor);
                        WalkTo(game,p,new Point(0,floor*p.Story,p.LiftZ-2.4f));WalkTo(game,p,new Point(p.LiftX,floor*p.Story,p.LiftZ-2.4f));Assert.That(game.Lift.Nearby,Is.True);yield return null;
                    }
                    game.Buildings.Leave();yield return null;
                }
            }
            Assert.That(buildings,Is.GreaterThan(20));Assert.That(landings,Is.GreaterThan(100));File.WriteAllText("Documentation/Validation/elevator-coverage.txt",buildings+" buildings, "+landings+" floor/roof landings, walk-out and return at every landing.\n");LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TravelButtonsWorkFromYachtsBoatsAndOnFoot()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();
            for(int destination=0;destination<3;destination++)
            {
                game.Worlds.VisitMarina(0);game.Interact();yield return null;Assert.That(game.Worlds.Yachts.Aboard,Is.True);
                game.Hud.ShowTravel(true);yield return null;if(destination==0)Capture(game,"80-travel-from-yacht");yield return null;ClickHud(game,Simulation.WorldNames[destination]);yield return null;
                AssertArrival(game,destination);Assert.That(game.Worlds.Yachts.Aboard,Is.False);Assert.That(game.State.InteriorGround,Is.Null);Assert.That(game.State.InteriorObstacles,Is.Empty);
                game.Hud.ShowTravel(true);yield return null;ClickHud(game,Simulation.WorldNames[destination]);yield return null;AssertArrival(game,destination);
            }
            game.Worlds.VisitMarina(0);game.Interact();yield return null;Point aboard=game.State.Position;game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Continue exploring here");yield return null;
            Assert.That(game.Worlds.Yachts.Aboard,Is.True);Assert.That(game.Hud.MenuOpen,Is.False);Assert.That(game.State.Position.Z,Is.EqualTo(aboard.Z));
            game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Return to Little City");yield return null;AssertArrival(game,0);Capture(game,"81-returned-to-little-city");
            game.Worlds.VisitMarina(1);Assert.That(game.Parking.EnterOrExit(),Is.True);game.State.Step(1,new DriveInput{Forward=1});game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Cloud Peaks");yield return null;AssertArrival(game,2);Assert.That(game.Parking.Occupied,Is.Null);
            game.Worlds.VisitMarina(2);Assert.That(game.State.Swimming,Is.True);game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Little City");yield return null;AssertArrival(game,0);Assert.That(game.State.Swimming,Is.False);
            var home=game.Worlds.ActivePlace.Root.GetComponentInChildren<ExplorableBuilding>();game.Buildings.Enter(home);var p=home.Plan;game.State.Place(p.World(new Point(p.LiftX,0,p.LiftZ-2.4f)));Assert.That(game.Lift.Travel(2),Is.True);game.Lift.Tick(.1f);
            game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Palm Cove");yield return null;AssertArrival(game,1);Assert.That(game.Lift.Moving,Is.False);Assert.That(game.Buildings.Inside,Is.False);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator SavedRouteButtonWorksFromYachtAndKeepsProgress()
        {
            yield return SceneManager.LoadSceneAsync("LittleCity");var game=Object.FindFirstObjectByType<CityGame>();while(!game.Ready)yield return null;game.Hud.BeginExploring();
            game.Worlds.VisitMarina(0);game.Interact();yield return null;Assert.That(game.Worlds.Yachts.Aboard,Is.True);game.Hud.ShowTravel(true);yield return null;ClickHud(game,"START / RESUME SKY ROUTE");
            int timeout=0;while(game.Worlds.Busy && timeout++<300)yield return null;Assert.That(game.Worlds.Busy,Is.False);Assert.That(game.State.WorldIndex,Is.EqualTo(3));Assert.That(game.State.CanTravel,Is.True);Assert.That(game.Worlds.Yachts.Aboard,Is.False);Assert.That(game.Hud.MenuOpen,Is.False);
            game.Worlds.BeginRoute(2);while(game.Worlds.Busy)yield return null;long saved=game.Worlds.CurrentStop;
            game.Hud.ShowTravel(true);yield return null;ClickHud(game,"Return to Little City");yield return null;AssertArrival(game,0);Assert.That(game.Worlds.Route.LastVisit,Is.EqualTo(saved));
            game.Hud.ShowTravel(true);yield return null;ClickHud(game,"START / RESUME SKY ROUTE");while(game.Worlds.Busy)yield return null;Assert.That(game.Worlds.CurrentStop,Is.EqualTo(saved));Assert.That(game.State.GeneratedTheme,Is.EqualTo(1));Assert.That(game.State.Airborne,Is.True);LogAssert.NoUnexpectedReceived();
        }
        static void AssertArrival(CityGame game,int destination)
        {
            Assert.That(game.State.WorldIndex,Is.EqualTo(destination));Assert.That(game.State.Mode,Is.EqualTo(Ride.Walk));Assert.That(game.State.Indoors,Is.False);Assert.That(game.Hud.MenuOpen,Is.False);Assert.That(game.State.Position.Y,Is.EqualTo(0));Assert.That(game.State.Blocked(game.State.Position,.4f),Is.False);Assert.That(game.Worlds.ActivePlace.Root.gameObject.activeInHierarchy,Is.True);
        }
        static void ClickHud(CityGame game,string caption)
        {
            Button found=null;foreach(var button in game.Hud.GetComponentsInChildren<Button>())if(button.GetComponentInChildren<Text>().text==caption){found=button;break;}
            Assert.That(found,Is.Not.Null,"Missing visible button: "+caption);Assert.That(found.IsInteractable(),Is.True,"Disabled button: "+caption);Canvas.ForceUpdateCanvases();
            var rect=found.GetComponent<RectTransform>();var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.EqualTo(found),"Another UI element intercepts "+caption);
            ExecuteEvents.Execute(found.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void View(CityGame game,Vector3 offset,float height)
        {
            Vector3 p=game.Explorer.Root.parent.position;
            Camera.main.transform.position=p+game.Explorer.Root.parent.TransformDirection(offset);Camera.main.transform.LookAt(p+Vector3.up*height);
        }

        static void Capture(CityGame game, string name,int width=1920,int height=1200)
        {
            // Render the real game and HUD at an iPad landscape aspect for visual inspection.
            Camera camera = Camera.main;
            Canvas canvas = game.Hud.GetComponent<Canvas>();
            RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
            float oldDistance = canvas.planeDistance, oldAspect = camera.aspect;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                target.Create(); camera.targetTexture = target; camera.aspect = (float)width / height;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .4f;
                foreach(var label in Object.FindObjectsByType<WorldLabel>(FindObjectsSortMode.None)){label.RefreshVisibility(camera);label.GetComponent<TMPro.TMP_Text>().ForceMeshUpdate();}
                foreach(var text in canvas.GetComponentsInChildren<Text>())text.ForceMeshUpdate();
                foreach(var map in canvas.GetComponentsInChildren<CityMap>())map.SetVerticesDirty();
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
                Directory.CreateDirectory("Documentation/Screenshots");
                File.WriteAllBytes("Documentation/Screenshots/v6-" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                camera.targetTexture = oldTarget; camera.aspect = oldAspect; RenderTexture.active = oldActive;
                target.Release(); Object.Destroy(target); Object.Destroy(texture);
            }
        }
    }
}
