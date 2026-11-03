using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TextMeshProUGUI;

namespace LittleCity
{
    public sealed class GameHUD : MonoBehaviour
    {
        public TouchStick Stick { get; private set; }
        public HoldAction Go { get; private set; }
        public HoldAction Stop { get; private set; }
        public HoldAction Rise { get; private set; }
        public HoldAction Descend { get; private set; }
        public HoldAction Jump { get; private set; }
        public HoldAction Drift { get; private set; }
        public HoldAction Wheelie { get; private set; }
        public HoldAction AirLoop,AirRoll,AirTurn;RectTransform airTricks;
        public HoldAction Nitro {get;private set;}public Action NightRequested,GlowRequested,NitroFitRequested;Button nightButton,glowButton,nitroFit;RectTransform carExtras;Text nitroStatus;
        public Action InteractRequested, StandRequested, VehicleRequested, RecenterRequested, RouteRequested;
        public HoldAction LookLeft,LookRight,LookUp,LookDown,ZoomIn,ZoomOut;
        Text routeCaption,journeyText;RectTransform journey;
        public Action<int> TravelRequested,MarinaRequested,HeroRequested;RectTransform heroPanel;Text heroScore;Button heroToggle;readonly Button[] powers=new Button[4];RectTransform marina;
        public Action<float> ZoomRequested;
        RectTransform travel;
        Text travelStatus, mapLabel;
        readonly Button[] destinations=new Button[4];
        readonly Image[] parkRides=new Image[4];
        Button parkRetry;
        Button soundButton;
        public Action<bool> GraphicsRequested;
        Button lowDef,highDef,originalStyle,cozyStyle,glassStyle;
        Button vehicleAction;
        public Action<BikeTrick> TrickRequested;
        public Action<int> CarRequested;
        RectTransform tricks, garageChoices;
        Button interaction;
        Text contextText;
        float displayedSpeed;
        public LookGesture Look { get; private set; }
        public bool MenuOpen { get; private set; } = true;
        public Action<int> CharacterSelected;
        public Action InputCleared;
        public Action<Ride> RideSelected;
        public Action ResetRequested, StartRequested;
        RectTransform safe, selection, help, bottom, flight;
        Text district, speed, hint, notice, status, startLabel, characterLabel, flightInfo;
        Button start;
        RawImage[] portraits = new RawImage[Simulation.CharacterNames.Length];
        Image[] choices = new Image[Simulation.CharacterNames.Length], rides = new Image[5];
        readonly Color navy = Art.Hex("223B53"), paper = Art.Hex("FFF9E9");
        public Action<int> FloorRequested;
        public TMP_InputField FloorInput { get; private set; }
        RectTransform elevator;Text floorTitle,floorRange,floorError;int floorCount;
        public bool ElevatorOpen=>elevator!=null && elevator.gameObject.activeSelf;
        TMP_FontAsset font;
        Sprite rounded;
        int lastNotice = -1;
        float noticeUntil;
        Simulation sim;
        RectTransform explore, cameraPanel, touchLayer, mapPanel, actionStack, telemetry;
        Button touchToggle;
        bool touchControls, heroActive;
        public bool TouchControlsVisible => touchControls;
        public bool ExploreOpen => explore != null && explore.gameObject.activeSelf;
        public bool ControlsOpen => cameraPanel != null && cameraPanel.gameObject.activeSelf;
        public bool ChoosingCharacter => selection != null && selection.gameObject.activeSelf;
        public Button VehicleButton => vehicleAction;
        public Button InteractionButton => interaction;
        readonly Color glass = new Color(.06f,.14f,.18f,.78f);


