using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleCity
{
    public sealed class CityGame : MonoBehaviour
    {
        public Shader sunshineShader;
        public Shader glassShader;
        public Shader worldTextShader;
        public CityInteractions Interactions { get; private set; }
        public RideParking Parking { get; private set; }
        public AdventureWorlds Worlds { get; private set; }
        public MarineAdventure Marine { get; private set; }
        public CamperLife Camper {get;private set;}
        public SkateParkLife Skate;
        public GameSounds Sounds {get;private set;}
        public GraphicsQuality Graphics {get;private set;}
        public BuildingLift Lift;
        public BuildingLife Buildings { get; private set; }
        public AsgardHero Hero { get; private set; }
        Transform contactShadow;
        float zoomFactor=1;
        public float ZoomFactor => zoomFactor;
        public float CameraYaw => cameraYaw;
        public float CameraPitch => cameraPitch;
        public void OrbitCamera(float yaw,float pitch){cameraYaw=Mathf.Repeat(cameraYaw+yaw,360);cameraPitch=Mathf.Clamp(cameraPitch+pitch,5,70);lastLook=Time.time;if(Mathf.Abs(yaw)>.001f)manualViewVersion++;}
        public void RecenterCamera(){zoomFactor=1;SnapCamera();}
        public void Zoom(float steps){zoomFactor=Mathf.Clamp(zoomFactor-steps*.13f,.55f,2.2f);}
        public Simulation State { get; private set; }
        public bool Ready { get; private set; }
        public GameHUD Hud { get; private set; }
        Camera gameCamera;
        CharacterModel person;
        VehicleModel vehicle;
        Transform actor;
        readonly Dictionary<Ride, VehicleModel> parked = new Dictionary<Ride, VehicleModel>();
        readonly List<RenderTexture> portraits = new List<RenderTexture>();
        readonly List<RenderTexture> gummyPortraits = new List<RenderTexture>();
        readonly List<RenderTexture> glassPortraits = new List<RenderTexture>();
        float cameraYaw, cameraPitch = 23, lastLook, clock;
        Vector3 cameraVelocity;
        bool focused = true;
        bool loadingInBackground,previousBackground;
        readonly WalkingHeading walkHeading=new WalkingHeading();
        int manualViewVersion;
        public System.Func<KeyCode,bool> HeldKey=Input.GetKey;
        readonly KeyReleaseGate releaseGate=new KeyReleaseGate();
        bool ReadKey(KeyCode key)=>releaseGate.Read(key,HeldKey);
        int lastImpact;
        float pedalDistance;
        public CharacterModel Explorer => person;
        public ExplorerFlip Flip {get;private set;}
        public VehicleModel CurrentVehicle => vehicle;
        public void ShowOutdoorVehicles(bool show) { Parking?.Show(show); }

        IEnumerator Start()
        {
            // Finish the loading/portrait coroutines even if another app gains focus.
            // Restore normal background pausing once the graphics are ready.
            previousBackground=Application.runInBackground;loadingInBackground=true;Application.runInBackground=true;
            bool validateDevice = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--validate-ipad") >= 0
                || System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath, "validate-ipad.request"));
            State = new Simulation();State.SelectCharacter(PlayerPrefs.GetInt("LittleCity.Character",0));Flip=new ExplorerFlip(this);Buildings=new BuildingLife(this);Lift=new BuildingLift(this); Parking=new RideParking(this); Art.ToonShader = sunshineShader; Art.GlassShader = glassShader;
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60; Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            QualitySettings.vSyncCount = 0; QualitySettings.antiAliasing = Application.isMobilePlatform ? 2 : 4;
            Time.maximumDeltaTime = .1f;
            QualitySettings.shadowDistance = Application.isMobilePlatform ? 45 : 115; QualitySettings.shadowCascades = 2;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = Application.isMobilePlatform ? ShadowResolution.Medium : ShadowResolution.High;
            QualitySettings.pixelLightCount = 1;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Art.Hex("769C9C") * .34f;
            RenderSettings.fog = true; RenderSettings.fogColor = Art.Hex("7FADB7");
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 430;
            var sun = new GameObject("Warm morning sunshine").AddComponent<Light>(); sun.transform.SetParent(transform);
            sun.type = LightType.Directional; sun.color = Art.Hex("F3DDB8"); sun.intensity = 1.05f;
            sun.transform.rotation = Quaternion.Euler(32, -38, 0); sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .72f; sun.shadowBias = .025f; sun.shadowNormalBias = .25f;
            RenderSettings.sun = sun;
            gameCamera = new GameObject("Third person camera").AddComponent<Camera>();
            gameCamera.transform.SetParent(transform); gameCamera.tag = "MainCamera";
            gameCamera.clearFlags = CameraClearFlags.SolidColor; gameCamera.backgroundColor = RenderSettings.fogColor;
            gameCamera.nearClipPlane = .15f; gameCamera.farClipPlane = 520; gameCamera.fieldOfView = 61;
            gameCamera.cullingMask = ~(1 << 9); gameCamera.allowHDR = false;
            gameCamera.transform.position = new Vector3(30, 32, -58); gameCamera.transform.LookAt(new Vector3(0, 1, 0));
            var hudObject = new GameObject("Landscape touch interface"); hudObject.transform.SetParent(transform);
            Hud = hudObject.AddComponent<GameHUD>(); Hud.Build(State);
            Hud.CharacterSelected = SelectCharacter; Hud.RideSelected = SelectRide;
            Hud.InputCleared=()=>releaseGate.Clear(HeldKey);
            Hud.FloorRequested=floor=>Lift.Travel(floor);Hud.ResetRequested = ResetHome; Hud.StartRequested = SnapCamera;
            Hud.InteractRequested = Interact; Hud.TrickRequested = trick => State.StartTrick(trick);
            Hud.VehicleRequested=()=>Parking.EnterOrExit();Hud.ZoomRequested=Zoom;Hud.TravelRequested=index=>Worlds?.VisitWorld(index);
            Hud.HeroRequested=action=>{if(action==0)Hero.Toggle();else if(action==1)Hero.ThrowHammer();else if(action==2)Hero.Lightning();else if(action==3)Hero.Slam();else Hero.ResetTargets();};Hud.MarinaRequested=stop=>Worlds.VisitMarina(stop);Hud.RecenterRequested=RecenterCamera;Hud.RouteRequested=()=>Worlds.ResumeRoute();
            Hud.StandRequested = () => State.ToggleStanding(); Hud.CarRequested = style => State.ChooseCar(style);
            yield return null;
            var city = new CityWorld(State);
            yield return city.Build(transform);
            if (validateDevice || Application.platform == RuntimePlatform.IPhonePlayer) Debug.Log("LITTLE_CITY_WORLD_BUILT");
            Interactions = new CityInteractions(this, city.Doors, city.Root);
            foreach(var car in city.GarageCars)Parking.Register(car,0,new[]{"Blue Comet","Sunshine Roadster","Mint Mini"}[car.CarStyle]);
            Worlds=new AdventureWorlds(this,city);Skate=new SkateParkLife(this);Marine=new MarineAdventure(this);Hero=new AsgardHero(this);Camper=new CamperLife(this);
            actor = Art.Group("Player", transform);contactShadow=ContactShadows.Create(transform,Vector3.zero,Vector2.one); RebuildActor();
            CreateParkedVehicles(); Parking.RefreshVisibility();
            yield return MakePortraits();
            Sounds=gameObject.AddComponent<GameSounds>();Sounds.Initialize(this,gameCamera);
            Ready = true; Hud.Ready(); Hud.Refresh();
            Graphics=gameObject.AddComponent<GraphicsQuality>();Graphics.Initialize(this,gameCamera);Hud.GraphicsRequested=high=>Graphics.SetHigh(high);
            var atmosphere=gameObject.AddComponent<CityAtmosphere>();atmosphere.Initialize(this);Hud.NightRequested=atmosphere.ToggleNight;Hud.GlowRequested=atmosphere.CycleGlow;Hud.NitroFitRequested=atmosphere.ToggleNitro;
            while(Graphics.Busy)yield return null;FinishBackgroundLoading();
            if(!Application.isEditor)Debug.Log("LITTLE_CITY_STARTUP_READY cozy="+Graphics.Cozy+" livingGlass="+Graphics.LivingGlass);
            if (validateDevice || Application.platform == RuntimePlatform.IPhonePlayer) Debug.Log("LITTLE_CITY_READY repair=2 portraits=" + portraits.Count + " shader=" + Art.Mat(Art.Blue).shader.name + " supported=" + Art.Mat(Art.Blue).shader.isSupported);
            if(validateDevice)
                yield return StartupDiagnostics.Capture(this, portraits);
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--show-jungle")>=0)
                yield return ShowNewAdventure();
        }

        public IEnumerator ShowNewAdventure()
        {
            if(!Ready)yield break;
            Hud.BeginExploring();SelectRide(Ride.Airplane);State.FinishFlightArrival(false);Worlds.BeginRoute(1);
            while(Worlds.Busy)yield return null;
            State.Place(Simulation.RunwayStart);SelectRide(Ride.Walk);State.Place(Simulation.Home);RefreshPlayer();
            State.Say("Welcome to your jungle! J/L orbit · I/K camera up/down · P recenter. Press 5 to fly; E climbs, Q descends. Green gate = next world.");
        }
        public void SelectCharacter(int index)
        {
            Hero?.Disable();State.SelectCharacter(index);PlayerPrefs.SetInt("LittleCity.Character",State.Character);PlayerPrefs.Save(); if (actor != null) RebuildActor(); Hud.Refresh();
        }
        public void SelectRide(Ride mode)
        {
            if(State.Mode==Ride.Yacht){State.Say("Dock with F before changing rides.");return;}
            if (!Ready || Hud.MenuOpen || State.UsesRampPhysics && State.Airborne || State.Mode == mode || State.Indoors || State.Resting!=RestPose.None || Interactions.Busy) return;
            Point parkedPosition=State.Position;float parkedYaw=State.Yaw;Parking.ReleaseForChange();State.SwitchRide(mode);if(State.WorldIndex==4 && mode!=Ride.Airplane)State.Place(parkedPosition,parkedYaw); Parking.QuickChange(); RebuildActor(); Hud.ClearInput(); SnapCamera(); Hud.Refresh();
        }
        public void ResetHome()
        {
            if (!Ready) return;
            if(State.WorldIndex==4 && !State.Indoors){State.RetryPark();RefreshPlayer();return;}
            Worlds?.Voyages.Reset();Camper?.Leave();Buildings?.Leave();Worlds?.Yachts.Leave(); Interactions?.Leave(); State.Reset(); Parking.QuickChange(); RebuildActor(); Hud.ClearInput(); SnapCamera(); Hud.Refresh();
        }
        void RebuildActor()
        {
            if (person != null) { person.Root.gameObject.SetActive(false); Destroy(person.Root.gameObject); }
            if (vehicle != null) { vehicle.Root.gameObject.SetActive(false); Destroy(vehicle.Root.gameObject); vehicle = null; }
            if (State.Mode != Ride.Walk) vehicle = Models.Vehicle(actor, State.Mode, State.CarStyle);
            person = Models.Character(actor, State.Character);person.Gummy.EnablePlayerGlow();Hero?.Dress(person);
            person.Root.localScale = Vector3.one * State.WalkingScale;
            if (vehicle != null)
            {
                vehicle.Root.GetComponent<CarDamageVisual>()?.Set(State.CarDents);
                person.Root.localScale = Vector3.one * (State.IsLargeVehicle||State.Mode==Ride.Helicopter?.62f:State.Mode == Ride.DirtBike ? .98f : State.Mode == Ride.Airplane ? .7f : State.Mode == Ride.RaceCar ? .4f : State.Mode == Ride.Bicycle ? .70f : .70f);
                person.Root.localPosition = vehicle.Seat;
                // The car occupant is seated below the roof and visible through the glass.
            }
            Parking?.RefreshVisibility();
            UpdateActor(0);Worlds?.Islands?.ApplyAccessory();
        }
        void CreateParkedVehicles()
        {
            Ride[] modes = { Ride.RaceCar, Ride.Bicycle, Ride.Motorcycle, Ride.Airplane, Ride.Helicopter };
            Vector3[] places = { new Vector3(-16, 0, -24), new Vector3(8, 0, -23), new Vector3(17, 0, -24), new Vector3(172, 0, -130),new Vector3(143,0,-76) };
            for (int i = 0; i < modes.Length; i++)
            {
                VehicleModel model = Models.Vehicle(transform, modes[i]); model.Root.position = places[i];
                model.Root.rotation = Quaternion.Euler(0, i >= 3 ? 0 : 90, 0); parked[modes[i]] = model; Parking.Register(model,0,Simulation.RideNames[(int)modes[i]]);
            }
            var pad=Art.Group("Helicopter landing pad",Worlds.ActivePlace.Root,new Vector3(143,.065f,-76));Art.Disk("Helipad surface",pad,Vector3.zero,7,.04f,Art.Hex("42696D"));
            foreach(int side in new[]{-1,1})Art.Box("Helipad H upright",pad,new Vector3(side*1.2f,.026f,0),new Vector3(.40f,.016f,4.2f),Art.White,0);
            Art.Box("Helipad H crossbar",pad,new Vector3(0,.026f,0),new Vector3(2.4f,.016f,.40f),Art.White,0);
        }

        public AmbientPeople People {get;private set;}
        IEnumerator MakePortraits()
        {
            var cam = new GameObject("Character portrait studio").AddComponent<Camera>();
            cam.enabled = false; cam.cullingMask = 1 << 9;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.9f, .95f, .9f, 0);
            var portraitLight = new GameObject("Portrait soft light").AddComponent<Light>();
            portraitLight.type=LightType.Directional;portraitLight.cullingMask=1<<9;portraitLight.intensity=.9f;portraitLight.shadows=LightShadows.None;portraitLight.transform.rotation=Quaternion.Euler(22,180,0);
            cam.orthographic = true; cam.orthographicSize = 1.8f; cam.nearClipPlane = .1f; cam.farClipPlane = 20;
            cam.transform.position = new Vector3(1002.3f, 2.35f, 5.2f); cam.transform.LookAt(new Vector3(1000, 1.65f, 0));
            for (int i = 0; i < Simulation.CharacterNames.Length; i++)
            {
                var model = Models.Character(transform, i); model.Root.position = new Vector3(1000, 0, 0);
                model.Root.localScale=Vector3.one*(i==4?.94f/.66f:i==3?.82f/.66f:1);
                foreach (Transform t in model.Root.GetComponentsInChildren<Transform>()) t.gameObject.layer = 9;
                model.Pose(0, 0, false, false);
                var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = Simulation.CharacterNames[i] + " portrait", antiAliasing = 2 };
                rt.Create(); portraits.Add(rt);
                bool fog = RenderSettings.fog; RenderSettings.fog = false;
                model.Gummy.SetStyle(false);
                cam.targetTexture = rt;cam.Render();
                var soft = new RenderTexture(512,512,16,RenderTextureFormat.ARGB32){name=Simulation.CharacterNames[i]+" bubbly portrait",antiAliasing=2};soft.Create();gummyPortraits.Add(soft);
                model.Gummy.SetLivingGlass(false);model.Gummy.SetStyle(true);model.Gummy.Draw(0);cam.targetTexture=soft;cam.Render();
                var glassy=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32){name=Simulation.CharacterNames[i]+" living glass portrait",antiAliasing=2};glassy.Create();glassPortraits.Add(glassy);
                model.Gummy.SetLivingGlass(true);cam.targetTexture=glassy;cam.Render();RenderSettings.fog=fog;
                Hud.SetPortrait(i, GummyCharacter.GlassSelected?glassy:GummyCharacter.Selected?soft:rt); model.Root.gameObject.SetActive(false); Destroy(model.Root.gameObject);
                yield return null;
            }
            cam.targetTexture = null; Destroy(cam.gameObject);Destroy(portraitLight.gameObject);
            People=new GameObject("Neighborhood visitors").AddComponent<AmbientPeople>();People.transform.SetParent(transform,false);People.Initialize(this);
        }

        public void RefreshPortraitStyle()
        {
            var selected=Graphics!=null && Graphics.LivingGlass?glassPortraits:Graphics!=null && Graphics.Cozy?gummyPortraits:portraits;
            for(int i=0;i<selected.Count;i++)Hud.SetPortrait(i,selected[i]);
        }

        void Update()
        {
            if (!Ready || !focused) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); clock += dt;
            releaseGate.Poll(HeldKey);
            if (Input.GetKeyDown(KeyCode.H)) Hud.ShowHelp();
            if(Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape)){if(Hud.MenuOpen)Hud.BeginExploring();else Hud.ShowExplore(true);}
            if (Input.GetKeyDown(KeyCode.R)) ResetHome();
            if (!Hud.MenuOpen)
            {
                for (int i = 0; i < 5; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) SelectRide((Ride)i);
                if (Input.GetKeyDown(KeyCode.Alpha6)) SelectRide(Ride.Skateboard);
                if (Input.GetKeyDown(KeyCode.Alpha7)) SelectRide(Ride.Scooter);
                if (Input.GetKeyDown(KeyCode.Alpha8)) SelectRide(Ride.MountainBike);
                if (Input.GetKeyDown(KeyCode.Alpha9)) SelectRide(Ride.MiniBike);
                if (Input.GetKeyDown(KeyCode.C)) Hud.ShowCharacters();
                if (Input.GetKeyDown(KeyCode.F)) Interact();
                if (Input.GetKeyDown(KeyCode.G)) Parking.EnterOrExit();
                if (Input.GetKeyDown(KeyCode.U)) Hero.Toggle();
                if (Input.GetKeyDown(KeyCode.Y)) Hero.ThrowHammer();
                if (Input.GetKeyDown(KeyCode.O)) Hero.Lightning();
                if (Input.GetKeyDown(KeyCode.N)) Hero.Slam();
                if (Input.GetKeyDown(KeyCode.Alpha0) && Hero.Available) Hero.ResetTargets();
                if (Input.GetKeyDown(KeyCode.P)) RecenterCamera();
                if (Input.GetKeyDown(KeyCode.M)) Hud.ShowMarina();
                if (Input.GetKeyDown(KeyCode.T)) Hud.ShowTravel(true);
                if (Mathf.Abs(Input.mouseScrollDelta.y)>.01f)Zoom(Input.mouseScrollDelta.y);
                if (Input.GetKeyDown(KeyCode.X)) State.StartTrick(BikeTrick.Endo);
                if (Input.GetKeyDown(KeyCode.V)) State.StartTrick(BikeTrick.Spin180);
                if (Input.GetKeyDown(KeyCode.B)) State.ToggleStanding();
            }
            Hud.ReconcilePointers();Flip.Input(ReadKey(KeyCode.Space)||Hud.Jump.Held,dt);
            if (!Hud.MenuOpen && (Graphics==null || !Graphics.Busy))
            {
                ProcessControls(dt);
            }
            Worlds.Voyages.Tick();Worlds.Islands.Tick(dt);Lift.Tick(dt);UpdateActor(dt);Hero.Tick(dt);Worlds.Yachts.Tick();
            string adventure=Worlds.Voyages.Context;if(string.IsNullOrEmpty(adventure))adventure=Worlds.Islands.Context;if(string.IsNullOrEmpty(adventure))adventure=Camper.Context;if(string.IsNullOrEmpty(adventure))adventure=Hero.Context;if(string.IsNullOrEmpty(adventure))adventure=Marine.Context;if(string.IsNullOrEmpty(adventure))adventure=Worlds.Yachts.Context;
            Hud.SetContext(string.IsNullOrEmpty(adventure)?Interactions.Context:adventure, Interactions.InGarage);
            Hud.SetHeroStatus(Hero.Available && !State.Indoors,Hero.Active,Hero.BrokenCount,Hero.TargetCount,Hero.HammerCooldown,Hero.LightningCooldown,Hero.SlamCooldown);Hud.SetRouteContext(Worlds.RouteCaption);Hud.SetVehicleContext(Parking.Context,State.Mode!=Ride.Walk && !State.Airborne || Parking.Nearest!=null);Hud.Refresh();
        }

        public void ProcessControls(float dt)
        {
                Hud.ReconcilePointers();releaseGate.Poll(HeldKey);
                if(!focused || Hud.MenuOpen)return;
                // Never replay a long loading/rendering stall as a large input jump.
                dt = Mathf.Clamp(dt, 0, .1f);
                if(Lift.Moving)return;
                Vector2 look = Hud.Look.Consume();
                if (Input.GetMouseButton(1)) look += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 15;
                if (look.sqrMagnitude > .01f)
                {
                    OrbitCamera(look.x*.14f,-look.y*.12f);
                }
                float orbit=(ReadKey(KeyCode.L)||Hud.LookRight.Held?1:0)-(ReadKey(KeyCode.J)||Hud.LookLeft.Held?1:0);
                float pitch=(ReadKey(KeyCode.I)||Hud.LookUp.Held?1:0)-(ReadKey(KeyCode.K)||Hud.LookDown.Held?1:0);
                if(orbit!=0 || pitch!=0)OrbitCamera(orbit*75*dt,pitch*45*dt);
                if(ReadKey(KeyCode.Equals)||ReadKey(KeyCode.KeypadPlus)||Hud.ZoomIn.Held)Zoom(dt*4);
                if(ReadKey(KeyCode.Minus)||ReadKey(KeyCode.KeypadMinus)||Hud.ZoomOut.Held)Zoom(-dt*4);
                float steer = Hud.Stick.Value.x, forward = Hud.Stick.Value.y;
                if (ReadKey(KeyCode.A) || ReadKey(KeyCode.LeftArrow)) steer -= 1;
                if (ReadKey(KeyCode.D) || ReadKey(KeyCode.RightArrow)) steer += 1;
                if (ReadKey(KeyCode.W) || ReadKey(KeyCode.UpArrow)) forward += 1;
                if (ReadKey(KeyCode.S) || ReadKey(KeyCode.DownArrow)) forward -= 1;
                bool go = Hud.Go.Held, brake = Hud.Stop.Held || State.Mode != Ride.Walk && !State.IsJumpRide && ReadKey(KeyCode.Space) || State.IsJumpRide && ReadKey(KeyCode.LeftControl);
                if (go && (State.Mode != Ride.Walk || Mathf.Abs(forward) + Mathf.Abs(steer) < .1f)) forward = 1;
                float movementYaw=walkHeading.Resolve(steer,forward,gameCamera.transform.eulerAngles.y,cameraYaw,manualViewVersion);
                var input = new DriveInput { Steering = steer, Forward = forward, CameraYaw = (State.Mode == Ride.Walk ? movementYaw : cameraYaw) * Mathf.Deg2Rad,
                    Jump = Hud.Jump.Held || ReadKey(KeyCode.Space), Drift = Hud.Drift.Held || ReadKey(KeyCode.LeftShift),Nitro=Hud.Nitro.Held||ReadKey(KeyCode.LeftAlt)||ReadKey(KeyCode.RightAlt),
                    AirLoop=ReadKey(KeyCode.X)||Hud.AirLoop.Held,AirRoll=ReadKey(KeyCode.Z)||Hud.AirRoll.Held,AirTurn=ReadKey(KeyCode.V)||Hud.AirTurn.Held,
                    Sprint = go || ReadKey(KeyCode.LeftShift), Brake = brake, Wheelie = ReadKey(KeyCode.Z) || Hud.Wheelie.Held,
                    Rise = Hud.Rise.Held || ReadKey(KeyCode.E), Descend = Hud.Descend.Held || ReadKey(KeyCode.Q) };
                if(Flip.Active)input=new DriveInput();
                Point before=State.Position;State.Step(dt, input);Buildings.TickBikeDoor();Worlds.CheckFlight(before);Skate?.Tick();
                if (State.Mode != Ride.Walk && Mathf.Abs(State.Speed) > .1f && Time.time - lastLook > .7f)
                    cameraYaw = Mathf.LerpAngle(cameraYaw, State.Yaw * Mathf.Rad2Deg, 1-Mathf.Exp(-dt * 5));
                if (State.ImpactVersion != lastImpact)
                {
                    lastImpact = State.ImpactVersion;
                    if (State.NeedsRepair && vehicle != null) CrashParts.Scatter(vehicle, State.ImpactSpeed);
                }
        }

        void UpdateActor(float dt)
        {
            if (actor == null || person == null) return;
            Point p = State.Position;
            if(contactShadow!=null)
            {
                contactShadow.gameObject.SetActive(!State.IsBoat && !State.IsAircraft && p.Y-State.GroundHeight(p)<1.8f);
                contactShadow.position=new Vector3(p.X,State.GroundHeight(p)+.12f,p.Z);
                float scale=State.IsLargeVehicle?4.3f:State.Mode==Ride.RaceCar?3.6f:State.Mode==Ride.Walk?1.1f:1.6f;
                contactShadow.localScale=new Vector3(scale,1,scale*(State.Mode==Ride.Walk?1:1.5f));
            }
            float trick = Mathf.Sin(State.TrickProgress * Mathf.PI);
            float extraPitch = State.Trick == BikeTrick.Wheelie ? -31 * trick : State.Trick == BikeTrick.Endo ? 26 * trick : 0;
            float lift = State.Trick == BikeTrick.Spin180 ? .85f * trick : Mathf.Abs(Mathf.Sin(extraPitch*Mathf.Deg2Rad)) * .7f + .4f*(1-Mathf.Cos(extraPitch*Mathf.Deg2Rad));
            actor.position = new Vector3(p.X, p.Y + .04f + State.BounceHeight + lift, p.Z);
            actor.rotation = State.Swimming ? Quaternion.Euler(65,State.Yaw*Mathf.Rad2Deg,0) : State.Resting==RestPose.Sleeping ? Quaternion.Euler(-90,State.Yaw*Mathf.Rad2Deg,0) : Quaternion.Euler(State.Pitch * Mathf.Rad2Deg + extraPitch, State.Yaw * Mathf.Rad2Deg, State.Bank * Mathf.Rad2Deg);
            person.Pose(clock, Hud.MenuOpen ? 0 : State.Speed, State.Mode != Ride.Walk || State.Resting==RestPose.Sitting, State.Mode == Ride.Bicycle);
            if(State.Mode==Ride.Walk){person.Root.localScale=Vector3.one*State.WalkingScale;person.Root.localPosition=Vector3.zero;person.Root.localRotation=Quaternion.identity;Flip.Apply(person);}
            person.SetSleeping(State.Resting==RestPose.Sleeping);if(State.Swimming){person.Swim(clock,State.Speed);actor.position-=Vector3.up*.2f;}
            if (vehicle != null)
            {
                vehicle.Root.GetComponent<CarDamageVisual>()?.Set(State.CarDents);
                float traveled = State.DistanceTravelled - pedalDistance; pedalDistance = State.DistanceTravelled;
                vehicle.Standing = State.Standing;vehicle.SuspensionCompression=State.LandingCompression;
                vehicle.Animate(dt > 0 ? traveled / dt * Mathf.Sign(State.Speed) : 0, dt, State.Throttle);
                if (State.Mode != Ride.Airplane) person.FitRide(vehicle, State.IsBicycle && State.Standing);
            }
            person.Gummy?.Motion(State.Grounded,State.VerticalSpeed,State.Resting==RestPose.Sleeping,State.Swimming);
            if(dt==0)person.Gummy?.Draw(0);
        }
        void LateUpdate()
        {
            if (!Ready) return;
            if(Hud.MenuOpen && !Hud.ChoosingCharacter)return;
            if (Hud.ChoosingCharacter)
            {
                Vector3 target = new Vector3(0, 2, 0);
                gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, new Vector3(32, 30, -51), Time.unscaledDeltaTime * 1.5f);
                gameCamera.transform.rotation = Quaternion.Slerp(gameCamera.transform.rotation, Quaternion.LookRotation(target - gameCamera.transform.position), Time.unscaledDeltaTime * 1.5f);
                return;
            }
            PositionCamera(false);
        }
        public void Interact() { if(!Ready || Hud.MenuOpen)return;if(Worlds.Voyages.Use() || Worlds.Islands.Use() || Camper.Use() || Buildings.Use() || Hero.Use() || Marine.Use() || Worlds.Yachts.Use())return;StartCoroutine(Interactions.Use()); }
        public void RefreshDriverPose(){UpdateActor(0);}
        public void RefreshPlayer(){RebuildActor();Hud.ClearInput();SnapCamera();Hud.Refresh();}
        public void RepairCar() { State.Repair(); RebuildActor(); }
        public void SnapCamera()
        {
            Physics.SyncTransforms();
            cameraYaw = State.Yaw * Mathf.Rad2Deg; cameraPitch = State.Indoors ? 13 : State.Mode == Ride.Airplane ? 16 : 16; walkHeading.Clear();manualViewVersion++;
            lastLook = -10; cameraVelocity = Vector3.zero; PositionCamera(true);
        }
        void PositionCamera(bool instant)
        {
            if (actor == null) return;
            float flightHeight=State.IsAircraft?Mathf.Max(0,State.Position.Y):0;
            bool high=Graphics!=null;
            gameCamera.farClipPlane=(high?900:520)+flightHeight*1.5f;
            RenderSettings.fogStartDistance=(high?100:160)+flightHeight*.6f;
            RenderSettings.fogEndDistance=(high?600:430)+flightHeight*1.8f;
            if(Lift.FrameCamera(gameCamera))return;
            if(Camper.FrameCamera(gameCamera))return;
            Vector3 target = actor.position + Vector3.up * (State.Mode==Ride.Yacht?7f:State.IsLargeVehicle?1.9f:State.Mode == Ride.DirtBike ? 2.1f : State.IsAircraft ? 2.3f : State.Resting==RestPose.Sleeping ? .25f : State.Mode == Ride.Walk ? (State.Character==4?1.85f:State.IsRider?State.WalkingScale*1.73f:1.15f) : 1.05f);
            float distance = State.Indoors ? (Buildings.Inside?4.5f:3.3f) : State.Mode==Ride.Yacht?110f:State.IsLargeVehicle?10.6f:State.IsAircraft ? 16 : State.IsBoat ? 13 : State.Mode == Ride.RaceCar ? 6.4f : State.IsJumpRide?7.0f:4.6f;
            if(State.Mode==Ride.Walk && State.IsRider && !State.Indoors && State.Resting==RestPose.None)distance*=1.5f;
            distance*=zoomFactor;
            gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, State.IsAircraft ? 64 : 61 + Mathf.Min(5,State.ActualSpeed*.18f), .08f);
            float viewYaw=cameraYaw,viewPitch=cameraPitch;
            if(Buildings.Inside && State.CanRideInside && Time.time-lastLook>.7f)viewPitch=8;
            if(Buildings.Inside && State.CanRideInside && Time.time-lastLook>.7f)
            {
                var plan=Buildings.Active.Plan;var local=plan.Local(State.Position);
                // Stay in the stairwell and follow its slope. A room-side camera sees
                // the rider through a floor edge; a horizontal chase hits the next flight.
                if(Mathf.Abs(local.X-plan.StairX)<1.8f && local.Z>plan.StairStart-.6f && local.Z<plan.StairEnd+.7f){viewYaw=plan.Yaw*Mathf.Rad2Deg;viewPitch=-Mathf.Atan2(plan.Story,plan.StairEnd-plan.StairStart)*Mathf.Rad2Deg;distance=Mathf.Min(distance,4.2f);}
            }
            Vector3 offset = Quaternion.Euler(viewPitch, viewYaw, 0) * new Vector3(0, 0, -distance);
            // At a tight elevator landing, start from the open room side so the camera
            // can show the whole character instead of being squeezed into their head.
            if(instant && Buildings.Inside && Physics.SphereCast(target,.32f,offset.normalized,out RaycastHit closeWall,distance,1<<8,QueryTriggerInteraction.Ignore) && closeWall.distance<2)
            {
                Vector3 roomSide=Quaternion.Euler(cameraPitch,cameraYaw+180,0)*new Vector3(0,0,-distance);
                float clearance=Physics.SphereCast(target,.32f,roomSide.normalized,out RaycastHit roomWall,distance,1<<8,QueryTriggerInteraction.Ignore)?roomWall.distance:distance;
                if(clearance>closeWall.distance+1){cameraYaw=Mathf.Repeat(cameraYaw+180,360);offset=roomSide;}
            }
            Vector3 desired = target + offset;
            if (Physics.SphereCast(target, .32f, offset.normalized, out RaycastHit hit, distance, 1 << 8, QueryTriggerInteraction.Ignore))
                desired = target + offset.normalized * Mathf.Max(State.Indoors?.35f:.8f, hit.distance - .25f);
            desired.y = Mathf.Max(State.Swimming?-.1f:.65f, desired.y);
            if(Buildings.Inside && State.CanRideInside){var plan=Buildings.Active.Plan;var local=plan.Local(new Point(desired.x,desired.y,desired.z));local.X=Mathf.Clamp(local.X,-plan.Width/2+.45f,plan.Width/2-.45f);local.Z=Mathf.Clamp(local.Z,-plan.Depth/2+.45f,plan.Depth/2-.45f);var inside=plan.World(local);desired=new Vector3(inside.X,inside.Y,inside.Z);}
            gameCamera.transform.position = instant ? desired : Vector3.SmoothDamp(gameCamera.transform.position, desired, ref cameraVelocity, .065f);
            gameCamera.transform.LookAt(target);
        }
        void OnApplicationFocus(bool focus) { focused = focus; walkHeading.Clear();if(!focus)Flip?.Cancel();if (Hud != null) Hud.ClearInput(); }
        void OnApplicationPause(bool paused) { focused = !paused; walkHeading.Clear();if(paused)Flip?.Cancel();if (Hud != null) Hud.ClearInput(); }
        void FinishBackgroundLoading(){if(loadingInBackground){Application.runInBackground=previousBackground;loadingInBackground=false;}}
        void OnDestroy()
        {
            FinishBackgroundLoading();
            foreach (var rt in portraits) { rt.Release(); Destroy(rt); }
            foreach (var rt in gummyPortraits) { rt.Release(); Destroy(rt); }
            foreach (var rt in glassPortraits) { rt.Release(); Destroy(rt); }
            Art.Release(); Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }
}
