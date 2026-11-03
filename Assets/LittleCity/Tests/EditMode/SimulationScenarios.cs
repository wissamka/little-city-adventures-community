using System;
using System.Collections.Generic;

namespace LittleCity.Tests
{
    public static class SimulationScenarios
    {
        public static readonly List<string> Passed = new List<string>();
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Passed.Add(message); }
        static void Advance(Simulation sim, float seconds, DriveInput input, float step = 1f / 60)
        { for (int i = 0; i < (int)Math.Round(seconds / step); i++) sim.Step(step, input); }
        public static void Run()
        {
            Passed.Clear();
            var s = new Simulation();
            Check(s.Mode == Ride.Walk && s.Position.Z == -24, "Starts safely on foot at Willow Park");
            for (int i = 0; i < Simulation.CharacterNames.Length; i++) { s.SelectCharacter(i); Check(s.Character == i, "Character selection " + i); }
            Advance(s, 2, new DriveInput { Forward = 1 });
            Check(s.Position.Z > -17 && s.Position.Z < -14, "Walking covers the expected distance");
            s.Reset(); Advance(s, 2, new DriveInput { Forward = 1, Sprint = true });
            Check(s.Position.Z > -12, "Running is faster than walking");
            s.Reset(); Advance(s, 2, new DriveInput { Forward = 1, CameraYaw = (float)Math.PI / 2 });
            Check(s.Position.X > 7 && Math.Abs(s.Position.Z + 24) < .01, "Walking follows the camera direction");

            var wall = new Simulation(); wall.Obstacles.Add(new Obstacle(0, -20, 3, .4f, 8));
            Advance(wall, 3, new DriveInput { Forward = 1, Sprint = true }, .1f);
            Check(wall.Position.Z < -20.8f, "Walking cannot tunnel through a building during slow frames");
            wall.Reset(); Advance(wall, 3, new DriveInput { Forward = 1, Steering = 1 });
            Check(wall.Position.X > 4, "Diagonal walking slides along a wall");

            float[] targetSpeed = { 0, 23, 16f, 17 };
            foreach (Ride ride in new[] { Ride.RaceCar, Ride.Bicycle, Ride.Motorcycle })
            {
                s = new Simulation(); s.SwitchRide(ride);
                Advance(s, 4, new DriveInput { Forward = 1 });
                Check(Math.Abs(s.Speed - targetSpeed[(int)ride]) < .1, ride + " reaches its distinct top speed");
                float heading = s.Yaw;
                Advance(s, .5f, new DriveInput { Forward = 1, Steering = 1 });
                Check(Math.Abs(s.Yaw - heading) > .2, ride + " steering turns the vehicle");
                Advance(s, 2, new DriveInput { Brake = true });
                Check(Math.Abs(s.Speed) < .01, ride + " braking brings it to rest");
                Advance(s, 2, new DriveInput { Forward = -1 });
                Check(s.Speed < -1, ride + " can reverse");
                s.SwitchRide(Ride.Walk);
                Check(s.Mode == Ride.Walk && s.Position.Y == 0 && !s.Blocked(s.Position, .5f), ride + " exits to safe ground");
            }
            s = new Simulation(); s.SwitchRide(Ride.RaceCar); s.Obstacles.Add(new Obstacle(12, -32, .25f, 5, 10));
            Advance(s, 5, new DriveInput { Forward = 1 }, .2f);
            Check(s.Position.X < 10.5f && s.Speed < 1, "Fast vehicle collision blocks a narrow wall without tunneling");

            s = new Simulation(); s.SwitchRide(Ride.Airplane);
            Advance(s, 1, new DriveInput { Forward = 1, Rise = true });
            Check(!s.Airborne, "Airplane cannot rise before takeoff speed");
            Advance(s, 3, new DriveInput { Forward = 1, Rise = true });
            Check(s.Airborne && s.Position.Y > 6, "Airplane accelerates and genuinely takes off");
            float altitude = s.Position.Y;
            Advance(s, 1, new DriveInput());
            Check(Math.Abs(s.Position.Y - altitude) < .05, "Airplane holds altitude with neutral climb input");
            Advance(s, 1, new DriveInput { Steering = 1 });
            Check(Math.Abs(s.Yaw) > .5, "Airplane steering changes heading in flight");
            s.SwitchRide(Ride.Walk);
            Check(s.Position.X == 0 && s.Position.Z == -24, "Leaving an airborne airplane returns safely to the park");

            s = new Simulation(); s.SwitchRide(Ride.Airplane);
            Advance(s, 4, new DriveInput { Forward = 1, Rise = true });
            Advance(s, 1, new DriveInput { Brake = true });
            int descentSteps = 0;
            while (s.Airborne && descentSteps++ < 240) s.Step(1f / 60, new DriveInput { Descend = true });
            Check(!s.Airborne && s.Position.Z > -125 && s.Position.Z < 150, "Controlled descent lands on the runway without resetting");
            Check(s.Notice.StartsWith("Lovely landing"), "Successful landing is acknowledged");
            Advance(s, 4, new DriveInput { Brake = true });
            Check(s.Speed == 0 && s.Throttle == 0, "Aircraft brakes to a full stop after landing");

            s = new Simulation(); s.SwitchRide(Ride.Airplane);
            Advance(s, 4, new DriveInput { Forward = 1, Rise = true });
            s.Obstacles.Add(new Obstacle(s.Position.X, s.Position.Z + 8, 5, 2, 80));
            Advance(s, .6f, new DriveInput());
            Check(s.Position.Z == Simulation.RunwayStart.Z && !s.Airborne, "Aircraft collision gently resets to the airfield");
            s = new Simulation(); s.SwitchRide(Ride.Airplane);
            Advance(s, 60, new DriveInput { Forward = 1, Rise = true });
            Check(s.Position.Y <= Simulation.Ceiling && Math.Abs(s.Position.X) <= Simulation.WorldLimitX && Math.Abs(s.Position.Z) <= Simulation.WorldLimitZ,
                "Flight remains inside the altitude and island boundaries");
            s.Reset(); Advance(s, 120, new DriveInput { Forward = 1, Sprint = true });
            Check(s.Position.Z <= Simulation.WorldLimitZ, "Walking stays on the island");
            s.Reset(); s.Step(float.NaN, new DriveInput { Forward = 1 });
            Check(!float.IsNaN(s.Position.Z), "Invalid timing cannot corrupt the player position");
            var a = new Simulation(); var b = new Simulation(); a.SwitchRide(Ride.RaceCar); b.SwitchRide(Ride.RaceCar);
            Advance(a, 3, new DriveInput { Forward = 1, Steering = .2f }, 1f / 30);
            Advance(b, 3, new DriveInput { Forward = 1, Steering = .2f }, 1f / 120);
            Check(Math.Abs(a.Position.X - b.Position.X) < .3 && Math.Abs(a.Position.Z - b.Position.Z) < .3,
                "Vehicle motion stays consistent at 30 and 120 updates per second");
            s=new Simulation(); s.Step(.016f,new DriveInput{Jump=true}); float peak=0;
            for(int i=0;i<70;i++){s.Step(.016f,new DriveInput{Jump=true});peak=Math.Max(peak,s.Position.Y);}
            Check(peak>.95f && peak<1.3f && s.Grounded,"Jump has a one-metre arc and lands on the ground without auto-repeat");
            s.Step(.02f,new DriveInput());s.Step(.02f,new DriveInput{Jump=true});Check(s.Position.Y>0,"Jump can be triggered again after release");
            s=new Simulation{PondEnabled=true};s.Obstacles.Add(new Obstacle(-13,6.35f,8.8f,.1f,1.4f));s.Obstacles.Add(new Obstacle(-13,9.65f,8.8f,.1f,1.4f));
            s.Place(new Point(-23,0,8));Advance(s,5,new DriveInput{Forward=1,CameraYaw=(float)Math.PI/2});
            Check(s.Position.X>-5,"Bridge deck can be crossed from shore to shore");
            s.Place(new Point(-13,.2f,8));Advance(s,1,new DriveInput{Forward=1});Check(s.Position.Z<9.2f,"Bridge rails prevent walking sideways into the pond");
            s.Place(new Point(-23,0,0));Advance(s,5,new DriveInput{Forward=1,CameraYaw=(float)Math.PI/2,Jump=true});Check(s.Position.X<-18,"Pond boundary blocks walking and jumping into water outside the bridge");
            s=new Simulation();s.SwitchRide(Ride.Bicycle);Advance(s,3,new DriveInput{Forward=1});
            Advance(s,4,new DriveInput{Forward=1,Wheelie=true});Check(s.Trick==BikeTrick.Wheelie && Math.Abs(s.TrickProgress-.5f)<.01f,"Wheelie remains raised beyond the old 1.5-second timer while held");
            Advance(s,.5f,new DriveInput{Forward=1});Check(s.Trick==BikeTrick.None,"Wheelie lowers after release");
            float before=s.Yaw;Check(s.StartTrick(BikeTrick.Spin180),"A moving bike starts a 180");Advance(s,.86f,new DriveInput{Forward=1});
            Check(Math.Abs(Math.Abs(s.Yaw-before)-(float)Math.PI)<.10f,"Bike 180 changes the actual riding direction");
            s.ToggleStanding();Check(s.Standing,"Standing riding state persists until toggled");
            s=new Simulation();s.SwitchRide(Ride.RaceCar);Advance(s,2,new DriveInput{Forward=1});Advance(s,.4f,new DriveInput{Forward=1,Steering=1,Drift=true});
            Check(s.Drifting && Math.Abs(s.VelocityYaw-s.Yaw)>.25f,"Drift separates travel direction from car heading");
            Advance(s,1,new DriveInput{Forward=1});Check(!s.Drifting && Math.Abs(s.VelocityYaw-s.Yaw)<.03f,"Releasing drift restores tire grip");
            s=new Simulation();s.SwitchRide(Ride.RaceCar);s.Obstacles.Add(new Obstacle(30,-32,.2f,4,5));Advance(s,4,new DriveInput{Forward=1});
            Check(!s.NeedsRepair && s.CarCrashes==1 && s.ImpactSpeed>=10 && s.ImpactVersion>0,"A significant car impact dents the body without breaking the first time");s.Repair();Check(!s.NeedsRepair,"A damaged car can be replaced immediately");
            s=new Simulation();s.SwitchRide(Ride.Bicycle);s.Obstacles.Add(new Obstacle(18,-32,.2f,4,5));Advance(s,3.4f,new DriveInput{Forward=1});
            Check(s.ImpactVersion>0 && !s.NeedsRepair,"A bicycle collision rebounds without disabling the bike");
            s=new Simulation();s.Place(new Point(135,0,-50),1);s.EnterInterior();Check(s.Indoors && s.Position.Z==-178,"Door entry moves into the explorable interior");s.SwitchRide(Ride.Airplane);Check(s.Mode==Ride.Walk,"Vehicles cannot spawn through an interior");s.ExitInterior();Check(!s.Indoors && Math.Abs(s.Position.X-135)<.01f,"House exit returns to the original exterior door");
            a=new Simulation();b=new Simulation();a.SwitchRide(Ride.Bicycle);b.SwitchRide(Ride.Bicycle);Advance(a,6,new DriveInput{Forward=1},.2f);Advance(b,6,new DriveInput{Forward=1},1f/60);
            Check(Math.Abs(a.DistanceTravelled-b.DistanceTravelled)<.2f,"Low rendering rates preserve physical metres travelled per second");
            Check(Math.Abs(a.ActualSpeed*3.6f-57.6f)<.1f,"Bicycle top speed is measured as 16 metres per second = 57.6 km/h");
            s=new Simulation();s.SwitchRide(Ride.Airplane);s.TravelTo(1);Check(s.WorldIndex==0,"Inter-world travel requires actual airborne altitude");
            Advance(s,5,new DriveInput{Forward=1,Rise=true});s.TravelTo(1);Check(s.WorldIndex==1 && s.Airborne && s.Position.Y==32,"Flight travel arrives safely above the destination runway");
            s=new Simulation{HarborWater=true};Check(!s.Blocked(new Point(-80,0,60),.5f),"The marina ocean allows swimming from shore");Check(!s.Blocked(new Point(-65,0,60),.5f),"The yacht pier remains walkable through the water boundary");
            s.Place(new Point(1,0,-27));s.EnterRoom(new Point(0,0,-27),8.7f,29.5f,"Yacht");s.ChangeDeck(new Point(6,4.4f,-25),"Sun deck");Advance(s,1,new DriveInput{Forward=1});Check(Math.Abs(s.Position.Y-4.4f)<.01f,"Walking on the upper yacht deck preserves its real floor height");
            Point rest=s.Position;s.BeginRest(RestPose.Sitting,new Point(5,4.7f,-20),0);Advance(s,2,new DriveInput{Forward=1,Jump=true});Check(s.Position.X==5 && s.Resting==RestPose.Sitting,"Sitting anchors the character while movement input is held");s.EndRest();Check(Math.Abs(s.Position.X-rest.X)<.01f && Math.Abs(s.Position.Z-rest.Z)<.01f,"Standing returns to the clear position beside the furniture");
            s=new Simulation();s.EnterVehicle(Ride.RaceCar,2,new Point(135,0,-57),1);Check(s.Mode==Ride.RaceCar && s.CarStyle==2 && s.Position.X==135,"Proximity entry preserves the selected parked model and world position");
            var route=new WorldRoute(76321);route.Visit(405);var restored=WorldRoute.Restore(route.Save());Check(restored.LastVisit==405 && restored.Describe(405).Seed==route.Describe(405).Seed,"Saved route restores the same seed and progress");
            var seeds=new System.Collections.Generic.HashSet<uint>();for(int i=1;i<=1000;i++)seeds.Add(route.Describe(i).Seed);Check(seeds.Count==1000,"A thousand route stops produce distinct reproducible seeds");
            Check(route.Describe(1).Theme==WorldTheme.Jungle && route.Describe(2).Theme==WorldTheme.NewYork && route.Describe(3).Theme==WorldTheme.Prehistoric && route.Describe(4).Theme==WorldTheme.Asgard,"The route includes all four requested themes in order");
            Check(WorldRoute.CrossedGate(new Point(172,32,160),new Point(172,32,170),true),"An actual forward flight crosses the next-world gate");
            Check(!WorldRoute.CrossedGate(new Point(120,32,160),new Point(120,32,170),true) && !WorldRoute.CrossedGate(new Point(172,10,160),new Point(172,10,170),true),"Flying beside or below a gate does not transition");
            Check(WorldRoute.CrossedGate(new Point(172,32,-165),new Point(172,32,-175),false),"Flying south crosses the return gate");
            s=new Simulation{HarborWater=true};s.Place(new Point(80,0,38));Advance(s,2,new DriveInput{Forward=1});Check(s.Swimming && s.Position.Y<-.25f,"Walking off the beach transitions to surface swimming");
            Advance(s,4,new DriveInput{Forward=1,CameraYaw=(float)Math.PI});Check(!s.Swimming && s.Position.Y==0,"Swimming back to shore resumes walking");
            s.Place(new Point(35,-1.05f,102));s.ClimbDivingPlatform();Check(s.OnDivePlatform && s.Position.Y==4.4f,"The offshore ladder reaches the actual elevated platform");s.Dive();Advance(s,2,new DriveInput{});Check(s.Swimming && !s.OnDivePlatform,"Diving off the board ends safely in swimming");
            s=new Simulation{HarborWater=true};s.EnterVehicle(Ride.Speedboat,0,new Point(80,-1.05f,70),0);Advance(s,5,new DriveInput{Forward=1});Check(Math.Abs(s.Speed-16)<.1f && s.Position.Y<0,"Speedboat reaches its distinct water speed while floating");
            Advance(s,1,new DriveInput{Forward=1,Steering=1});Check(Math.Abs(s.Yaw)>.5f,"Speedboat steering changes its water course");Advance(s,3,new DriveInput{Brake=true});Check(s.ExitVehicle() && s.Swimming,"A stopped boat exits safely into swimming");
            s=new Simulation{HarborWater=true};s.EnterVehicle(Ride.Motorboat,0,new Point(80,-1.05f,48),(float)Math.PI);Advance(s,5,new DriveInput{Forward=1});Check(s.Position.Z>=42,"Motorboats cannot drive onto the shore");
            var headingControl=new WalkingHeading();float walkingReference=headingControl.Resolve(0,1,42,90,0);Check(walkingReference==42,"Forward starts along the visible camera direction instead of a smoothed target yaw");
            Check(headingControl.Resolve(0,1,80,80,0)==42,"Automatic chase motion cannot steer held forward input");
            Check(headingControl.Resolve(0,1,80,120,1)==120,"Explicit keyboard or touch orbit updates held movement direction");
            headingControl.Resolve(0,0,130,130,1);Check(headingControl.Resolve(0,1,140,160,1)==140,"Releasing and pressing forward chooses the new visible view");
            var buildingPlan=new BuildingPlan{X=0,Z=0,Width=16,Depth=16,Floors=5,Story=3.2f,Name="Test tower"};
            s=new Simulation();s.EnterBuilding(new Point(buildingPlan.StairX,0,buildingPlan.StairStart-.5f),0,0,7.8f,7.8f,"Test tower",buildingPlan.FloorAt,0);
            Advance(s,3,new DriveInput{Forward=1});Check(s.Position.Y>3.1f && s.Position.Y<3.3f,"Walking up actual stair geometry rises smoothly to the next floor");
            s.Place(new Point(buildingPlan.StairX,3.2f,buildingPlan.StairEnd+.3f),(float)Math.PI);Advance(s,3,new DriveInput{Forward=1,CameraYaw=(float)Math.PI});Check(s.Position.Y<.1f,"The same connected staircase allows descending");
            s.Place(new Point(0,0,0));s.InteriorObstacles.Add(new Obstacle(0,0,1,1,4,3.2f));Check(!s.Blocked(s.Position,.5f),"Furniture upstairs does not collide with the player downstairs");
        }
    }
}