        public void Build(Simulation simulation)
        {
            sim = simulation;
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            rounded = MakeRoundedSprite();
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1366, 900); scaler.matchWidthOrHeight = .65f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Touch event system", typeof(EventSystem), typeof(StandaloneInputModule));
            safe = Rect("Safe area", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safe.gameObject.AddComponent<SafeArea>();
            var look = Panel("Drag to look", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
            Look = look.gameObject.AddComponent<LookGesture>();
            bottom = Rect("Play controls", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var brand = Panel("Location", bottom, new Vector2(0,1), new Vector2(0,1), new Vector2(18,-72),new Vector2(285,54),glass);
            Label("LITTLE CITY",brand,new Vector2(12,32),new Vector2(260,16),11,new Color(.75f,.9f,.9f),FontStyle.Bold);
            district=Label("Willow Park",brand,new Vector2(12,8),new Vector2(260,25),17,Color.white);district.enableAutoSizing=true;district.fontSizeMin=13;district.fontSizeMax=17;
            var utility=Rect("Navigation",bottom,Vector2.one,Vector2.one,new Vector2(-257,-54),new Vector2(239,36));
            Chip("Explore · Tab",utility,Vector2.zero,new Vector2(119,36),()=>ShowExplore(!ExploreOpen));
            Chip("Controls",utility,new Vector2(127,0),new Vector2(112,36),()=>ShowControls(!ControlsOpen));
            mapPanel=Panel("Map",bottom,Vector2.one,Vector2.one,new Vector2(-158,-199),new Vector2(140,132),glass);
            var map=Rect("City map",mapPanel,Vector2.zero,Vector2.zero,new Vector2(7,25),new Vector2(126,100));
            var cityMap=map.gameObject.AddComponent<CityMap>();cityMap.Simulation=sim;cityMap.raycastTarget=false;
            mapLabel=Label("N ↑",mapPanel,new Vector2(8,3),new Vector2(125,19),10,Color.white);
            notice=Label("",bottom,Vector2.zero,new Vector2(355,54),14,Color.white);
            var nr=notice.rectTransform;nr.anchorMin=nr.anchorMax=new Vector2(0,1);nr.anchoredPosition=new Vector2(28,-134);
            notice.gameObject.AddComponent<Shadow>().effectColor=new Color(0,0,0,.65f);
            telemetry=Panel("Speed and altitude",bottom,new Vector2(1,0),new Vector2(1,0),new Vector2(-130,18),new Vector2(112,54),glass);
            speed=Label("0 km/h",telemetry,new Vector2(12,26),new Vector2(90,22),16,Color.white);
            flightInfo=Label("",telemetry,new Vector2(12,5),new Vector2(90,18),11,new Color(.75f,.9f,.9f));
            actionStack=Rect("Nearby actions",bottom,Vector2.zero,Vector2.zero,new Vector2(18,18),new Vector2(365,92));
            interaction=Chip("",actionStack,Vector2.zero,new Vector2(365,38),()=>InteractRequested?.Invoke());contextText=interaction.GetComponentInChildren<Text>();
            vehicleAction=Chip("",actionStack,new Vector2(0,44),new Vector2(365,38),()=>VehicleRequested?.Invoke());
            garageChoices=Rect("Choose your garage car",bottom,Vector2.zero,Vector2.zero,new Vector2(18,117),new Vector2(365,40));
            string[] carNames={"Comet","Roadster","Mini"};for(int i=0;i<3;i++){int style=i;Chip(carNames[i],garageChoices,new Vector2(i*123,0),new Vector2(117,38),()=>CarRequested?.Invoke(style));}

            touchLayer=Rect("Optional touch controls",bottom,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var stick=Panel("Move joystick",touchLayer,Vector2.zero,Vector2.zero,new Vector2(24,24),new Vector2(142,142),new Color(.06f,.14f,.18f,.28f));
            Stick=stick.gameObject.AddComponent<TouchStick>();var knob=Panel("Thumb",stick,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(56,56),new Color(1,1,1,.55f));knob.pivot=new Vector2(.5f,.5f);knob.anchoredPosition=Vector2.zero;knob.GetComponent<Image>().raycastTarget=false;Stick.Knob=knob;
            var pedals=Rect("Go and stop",touchLayer,new Vector2(1,0),new Vector2(1,0),new Vector2(-184,24),new Vector2(166,150));
            Go=Hold("GO",pedals,new Vector2(84,0),new Vector2(82,82),new Color(.1f,.6f,.57f,.65f),18);
            Stop=Hold("STOP",pedals,Vector2.zero,new Vector2(72,64),new Color(.8f,.3f,.3f,.65f),15);
            flight=Rect("Flight controls",pedals,Vector2.zero,Vector2.zero,new Vector2(0,90),new Vector2(166,50));
            Rise=Hold("UP\nE",flight,new Vector2(84,0),new Vector2(82,50),glass,14);Descend=Hold("DOWN\nQ",flight,Vector2.zero,new Vector2(72,50),glass,14);
            Jump=Hold("JUMP",pedals,new Vector2(84,90),new Vector2(82,50),glass,15);Drift=Hold("DRIFT",pedals,new Vector2(84,90),new Vector2(82,50),glass,15);
            Nitro=Hold("NOS",pedals,new Vector2(0,90),new Vector2(72,50),glass,15);
            touchControls=Application.isMobilePlatform;touchLayer.gameObject.SetActive(touchControls);

            explore=Panel("Explore menu",safe,Vector2.one,Vector2.one,new Vector2(-436,-603),new Vector2(418,536),paper);
            Label("Where next?",explore,new Vector2(20,481),new Vector2(345,36),25,navy,FontStyle.Bold);
            Button("×",explore,new Vector2(368,484),new Vector2(30,32),()=>ShowExplore(false),24);
            Button("Marina · boats & yachts   M",explore,new Vector2(20,423),new Vector2(378,45),ShowMarina,18);
            Button("Worlds & saved sky route   T",explore,new Vector2(20,370),new Vector2(378,45),()=>ShowTravel(true),18);
            var dock=Panel("Choose a ride",explore,Vector2.zero,Vector2.zero,new Vector2(20,203),new Vector2(378,153),new Color(.9f,.92f,.87f));
            Label("Your ride",dock,new Vector2(10,126),new Vector2(355,24),16,navy);
            string[] labels={"Walk","Car","Bicycle","Moto","Plane"};
            for(int i=0;i<5;i++){int index=i;var btn=Button((i+1)+"\n"+labels[i],dock,new Vector2(6+i*74,60),new Vector2(70,61),()=>{BeginExploring();RideSelected?.Invoke((Ride)index);},14);rides[i]=btn.GetComponent<Image>();}
            Ride[] extraRides={Ride.Skateboard,Ride.Scooter,Ride.MountainBike,Ride.MiniBike};string[] extraNames={"6 Board","7 Scooter","8 MTB","9 Mini"};
            for(int i=0;i<4;i++){int n=i;var b=Button(extraNames[i],dock,new Vector2(6+i*92,9),new Vector2(88,43),()=>{BeginExploring();RideSelected?.Invoke(extraRides[n]);},13);parkRides[i]=b.GetComponent<Image>();}
            Button("Skatepark · jumps & rides",explore,new Vector2(20,151),new Vector2(378,42),()=>TravelRequested?.Invoke(4),17);
            parkRetry=Chip("Retry · R",bottom,new Vector2(-55,18),new Vector2(110,36),()=>ResetRequested?.Invoke(),14);
            var retryRect=(RectTransform)parkRetry.transform;retryRect.anchorMin=retryRect.anchorMax=new Vector2(.5f,0);parkRetry.gameObject.SetActive(false);
            Button("Home · R",explore,new Vector2(20,105),new Vector2(183,42),()=>{ResetRequested?.Invoke();BeginExploring();},16);
            Button("People · C",explore,new Vector2(211,105),new Vector2(187,42),ShowCharacters,16);
            routeCaption=Label("",explore,new Vector2(20,20),new Vector2(378,68),13,navy);
            foreach(Transform child in explore)if(child is RectTransform childRect)childRect.anchoredPosition+=Vector2.up*50;
            explore.sizeDelta+=Vector2.up*50;explore.anchoredPosition-=Vector2.up*50;
            Button("Dirt bike",explore,new Vector2(20,18),new Vector2(90,42),()=>{BeginExploring();RideSelected?.Invoke(Ride.DirtBike);},14);
            Button("Van",explore,new Vector2(116,18),new Vector2(90,42),()=>{BeginExploring();RideSelected?.Invoke(Ride.Van);},14);
            Button("Camper RV",explore,new Vector2(212,18),new Vector2(90,42),()=>{BeginExploring();RideSelected?.Invoke(Ride.Camper);},14);
            Button("Helicopter",explore,new Vector2(308,18),new Vector2(90,42),()=>{BeginExploring();RideSelected?.Invoke(Ride.Helicopter);},14);

            cameraPanel=Panel("Controls drawer",bottom,Vector2.one,Vector2.one,new Vector2(-378,-541),new Vector2(360,474),paper);
            Label("Controls",cameraPanel,new Vector2(18,428),new Vector2(290,30),23,navy,FontStyle.Bold);
            Button("×",cameraPanel,new Vector2(313,427),new Vector2(30,32),()=>ShowControls(false),24);
            hint=Label("",cameraPanel,new Vector2(18,319),new Vector2(324,99),15,navy);
            Label("Camera · drag to look",cameraPanel,new Vector2(18,282),new Vector2(324,26),16,navy,FontStyle.Bold);
            LookLeft=Hold("Left · J",cameraPanel,new Vector2(18,232),new Vector2(76,42),Art.Teal,14);LookRight=Hold("Right · L",cameraPanel,new Vector2(100,232),new Vector2(76,42),Art.Teal,14);
            LookUp=Hold("Up · I",cameraPanel,new Vector2(182,232),new Vector2(76,42),Art.Teal,14);LookDown=Hold("Down · K",cameraPanel,new Vector2(264,232),new Vector2(78,42),Art.Teal,14);
            ZoomIn=Hold("Zoom +",cameraPanel,new Vector2(18,181),new Vector2(102,42),Art.Teal,14);ZoomOut=Hold("Zoom −",cameraPanel,new Vector2(128,181),new Vector2(102,42),Art.Teal,14);Button("Center · P",cameraPanel,new Vector2(238,181),new Vector2(104,42),()=>RecenterRequested?.Invoke(),14);
            tricks=Rect("Bike tricks",cameraPanel,Vector2.zero,Vector2.zero,new Vector2(18,127),new Vector2(324,42));
            Wheelie=Hold("Z Wheelie",tricks,Vector2.zero,new Vector2(77,39),Art.Teal,12);
            string[] trickNames={"Z Wheelie","X Endo","V 180","B Stand"};for(int i=1;i<4;i++){int n=i;Button(trickNames[i],tricks,new Vector2(i*82,0),new Vector2(77,39),()=>{if(n==3)StandRequested?.Invoke();else TrickRequested?.Invoke((BikeTrick)(n+1));},12);}
            touchToggle=Button("Touch controls: off",cameraPanel,new Vector2(18,72),new Vector2(324,42),()=>SetTouchControls(!touchControls),16);
            Button("All controls & help · H",cameraPanel,new Vector2(18,19),new Vector2(188,42),ShowHelp,14);
            soundButton=Button("Sound: 70%",cameraPanel,new Vector2(214,19),new Vector2(128,42),()=>GameSounds.CycleVolume(),14);
            foreach(Transform child in cameraPanel)if(child is RectTransform rect)rect.anchoredPosition+=Vector2.up*60;
            cameraPanel.sizeDelta+=Vector2.up*60;cameraPanel.anchoredPosition-=Vector2.up*60;
            lowDef=Button("Low Def",cameraPanel,new Vector2(18,19),new Vector2(157,42),()=>GraphicsRequested?.Invoke(false),16);
            highDef=Button("High Def",cameraPanel,new Vector2(185,19),new Vector2(157,42),()=>GraphicsRequested?.Invoke(true),16);
            airTricks=Rect("Plane maneuvers",cameraPanel,Vector2.zero,Vector2.zero,new Vector2(18,187),new Vector2(324,42));
            AirLoop=Hold("X Loop",airTricks,Vector2.zero,new Vector2(102,42),Art.Teal,14);
            AirRoll=Hold("Z Roll",airTricks,new Vector2(110,0),new Vector2(102,42),Art.Teal,14);
            AirTurn=Hold("V U-turn",airTricks,new Vector2(220,0),new Vector2(104,42),Art.Teal,14);
            carExtras=Rect("Car customization",cameraPanel,Vector2.zero,Vector2.zero,new Vector2(18,187),new Vector2(324,42));
            nitroFit=Button("Fit NOS",carExtras,Vector2.zero,new Vector2(145,42),()=>NitroFitRequested?.Invoke(),14);nitroStatus=Label("",carExtras,new Vector2(155,0),new Vector2(169,42),12,navy);
            foreach(Transform child in cameraPanel)if(child is RectTransform rect)rect.anchoredPosition+=Vector2.up*54;
            cameraPanel.sizeDelta+=Vector2.up*54;cameraPanel.anchoredPosition-=Vector2.up*54;
            nightButton=Button("Daylight",cameraPanel,new Vector2(18,19),new Vector2(157,42),()=>NightRequested?.Invoke(),15);
            glowButton=Button("Underglow: off",cameraPanel,new Vector2(185,19),new Vector2(157,42),()=>GlowRequested?.Invoke(),14);
            foreach(Transform child in cameraPanel)if(child is RectTransform rect)rect.anchoredPosition+=Vector2.up*54;
            cameraPanel.sizeDelta+=Vector2.up*54;cameraPanel.anchoredPosition-=Vector2.up*54;
            originalStyle=Button("Style: Original",cameraPanel,new Vector2(18,19),new Vector2(100,42),()=>GraphicsQuality.Current?.SetCozy(false),12);
            cozyStyle=Button("Style: Cozy",cameraPanel,new Vector2(126,19),new Vector2(100,42),()=>GraphicsQuality.Current?.SetCozy(true),12);
            glassStyle=Button("Style: Living Glass",cameraPanel,new Vector2(234,19),new Vector2(108,42),()=>GraphicsQuality.Current?.SetLivingGlass(true),12);
            cameraPanel.gameObject.SetActive(false);explore.gameObject.SetActive(false);
            BuildSelection(); BuildHelp(); BuildTravel(); BuildJourney();BuildMarina();BuildHero();BuildElevator(); Refresh(); bottom.gameObject.SetActive(false);
        }

        void BuildSelection()
        {
            selection = Panel("Choose your explorer", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.08f, .18f, .25f, .52f));
            var card = Panel("Explorer card", selection, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-430, -292), new Vector2(860, 584), paper);
            Label("Your city. Your adventure.", card, new Vector2(28, 506), new Vector2(800, 52), 36, navy, FontStyle.Bold).alignment = TextAlignmentOptions.Center;
            Label("Choose an explorer", card, new Vector2(30, 466), new Vector2(800, 34), 22, navy).alignment = TextAlignmentOptions.Center;
            Color[] colors = { Art.Blue, Art.Teal, Art.Hex("F58A29"), Art.Hex("BC3842"), Art.Hex("BC3842"), Art.Blue };
            for (int i = 0; i < Simulation.CharacterNames.Length; i++)
            {
                int index = i;
                var button = Button("", card, new Vector2(31 + i * 134, 183), new Vector2(126, 246), () => CharacterSelected?.Invoke(index));
                button.transition=Selectable.Transition.None;
                choices[i] = button.GetComponent<Image>();
                RectTransform picture = Rect("3D explorer portrait", button.transform, Vector2.zero, Vector2.zero, new Vector2(3, 82), new Vector2(120, 120));
                portraits[i] = picture.gameObject.AddComponent<RawImage>(); portraits[i].raycastTarget = false; portraits[i].enabled = false;
                Label(Simulation.CharacterNames[i], button.transform, new Vector2(3, 20), new Vector2(120, 40), 20, colors[i], FontStyle.Bold).alignment = TextAlignmentOptions.Center;
            }
            characterLabel = Label("Sky is ready!", card, new Vector2(30, 122), new Vector2(800, 36), 21, navy);
            characterLabel.alignment = TextAlignmentOptions.Center;
            start = Button("Getting the city ready…", card, new Vector2(247, 36), new Vector2(366, 71), () =>
            {
                BeginExploring();
            }, 25);
            start.GetComponent<Image>().color = Art.Teal; start.interactable = false;
            startLabel = start.GetComponentInChildren<Text>();
            status = Label("Building your sunny neighborhood", card, new Vector2(28, 4), new Vector2(802, 28), 15, navy);
            status.alignment = TextAlignmentOptions.Center;
        }

        void BuildHelp()
        {
            help = Panel("How to play", safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.08f, .18f, .25f, .6f));
            var card = Panel("Help card", help, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-530, -405), new Vector2(1060, 810), paper);
            Label("Let's explore!", card, new Vector2(34, 742), new Vector2(992, 50), 34, navy, FontStyle.Bold);
            Label("WALK & EXPLORE\nWASD / arrows / stick move. Shift / GO runs.\nSpace / JUMP leaps. 1–5 choose your ride.\nG enter / exit a nearby vehicle (stop to exit).\n\nBUILDINGS\nF opens the door. Walk up the staircase;\nreturn along the landing to the next flight.\nTall buildings also have lifts. F at the lift\nopens its floor keypad; choose a number or Roof.\nReturn to floor 1 and use the front door to leave.\n\nBIKE & CAR\nHold Z / Wheelie · X endo · V hop 180 · B stand.\nKeep moving to start a bicycle trick.\nShift / DRIFT slides cars. Space brakes.\nA hard crash breaks parts; F repairs the car.\n\nCAMERA\nJ / L orbit · I / K look up / down · P recenter.\n+ / − or mouse wheel zoom. Drag also works.", card, new Vector2(34,145),new Vector2(485,584),20,navy);
            Label("BOATS & YACHTS · M\nOpen the marina guide from any world.\nChoose six boats or three furnished yachts.\nG drives boats; WASD sails; Space brakes.\nF boards yachts and uses furniture.\nWalk up the aft stairs to change decks.\nF attaches / stows the speedboat tube.\nWalk into the sea to swim. F at the offshore\nladder climbs; F on the board dives.\n\nPLANE & SKY ROUTE\nW / GO powers up.\nE / UP climbs · Q / DOWN descends.\nSpace / STOP slows. Land on the runway.\nGreen north gate: next stop. Blue south: return.\nT opens worlds and saved route above 18 m.\n\nTHOR IN ASGARD · SKY ROUTE STOP 4\nU becomes Thor / returns to your explorer.\nY throws / recalls the hammer · O lightning.\nN ground slam · 0 restores nine rune targets.\nThe power buttons show cooldown times.",card,new Vector2(549,145),new Vector2(478,584),20,navy);
            Label("Double Space: triple backflip · Single Space: jump · M: marina · R: home",card,new Vector2(34,101),new Vector2(992,32),19,navy).alignment=TextAlignmentOptions.Center;
            Button("Model credits",card,new Vector2(34,28),new Vector2(205,44),()=>Application.OpenURL("https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept"),14);
            Label("CarConcept · DGG / Eric Chadwick\nCC BY 4.0 · adapted mesh/materials",card,new Vector2(755,22),new Vector2(278,52),12,navy);
            Button("Back to exploring", card, new Vector2(358, 20), new Vector2(344, 62), () =>
            { help.gameObject.SetActive(false); MenuOpen = selection.gameObject.activeSelf; bottom.gameObject.SetActive(!MenuOpen); ClearInput(); }, 23);
            help.gameObject.SetActive(false);
        }

