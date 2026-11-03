using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class CharacterModel
    {
        public Transform Root, Head, Body,LongHair;
        public GummyCharacter Gummy;
        public bool DrivingCar {get;private set;}
        public readonly Transform[] Upper = new Transform[4], Lower = new Transform[4], Ends = new Transform[4];
        public float FootError { get; private set; }
        public float HandError { get; private set; }
        static void Segment(Transform t, Vector3 from, Vector3 to, float width)
        {
            t.localPosition = (from + to) * .5f;
            t.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            t.localScale = new Vector3(width, (to - from).magnitude + width * .25f, width);
        }
        void Limb(int i, Vector3 start, Vector3 end, float a, float b, Vector3 bend)
        {
            Vector3 d = end - start; float length = Mathf.Clamp(d.magnitude, .01f, a + b - .001f);
            Vector3 axis = d.normalized, pole = (bend - Vector3.Dot(bend, axis) * axis).normalized;
            float along = (a * a - b * b + length * length) / (2 * length);
            Vector3 joint = start + axis * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Vector3 reached = start + axis * length;
            Segment(Upper[i], start, joint, i < 2 ? .30f : .25f);
            Segment(Lower[i], joint, reached, i < 2 ? .23f : .20f);
            Ends[i].localPosition = reached;
            Ends[i].localRotation = Quaternion.identity;
        }
        public void Pose(float time, float speed, bool seated, bool pedaling)
        {
            DrivingCar=false;Body.localRotation = Quaternion.identity;
            if(LongHair!=null){LongHair.localScale=Vector3.one;LongHair.localRotation=Quaternion.identity;}
            float stride = Mathf.Sin(time * 9) * Mathf.Min(1, Mathf.Abs(speed) / 3);
            for (int side = -1; side <= 1; side += 2)
            {
                int leg = side < 0 ? 0 : 1, arm = leg + 2;
                Vector3 hip = new Vector3(side * .2f, 1.08f, 0);
                Vector3 foot = new Vector3(side * .2f, seated ? .56f : .14f + Mathf.Max(0, stride * side) * .16f, seated ? .7f : stride * side * .38f);
                Limb(leg, hip, foot, .53f, .52f, Vector3.forward);
                Limb(arm, new Vector3(side * .43f, 1.76f, 0), new Vector3(side * .46f, seated ? 1.30f : 1.03f, seated ? .65f : -stride * side * .22f), .42f, .40f, Vector3.back);
            }
            Head.localRotation = Quaternion.Euler(0, Mathf.Sin(time * .65f) * (Mathf.Abs(speed) < .1 ? 4 : 0), 0);
            Gummy?.Pose(time,speed,seated);
        }
        public void RaiseHammer(bool raised){if(raised){Limb(3,new Vector3(.43f,1.76f,0),new Vector3(.6f,2.42f,.3f),.42f,.40f,Vector3.forward);}}
        public void Swim(float time,float speed)
        {
            Pose(time,speed,false,false);
            for(int side=-1;side<=1;side+=2)
            {
                int leg=side<0?0:1;float phase=time*4+leg*Mathf.PI;
                Limb(leg,new Vector3(side*.2f,1.08f,0),new Vector3(side*.3f,.18f,Mathf.Sin(phase)*.24f),.53f,.52f,Vector3.forward);
                Limb(leg+2,new Vector3(side*.43f,1.76f,0),new Vector3(side*.9f,1.7f+Mathf.Sin(phase)*.25f,Mathf.Cos(phase)*.25f),.42f,.40f,Vector3.back);
            }
            Head.localRotation=Quaternion.Euler(-35,0,0);
            
        }
        public void SetSleeping(bool sleeping)
        {
            Gummy?.Sleep(sleeping);
            foreach(Transform child in Head)
            {
                if(child.name=="Eye white" || child.name=="Warm eyes" || child.name=="Eye sparkle" || child.name=="Eye pupil")child.gameObject.SetActive(!sleeping);
                if(child.name=="Closed eye")child.gameObject.SetActive(sleeping);
            }
        }
        public void FitRide(VehicleModel vehicle, bool standing)
        {
            DrivingCar=vehicle.Mode==Ride.RaceCar && vehicle.CarStyle!=3;
            if(DrivingCar)Root.localScale=Vector3.one*vehicle.ToyDriverScale;
            if(DrivingCar)Root.rotation=vehicle.Root.rotation;
            Vector3 hip = vehicle.Root.TransformPoint(vehicle.Seat + (standing ? new Vector3(0,.2f,.12f) : Vector3.zero));
            Root.position = hip - Root.TransformVector(new Vector3(0, 1.08f, 0));
            float lean=DrivingCar?vehicle.DriverLean:(vehicle.Mode == Ride.Bicycle || vehicle.Mode==Ride.MountainBike || vehicle.Mode==Ride.MiniBike) ? (standing ? 46 : 38) : vehicle.Mode == Ride.Motorcycle || vehicle.Mode==Ride.DirtBike ? 38 : 0;
            Body.localRotation = Quaternion.Euler(lean,0,0);
            FootError = HandError = 0;
            for (int i = 0; i < 2; i++)
            {
                int side = i == 0 ? -1 : 1;
                Vector3 foot = vehicle.Pedals.Count == 2 ? vehicle.Pedals[i].position + vehicle.Root.up * .065f : vehicle.Root.TransformPoint(new Vector3(vehicle.Seat.x + side * .12f, .53f, .25f));
                Vector3 hand = vehicle.Root.TransformPoint(vehicle.Grips[i]);
                Vector3 shoulder = Root.InverseTransformPoint(Body.TransformPoint(new Vector3(side * .43f, .68f, 0)));
                var hipLocal=new Vector3(side*.2f,1.08f,0);var footLocal=Root.InverseTransformPoint(foot);var handLocal=Root.InverseTransformPoint(hand);
                float legs=(DrivingCar || vehicle.Mode==Ride.MiniBike)?Mathf.Max(1.05f,Vector3.Distance(hipLocal,footLocal)/.86f):1.05f;
                float arms=(DrivingCar || vehicle.Mode==Ride.MiniBike)?Mathf.Max(.82f,Vector3.Distance(shoulder,handLocal)/.90f):.82f;
                Limb(i,hipLocal,footLocal,legs*.505f,legs*.495f,DrivingCar?Vector3.up:Vector3.forward);
                Limb(i+2,shoulder,handLocal,arms*.5122f,arms*.4878f,Vector3.back+Vector3.right*side*.4f);
                FootError = Mathf.Max(FootError, Vector3.Distance(Ends[i].position, foot));
                HandError = Mathf.Max(HandError, Vector3.Distance(Ends[i + 2].position, hand));
            }
            Head.localRotation = Quaternion.Euler(-lean*.55f,0,0);
            if(LongHair!=null){LongHair.localScale=new Vector3(1,.72f,1);LongHair.localRotation=Quaternion.Euler(8,0,0);}
        }
    }

    public sealed class VehicleModel
    {
        public Transform Root, Propeller, Crank;
        public float SuspensionCompression;
        public readonly List<Transform> Suspension = new List<Transform>();
        public Ride Mode;
        public int CarStyle;
        public float PedalPhase;
        public bool Standing;
        public readonly List<Transform> Wheels = new List<Transform>();
        public readonly List<Transform> Pedals = new List<Transform>();
        public Vector3 Seat;
        public float DriverScale=.5f,ToyDriverScale=.44f,DriverLean;
        public readonly Vector3[] Grips = new Vector3[2];
        public void Animate(float speed, float dt, float throttle)
        {
            var helicopter=Root.GetComponent<HelicopterRig>();if(helicopter!=null)helicopter.Animate(dt,throttle);
            var graphics=Root.GetComponent<GraphicsCar>();if(graphics!=null)graphics.AnimateConcept(speed,dt);
            foreach(var spring in Suspension)spring.localScale=new Vector3(1,1-SuspensionCompression*.4f,1);
            foreach (Transform wheel in Wheels) wheel.Rotate(Vector3.right, speed * dt / (Mode==Ride.Van || Mode==Ride.Camper?.62f:Mode==Ride.MiniBike ? .1508f : Mode == Ride.Bicycle ? .3975f : .47f) * Mathf.Rad2Deg, Space.Self);
            if (Propeller != null) Propeller.Rotate(Vector3.forward, (200 + throttle * 1800) * dt, Space.Self);
            if (Crank != null)
            {
                if(Standing)PedalPhase=Mathf.PI/2;else PedalPhase += speed * dt * 1.3f;
                Crank.localRotation = Quaternion.Euler(PedalPhase * Mathf.Rad2Deg, 0, 0);
                foreach (Transform pedal in Pedals) pedal.rotation = Root.rotation;
            }
        }
    }

    public static partial class Models
    {
        public static CharacterModel Character(Transform parent, int index)
        {
            var rig = new CharacterModel { Root = Art.Group(Simulation.CharacterNames[index], parent) };
            Transform r = rig.Root;
            Color skin = Art.Hex(index == 2 ? "A9683D" : index == 1 ? "E7AE7C" : "F3BB8C");
            Color jacket = index == 4 ? Art.Hex("725BB0") : index == 5 ? Art.Hex("44834A") : index == 3 ? Art.Hex("BC3842") : index == 0 ? Art.Blue : index == 1 ? Art.Teal : Art.Hex("F58A29");
            Color pants = Art.Hex(index == 3 ? "263448" : index == 0 ? "A99261" : index == 1 ? "324957" : "647B3B");
            Color shoes = index == 3 ? Art.Hex("C52C36") : index == 0 ? Art.Blue : index == 1 ? Art.Hex("8864B6") : Art.Hex("EB7629");
            Color hair = Art.Hex(index == 3 ? "6F4838" : index == 0 ? "653C27" : "30261F");
            Art.Ball("Soft tailored hoodie", r, new Vector3(0, 1.46f, 0), new Vector3(.83f, .86f, .53f), jacket);
            Art.Box("Cream shirt", r, new Vector3(0, 1.46f, .25f), new Vector3(.27f, .6f, .06f), Art.White,.028f);
            Art.Ball("Adventure badge",r,new Vector3(.19f,1.6f,.27f),new Vector3(.12f,.14f,.035f),Art.Yellow);
            Art.Ball("Soft hood", r, new Vector3(0, 1.82f, -.16f), new Vector3(.65f, .39f, .5f), jacket);
            Art.Box("Pocket left", r, new Vector3(-.25f, 1.28f, .22f), new Vector3(.16f, .19f, .07f), jacket * .9f);
            Art.Box("Pocket right", r, new Vector3(.25f, 1.28f, .22f), new Vector3(.16f, .19f, .07f), jacket * .9f);
            Art.Tube("Drawstring", r, new Vector3(-.12f, 1.77f, .26f), new Vector3(-.12f, 1.56f, .27f), .016f, Art.White);
            Art.Tube("Drawstring", r, new Vector3(.12f, 1.77f, .26f), new Vector3(.12f, 1.56f, .27f), .016f, Art.White);
            Art.Box("Backpack", r, new Vector3(0, 1.5f, -.32f), new Vector3(.54f, .6f, .26f), index == 1 ? shoes : Art.Teal, .1f);
            // Independent two-bone limbs let shoes track pedals and hands meet the grips.
            for (int i = 0; i < 4; i++)
            {
                bool leg = i < 2;
                rig.Upper[i] = Art.Ball(leg ? "Rounded thigh" : "Rounded sleeve", r, Vector3.zero, Vector3.one, leg ? pants : jacket).transform;
                rig.Lower[i] = Art.Ball(leg ? "Calf" : "Forearm sleeve", r, Vector3.zero, Vector3.one, leg ? (index == 0 ? skin : pants) : jacket).transform;
                rig.Ends[i] = Art.Group(leg ? "Foot contact" : "Hand contact", r);
                if (leg)
                {
                    Art.Box("Sneaker", rig.Ends[i], new Vector3(0, .005f, .045f), new Vector3(.34f, .22f, .47f), shoes, .10f);
                    Art.Box("White sole", rig.Ends[i], new Vector3(0, -.075f, .045f), new Vector3(.35f, .065f, .49f), Art.White, .025f);
                    Art.Ball("Sock", rig.Ends[i], new Vector3(0, .11f, -.015f), new Vector3(.22f, .17f, .25f), Art.White);
                    for (int lace = 0; lace < 3; lace++) Art.Tube("Lace", rig.Ends[i], new Vector3(-.10f, .10f, .05f + lace * .045f), new Vector3(.10f, .10f, .05f + lace * .045f), .013f, Art.White);
                    if(index==3)
                    {
                        var shoe=rig.Ends[i];
                        Art.Box("Basketball high top collar",shoe,new Vector3(0,.17f,-.085f),new Vector3(.32f,.30f,.24f),Art.Ink,.05f);
                        Art.Box("Red ankle overlay",shoe,new Vector3(0,.15f,-.15f),new Vector3(.34f,.20f,.12f),shoes,.025f);
                        Art.Box("White toe panel",shoe,new Vector3(0,.06f,.20f),new Vector3(.27f,.12f,.14f),Art.White,.04f);
                        Art.Box("Padded tongue",shoe,new Vector3(0,.18f,.025f),new Vector3(.15f,.27f,.07f),Art.White,.025f);
                        foreach(int side in new[]{-1,1})
                        {
                            Art.Box("White leather side panel",shoe,new Vector3(side*.17f,.04f,.005f),new Vector3(.022f,.13f,.24f),Art.White,.01f);
                            Art.Tube("Black swept side trim",shoe,new Vector3(side*.188f,.00f,-.10f),new Vector3(side*.188f,.08f,.13f),.022f,Art.Ink);
                        }
                        for(int lace=0;lace<3;lace++)Art.Tube("High top lace",shoe,new Vector3(-.065f,.16f+lace*.05f,.065f),new Vector3(.065f,.16f+lace*.05f,.065f),.012f,Art.Ink);
                        Art.Box("Red outsole",shoe,new Vector3(0,-.11f,.045f),new Vector3(.35f,.035f,.49f),shoes,.012f);
                    }
                }
                else
                {
                    Art.Ball("Friendly mitten hand", rig.Ends[i], Vector3.zero, new Vector3(.23f, .27f, .22f), skin);
                    Art.Ball("Thumb", rig.Ends[i], new Vector3(i == 2 ? .08f : -.08f, -.025f, .065f), new Vector3(.12f, .17f, .12f), skin);
                }
            }
            rig.Head = Art.Group("Face and hair", r, new Vector3(0, 2.16f, 0));
            Transform head = rig.Head;
            if (index == 1)
            {
                // Long hair stays behind the face; no dark volume wraps around the chin.
                Art.Ball("Maya hair crown",head,new Vector3(0,.20f,-.15f),new Vector3(.78f,.42f,.53f),hair);
                rig.LongHair=Art.Group("Maya long hair",head,new Vector3(0,.15f,-.08f));
                Art.Ball("Continuous long hair curtain",rig.LongHair,new Vector3(0,-.60f,-.29f),new Vector3(.76f,1.52f,.30f),hair);
                for(int lockIndex=0;lockIndex<9;lockIndex++)
                {
                    float x=(lockIndex-4)*.085f,z=-.36f+Mathf.Abs(lockIndex-4)*.014f,length=1.62f-Mathf.Abs(lockIndex-4)*.07f;
                    var strand=Art.Ball("Waist length hair lock",rig.LongHair,new Vector3(x,-length*.52f,z),new Vector3(.11f,length,.12f),lockIndex%3==0?Color.Lerp(hair,Art.Hex("755440"),.25f):hair);
                    strand.transform.localRotation=Quaternion.Euler(-5,0,(lockIndex-4)*-2);
                }
                foreach(int side in new[]{-1,1})Art.Ball("Long side hair",rig.LongHair,new Vector3(side*.35f,-.55f,-.035f),new Vector3(.13f,1.28f,.22f),hair);
            }
            Art.Ball("Face", head, Vector3.zero, new Vector3(.72f, .79f, .65f), skin);
            for (int side = -1; side <= 1; side += 2)
            {
                Art.Ball("Ear", head, new Vector3(side * .35f, -.02f, 0), new Vector3(.17f, .24f, .18f), skin);
                Art.Ball("Eye white", head, new Vector3(side * .145f, .04f, .289f), new Vector3(.195f, .23f, .085f), Color.white);
                Art.Ball("Warm eyes", head, new Vector3(side * .143f, .037f, .329f), new Vector3(.105f, .145f, .044f), Art.Hex("362B29"));
                if(index==3)Art.Ball("Eye pupil",head,new Vector3(side*.143f,.035f,.352f),new Vector3(.043f,.07f,.015f),Art.Hex("172C49"));
                Art.Ball("Eye sparkle", head, new Vector3(side * .143f - .022f, .072f, .353f), new Vector3(.035f, .04f, .017f), Color.white);
                var closed=Art.Tube("Closed eye",head,new Vector3(side*.145f-.08f,.028f,.35f),new Vector3(side*.145f+.08f,.028f,.35f),.019f,hair);closed.SetActive(false);
                Art.Tube("Eyebrow", head, new Vector3(side * .145f - .085f, .191f, .286f), new Vector3(side * .145f + .07f, .2f, .282f), .025f, hair);
                Art.Ball("Cheek", head, new Vector3(side * .24f, -.075f, .253f), new Vector3(.1f, .07f, .025f), Color.Lerp(skin, Art.Red, .27f));
            }
            Art.Ball("Button nose", head, new Vector3(0, -.045f, .327f), new Vector3(.105f, .12f, .11f), skin);
            Vector3 last = new Vector3(-.115f, -.135f, .295f);
            for (int i = 1; i <= 8; i++)
            {
                float x = -.115f + i * .23f / 8;
                Vector3 p = new Vector3(x, -.19f + (x / .115f) * (x / .115f) * .055f, .302f);
                Art.Tube("Smile", head, last, p, .016f, Art.Hex("8E4734")); last = p;
            }
            if (index == 0 || index == 3)
            {
                Art.Ball("Swept hair", head, new Vector3(0, .27f, -.055f), new Vector3(.79f, .39f, .65f), hair);
                for (int i = 0; i < 5; i++)
                {
                    var lockObj = Art.Ball("Hair swoop", head, new Vector3(-.28f + .12f * i, .29f + .028f * i, .15f), new Vector3(.3f, .18f, .43f), hair);
                    lockObj.transform.localRotation = Quaternion.Euler(0, -35, -20);
                }
            }
            else if (index == 1)
            {
                Art.Ball("Yellow beanie", head, new Vector3(0, .3f, -.025f), new Vector3(.85f, .47f, .73f), Art.Yellow);
                Art.Box("Beanie cuff", head, new Vector3(0, .23f, .035f), new Vector3(.79f, .13f, .65f), Art.Yellow, .06f);
                Art.Box("Beanie badge", head, new Vector3(.12f, .25f, .365f), new Vector3(.12f, .08f, .025f), Art.White);
            }
            else
            {
                for (int row = 0; row < 3; row++) for (int i = 0; i < 9; i++)
                {
                    float a = i * Mathf.PI * 2 / 9 + row * .24f, radius = .28f * (1 - row * .18f);
                    Art.Ball("Curly hair", head, new Vector3(Mathf.Sin(a) * radius, .19f + row * .10f, Mathf.Cos(a) * radius - .045f), Vector3.one * .24f, hair);
                }
                for(int i=0;i<7;i++)
                {
                    var fringe=Art.Ball("Leo fringe over eyes",head,new Vector3(-.29f+i*.095f,.15f+(i%3)*.022f,.305f),new Vector3(.16f,.36f,.14f),hair);
                    fringe.transform.localRotation=Quaternion.Euler(-8,0,-16+i*3);
                }
            }
            rig.Body = Art.Group("Flexible torso", r, new Vector3(0, 1.08f, 0));
            var children = new List<Transform>(); foreach (Transform child in r) children.Add(child);
            foreach (Transform child in children)
            {
                if (child == rig.Body || System.Array.IndexOf(rig.Upper, child) >= 0 || System.Array.IndexOf(rig.Lower, child) >= 0 || System.Array.IndexOf(rig.Ends, child) >= 0) continue;
                child.SetParent(rig.Body, true);
            }
            rig.Head.localScale = Vector3.one * 1.12f;
            GummyCharacter.Attach(rig,index);rig.Pose(0, 0, false, false);
            return rig;
        }

        public static VehicleModel Vehicle(Transform parent, Ride mode, int carStyle = 0)
        {
            var model = new VehicleModel { Root = Art.Group(Simulation.RideNames[(int)mode], parent), Mode = mode, CarStyle = carStyle };
            if(mode==Ride.Yacht)YachtVoyages.BuildVehicle(model);
            if (mode == Ride.RaceCar) Car(model, carStyle);
            if(mode==Ride.Van || mode==Ride.Camper)RoadCamper(model);
            if (mode == Ride.Bicycle || mode == Ride.Motorcycle || mode==Ride.MountainBike) Bike(model, mode == Ride.Motorcycle);
            if(mode==Ride.MiniBike)MiniBmx(model);
            if(mode==Ride.DirtBike)DirtBike(model);
            if(mode==Ride.Skateboard || mode==Ride.Scooter)Board(model,mode==Ride.Scooter);
            if (mode == Ride.Airplane) Plane(model);
            if(mode==Ride.Helicopter)Helicopter(model);
            if(mode==Ride.Motorboat || mode==Ride.Speedboat)MarineAdventure.BuildBoat(model);
            if (mode == Ride.Bicycle || mode == Ride.Motorcycle) model.Root.localScale = Vector3.one * .75f;
            if(mode==Ride.MountainBike){model.Root.localScale=Vector3.one*.86f;AddSuspension(model);}
            if(mode==Ride.MiniBike)model.Root.localScale=Vector3.one*.52f;
            GraphicsCar.Attach(model);CarDamageVisual.Attach(model);
            GraphicsBike.Attach(model);
            GraphicsOffroad.Attach(model);
            GraphicsPlane.Attach(model);
            GraphicsBoat.Attach(model);
            GraphicsBoard.Attach(model);
            return model;
        }

        static void Wheel(VehicleModel model, Vector3 pos, float radius, float width, bool spokes)
        {
            var pivot = Art.Group("Spinning wheel", model.Root, pos); model.Wheels.Add(pivot);
            Art.Ring("Rubber tire", pivot, radius * .87f, radius * .13f, width * .5f, Art.Ink);
            Art.Ring("Alloy rim", pivot, radius * .72f, radius * .035f, width * .3f, Art.Hex("ADC7D3"));
            foreach (int side in new[] { -1, 1 })
            {
                if (spokes)
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        Art.Tube("Spoke", pivot, new Vector3(side * width * .57f, 0, 0),
                            new Vector3(side * width * .33f, Mathf.Sin(a) * radius * .73f, Mathf.Cos(a) * radius * .73f), .015f, Art.White);
                    }
                var cap = Art.Disk("Hub", pivot, new Vector3(side * width * .6f, 0, 0), radius * .24f, .04f, Art.Ink);
                cap.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
        }

        static void SafariJeep(VehicleModel m)
        {
            var r=m.Root;Color tan=Art.Hex("C9BC8F"),olive=Art.Hex("75865A"),red=Art.Hex("B74739");
            Art.Box("Chassis",r,new Vector3(0,.65f,0),new Vector3(2.1f,.3f,4.5f),Art.Ink,.08f);
            Art.Box("Nose",r,new Vector3(0,1.12f,1.45f),new Vector3(2,.72f,1.7f),tan,.12f);
            Art.Box("Open safari cabin floor",r,new Vector3(0,.94f,-.6f),new Vector3(1.9f,.25f,2.4f),olive,.06f);
            foreach(int side in new[]{-1,1})
            {
                Art.Box("Red park-tour stripe",r,new Vector3(side*1.025f,1.3f,1.3f),new Vector3(.03f,.23f,1.8f),red,0);
                Art.Box("Jeep door sill",r,new Vector3(side*.97f,1.16f,-.55f),new Vector3(.14f,.6f,2.3f),tan,.04f);
                Art.Box("Red door stripe",r,new Vector3(side*1.06f,1.26f,-.55f),new Vector3(.03f,.22f,2.1f),red,0);
                foreach(int end in new[]{-1,1}){Wheel(m,new Vector3(side*1.12f,.62f,end*1.43f),.62f,.38f,false);Art.Box("Rugged wheel fender",r,new Vector3(side*1.07f,1.25f,end*1.43f),new Vector3(.56f,.13f,1.35f),olive,.05f);}
                Art.Tube("Safari roll cage",r,new Vector3(side*.94f,1,-1.6f),new Vector3(side*.94f,2.35f,-1.6f),.075f,olive);
                Art.Tube("Safari roll cage",r,new Vector3(side*.94f,1,.48f),new Vector3(side*.94f,2.35f,.48f),.075f,olive);
                Art.Tube("Safari roll cage top",r,new Vector3(side*.94f,2.35f,-1.6f),new Vector3(side*.94f,2.35f,.48f),.075f,olive);
                Art.Box("Safari seat",r,new Vector3(side*.45f,1.1f,-.7f),new Vector3(.7f,.25f,.8f),Art.Hex("7E6D52"),.1f);Art.Box("Safari seat back",r,new Vector3(side*.45f,1.49f,-1.06f),new Vector3(.7f,.8f,.18f),Art.Hex("7E6D52"),.05f);
                Art.Ball("Round headlamp",r,new Vector3(side*.72f,1.22f,2.33f),new Vector3(.4f,.4f,.1f),Art.Yellow);
            }
            Art.Tube("Roll cage crossbar",r,new Vector3(-.94f,2.35f,-1.6f),new Vector3(.94f,2.35f,-1.6f),.075f,olive);
            var tire=Art.Ring("Rear spare tire",r,.55f,.14f,.16f,Art.Ink);tire.transform.localPosition=new Vector3(.3f,1.35f,-2.35f);tire.transform.localRotation=Quaternion.Euler(0,90,0);
            Art.Box("Safari expedition crate",r,new Vector3(-.55f,1.2f,-1.9f),new Vector3(.65f,.7f,.6f),olive,.05f);
            for(int i=-3;i<=3;i++)Art.Box("Vertical Jeep grille slot",r,new Vector3(i*.17f,1.17f,2.34f),new Vector3(.08f,.44f,.03f),Art.Ink,0);
            Art.Box("Jeep front bumper",r,new Vector3(0,.73f,2.46f),new Vector3(2.2f,.23f,.24f),olive,.04f);
            m.Seat=new Vector3(-.43f,1.26f,-.69f);m.Grips[0]=new Vector3(-.57f,1.52f,-.38f);m.Grips[1]=new Vector3(-.29f,1.52f,-.38f);
            foreach(int side in new[]{-1,1})m.Pedals.Add(Art.Group("Driver foot contact",r,new Vector3(-.43f+side*.12f,.92f,-.38f)));
            var steering=Art.Group("Safari steering wheel",r,new Vector3(-.43f,1.52f,-.38f));steering.localRotation=Quaternion.Euler(0,90,0);Art.Ring("Steering rim",steering,.16f,.019f,.019f,Art.Ink);
            var wheel=Art.Ring("Safari steering wheel",r,.27f,.045f,.04f,Art.Ink);wheel.transform.localPosition=new Vector3(-.43f,1.52f,.18f);wheel.transform.localRotation=Quaternion.Euler(0,90,60);
            AdventureWorlds.Caption("DINO PARK • 04",r,new Vector3(0,1.68f,1.42f),0,1.5f);
        }
        static void Car(VehicleModel m, int style)
        {
            if(style==3){SafariJeep(m);return;}
            Transform r = m.Root; m.Seat = new Vector3(-.43f, .79f, -.27f);
            Color paint = style == 1 ? Art.Yellow : style == 2 ? Art.Teal : Art.Blue;
            m.Grips[0] = new Vector3(-.57f, 1.03f, .05f); m.Grips[1] = new Vector3(-.29f, 1.03f, .05f);
            Art.Box("Cabin floor", r, new Vector3(0, .46f, 0), new Vector3(2.12f, .2f, 3.9f), Art.Ink);
            Art.Box("Sculpted hood", r, new Vector3(0, .79f, 1.3f), new Vector3(2.22f, .48f, 1.49f), paint, .22f);
            Art.Box("Rear deck", r, new Vector3(0, .79f, -1.53f), new Vector3(2.22f, .48f, 1.02f), paint, .22f);
            Art.Box("Nose", r, new Vector3(0, .76f, 1.52f), new Vector3(2.14f, .5f, 1.02f), paint, .24f);
            if (style != 1) Art.Box("Roof", r, new Vector3(0, 1.56f, -.35f), new Vector3(1.68f, .16f, 1.28f), paint);
            Art.GlassPane("Clear windshield", r, new Vector3(-.82f, 1.02f, .82f), new Vector3(.82f, 1.02f, .82f), new Vector3(.78f, 1.49f, .29f), new Vector3(-.78f, 1.49f, .29f));
            Art.GlassPane("Clear rear window", r, new Vector3(-.82f, 1.02f, -1.07f), new Vector3(.82f, 1.02f, -1.07f), new Vector3(.78f, 1.49f, -.97f), new Vector3(-.78f, 1.49f, -.97f));
            Art.Box("Dashboard", r, new Vector3(0, .94f, .62f), new Vector3(1.7f, .14f, .28f), Art.Ink);
            var steering = Art.Group("Steering wheel", r, new Vector3(-.43f, 1.05f, .05f));
            steering.localRotation = Quaternion.Euler(0, 90, 0);
            Art.Ring("Steering rim", steering, .16f, .025f, .025f, Art.Ink);
            Art.Tube("Steering spoke", steering, new Vector3(0, -.15f, 0), new Vector3(0, .15f, 0), .017f, Art.White);
            foreach (int side in new[] { -1, 1 })
            {
                Art.Box("Rounded side body", r, new Vector3(side * 1.02f, .72f, -.1f), new Vector3(.27f, .54f, 3.85f), paint, .13f);
                Art.Box("Seat cushion", r, new Vector3(side * .43f, .68f, -.21f), new Vector3(.63f, .18f, .68f), Art.Hex("304859"));
                Art.Box("Seat back", r, new Vector3(side * .43f, .95f, -.57f), new Vector3(.62f, .6f, .16f), Art.Hex("304859"));
                Art.Box("Seat headrest", r, new Vector3(side * .43f, 1.28f, -.56f), new Vector3(.34f, .24f, .16f), Art.Red);
                Art.GlassPane("Clear side window", r, new Vector3(side * .83f, 1.02f, -.99f), new Vector3(side * .83f, 1.02f, .76f), new Vector3(side * .79f, 1.49f, .28f), new Vector3(side * .79f, 1.49f, -.96f));
                Art.Tube("Front window pillar", r, new Vector3(side * .85f, 1, .83f), new Vector3(side * .81f, 1.55f, .28f), .045f, paint);
                Art.Tube("Rear window pillar", r, new Vector3(side * .85f, 1, -1.07f), new Vector3(side * .81f, 1.55f, -.97f), .045f, paint);
                Art.Box("Door handle", r, new Vector3(side * 1.16f, .91f, -.53f), new Vector3(.04f, .06f, .22f), Art.White);
                Wheel(m, new Vector3(side * 1.1f, .48f, 1.26f), .47f, .33f, true);
                Wheel(m, new Vector3(side * 1.1f, .48f, -1.27f), .47f, .33f, true);
                Art.WheelArch(r,new Vector3(side*1.09f,.48f,1.26f),paint);
                Art.WheelArch(r,new Vector3(side*1.09f,.48f,-1.27f),paint);
                Art.Ball("Racing roundel",r,new Vector3(side*1.165f,.73f,-.2f),new Vector3(.035f,.40f,.46f),Art.White);
                Art.Box("White racing stripe", r, new Vector3(side * .23f, 1.027f, 1.24f), new Vector3(.21f, .025f, 1.21f), Art.White, 0);
                if (style != 1) Art.Box("Roof stripe", r, new Vector3(side * .23f, 1.65f, -.35f), new Vector3(.21f, .025f, 1.1f), Art.White, 0);
                Art.Ball("Headlamp", r, new Vector3(side * .8f, .85f, 1.91f), new Vector3(.44f, .28f, .13f), Art.White);
                Art.Box("Brake light", r, new Vector3(side * .83f, .87f, -2.02f), new Vector3(.39f, .2f, .08f), Art.Red);
                if (style == 0) Art.Box("Spoiler support", r, new Vector3(side * .72f, 1.19f, -1.65f), new Vector3(.09f, .45f, .12f), Art.Ink);
                Art.Ball("Red mirror", r, new Vector3(side * 1.11f, 1.24f, .48f), new Vector3(.31f, .18f, .26f), Art.Red);
            }
            if (style == 0) Art.Box("Rear spoiler", r, new Vector3(0, 1.41f, -1.68f), new Vector3(2.32f, .12f, .4f), paint);
            if (style == 2)
            {
                Art.Box("Mini luggage rack", r, new Vector3(0, 1.75f, -.35f), new Vector3(1.15f, .12f, .75f), Art.White);
                Art.Box("Weekend suitcase", r, new Vector3(0, 1.98f, -.35f), new Vector3(.85f, .4f, .6f), Art.Hex("B87953"), .14f);
                r.localScale = new Vector3(.9f, 1.1f, .85f);
                r.Find("Mini luggage rack").SetParent(r.Find("Roof"),true);r.Find("Weekend suitcase").SetParent(r.Find("Roof"),true);
            }
            Art.Box("Front grille", r, new Vector3(0, .52f, 2.04f), new Vector3(1.33f, .18f, .06f), Art.Ink);
        }

        static void MiniBmx(VehicleModel m)
        {
            var r=m.Root;var purple=Art.Hex("A974CE");var chrome=Art.Hex("C8D5DD");
            Vector3 rear=new Vector3(0,.29f,-.52f),front=new Vector3(0,.29f,.60f),crank=new Vector3(0,.34f,-.08f),saddle=new Vector3(0,.63f,-.24f),neck=new Vector3(0,.70f,.36f);
            Wheel(m,rear,.29f,.15f,true);Wheel(m,front,.29f,.15f,true);
            foreach(var pair in new[]{new[]{rear,saddle},new[]{rear,crank},new[]{saddle,crank},new[]{saddle,neck},new[]{crank,neck}})Art.Tube("Tiny BMX frame",r,pair[0],pair[1],.046f,purple);
            foreach(int side in new[]{-1,1})Art.Tube("Short BMX fork",r,neck+Vector3.right*side*.08f,front+Vector3.right*side*.08f,.038f,chrome);
            Art.Tube("BMX seat post",r,saddle,new Vector3(0,.77f,-.24f),.032f,chrome);
            Art.Box("Tiny BMX saddle",r,new Vector3(0,.80f,-.25f),new Vector3(.30f,.10f,.34f),Art.Ink,.04f);
            Art.Tube("Tall BMX stem",r,neck,new Vector3(0,1.25f,.32f),.05f,chrome);
            Art.Tube("BMX handlebar base",r,new Vector3(-.16f,1.25f,.32f),new Vector3(.16f,1.25f,.32f),.039f,chrome);
            Art.Tube("High BMX crossbar",r,new Vector3(-.36f,2.07f,.20f),new Vector3(.36f,2.07f,.20f),.027f,chrome);
            m.Crank=Art.Group("Rotating crank axle",r,crank);
            foreach(int side in new[]{-1,1})
            {
                Art.Tube("Extra tall BMX handlebar",r,new Vector3(side*.16f,1.25f,.32f),new Vector3(side*.39f,2.45f,.18f),.039f,chrome);
                Art.Tube("Wide BMX grip",r,new Vector3(side*.39f,2.45f,.18f),new Vector3(side*.65f,2.45f,.08f),.055f,Art.Ink);
                Art.Tube("BMX axle peg",r,front+Vector3.right*side*.08f,front+Vector3.right*side*.29f,.055f,chrome);
                m.Grips[side<0?0:1]=new Vector3(side*.53f,2.45f,.125f);
                var pedal=Art.Group("Pedal contact",m.Crank,new Vector3(side*.24f,side*.14f,0));m.Pedals.Add(pedal);
                Art.Tube("Short BMX crank arm",m.Crank,new Vector3(side*.12f,0,0),pedal.localPosition,.023f,chrome);
                Art.Box("BMX pedal",pedal,Vector3.zero,new Vector3(.19f,.05f,.15f),Art.Ink,.01f);
            }
            m.Seat=new Vector3(0,.87f,-.24f);
        }

        static void Bike(VehicleModel m, bool motor)
        {
            Transform r = m.Root; float radius = motor ? .47f : .53f;
            Color color = motor ? Art.Red : m.Mode==Ride.MountainBike?Art.Hex("E3A154"):m.Mode==Ride.MiniBike?Art.Hex("A974CE"):Art.Green;
            Wheel(m, new Vector3(0, radius, -.93f), radius, motor ? .27f : .115f, true);
            Wheel(m, new Vector3(0, radius, 1.02f), radius, motor ? .27f : .115f, true);
            Vector3 rear = new Vector3(0, radius, -.93f), front = new Vector3(0, radius, 1.02f),
                crank = new Vector3(0, .53f, -.1f), saddle = new Vector3(0, motor ? 1.16f : 1.0f, -.43f), neck = new Vector3(0, 1.28f, .62f);
            Art.Tube("Frame", r, rear, saddle, .052f, color); Art.Tube("Frame", r, rear, crank, .052f, color);
            Art.Tube("Frame", r, saddle, crank, .06f, color); Art.Tube("Top tube", r, saddle, neck, .065f, color);
            Art.Tube("Down tube", r, crank, neck, .065f, color); Art.Tube("Front fork", r, neck, front, .055f, Art.Hex("AFC6CE"));
            Art.Tube("Seat post", r, saddle, new Vector3(0, motor ? 1.39f : 1.16f, -.43f), .045f, Art.Ink);
            Art.Box("Saddle", r, new Vector3(0, motor ? 1.43f : 1.2f, -.44f), new Vector3(motor ? .46f : .32f, .14f, motor ? .86f : .46f), Art.Ink);
            Art.Tube("Handlebar stem", r, neck, new Vector3(0, 1.55f, .63f), .045f, Art.Ink);
            Art.Tube("Handlebars", r, new Vector3(-.49f, 1.55f, .63f), new Vector3(.49f, 1.55f, .63f), .045f, Art.Ink);
            m.Crank = Art.Group("Rotating crank axle", r, crank);
            foreach (int side in new[] { -1, 1 })
            {
                var pedal = Art.Group("Pedal contact", m.Crank, new Vector3(side * .28f, (motor ? 0 : side * .20f), 0));
                m.Pedals.Add(pedal);
                Art.Tube("Crank arm", m.Crank, new Vector3(side * .17f, 0, 0), new Vector3(side * .28f, side * .20f, 0), .025f, Art.White);
                Art.Box("Pedal platform", pedal, Vector3.zero, new Vector3(.23f, .06f, .19f), Art.Ink);
                m.Grips[side < 0 ? 0 : 1] = new Vector3(side * .43f, 1.55f, .63f);
                if (motor)
                {
                    Art.Tube("Mirror stalk", r, new Vector3(side * .4f, 1.55f, .63f), new Vector3(side * .51f, 1.86f, .62f), .025f, Art.Ink);
                    Art.Ball("Mirror", r, new Vector3(side * .52f, 1.89f, .62f), new Vector3(.23f, .14f, .07f), Art.Ink);
                }
            }
            if (motor)
            {
                Art.Ball("Red fuel tank", r, new Vector3(0, 1.18f, .18f), new Vector3(.75f, .51f, .85f), Art.Red);
                Art.Box("Engine", r, new Vector3(0, .71f, -.04f), new Vector3(.55f, .49f, .58f), Art.Ink);
                for (int i = 0; i < 4; i++) Art.Box("Engine fin", r, new Vector3(0, .55f + i * .09f, .1f), new Vector3(.63f, .035f, .35f), Art.Hex("9AAAB4"), 0);
                Art.Tube("Exhaust", r, new Vector3(.36f, .45f, -.96f), new Vector3(.36f, .47f, -.25f), .095f, Art.Hex("C8D5DD"));
                Art.Ball("Round headlamp", r, new Vector3(0, 1.32f, .88f), new Vector3(.36f, .36f, .18f), Art.White);
                Art.Box("Red tail", r, new Vector3(0, 1.3f, -.99f), new Vector3(.47f, .18f, .45f), Art.Red);
            }
            m.Seat = new Vector3(0, motor ? 1.51f : 1.29f, motor ? -.22f : -.43f);
            if (motor) m.Crank = null;
        }

        static void Board(VehicleModel m,bool scooter)
        {
            var r=m.Root;Color paint=scooter?Art.Hex("EEA15B"):Art.Hex("578CCC");
            Art.Box(scooter?"Scooter deck":"Maple skateboard deck",r,new Vector3(0,.23f,0),new Vector3(.46f,.09f,1.25f),paint,.06f);
            Art.Box("Grip tape",r,new Vector3(0,.279f,0),new Vector3(.38f,.008f,1.04f),Art.Ink,.025f);
            if(scooter)
            {
                Wheel(m,new Vector3(0,.14f,-.53f),.14f,.075f,false);Wheel(m,new Vector3(0,.14f,.64f),.14f,.075f,false);
                Art.Tube("Scooter steering column",r,new Vector3(0,.17f,.6f),new Vector3(0,1.25f,.48f),.045f,Art.White);
                Art.Tube("Scooter handlebars",r,new Vector3(-.32f,1.25f,.48f),new Vector3(.32f,1.25f,.48f),.055f,paint);
            }
            else foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})Wheel(m,new Vector3(side*.24f,.105f,end*.43f),.105f,.08f,false);
            m.Seat=new Vector3(0,1.00f,-.07f);
            for(int i=0;i<2;i++)
            {
                int side=i==0?-1:1;m.Pedals.Add(Art.Group("Deck foot contact",r,new Vector3(side*.09f,.27f,side*.25f)));
                m.Grips[i]=scooter?new Vector3(side*.30f,1.25f,.48f):new Vector3(side*.48f,1.16f,.05f);
            }
        }
        static void AddSuspension(VehicleModel m)
        {
            foreach(int side in new[]{-1,1})
            {
                var spring=Art.Group("Front suspension spring",m.Root,new Vector3(side*.095f,.64f,.90f));m.Suspension.Add(spring);
                Art.Tube("Telescopic fork",spring,Vector3.zero,new Vector3(0,.55f,0),.042f,Art.Ink);
                for(int i=0;i<7;i++){var ring=Art.Ring("Visible fork coil",spring,.065f,.016f,.016f,Art.Yellow);ring.transform.localPosition=new Vector3(0,.05f+i*.06f,0);ring.transform.localRotation=Quaternion.Euler(0,0,90);}
            }
            var rear=Art.Group("Rear suspension shock",m.Root,new Vector3(.08f,.60f,-.3f));m.Suspension.Add(rear);
            Art.Tube("Rear shock piston",rear,Vector3.zero,new Vector3(0,.45f,0),.05f,Art.Ink);
            for(int i=0;i<6;i++){var ring=Art.Ring("Rear shock coil",rear,.07f,.018f,.018f,Art.Yellow);ring.transform.localPosition=new Vector3(0,.04f+i*.06f,0);ring.transform.localRotation=Quaternion.Euler(0,0,90);}
        }

        static void Plane(VehicleModel m)
        {
            Transform r = m.Root; m.Seat = new Vector3(0, .62f, .1f);
            Art.Ball("Fuselage", r, new Vector3(0, 1.36f, 0), new Vector3(1.64f, 1.56f, 6.5f), Art.White);
            Art.Ball("Blue nose", r, new Vector3(0, 1.37f, 2.46f), new Vector3(1.6f, 1.52f, 1.43f), Art.Blue);
            Art.Box("Main wing", r, new Vector3(0, 1.05f, .45f), new Vector3(9.4f, .2f, 1.55f), Art.White, .09f);
            Art.Box("Blue leading edge", r, new Vector3(0, 1.08f, 1.13f), new Vector3(9.2f, .17f, .22f), Art.Blue);
            Art.Box("Tailplane", r, new Vector3(0, 1.51f, -2.49f), new Vector3(3.53f, .14f, .85f), Art.Blue);
            Art.Box("Tail fin", r, new Vector3(0, 2.1f, -2.53f), new Vector3(.15f, 1.37f, 1.1f), Art.Blue, .065f);
            Art.Box("Tail red flash", r, new Vector3(0, 2.48f, -2.54f), new Vector3(.18f, .18f, .98f), Art.Red);
            Art.Ball("Cockpit canopy", r, new Vector3(0, 1.99f, .22f), new Vector3(1.17f, .73f, 1.68f), Art.Hex("6CBECF"));
            // The pilot sits just in front of the canopy so their head stays visible.
            m.Seat = new Vector3(0, .88f, .9f);
            for (int side = -1; side <= 1; side += 2)
            {
                Art.Box("Red wingtip", r, new Vector3(side * 4.53f, 1.07f, .44f), new Vector3(.43f, .24f, 1.53f), Art.Red);
                Art.Box("Red fuselage stripe", r, new Vector3(side * .742f, 1.35f, .1f), new Vector3(.035f, .13f, 3.6f), Art.Red, 0);
                Art.Tube("Landing strut", r, new Vector3(side * .54f, 1, .9f), new Vector3(side * 1.05f, .34f, 1.03f), .055f, Art.Ink);
                Wheel(m, new Vector3(side * 1.05f, .33f, 1.03f), .32f, .2f, false);
            }
            Wheel(m, new Vector3(0, .21f, -2.46f), .2f, .13f, false);
            m.Propeller = Art.Group("Propeller", r, new Vector3(0, 1.38f, 3.21f));
            Art.Box("Propeller blades", m.Propeller, Vector3.zero, new Vector3(.18f, 2.53f, .08f), Art.Ink);
            Art.Box("Red propeller tip", m.Propeller, new Vector3(0, 1.14f, .01f), new Vector3(.2f, .27f, .09f), Art.Red);
            Art.Box("Red propeller tip", m.Propeller, new Vector3(0, -1.14f, .01f), new Vector3(.2f, .27f, .09f), Art.Red);
            Art.Ball("Spinner", r, new Vector3(0, 1.38f, 3.23f), new Vector3(.39f, .39f, .48f), Art.White);
        }
    }
}
