using System;
using System.Collections.Generic;

namespace LittleCity
{
    // No engine dependencies: the exact movement used by the game also runs in CoreChecks.
    public enum Ride { Walk, RaceCar, Bicycle, Motorcycle, Airplane, Motorboat, Speedboat, Skateboard, Scooter, MountainBike, MiniBike, DirtBike, Van, Camper, Helicopter, Yacht }
    public enum BikeTrick { None, Wheelie, Endo, Spin180 }
    public enum RestPose { None, Sitting, Sleeping }

    public struct Point
    {
        public float X, Y, Z;
        public Point(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Point operator +(Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Point operator *(Point a, float b) => new Point(a.X * b, a.Y * b, a.Z * b);
    }

    public struct DriveInput
    {
        public float Steering, Forward, CameraYaw;
        public bool Brake, Rise, Descend, Sprint, Jump, Drift, Wheelie,Nitro,AirLoop,AirRoll,AirTurn;
    }

    public struct Obstacle
    {
        public float X, Z, HalfX, HalfZ, Height,Bottom,Yaw;
        public Obstacle(float x, float z, float halfX, float halfZ, float height,float bottom=-1000,float yaw=0)
        { X = x; Z = z; HalfX = halfX; HalfZ = halfZ; Height = height;Bottom=bottom;Yaw=yaw; }
    }

    public sealed partial class Simulation
    {
        public const float WorldLimitX = 900, WorldLimitZ = 800, Ceiling = 420;
        public const float TakeoffSpeed = 14;
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<Obstacle> InteriorObstacles = new List<Obstacle>();
        public bool PondEnabled;
        public int WorldIndex { get; private set; }
        public static readonly string[] WorldNames = { "Little City", "Palm Cove", "Cloud Peaks", "Sky route", "Skyline Skatepark" };
        public bool HarborWater;
        public bool OnDivePlatform { get; private set; }
        public bool IsBicycle => Mode==Ride.Bicycle || Mode==Ride.MountainBike || Mode==Ride.MiniBike;
        public bool IsJumpRide => Mode==Ride.DirtBike || Mode==Ride.Skateboard || Mode==Ride.Scooter || Mode==Ride.MountainBike || Mode==Ride.MiniBike || Mode==Ride.Bicycle && (WorldIndex==4 || Indoors);
        public bool UsesRampPhysics => IsJumpRide || Mode==Ride.RaceCar;
        public bool CanRideInside => IsBicycle || Mode==Ride.DirtBike;
        public Func<Point,float,bool> InteriorBoundary;
        public Func<Point,float> OutdoorGround;
        public float BoundsX => WorldIndex==0?WorldLimitX:WorldIndex==1?850:224;
        public float BoundsZ => WorldIndex==0?WorldLimitZ:WorldIndex==1?1080:200;
        public Point ParkCheckpoint=new Point(-60,0,-132);
        public int ParkRetries {get;private set;}
        public int ParkLandings {get;private set;}
        public float LandingCompression {get;private set;}
        public float LastLandingSpeed {get;private set;}
        public void LaunchIslandBounce(float impulse){if(Mode==Ride.Walk&&Grounded&&!Indoors)VerticalSpeed=Clamp(impulse,0,12);}
        public void LaunchPark(float impulse){if(IsJumpRide && Grounded)VerticalSpeed=impulse;}
        public void RetryPark(){Place(ParkCheckpoint,0);ResetCarDamage();Pitch=Bank=0;LandingCompression=0;ParkRetries++;Say("Back at your checkpoint. Build speed and try again!");}
        public bool IsBoat => Mode==Ride.Motorboat || Mode==Ride.Speedboat || Mode==Ride.Yacht;
        public bool IsAircraft => Mode==Ride.Airplane||Mode==Ride.Helicopter;
        public bool IsLargeVehicle => Mode==Ride.Van || Mode==Ride.Camper;
        public bool IsRoadCar => Mode==Ride.RaceCar || IsLargeVehicle;
        public float RoadRadius => Mode==Ride.Camper?1.76f:Mode==Ride.Van?1.56f:Mode==Ride.RaceCar?1.35f:.42f;
        public float RoadHalfLength => Mode==Ride.Camper?4.1f:3.4f;
        public bool IsOcean(Point p)=>WorldIndex==1&&p.Z>205 ? IslandLayout.Ground(p)<-.25f : HarborWater && p.Z>39 && p.X<120 && !(Math.Abs(p.X+65)<2.2f && p.Z<63) && !(Math.Abs(p.X+130)<2.2f && p.Z<68) && !(Math.Abs(p.X-22)<1.5f && p.Z<49) && !(Math.Abs(p.X-55)<1.5f && p.Z<49) && !(Math.Abs(p.X+5)<2.2f && p.Z<68) && !(Math.Abs(p.X-90)<1.5f && p.Z<49) && !(Math.Abs(p.X-104)<1.5f && p.Z<49) && !(Math.Abs(p.X+170)<1.5f && p.Z<49) && !(Math.Abs(p.X+185)<1.5f && p.Z<49);
        public bool Swimming => Mode==Ride.Walk && !Indoors && !OnDivePlatform && IsOcean(Position) && Position.Y<-.25f;
        public void ClimbDivingPlatform(){if(Mode!=Ride.Walk)return;OnDivePlatform=true;Place(new Point(35,4.4f,108));Say("On the diving platform! F to dive; move to explore the board.");}
        public void Dive(){if(!OnDivePlatform)return;OnDivePlatform=false;Place(new Point(35,4.4f,119));VerticalSpeed=3;Say("Splash! Swim with WASD. F by the ladder to climb back up.");}
        public string GeneratedWorldName="Sky route";
        public int GeneratedTheme;
        public string WorldName => WorldIndex==3 ? GeneratedWorldName : WorldNames[WorldIndex];
        public RestPose Resting { get; private set; }
        public string RoomName = "Inside";
        public float FloorLevel;
        public Func<Point,float> InteriorGround;
        public float RoomX, RoomZ = -173.5f, RoomHalfX = 8.5f, RoomHalfZ = 6.5f;
        Point restExit; float restYaw;
        public bool CanTravel => IsAircraft && Airborne && Position.Y >= 18 && !Indoors && AirManeuver==AirTrick.None;
        public void TravelTo(int world)
        {
            if (!CanTravel || world < 0 || world > 4 || world == 3 || world == WorldIndex) return;
            WorldIndex=world; Resting=RestPose.None; Position=new Point(172,32,-125);Yaw=VelocityYaw=0;Speed=18;Throttle=.65f;Pitch=Bank=0;
            Say("Welcome to "+WorldNames[world]+"! Land on the runway, then explore.");
        }
        public void ArriveMarina(Point spawn){Reset();WorldIndex=1;HarborWater=true;PondEnabled=false;Place(spawn);Say("Palm Cove marina: F boards the yachts; G boards the boats. M opens the marina guide.");}
        public void ArriveOnFoot(int world)
        {
            if(world<0 || world>4 || world==3)return;
            Reset();WorldIndex=world;HarborWater=world==1;PondEnabled=world==0;InteriorObstacles.Clear();
            Place(world==4?new Point(-60,0,-145):world==1?new Point(-65,0,58):Home);Say("Welcome to "+WorldNames[world]+"!");
        }
        public void ArriveGenerated(string name,int theme,bool returning)
        {
            if(!CanTravel)return;
            WorldIndex=3;GeneratedWorldName=name;GeneratedTheme=theme;FinishFlightArrival(returning);
        }
        public void FinishFlightArrival(bool returning)
        {
            Resting=RestPose.None;Place(new Point(172,32,returning?122:-125),returning?(float)Math.PI:0);Speed=18;Throttle=.65f;Pitch=Bank=0;
        }
        public bool EnterVehicle(Ride mode,int style,Point position,float yaw)
        {
            if(Mode!=Ride.Walk || Indoors || Resting!=RestPose.None)return false;
            Mode=mode;CarStyle=Math.Max(0,Math.Min(3,style));Place(position,yaw);Throttle=Pitch=Bank=0;ResetCarDamage();Standing=false;
            Say("You're in! G to step out when stopped. "+(IsAircraft?"E climbs; Q descends.":"GO to drive."));return true;
        }
        public bool ExitVehicle()
        {
            if(Mode==Ride.Walk || Airborne || Math.Abs(Speed)>.8f)return false;
            SwitchRide(Ride.Walk);return true;
        }
        public void BeginRest(RestPose pose,Point anchor,float yaw)
        {
            if(Mode!=Ride.Walk || Resting!=RestPose.None)return;
            restExit=Position;restYaw=Yaw;Resting=pose;Place(anchor,yaw);
            Say(pose==RestPose.Sleeping?"Sweet dreams. F to wake up.":"Make yourself comfortable. F to stand up.");
        }
        public void EndRest(){if(Resting==RestPose.None)return;Resting=RestPose.None;Place(restExit,restYaw);Say("Ready to explore again!");}
        public void EnterBuilding(Point spawn,float x,float z,float hx,float hz,string name,Func<Point,float> ground,float yaw)
        {
            outside=Position;outsideYaw=Yaw;Indoors=true;RoomX=x;RoomZ=z;RoomHalfX=hx;RoomHalfZ=hz;RoomName=name;FloorLevel=0;InteriorGround=ground;Place(spawn,yaw);
        }
        public void EnterRoom(Point spawn,float hx,float hz,string name)
        {
            InteriorGround=null;outside=Position;outsideYaw=Yaw;Indoors=true;RoomX=RoomZ=0;RoomHalfX=hx;RoomHalfZ=hz;FloorLevel=spawn.Y;RoomName=name;Place(spawn);
        }
        public void ChangeDeck(Point spawn,string name){EndRest();FloorLevel=spawn.Y;RoomName=name;Place(spawn);}

        public bool Indoors { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float ActualSpeed { get; private set; }
        public float VelocityYaw { get; private set; }
        public bool Drifting { get; private set; }
        public BikeTrick Trick { get; private set; }
        public float TrickProgress { get; private set; }
        bool wheelieWasHeld, wheelieRequiresRelease, previousWheelieInput;
        public void CancelWheelie()
        {
            if (Trick == BikeTrick.Wheelie) { Trick = BikeTrick.None; TrickProgress = 0; }
            wheelieWasHeld = false; wheelieRequiresRelease |= previousWheelieInput;
        }
        public bool Standing { get; private set; }
        public int CarStyle { get; private set; }
        public float ImpactSpeed { get; private set; }
        public int ImpactVersion { get; private set; }
        public bool NeedsRepair { get; private set; }
        public float BounceHeight => Mode == Ride.Bicycle && impactCooldown > 0 ? (float)Math.Sin(Math.Min(1, (1.2f-impactCooldown)/.75f)*(float)Math.PI)*Math.Min(.75f,ImpactSpeed*.055f) : 0;
        float reboundX, reboundZ, impactCooldown;
        public void Repair() { ResetCarDamage(); Speed = 0; reboundX = reboundZ = 0; Say("Fresh wheels! Ready to go again."); }
        bool jumpHeld;
        Point outside;
        float outsideYaw;
        public bool Grounded => Mode != Ride.Airplane && Position.Y <= GroundHeight(Position) + .015f;
        public float GroundHeight(Point p) => !Indoors&&WorldIndex==1&&p.Z>205 ? IslandLayout.Ground(p) : !Indoors && OutdoorGround!=null ? OutdoorGround(p) : Indoors ? (InteriorGround==null?FloorLevel:InteriorGround(p)) : OnDivePlatform && Math.Abs(p.X-35)<5 && p.Z>103 && p.Z<120 ? 4.4f : IsOcean(p) ? -1.05f : !Indoors && PondEnabled && Math.Abs(p.Z - 8) < 1.65f && Math.Abs(p.X + 13) < 9 ? .2f * Clamp((9 - Math.Abs(p.X + 13)) / 1.2f, 0, 1) : 0;
        public void Place(Point p, float yaw = 0) { CancelAirManeuver();CancelFlipMotion();Position = p; Yaw = VelocityYaw = yaw; Speed = ActualSpeed = VerticalSpeed = 0; Trick = BikeTrick.None; CancelWheelie(); }
        public void EnterInterior()
        {
            InteriorGround=null;outside = Position; outsideYaw = Yaw; Indoors = true; RoomX=0;RoomZ=-173.5f;RoomHalfX=8.5f;RoomHalfZ=6.5f;FloorLevel=0;RoomName="Home";
            Place(new Point(0, 0, -178)); Say("Come in! Explore the rooms. F at the door to leave.");
        }
        public void ExitInterior() { InteriorGround=null;Resting=RestPose.None; FloorLevel=0; Indoors = false; Place(outside, outsideYaw + (float)Math.PI); Say("Back in the neighborhood!"); }
        public void ChooseCar(int style) { CarStyle = Math.Max(0, Math.Min(2, style)); Say(new[] { "Blue Comet", "Sunshine Roadster", "Mint Mini" }[CarStyle] + " selected. Exit the garage and press 2 to drive."); }
        public void ToggleStanding() { if (IsBicycle) { Standing = !Standing; Say(Standing ? "Standing ride! Hands on the bars, feet on the pedals." : "Back in the saddle."); } }
        public bool StartTrick(BikeTrick trick)
        {
            if (!IsBicycle || Trick != BikeTrick.None || Speed < 1.5f || !Grounded) return false;
            Trick = trick; TrickProgress = 0; wheelieWasHeld = false; Say(trick == BikeTrick.Spin180 ? "Bunny hop 180!" : trick == BikeTrick.Endo ? "Endo! Balance on the front wheel." : "Wheelie! Balance on the back wheel."); return true;
        }
        public Point Position { get; private set; }
        public Ride Mode { get; private set; }
        public float Yaw { get; private set; }
        public float Speed { get; private set; }
        public float Throttle { get; private set; }
        public float Pitch { get; private set; }
        public float Bank { get; private set; }
        public bool Airborne => Mode == Ride.Airplane ? Position.Y > .001f : Mode==Ride.Helicopter?Position.Y>GroundHeight(Position)+.02f:UsesRampPhysics && !Grounded;
        public int Character { get; private set; }
        public string Notice { get; private set; }
        public int NoticeVersion { get; private set; }
        public float DistanceTravelled { get; private set; }
        public static readonly Point Home = new Point(0, 0, -24);
        public static readonly Point RunwayStart = new Point(172, 0, -130);
        public static readonly Point Helipad = new Point(143,0,-76);
        public static readonly string[] RideNames = { "On foot", "Race car", "Bicycle", "Motorcycle", "Airplane", "Motorboat", "Speedboat", "Skateboard", "Scooter", "Mountain bike", "Mini bike", "Dirt bike", "Touring van", "Camper RV", "Helicopter", "Yacht" };
        public static readonly string[] CharacterNames = { "Sky", "Maya", "Leo", "Rider", "River", "Parker" };
        public bool IsRider => Character == 3;
        public float WalkingScale => Character==4 ? .94f : IsRider ? (Indoors || Resting!=RestPose.None ? .82f : 1.65f) : .66f;
        public float PedalEffort { get; private set; }

        public Simulation() { Reset(); }

        public void SelectCharacter(int index)
        {
            Character = Math.Max(0, Math.Min(CharacterNames.Length - 1, index));
            Say("Let's explore, " + CharacterNames[Character] + "!");
        }

        public void Reset()
        {
            CancelAirManeuver();CancelFlipMotion();Mode = Ride.Walk; Position = Home; Yaw = 0; Speed = 0;
            Throttle = 0; Pitch = 0; Bank = 0; ActualSpeed = 0; VerticalSpeed = 0; VelocityYaw = Yaw;
            InteriorGround=null;InteriorBoundary=null;OnDivePlatform=false;Resting=RestPose.None; FloorLevel=0; ResetCarDamage(); impactCooldown = reboundX = reboundZ = 0; Indoors = false; Standing = false; Trick = BikeTrick.None; CancelWheelie(); Drifting = false; jumpHeld = false;
            Say("Welcome to Little City!");
        }

        public void SwitchRide(Ride next)
        {
            if (next == Mode || Indoors || Resting!=RestPose.None) return;
            CancelAirManeuver();CancelFlipMotion();
            if (next == Ride.Walk)
            {
                // Never drop the player out of an aircraft in mid-air.
                Position = Airborne ? Home : FindExit(Position);
                Mode = next;Position=new Point(Position.X,GroundHeight(Position),Position.Z);
                Say("Back on foot. Happy exploring!");
            }
            else
            {
                Mode = next; ResetCarDamage(); reboundX = reboundZ = 0;
                Position = next == Ride.Helicopter?Helipad:next == Ride.Airplane ? RunwayStart : new Point(0, 0, -32);
                Yaw = IsAircraft ? 0 : (float)Math.PI / 2;
                Say(next==Ride.Helicopter?"E / UP lifts off. W/S moves, A/D turns, Space hovers, Q / DOWN lands.":next == Ride.Airplane ? "Hold GO, then UP to take off. Fly back to the runway to land."
                    : "Your " + RideNames[(int)next].ToLowerInvariant() + " is ready on Park Avenue!");
            }
            Speed = 0; Throttle = 0; Pitch = 0; Bank = 0; VelocityYaw = Yaw; ActualSpeed = VerticalSpeed = 0; Trick = BikeTrick.None; CancelWheelie(); Standing = Drifting = false;
        }

        Point FindExit(Point origin)
        {
            for (int ring = 1; ring <= 5; ring++)
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * (float)Math.PI / 6;
                    Point candidate = new Point(origin.X + (float)Math.Sin(angle) * ring * 2,
                        0, origin.Z + (float)Math.Cos(angle) * ring * 2);
                    if (!Blocked(candidate, .5f) && InBounds(candidate)) return candidate;
                }
            return Home;
        }

        public void Step(float dt, DriveInput input)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if(Resting!=RestPose.None){Speed=ActualSpeed=PedalEffort=0;return;}
            // Substeps avoid tunneling even during a slow rendering frame.
            dt = Math.Min(dt, 1f);
            if (input.Jump && !jumpHeld && (Mode == Ride.Walk || IsJumpRide) && Grounded && !Swimming)
                VerticalSpeed = Mode==Ride.MountainBike?13.5f:Mode==Ride.MiniBike?10f:IsJumpRide?10.8f:6.2f;
            jumpHeld = input.Jump;
            PedalEffort=IsBicycle && !input.Brake ? Math.Abs(Clamp(input.Forward,-1,1)) : 0;
            float distanceBefore = DistanceTravelled;
            int count = Math.Max(1, (int)Math.Ceiling(dt / .016f));
            for (int i = 0; i < count; i++) Integrate(dt / count, input);
            ActualSpeed = (DistanceTravelled - distanceBefore) / dt;
            if(WorldIndex==4 && Position.Y < -3) RetryPark();
        }

        void Integrate(float dt, DriveInput input)
        {
            TickNitro(dt,input);
            if(TickAirManeuver(dt,input))return;
            if(FlipMotionActive){IntegrateFlip(dt);return;}
            float steer = Clamp(input.Steering, -1, 1), forward = Clamp(input.Forward, -1, 1);
            Point previous = Position;
            impactCooldown = Math.Max(0, impactCooldown - dt);RearmCarCrash();
            LandingCompression=Approach(LandingCompression,0,dt*(Mode==Ride.MountainBike?2:5));
            if (Math.Abs(reboundX)+Math.Abs(reboundZ) > .05f)
            {
                MoveGround(new Point(reboundX*dt,0,reboundZ*dt), RoadRadius, false);
                reboundX = Approach(reboundX,0,dt*8); reboundZ = Approach(reboundZ,0,dt*8);
            }
            if (Mode == Ride.Walk || UsesRampPhysics)
            {
                float floor = GroundHeight(Position);
                if (VerticalSpeed > 0 || Position.Y > floor + .001f)
                {
                    VerticalSpeed -= 17f * dt;
                    Point raised = Position; raised.Y = Math.Max(floor, raised.Y + VerticalSpeed * dt); Position = raised;
                    if (raised.Y <= floor)
                    {
                        if(UsesRampPhysics && VerticalSpeed < -.5f){ParkLandings++;LastLandingSpeed=-VerticalSpeed;LandingCompression=Clamp(-VerticalSpeed*(Mode==Ride.MountainBike?.055f:.028f),0,1);}
                        VerticalSpeed = 0;
                    }
                }
            }
            previousWheelieInput = input.Wheelie;
            if (!input.Wheelie) wheelieRequiresRelease = false;
            bool holdWheelie = input.Wheelie && !wheelieRequiresRelease && !input.Brake && IsBicycle && Speed >= 1.5f && Grounded;
            if (holdWheelie && Trick == BikeTrick.None) StartTrick(BikeTrick.Wheelie);
            if (Trick == BikeTrick.Wheelie && holdWheelie) wheelieWasHeld = true;
            if (Trick != BikeTrick.None)
            {
                float duration = Trick == BikeTrick.Spin180 ? .85f : 1.5f;
                if (Trick == BikeTrick.Wheelie && wheelieWasHeld)
                {
                    // sin(pi * progress) raises over 0.35s, holds at its peak, then lowers over 0.35s.
                    if (holdWheelie) TrickProgress = Approach(TrickProgress, .5f, dt / .7f);
                    else TrickProgress = Math.Max(TrickProgress, 1 - TrickProgress) + dt / .7f;
                }
                else TrickProgress += dt / duration;
                if (Trick == BikeTrick.Spin180) Yaw += (float)Math.PI * dt / duration;
                if (TrickProgress >= 1) { Trick = BikeTrick.None; TrickProgress = 0; VelocityYaw = Yaw; }
            }
            if (Mode == Ride.Walk)
            {
                float magnitude = Math.Min(1, (float)Math.Sqrt(steer * steer + forward * forward));
                Speed = magnitude<=.08f ? 0 : Approach(Speed, magnitude * (Swimming ? (input.Sprint?3.4f:2.3f) : input.Sprint ? (IsRider ? 10 : 7) : (IsRider ? 5.6f : 4.3f)), dt * 22);
                if (magnitude > .08f)
                {
                    float desired = input.CameraYaw + (float)Math.Atan2(steer, forward);
                    Yaw += Wrap(desired - Yaw) * Math.Min(1, dt * 12);
                    MoveGround(new Point((float)Math.Sin(desired) * Speed * dt, 0,
                        (float)Math.Cos(desired) * Speed * dt), .5f);
                }
            }
            else if (Mode == Ride.Airplane) Fly(dt, steer, forward, input);
            else if (Mode == Ride.Helicopter) FlyHelicopter(dt,steer,forward,input);
            else if(IsBoat)
            {
                float target=forward*(Mode==Ride.Yacht?22:Mode==Ride.Speedboat?16:9);Speed=Approach(Speed,input.Brake?0:target,dt*(input.Brake?9:Mode==Ride.Yacht?2.6f:3.6f));
                float proposedYaw=Yaw+steer*Math.Sign(Speed)*Math.Min(1,Math.Abs(Speed)/3)*dt*(Mode==Ride.Yacht?.32f:.95f);if(Mode!=Ride.Yacht||IslandLayout.HullInWater(Position,proposedYaw))Yaw=proposedYaw;VelocityYaw+=Wrap(Yaw-VelocityYaw)*Math.Min(1,dt*3.5f);
                Bank=Approach(Bank,-steer*Math.Min(1,Math.Abs(Speed)/8)*.13f,dt*.6f);
                MoveGround(new Point((float)Math.Sin(VelocityYaw)*Speed*dt,0,(float)Math.Cos(VelocityYaw)*Speed*dt),1.65f);
            }
            else
            {
                float top = Mode == Ride.DirtBike ? 26 : Mode == Ride.RaceCar ? (CarStyle == 3 ? 15 : CarStyle == 2 ? 18 : CarStyle == 1 ? 21 : 23) : Mode == Ride.Motorcycle ? 17 : Mode==Ride.MountainBike?24:Mode==Ride.MiniBike?20:Mode==Ride.Scooter?17:Mode==Ride.Skateboard?16:16f;
                if(IsLargeVehicle)top=Mode==Ride.Camper?15:19;
                if(Indoors && CanRideInside)top=3.2f;
                float accel = Mode == Ride.RaceCar ? 8 : Mode == Ride.Motorcycle ? 6.5f : Mode==Ride.MiniBike?9:Mode==Ride.MountainBike?8:IsJumpRide?5.5f:7;
                if(IsLargeVehicle)accel=Mode==Ride.Camper?4:5;
                if(NitroActive){top*=1.45f;accel+=10;}
                float target = forward >= 0 ? forward * top : forward * top * .28f;
                Drifting = Mode == Ride.RaceCar && input.Drift && Math.Abs(Speed) > 4;
                if (Trick == BikeTrick.Endo) target = Math.Min(target, 2);
                if (NeedsRepair) target = 0;
                Speed = Approach(Speed, input.Brake ? 0 : Drifting ? target * .88f : target,
                    dt * (input.Brake ? 22 : Math.Abs(forward) < .06f ? 3 : accel));
                float previousYaw=Yaw;
                Yaw += steer * (Indoors && CanRideInside ? 1 : Math.Sign(Speed) * Math.Min(1, Math.Abs(Speed) / 4)) * dt *
                    (IsLargeVehicle?.7f:Mode==Ride.MiniBike?3.1f:Mode==Ride.MountainBike?1.7f:Mode==Ride.Skateboard?2.1f:Mode==Ride.Scooter?2.5f:Mode == Ride.Bicycle ? 2.3f : Mode == Ride.Motorcycle ? 1.9f : Drifting ? 2.15f : 1.5f);
                if(Indoors && CanRideInside && Blocked(Position,.42f))Yaw=previousYaw;
                if(IsLargeVehicle && Blocked(Position,RoadRadius))Yaw=previousYaw;
                Bank = Approach(Bank, IsRoadCar ? 0 : -steer * Math.Min(1, Math.Abs(Speed) / 9) * .22f, dt * 1.5f);
                VelocityYaw += Wrap(Yaw - VelocityYaw) * Math.Min(1, dt * (Drifting ? 1.7f : 13));
                MoveGround(new Point((float)Math.Sin(VelocityYaw) * Speed * dt, 0,
                    (float)Math.Cos(VelocityYaw) * Speed * dt), RoadRadius);
            }
            if(Mode==Ride.RaceCar)
            {
                float desired;
                if(!Grounded)desired=-(float)Math.Atan2(VerticalSpeed,Math.Max(8,Math.Abs(Speed)))*.65f;
                else
                {
                    float axleX=(float)Math.Sin(Yaw)*1.25f,axleZ=(float)Math.Cos(Yaw)*1.25f;
                    float front=GroundHeight(Position+new Point(axleX,0,axleZ)),back=GroundHeight(Position+new Point(-axleX,0,-axleZ));
                    // Do not tip into the empty gap before the rear axle reaches the lip.
                    desired=Math.Abs(front-back)>2.5f?Pitch:-(float)Math.Atan2(front-back,2.5f);
                }
                Pitch=Approach(Pitch,Clamp(desired,-.55f,.55f),dt*2.2f);
            }
            Yaw = Wrap(Yaw);
            float dx = Position.X - previous.X, dz = Position.Z - previous.Z;
            DistanceTravelled += (float)Math.Sqrt(dx * dx + dz * dz);
        }

        void Fly(float dt, float steer, float forward, DriveInput input)
        {
            // GO raises the throttle; STOP lowers it. Neutral retains cruise throttle.
            Throttle = Clamp(Throttle + forward * dt * .6f - (input.Brake ? dt * .9f : 0), 0, 1);
            Speed = Approach(Speed, Throttle * 32, dt * (input.Brake ? 11 : 6));
            bool flying = Airborne;
            float climb = input.Rise ? 1 : input.Descend ? -1 : 0;
            float vertical = 0;
            if (Speed >= TakeoffSpeed || flying)
            {
                vertical = climb * 9;
                if (Speed < TakeoffSpeed) vertical = Math.Min(vertical, -(TakeoffSpeed - Speed) * .6f);
            }
            Yaw += steer * dt * (flying ? .68f : .6f) * Math.Min(1, Speed / 5);
            Pitch = Approach(Pitch, -vertical / 32, dt * .8f);
            Bank = Approach(Bank, flying ? -steer * .4f : 0, dt * .8f);
            Point proposed = Position + new Point((float)Math.Sin(Yaw) * Speed,
                vertical, (float)Math.Cos(Yaw) * Speed) * dt;
            proposed.Y = Clamp(proposed.Y, 0, Ceiling);
            if (!InBounds(proposed))
            {
                // A wide turn back into the island keeps flying safe and recoverable.
                Yaw = (float)Math.Atan2(-Position.X, -Position.Z);
                proposed.X = Clamp(Position.X, -BoundsX + 2, BoundsX - 2);
                proposed.Z = Clamp(Position.Z, -BoundsZ + 2, BoundsZ - 2);
                Say("Let's turn back toward the city!");
            }
            if (Blocked(proposed, 3.5f))
            {
                SafeRunwayReturn("A soft landing! You're back at the airfield.");
                return;
            }
            if (flying && proposed.Y <= .02f)
            {
                bool runway = Math.Abs(proposed.X - 172) <= 9 && proposed.Z > -156 && proposed.Z < 150;
                if (!runway || Speed > 24 || vertical < -9.1f)
                {
                    SafeRunwayReturn("Let's land on the runway. Ready for another flight?");
                    return;
                }
                Say("Lovely landing! Hold STOP to slow down.");
            }
            Position = proposed;
        }

        void SafeRunwayReturn(string message)
        {
            Position = RunwayStart; Yaw = 0; Speed = 0; Throttle = 0; Pitch = 0; Bank = 0;
            Say(message);
        }

        void MoveGround(Point delta, float radius, bool impact = true)
        {
            Point candidate = Position + delta;
            float floor = GroundHeight(candidate);
            if(Indoors && InteriorGround!=null && floor-candidate.Y>.45f && VerticalSpeed<=0)return;
            if(IsRoadCar && floor-Position.Y>.45f && Grounded){Speed=0;return;}
            if(OnDivePlatform && (Math.Abs(candidate.X-35)>=5 || candidate.Z<=103 || candidate.Z>=120))OnDivePlatform=false;
            if(UsesRampPhysics && floor-Position.Y>.6f && !Grounded)return;
            if(UsesRampPhysics && VerticalSpeed<=0 && Grounded && floor<Position.Y-.6f)
                VerticalSpeed=Mode==Ride.RaceCar?Math.Min(11.5f,4.5f+Math.Abs(Speed)*.25f):4.5f+Math.Abs(Speed)*.25f;
            bool snapToGround = Mode != Ride.Walk && !UsesRampPhysics || VerticalSpeed <= 0 && Position.Y <= GroundHeight(Position) + .02f;
            if(UsesRampPhysics && VerticalSpeed<-.5f && (snapToGround || candidate.Y<=floor+.02f))
            {ParkLandings++;LastLandingSpeed=-VerticalSpeed;LandingCompression=Clamp(-VerticalSpeed*(Mode==Ride.MountainBike?.055f:.028f),0,1);VerticalSpeed=0;}
            if (snapToGround) candidate.Y = floor;
            else candidate.Y = Math.Max(candidate.Y, floor);
            if (Indoors) { candidate.X = Clamp(candidate.X, RoomX-RoomHalfX+radius, RoomX+RoomHalfX-radius); candidate.Z = Clamp(candidate.Z, RoomZ-RoomHalfZ+radius, RoomZ+RoomHalfZ-radius); }
            candidate.X = Clamp(candidate.X, -BoundsX + radius, BoundsX - radius);
            candidate.Z = Clamp(candidate.Z, -BoundsZ + radius, BoundsZ - radius);
            if(IsLargeVehicle)
            {
                float c=Math.Abs((float)Math.Cos(Yaw)),s=Math.Abs((float)Math.Sin(Yaw));
                float hx=c*RoadRadius+s*RoadHalfLength,hz=s*RoadRadius+c*RoadHalfLength;
                candidate.X=Clamp(candidate.X,-BoundsX+hx,BoundsX-hx);candidate.Z=Clamp(candidate.Z,-BoundsZ+hz,BoundsZ-hz);
            }
            if (!Blocked(candidate, radius)) { Position = candidate; return; }
            if (impact && Mode != Ride.Walk && Math.Abs(Speed) > 1.8f && impactCooldown <= 0)
            {
                ImpactSpeed = Math.Abs(Speed); ImpactVersion++; impactCooldown = 1.2f;
                float length = Math.Max(.001f,(float)Math.Sqrt(delta.X*delta.X+delta.Z*delta.Z));
                reboundX = -delta.X/length*ImpactSpeed*.38f; reboundZ=-delta.Z/length*ImpactSpeed*.38f;
                Trick=BikeTrick.None;
                if (RecordCarCrash(delta,candidate,radius)) { }
                else Say(Mode == Ride.Bicycle ? "Boing! Your bike bounced back. Ready to ride!" : "Bump! Ease off and steer away.");
            }
            // Sliding along walls prevents getting stuck when walking diagonally.
            Point xOnly = new Point(candidate.X, candidate.Y, Position.Z);
            Point zOnly = new Point(Position.X, candidate.Y, candidate.Z);
            if (!Blocked(xOnly, radius)) Position = xOnly;
            else if (!Blocked(zOnly, radius)) Position = zOnly;
            if (Mode != Ride.Walk) Speed = 0;
        }

        public Func<Point,float,bool> TrafficObstruction;
        public bool Blocked(Point point, float radius)
        {
            if(Indoors && InteriorBoundary!=null && !InteriorBoundary(point,radius))return true;
            if(!Indoors&&TrafficObstruction!=null&&TrafficObstruction(point,radius))return true;
            if(!Indoors && Mode==Ride.Yacht && !IslandLayout.HullInWater(point,Yaw))return true;
            if(!Indoors && IsBoat && (!IsOcean(point) || point.Z<42 || point.Z<205&&point.X>117))return true;
            if(!Indoors && WorldIndex==1 && point.Z>205 && !IsAircraft && Mode!=Ride.Walk && !IsBoat && IsOcean(point))return true;
            if(!Indoors && HarborWater && point.Z<=205 && !IsAircraft && Mode!=Ride.Walk && !IsBoat && point.Z>39 && point.X<120)
            {
                bool pier = (Math.Abs(point.X+65)<2.2f-radius && point.Z<63) || (Math.Abs(point.X+130)<2.2f-radius && point.Z<68);
                if(!pier)return true;
            }
            if (!Indoors && PondEnabled && (!IsAircraft || point.Y < 1.5f))
            {
                float ex = (point.X + 13) / (7.9f + radius), ez = (point.Z - 8) / (11 + radius);
                bool bridge = Math.Abs(point.Z - 8) <= 1.5f - radius;
                if (ex * ex + ez * ez < 1 && !bridge) return true;
            }
            foreach (Obstacle obstacle in Indoors ? InteriorObstacles : Obstacles)
            {
                if (point.Y >= obstacle.Height + .02f || point.Y+.7f<obstacle.Bottom) continue;
                if(Indoors && CanRideInside){if(BikeOverlap(point,Yaw,.42f,BikeHalfLength,obstacle))return true;continue;}
                if(Mode==Ride.Yacht){if(BikeOverlap(point,Yaw,11,48,obstacle))return true;continue;}
                if(IsLargeVehicle && radius>1){if(BikeOverlap(point,Yaw,RoadRadius,RoadHalfLength,obstacle))return true;continue;}
                float localX=point.X-obstacle.X,localZ=point.Z-obstacle.Z;
                if(obstacle.Yaw!=0){float c=(float)Math.Cos(obstacle.Yaw),s=(float)Math.Sin(obstacle.Yaw),x=localX;localX=x*c-localZ*s;localZ=x*s+localZ*c;}
                float dx = Math.Max(Math.Abs(localX) - obstacle.HalfX, 0);
                float dz = Math.Max(Math.Abs(localZ) - obstacle.HalfZ, 0);
                if (dx * dx + dz * dz < radius * radius) return true;
            }
            return false;
        }

        public string District => Indoors ? RoomName : WorldIndex==4 ? "Skyline Skatepark" : WorldIndex==3 ? GeneratedWorldName : WorldIndex==1&&Position.Z>205 ? (IslandLayout.At(Position,15)>=0?IslandLayout.Names[IslandLayout.At(Position,15)]:"Island Sea") : WorldIndex==1 ? (Position.X>128 ? "Palm Cove Airfield" : "Palm Cove Marina") : WorldIndex==2 ? "Cloud Peaks" : Math.Abs(Position.X)>230 || Math.Abs(Position.Z)>210 ? "Greater Little City" : Position.X > 128 ? "Sunny Airfield" :
            Math.Abs(Position.X) < 28 && Math.Abs(Position.Z) < 28 ? "Willow Park" :
            Position.Z > 35 ? "Old Town" : Position.X < -35 ? "Garden Quarter" : "Park Avenue";
        public void Say(string message) { Notice = message; NoticeVersion++; }
        bool InBounds(Point p) => Math.Abs(p.X) <= BoundsX && Math.Abs(p.Z) <= BoundsZ;
        static float Approach(float value, float target, float delta) => value < target ? Math.Min(target, value + delta) : Math.Max(target, value - delta);
        static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
        static float Wrap(float value)
        {
            while (value > Math.PI) value -= (float)Math.PI * 2;
            while (value < -Math.PI) value += (float)Math.PI * 2;
            return value;
        }
    }
}