        public void SetPortrait(int index, Texture texture) { portraits[index].texture = texture; portraits[index].enabled = texture != null; }
        public void BeginExploring(){elevator?.gameObject.SetActive(false);explore?.gameObject.SetActive(false);MenuOpen=false;selection.gameObject.SetActive(false);help.gameObject.SetActive(false);marina?.gameObject.SetActive(false);travel?.gameObject.SetActive(false);bottom.gameObject.SetActive(true);ClearInput();StartRequested?.Invoke();}
        public void Ready() { start.interactable = true; startLabel.text = "Let's explore!"; status.text = "Walk, ride, fly. Go wherever curiosity takes you."; }
        public void ShowCharacters() { HideDrawers();MenuOpen = true; selection.gameObject.SetActive(true); bottom.gameObject.SetActive(false); ClearInput(); }
        public void ShowHelp() { HideDrawers();MenuOpen = true; help.gameObject.SetActive(true); bottom.gameObject.SetActive(false); ClearInput(); }
        public void ClearInput() { AirLoop?.Clear();AirRoll?.Clear();AirTurn?.Clear();sim?.CancelAirManeuver();Nitro?.Clear();sim?.CancelNitro();Wheelie?.Clear(); sim?.CancelWheelie(); Stick?.Clear(); Go?.Clear(); Stop?.Clear(); Rise?.Clear(); Descend?.Clear(); Jump?.Clear(); Drift?.Clear(); Look?.Clear(); LookLeft?.Clear();LookRight?.Clear();LookUp?.Clear();LookDown?.Clear();ZoomIn?.Clear();ZoomOut?.Clear();InputCleared?.Invoke(); }
        public void ReconcilePointers(){AirLoop?.Reconcile();AirRoll?.Reconcile();AirTurn?.Reconcile();Nitro?.Reconcile();Stick?.Reconcile();Go?.Reconcile();Stop?.Reconcile();Rise?.Reconcile();Descend?.Reconcile();Jump?.Reconcile();Drift?.Reconcile();Wheelie?.Reconcile();Look?.Reconcile();LookLeft?.Reconcile();LookRight?.Reconcile();LookUp?.Reconcile();LookDown?.Reconcile();ZoomIn?.Reconcile();ZoomOut?.Reconcile();}
        public void Refresh()
        {
            if (sim == null) return;
            district.text = sim.District;mapLabel.text=sim.Indoors?"ROOM MAP":"N ↑   "+(sim.WorldIndex==3?WorldRoute.ThemeNames[sim.GeneratedTheme]:sim.WorldName).ToUpperInvariant();
            displayedSpeed = sim.ActualSpeed < .02f || sim.Resting != RestPose.None ? 0 : Mathf.Lerp(displayedSpeed, sim.ActualSpeed * 3.6f, 1-Mathf.Exp(-Time.unscaledDeltaTime*9));
            speed.text = Mathf.RoundToInt(displayedSpeed) + " km/h";
            if(soundButton!=null)soundButton.GetComponentInChildren<Text>().text=GameSounds.Volume<=0?"Sound: off":"Sound: "+Mathf.RoundToInt(GameSounds.Volume*100)+"%";
            if(lowDef!=null && GraphicsQuality.Current!=null){bool high=GraphicsQuality.Current.High;lowDef.GetComponentInChildren<Text>().text=high?"Low Def":"Low Def (on)";highDef.GetComponentInChildren<Text>().text=high?"High Def (on)":"High Def";lowDef.interactable=highDef.interactable=!GraphicsQuality.Current.Busy;}
            if(cozyStyle!=null && GraphicsQuality.Current!=null)
            {
                bool cozy=GraphicsQuality.Current.Cozy,glass=GraphicsQuality.Current.LivingGlass;
                originalStyle.GetComponentInChildren<Text>().text="Original";cozyStyle.GetComponentInChildren<Text>().text="Bubbly";glassStyle.GetComponentInChildren<Text>().text="Living Glass";
                originalStyle.GetComponent<Image>().color=!cozy?Art.Hex("C7DCCB"):paper;cozyStyle.GetComponent<Image>().color=cozy&&!glass?Art.Hex("C7DCCB"):paper;glassStyle.GetComponent<Image>().color=glass?Art.Hex("C7DCCB"):paper;
                originalStyle.interactable=cozyStyle.interactable=glassStyle.interactable=!GraphicsQuality.Current.Busy;
            }
            Jump.gameObject.SetActive((sim.Mode == Ride.Walk || sim.IsJumpRide) && !sim.Swimming);
            parkRetry.gameObject.SetActive(sim.WorldIndex==4 && !MenuOpen);
            for(int i=0;i<4;i++){parkRides[i].color=(int)sim.Mode==i+7?Art.Hex("9BDFC5"):paper;parkRides[i].GetComponent<Button>().interactable=!sim.Indoors && sim.Resting==RestPose.None;}
            Drift.gameObject.SetActive(sim.Mode == Ride.RaceCar);
            Nitro.gameObject.SetActive(sim.Mode==Ride.RaceCar&&sim.NitroInstalled);carExtras.gameObject.SetActive(sim.Mode==Ride.RaceCar);
            nitroFit.GetComponentInChildren<Text>().text=sim.NitroInstalled?"Remove NOS":"Fit NOS";nitroStatus.text=sim.NitroInstalled?"Hold Option / Alt\nNOS "+Mathf.RoundToInt(sim.NitroCharge*100)+"%":"3-second boost";
            if(CityAtmosphere.Current!=null){nightButton.GetComponentInChildren<Text>().text=CityAtmosphere.Current.Night?"Night · switch to day":"Day · switch to night";glowButton.GetComponentInChildren<Text>().text="Underglow: "+CityAtmosphere.Current.GlowName;glowButton.interactable=sim.Mode==Ride.RaceCar;}
            tricks.gameObject.SetActive(sim.IsBicycle);
            airTricks.gameObject.SetActive(sim.Mode==Ride.Airplane);
            flight.gameObject.SetActive(sim.IsAircraft);
            flightInfo.text = sim.IsAircraft ? Mathf.RoundToInt(sim.Position.Y) + " m altitude\n" + Mathf.RoundToInt(sim.Throttle * 100) + "% power" : "";
            hint.text = sim.Mode==Ride.Yacht?"W/S sails or reverses · A/D steers · Space / STOP brakes · F at a mooring ring docks · F at sea explores the yacht":sim.Mode==Ride.Helicopter?"E / UP lifts off · Q / DOWN descends · W/S forward/back · A/D turns · Space / STOP hovers · G exits after landing":sim.IsJumpRide ? "GO / W builds speed · Space / JUMP launches · Ctrl / STOP brakes · R / Retry at checkpoints" : sim.IsBoat ? "WASD / GO to sail • Space / STOP • G step out when stopped" : sim.Swimming ? "SWIM with WASD / stick • F at ladder • R returns to shore" : sim.Mode == Ride.Walk ? "WASD / stick to move • Space to jump • F to open doors" : sim.Mode == Ride.Airplane ?
                (sim.AirManeuver!=AirTrick.None?sim.AirManeuver+" · "+Mathf.RoundToInt(sim.AirProgress*100)+"% · Space / STOP cancels":sim.Airborne ? "E / UP climbs · Q / DOWN descends. Above 70 m: X loop · Z roll · V U-turn. Space / STOP cancels. Land on the runway." : sim.Speed < Simulation.TakeoffSpeed ? "Hold W / GO to gain speed, then hold E / UP" : "Ready for takeoff! Hold E / UP") :
                sim.Mode == Ride.RaceCar ? "Shift / DRIFT slides · Space / STOP brakes · Fit NOS below, then hold Option / Alt or NOS while accelerating. Release to recharge." : sim.Mode == Ride.Bicycle ? "Pedal with GO • Hold Z / Wheelie · X endo · V 180 · B stand" : "Steer with the left stick • Hold GO • STOP to brake";
            for (int i = 0; i < choices.Length; i++) choices[i].color = i == sim.Character ? Art.Hex("CEEBDF") : Art.Hex("EBEFE6");
            for (int i = 0; i < 5; i++) { rides[i].color = i == (int)sim.Mode ? Art.Hex("9BDFC5") : paper; rides[i].GetComponent<Button>().interactable = !sim.Indoors && sim.Resting==RestPose.None; }
            if(travel!=null && travel.gameObject.activeSelf)
            {
                travelStatus.text="Choose a place to arrive on foot. Travel is available from anywhere.";
                for(int i=0;i<4;i++){destinations[i].interactable=true;destinations[i].GetComponent<Image>().color=(i==3?4:i)==sim.WorldIndex?Art.Hex("CEEBDF"):Art.Hex("EBEFE6");}
            }
            characterLabel.text = sim.IsRider ? "Rider · Tall strides. Fast feet." : Simulation.CharacterNames[sim.Character] + " is ready!";
            if (lastNotice != sim.NoticeVersion) { lastNotice = sim.NoticeVersion; notice.text = sim.Notice; noticeUntil = Time.unscaledTime + 3.5f; }
            notice.enabled = !MenuOpen && Time.unscaledTime < noticeUntil;
            if(notice.enabled){Color c=notice.color;c.a=Mathf.Clamp01((noticeUntil-Time.unscaledTime)*2);notice.color=c;}
            telemetry.gameObject.SetActive(sim.Mode!=Ride.Walk);
            flightInfo.text=sim.Mode==Ride.RaceCar&&sim.NitroInstalled?(sim.NitroActive?"NOS BOOST":"NOS "+Mathf.RoundToInt(sim.NitroCharge*100)+"%") :sim.IsAircraft?Mathf.RoundToInt(sim.Position.Y)+" m altitude":sim.IsBoat?"ON THE WATER":Simulation.RideNames[(int)sim.Mode];
            telemetry.anchoredPosition=new Vector2(-130,touchControls?181:18);
            actionStack.anchoredPosition=new Vector2(18,touchControls?182:heroActive?123:18);
            mapPanel.gameObject.SetActive(!ControlsOpen);
            if(touchToggle!=null)touchToggle.GetComponentInChildren<Text>().text="Touch controls: "+(touchControls?"on":"off");
        }

        void BuildTravel()
        {
            travel=Panel("Flight destinations",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.08f,.18f,.25f,.85f));
            var card=Panel("Travel map",travel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-445,-315),new Vector2(890,630),paper);
            Label("Where shall we go?",card,new Vector2(25,532),new Vector2(840,65),34,navy,FontStyle.Bold).alignment=TextAlignmentOptions.Center;
            string[] descriptions={"Your colorful city\nHomes • garage • park","Tropical marina\nThree yachts • six boats","Snowy mountains\nCabins • pine forest","Huge jumps & a bowl\nBoards • scooters • bikes"};
            for(int i=0;i<4;i++)
            {
                int destination=i==3?4:i;
                destinations[i]=Button(Simulation.WorldNames[destination],card,new Vector2(25+i*215,398),new Vector2(199,86),()=>TravelRequested?.Invoke(destination),23);
                Label(descriptions[i],card,new Vector2(28+i*215,309),new Vector2(196,76),17,navy).alignment=TextAlignmentOptions.Center;
            }
            Label("THE ONGOING SKY ROUTE",card,new Vector2(30,249),new Vector2(820,42),24,navy,FontStyle.Bold);
            Label("Jungle → New York → Prehistoric → Asgard → new variations\nStart flying at your saved stop, ready to explore.\nNorth gate continues the route; south gate goes back.",card,new Vector2(30,153),new Vector2(820,91),20,navy);
            Button("START / RESUME SKY ROUTE",card,new Vector2(255,94),new Vector2(380,55),()=>RouteRequested?.Invoke(),20);
            travelStatus=Label("",card,new Vector2(25,69),new Vector2(840,23),18,navy);travelStatus.alignment=TextAlignmentOptions.Center;
            Button("Return to Little City",card,new Vector2(25,18),new Vector2(408,45),()=>TravelRequested?.Invoke(0),20);
            Button("Continue exploring here",card,new Vector2(457,18),new Vector2(408,45),BeginExploring,20);
            travel.gameObject.SetActive(false);
        }
        public void ShowTravel(bool show){if(show)HideDrawers();travel.gameObject.SetActive(show);MenuOpen=show;bottom.gameObject.SetActive(!show);ClearInput();Refresh();}
        void BuildHero()
        {
            heroPanel=Rect("Thor abilities",bottom,Vector2.zero,Vector2.zero,new Vector2(18,18),new Vector2(365,96));
            heroScore=Label("",heroPanel,new Vector2(8,69),new Vector2(160,24),13,Color.white);heroScore.gameObject.AddComponent<Shadow>();
            heroToggle=Chip("U · Become Thor",heroPanel,new Vector2(176,64),new Vector2(189,32),()=>HeroRequested?.Invoke(0));
            string[] names={"Y · Hammer","O · Lightning","N · Slam","0 · Reset"};
            for(int i=0;i<4;i++){int n=i+1;powers[i]=Chip(names[i],heroPanel,new Vector2(i*92,8),new Vector2(88,46),()=>HeroRequested?.Invoke(n),12);}
            heroPanel.gameObject.SetActive(false);
        }
        public void SetHeroStatus(bool available,bool active,int count,int total,float hammer,float lightning,float slam)
        {
            heroActive=available;heroPanel.gameObject.SetActive(available);
            heroPanel.anchoredPosition=new Vector2(18,touchControls?282:18);
            heroScore.text=active?"Rune targets   "+count+" / "+total:"Asgard";
            heroToggle.GetComponentInChildren<Text>().text=active?"U · Explorer":"U · Become Thor";
            string[] names={"Y · Hammer","O · Lightning","N · Slam","0 · Reset"};float[] times={hammer,lightning,slam,0};
            for(int i=0;i<4;i++){powers[i].gameObject.SetActive(active);powers[i].GetComponentInChildren<Text>().text=names[i]+(times[i]>.05f?"\n"+Mathf.CeilToInt(times[i])+"s":"");}
        }
        void BuildElevator()
        {
            elevator=Panel("Elevator floor selection",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.04f,.12f,.16f,.45f));
            var card=Panel("Elevator keypad",elevator,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-220,-285),new Vector2(440,570),paper);
            floorTitle=Label("Elevator",card,new Vector2(25,512),new Vector2(390,35),25,navy,FontStyle.Bold);
            floorRange=Label("",card,new Vector2(25,468),new Vector2(390,34),15,navy);
            var field=Panel("Floor number",card,Vector2.zero,Vector2.zero,new Vector2(25,389),new Vector2(390,66),Color.white);
            FloorInput=field.gameObject.AddComponent<TMP_InputField>();var viewport=Rect("Number viewport",field,Vector2.zero,Vector2.one,new Vector2(14,5),new Vector2(-14,-5));viewport.gameObject.AddComponent<RectMask2D>();var text=Label("",viewport,Vector2.zero,new Vector2(362,56),30,navy);text.alignment=TextAlignmentOptions.Left;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;FloorInput.textViewport=viewport;FloorInput.textComponent=text;FloorInput.contentType=TMP_InputField.ContentType.IntegerNumber;FloorInput.characterLimit=3;FloorInput.lineType=TMP_InputField.LineType.SingleLine;FloorInput.onValueChanged.AddListener(_=>{if(floorError!=null)floorError.text="";});FloorInput.onSubmit.AddListener(_=>SubmitFloor());
            for(int i=1;i<=9;i++){int digit=i;Button(i.ToString(),card,new Vector2(25+(i-1)%3*134,321-(i-1)/3*58),new Vector2(122,50),()=>AppendFloorDigit(digit),22);}
            Button("Clear",card,new Vector2(25,147),new Vector2(122,50),()=>FloorInput.text="",16);Button("0",card,new Vector2(159,147),new Vector2(122,50),()=>AppendFloorDigit(0),22);Button("Roof",card,new Vector2(293,147),new Vector2(122,50),()=>FloorRequested?.Invoke(floorCount),18);
            floorError=Label("",card,new Vector2(25,110),new Vector2(390,30),14,Art.Red);
            Button("Cancel",card,new Vector2(25,37),new Vector2(170,56),BeginExploring,18);
            var go=Button("Go to floor",card,new Vector2(207,37),new Vector2(208,56),SubmitFloor,19);go.GetComponent<Image>().color=Art.Teal;go.GetComponentInChildren<Text>().color=Color.white;elevator.gameObject.SetActive(false);
        }
        public void ShowElevator(string building,int floors,int current)
        {
            HideDrawers();floorCount=floors;floorTitle.text="Elevator";floorRange.text="Floors 1–"+floors+" · 1 is ground floor · Roof above "+floors;floorError.text="";FloorInput.text="";MenuOpen=true;bottom.gameObject.SetActive(false);elevator.gameObject.SetActive(true);ClearInput();FloorInput.ActivateInputField();
        }
        public void SubmitFloor()
        {
            if(!int.TryParse(FloorInput.text,out int number) || number<1 || number>floorCount){floorError.text="Enter a floor from 1 to "+floorCount+", or choose Roof.";return;}
            FloorRequested?.Invoke(number-1);
        }
        void AppendFloorDigit(int digit){FloorInput.text+=digit.ToString();FloorInput.caretPosition=FloorInput.text.Length;}
        void BuildMarina()
        {
            marina=Panel("Marina guide",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.08f,.18f,.25f,.88f));
            var card=Panel("Ferry stops",marina,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-360,-230),new Vector2(720,460),paper);
            Label("BOATS, YACHTS & THE OCEAN",card,new Vector2(28,372),new Vector2(665,50),28,navy,FontStyle.Bold);
            Label("Board a yacht, walk upstairs, and take the helm with F.",card,new Vector2(28,329),new Vector2(665,35),18,navy);
            Button("SAIL A YACHT TO THE FUN ISLANDS",card,new Vector2(35,242),new Vector2(650,65),()=>MarinaRequested?.Invoke(0),21);
            Button("SIX BOATS & SPEEDBOATS · G to drive",card,new Vector2(35,165),new Vector2(650,65),()=>MarinaRequested?.Invoke(1),21);
            Button("DIVING PLATFORM · SWIMMING & SHARKS",card,new Vector2(35,88),new Vector2(650,65),()=>MarinaRequested?.Invoke(2),20);
            Button("Back",card,new Vector2(260,18),new Vector2(200,50),BeginExploring,20);marina.gameObject.SetActive(false);
        }
        public void ShowMarina(){HideDrawers();MenuOpen=true;marina.gameObject.SetActive(true);bottom.gameObject.SetActive(false);ClearInput();}
        public void SetRouteContext(string caption){routeCaption.text=caption;routeCaption.gameObject.SetActive(true);}
        void BuildJourney()
        {
            journey=Panel("Flying to the next world",safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(.06f,.17f,.24f,.96f));
            journeyText=Label("",journey,Vector2.zero,new Vector2(1000,200),38,paper,FontStyle.Bold);
            journeyText.rectTransform.anchorMin=journeyText.rectTransform.anchorMax=new Vector2(.5f,.5f);journeyText.rectTransform.anchoredPosition=new Vector2(-500,-100);journeyText.alignment=TextAlignmentOptions.Center;
            journey.gameObject.SetActive(false);
        }
        public void ShowJourney(bool show,string text){travel.gameObject.SetActive(false);journey.gameObject.SetActive(show);journeyText.text=text;MenuOpen=show;bottom.gameObject.SetActive(!show);ClearInput();}
        public void SetVehicleContext(string caption,bool actionable){vehicleAction.gameObject.SetActive(actionable && caption.StartsWith("G"));vehicleAction.GetComponentInChildren<Text>().text=caption;}
        public void SetContext(string caption,bool inGarage)
        {
            bool action=!string.IsNullOrEmpty(caption) && caption.StartsWith("F ");
            interaction.gameObject.SetActive(action);contextText.text=caption;
            vehicleAction.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,action?44:0);
            garageChoices.gameObject.SetActive(inGarage);
        }
        void HideDrawers(){explore?.gameObject.SetActive(false);ShowControls(false);}
        public void ShowExplore(bool show)
        {
            ShowControls(false);explore.gameObject.SetActive(show);MenuOpen=show;ClearInput();
            if(show){selection.gameObject.SetActive(false);help.gameObject.SetActive(false);bottom.gameObject.SetActive(true);}
        }
        public void ShowControls(bool show){if(show && ExploreOpen){explore.gameObject.SetActive(false);MenuOpen=false;ClearInput();}cameraPanel?.gameObject.SetActive(show);if(!show){LookLeft?.Clear();LookRight?.Clear();LookUp?.Clear();LookDown?.Clear();ZoomIn?.Clear();ZoomOut?.Clear();}}
        public void SetTouchControls(bool show){ClearInput();touchControls=show;touchLayer.gameObject.SetActive(show);Refresh();}
        Button Chip(string text,Transform parent,Vector2 pos,Vector2 size,Action action,int fontSize=14)
        {
            var b=Button(text,parent,pos,size,action,fontSize);b.GetComponent<Image>().color=glass;
            var t=b.GetComponentInChildren<Text>();t.color=Color.white;t.fontStyle=FontStyles.Normal;t.alignment=TextAlignmentOptions.MidlineLeft;t.margin=new Vector4(10,0,8,0);return b;
        }
        HoldAction Hold(string text, Transform parent, Vector2 pos, Vector2 size, Color color,int fontSize=23)
        { var b = Button(text, parent, pos, size, () => { }, fontSize); b.GetComponent<Image>().color = color; b.GetComponentInChildren<Text>().color = Color.white; return b.gameObject.AddComponent<HoldAction>(); }
        Button Button(string text, Transform parent, Vector2 pos, Vector2 size, Action action, int fontSize = 18)
        {
            var r = Panel(text + " button", parent, Vector2.zero, Vector2.zero, pos, size, paper);
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
            var colors = b.colors; colors.highlightedColor = new Color(.9f, .96f, .95f); colors.pressedColor = new Color(.72f, .86f, .82f); b.colors = colors;
            b.onClick.AddListener(() => action());
            Label(text, r, Vector2.zero, size, fontSize, navy, FontStyle.Bold).alignment = TextAlignmentOptions.Center;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            return b;
        }
        Text Label(string text, Transform parent, Vector2 pos, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            var r = Rect(text, parent, Vector2.zero, Vector2.zero, pos, size);
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = fontSize; t.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal; t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }
        RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size, Color color)
        {
            var r = Rect(name, parent, min, max, pos, size); var image = r.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color; return r;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = min; r.anchorMax = max; r.pivot = Vector2.zero;
            if (min != max) { r.offsetMin = pos; r.offsetMax = size; } else { r.sizeDelta = size; r.anchoredPosition = pos; }
            return r;
        }
        static Sprite MakeRoundedSprite()
        {
            const int size = 64, radius = 18;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Rounded control surface", filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (32 - radius), 0), dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (32 - radius), 0);
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            texture.SetPixels(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        void OnDestroy() { if (rounded != null) { Destroy(rounded.texture); Destroy(rounded); } }
    }
}
