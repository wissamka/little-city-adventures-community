using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LittleCity
{
    public sealed class CityWorld
    {
        public Transform Root { get; private set; }
        public readonly List<VehicleModel> GarageCars = new List<VehicleModel>();
        public readonly List<DoorSite> Doors = new List<DoorSite>();
        readonly Simulation sim;
        readonly System.Random random = new System.Random(27);
        readonly Color grass = Art.Hex("698772"), road = Art.Hex("475D68"), paving = Art.Hex("A4ABA0"), leaf = Art.Hex("52786C");
        public static readonly int[] Streets = { -96, -32, 32, 96 };
        public CityWorld(Simulation simulation) { sim = simulation; }

        public IEnumerator Build(Transform parent)
        {
            Root = Art.Group("Little City • handcrafted procedural world", parent);
            Art.Box("Ocean", Root, new Vector3(0, -2.8f, 0), new Vector3(2300, 2, 2300), Art.Hex("4C7C88"), 0);
            Art.Box("Island sand", Root, new Vector3(0, -1.2f, 0), new Vector3(1845, 2, 1645), Art.Hex("C5BC9C"), 0);
            Art.Box("Island lawn", Root, new Vector3(0, -.15f, 0), new Vector3(1810, .3f, 1610), grass, 0);
            foreach (int s in Streets)
            {
                Road(new Vector3(s, 0, 0), new Vector3(12, .055f, 218));
                Road(new Vector3(0, 0, s), new Vector3(218, .055f, 12));
            }
            Road(new Vector3(133, 0, -96), new Vector3(70, .055f, 12));
            for (int bx = -1; bx <= 1; bx++) for (int bz = -1; bz <= 1; bz++)
            {
                Art.Box("Sidewalk block", Root, new Vector3(bx * 64, -.005f, bz * 64), new Vector3(54, .08f, 54), paving, .035f);
                Art.Box("Block garden", Root, new Vector3(bx * 64, .012f, bz * 64), new Vector3(47.5f, .06f, 47.5f), grass, .025f);
            }
            RoadMarkings();
            yield return null;
            for (int bx = -1; bx <= 1; bx++) for (int bz = -1; bz <= 1; bz++)
            {
                if (bx == 0 && bz == 0) continue;
                float x = bx * 64, z = bz * 64;
                Building(x - 13, z - 13, bx + bz + 4, 180);
                Building(x + 13, z - 13, bx - bz + 7, 180);
                Building(x - 13, z + 13, bx - bz + 9, 0);
                Building(x + 13, z + 13, bx + bz + 11, 0);
                for (int i = -1; i <= 1; i++)
                {
                    Tree(x - 24, z + i * 17, 1);
                    Tree(x + 24, z + i * 17, 1);
                }
                yield return null;
            }
            Park();
            IllustratedScenery.ParkDetails(Root);
            Airfield();
            yield return null;
            for (int i = 0; i < 46; i++)
            {
                float x = -208 + (float)random.NextDouble() * 308;
                float z = (i % 2 == 0 ? -1 : 1) * (128 + (float)random.NextDouble() * 55);
                Tree(x, z, 1.2f + (float)random.NextDouble() * .7f);
            }
            for (int i = 0; i < 12; i++)
            {
                float x = -205 + i * 34;
                Art.Ball("Distant rounded hill", Root, new Vector3(x, 0, 216), new Vector3(48, 24 + i % 3 * 12, 42), Art.Hex(i % 2 == 0 ? "78B884" : "6CAA84"));
            }
            Sign("WILLOW PARK", new Vector3(10, 2.8f, -22), 180, Art.Teal);
            Sign("SUNNY AIRFIELD", new Vector3(136, 3.1f, -86), 180, Art.Blue);
            Art.CombineStatic(Root);
            ExpandedCity.Build(Root,sim);
        }

        void Road(Vector3 pos, Vector3 size) => Art.Box("Connected street", Root, pos, size, road, 0);

        void RoadMarkings()
        {
            foreach (int s in Streets)
            {
                for (int i = -102; i <= 102; i += 8)
                {
                    bool intersection = false;
                    foreach (int t in Streets) if (Mathf.Abs(i - t) < 10) intersection = true;
                    if (intersection) continue;
                    Art.Box("Lane dash", Root, new Vector3(s, .032f, i), new Vector3(.2f, .015f, 3.3f), Art.White, 0);
                    Art.Box("Lane dash", Root, new Vector3(i, .032f, s), new Vector3(3.3f, .015f, .2f), Art.White, 0);
                }
                foreach (int t in Streets)
                {
                    for (int i = -4; i <= 4; i += 2)
                        foreach (int side in new[] { -1, 1 })
                        {
                            Art.Box("Crosswalk", Root, new Vector3(s + i, .036f, t + side * 8), new Vector3(1, .02f, 2.5f), Art.White, 0);
                            Art.Box("Crosswalk", Root, new Vector3(s + side * 8, .036f, t + i), new Vector3(2.5f, .02f, 1), Art.White, 0);
                        }
                    Lamp(s + 8.5f, t + 8.5f);
                    Art.Tube("Street sign post", Root, new Vector3(s - 8, 0, t - 8), new Vector3(s - 8, 3.3f, t - 8), .065f, Art.Ink);
                    Art.Box("Green street sign", Root, new Vector3(s - 8, 3.15f, t - 8), new Vector3(1.9f, .4f, .13f), Art.Teal);
                }
            }
        }

        void Building(float x,float z,int variation,float yaw)
        {
            string[] names={"BAKERY","BOOKS","GELATO","TOYS","FLOWERS","CAFE"};
            Color[] walls={Art.Hex("B7A48B"),Art.Hex("8DADB1"),Art.Hex("B88E7E"),Art.Hex("959DAC"),Art.Hex("C0B695"),Art.Hex("91AA96")};
            int v=Mathf.Abs(variation)%6,floors=3+Mathf.Abs(variation)%3;float width=15+v%2;
            var building=BuildingArchitecture.Build(Root,new Vector3(x,0,z),width,15,floors,3.2f,yaw,walls[v],variation,names[v]+" house");Doors.Add(BuildingArchitecture.DoorSite(building));
            foreach(int side in new[]{-1,1})Art.Box("Colorful awning",building.transform,new Vector3(side*4,3.3f,8),new Vector3(4,.18f,1.5f),v%2==0?Art.Red:Art.Teal,.05f);
            Solid(x,z,width/2+.3f,7.8f,floors*3.2f+1);
        }

        void Park()
        {
            Art.Box("Park cross path", Root, new Vector3(0, .055f, 0), new Vector3(5.3f, .04f, 49), Art.Hex("536A64"), 0);
            Art.Box("Park cross path", Root, new Vector3(0, .058f, -5), new Vector3(49, .04f, 4), Art.Hex("536A64"), 0);
            var edge = Art.Disk("Pond sandy edge", Root, new Vector3(-13, .05f, 8), 1, .08f, Art.Hex("E7CE91"));
            edge.transform.localScale = new Vector3(17.5f, .07f, 24);
            var pond = Art.Disk("Turquoise pond", Root, new Vector3(-13, .13f, 8), 1, .03f, Art.Hex("638F98"));
            pond.transform.localScale = new Vector3(15.8f, .03f, 22);
            sim.PondEnabled = true;
            Solid(-13, 6.35f, 8.8f, .10f, 1.4f); Solid(-13, 9.65f, 8.8f, .10f, 1.4f);
            // The simulation follows this deck and its shallow approach ramps; rails block both walkers and wheels.
            for (int plank = -9; plank <= 9; plank++)
                Art.Box("Pond bridge plank", Root, new Vector3(-13 + plank * .92f, .14f, 8), new Vector3(.86f, .12f, 3.2f), Art.Hex("B88254"));
            for (int i = -4; i <= 4; i++) foreach (int side in new[] { -1, 1 })
            {
                Art.Tube("Bridge post", Root, new Vector3(-13 + i * 2, .1f, 8 + side * 1.65f), new Vector3(-13 + i * 2, 1.3f, 8 + side * 1.65f), .075f, Art.White);
                Art.Tube("Bridge rail", Root, new Vector3(-21.8f, 1.1f, 8 + side * 1.65f), new Vector3(-4.2f, 1.1f, 8 + side * 1.65f), .065f, Art.White);
            }
            for (int i = 0; i < 7; i++)
            {
                float x = -16 + i % 3 * 2.3f, z = 1 + (i / 3) * 12;
                Art.Disk("Lily pad", Root, new Vector3(x, .2f, z), .65f, .035f, leaf);
                Art.Ball("Water lily", Root, new Vector3(x, .27f, z), new Vector3(.45f, .19f, .45f), Art.Hex("F49BA5"));
            }
            Duck(-10, 2); Duck(-15, 15); Duck(-13.6f, 15.4f);
            foreach (Vector2 p in new[] { new Vector2(11, 15), new Vector2(21, 16), new Vector2(17, -16), new Vector2(-19, -16), new Vector2(-8, -17), new Vector2(20, 1) })
            { Tree(p.x, p.y, 1.1f); Flowers(p.x + 2, p.y + 1); }
            Bench(6, 5, 90); Bench(6, -12, 90); Bench(-5, 19, 180); Bench(16, -8, 0);
            var fountain = Art.Disk("Fountain base", Root, new Vector3(13, .24f, 7), 3.4f, .5f, Art.White);
            Art.Disk("Fountain water", Root, new Vector3(13, .52f, 7), 2.9f, .04f, Art.Hex("638F98"));
            Art.Disk("Fountain pedestal", Root, new Vector3(13, 1.05f, 7), .7f, 1.2f, Art.White);
            Art.Ball("Fountain globe", Root, new Vector3(13, 1.9f, 7), Vector3.one * 1.6f, Art.Teal);
            Solid(13, 7, 3.3f, 3.3f, 2.7f);
            Sign("EXPLORE • PLAY • FLY", new Vector3(9, 1.9f, -20), 180, Art.Teal, .22f);
        }

        void Airfield()
        {
            Art.Box("Airfield apron", Root, new Vector3(147, .035f, -55), new Vector3(44, .05f, 82), Art.Hex("B2BDB4"), 0);
            Art.Box("Runway", Root, new Vector3(172, .06f, -3), new Vector3(22, .08f, 310), road, 0);
            for (int z = -144; z <= 142; z += 12)
            {
                Art.Box("Runway centerline", Root, new Vector3(172, .109f, z), new Vector3(.5f, .015f, 5), Art.White, 0);
                foreach (int side in new[] { -1, 1 })
                {
                    Art.Box("Runway edge", Root, new Vector3(172 + side * 10, .11f, z), new Vector3(.2f, .015f, 12), Art.White, 0);
                    Art.Ball("Runway light", Root, new Vector3(172 + side * 11.5f, .2f, z), new Vector3(.4f, .3f, .4f), Art.Yellow);
                }
            }
            for (int i = -3; i <= 3; i++) Art.Box("Threshold stripe", Root, new Vector3(172 + i * 2.2f, .11f, -145), new Vector3(1.2f, .015f, 8), Art.White, 0);
            Transform hangar = Art.Group("Blue hangar", Root, new Vector3(135, 0, -35));
            Art.Box("Hangar walls", hangar, new Vector3(0, 4, 0), new Vector3(26, 8, 25), Art.White, .4f);
            Art.Ball("Curved hangar roof", hangar, new Vector3(0, 8, 0), new Vector3(27, 8, 26), Art.Blue);
            var garageDoor = Art.Group("Opening garage shutter", hangar, new Vector3(0, 0, -12.65f));
            garageDoor.gameObject.AddComponent<MovingScenery>();
            Art.Box("Hangar doors", garageDoor, new Vector3(0, 3.8f, 0), new Vector3(21, 7.3f, .22f), Art.Hex("497C98"));
            for (int i = -4; i <= 4; i++) Art.Box("Door ribs", garageDoor, new Vector3(i * 2.4f, 3.8f, -.17f), new Vector3(.1f, 7.2f, .1f), Art.White, 0);
            Doors.Add(new DoorSite { Name = "Airfield Garage", Hinge = garageDoor, Entrance = new Vector3(135, 0, -49.7f), Garage = true });
            var sign = Art.Group("Airfield garage sign", hangar, new Vector3(0, 7.8f, -13)); sign.localRotation = Quaternion.Euler(0, 180, 0);
            Art.Box("Garage signboard", sign, Vector3.zero, new Vector3(18, 1.5f, .2f), Art.Blue);
            WorldText("AIRFIELD GARAGE", sign, new Vector3(0, 0, .16f), .25f, Art.White, 16);
            for (int i = 0; i < 3; i++)
            {
                var display = Models.Vehicle(Root, Ride.RaceCar, i); display.Root.gameObject.AddComponent<MovingScenery>();GarageCars.Add(display); display.Root.position = new Vector3(128 + i * 7, 0, -57);
                display.Root.rotation = Quaternion.Euler(0, 155, 0);
            }
            Solid(135, -35, 13.3f, 13, 12);
            Art.Box("Control tower", Root, new Vector3(131, 7, 16), new Vector3(7, 14, 7), Art.White, .5f);
            Art.Box("Control tower windows", Root, new Vector3(131, 14.5f, 16), new Vector3(10, 3.8f, 10), Art.Hex("71BACB"), .6f);
            Art.Box("Control tower roof", Root, new Vector3(131, 16.8f, 16), new Vector3(11.5f, .65f, 11.5f), Art.Blue, .3f);
            Solid(131, 16, 5.5f, 5.5f, 17.5f);
            Art.Tube("Windsock pole", Root, new Vector3(196, 0, -63), new Vector3(196, 7, -63), .11f, Art.White);
            for (int i = 0; i < 5; i++)
                Art.Ball("Striped windsock", Root, new Vector3(196 + i * .65f, 7 - i * .09f, -63), new Vector3(.8f, .8f - i * .09f, .8f - i * .09f), i % 2 == 0 ? Art.Red : Art.White);
        }

        void Tree(float x, float z, float scale)
        {
            IllustratedScenery.Tree(Root,new Vector3(x,0,z),scale);
            sim.Obstacles.Add(new Obstacle(x, z, .29f * scale, .29f * scale, 3.8f * scale));
        }

        void Flowers(float x, float z)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                Vector3 p = new Vector3(x + Mathf.Sin(a) * .65f, .35f, z + Mathf.Cos(a) * .65f);
                Art.Tube("Flower stalk", Root, p - Vector3.up * .35f, p, .025f, leaf);
                Art.Ball("Flower", Root, p, new Vector3(.4f, .19f, .4f), i % 2 == 0 ? Art.Yellow : Art.Red);
            }
        }

        void Bench(float x, float z, float yaw)
        {
            Transform r = Art.Group("Park bench", Root, new Vector3(x, 0, z)); r.localRotation = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < 4; i++)
            {
                Art.Box("Seat slat", r, new Vector3(0, .7f, -.35f + i * .23f), new Vector3(2.8f, .12f, .2f), Art.Hex("B78551"));
                Art.Box("Back slat", r, new Vector3(0, 1.03f + i * .18f, -.48f), new Vector3(2.8f, .15f, .12f), Art.Hex("B78551"));
            }
            foreach (int side in new[] { -1, 1 }) Art.Box("Bench leg", r, new Vector3(side, .35f, 0), new Vector3(.12f, .7f, .9f), Art.Ink);
            bool rotated = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > .5f;
            Solid(x, z, rotated ? .5f : 1.45f, rotated ? 1.45f : .5f, 1.75f);
        }
        void Lamp(float x, float z)
        {
            Art.Tube("Lamp post", Root, new Vector3(x, 0, z), new Vector3(x, 5.1f, z), .095f, Art.Ink);
            Art.Ball("Lamp globe", Root, new Vector3(x, 5.3f, z), new Vector3(.75f, .88f, .75f), Art.White);
            Art.Box("Lamp cap", Root, new Vector3(x, 5.79f, z), new Vector3(.8f, .12f, .8f), Art.Teal);
            Solid(x, z, .13f, .13f, 5.8f);
        }
        void Duck(float x, float z)
        {
            Art.Ball("Duck body", Root, new Vector3(x, .37f, z), new Vector3(.76f, .45f, 1.05f), Art.White);
            Art.Ball("Duck head", Root, new Vector3(x, .7f, z + .32f), Vector3.one * .42f, Art.White);
            Art.Box("Duck bill", Root, new Vector3(x, .68f, z + .59f), new Vector3(.25f, .1f, .29f), Art.Yellow);
        }
        void Solid(float x, float z, float hx, float hz, float height)
        {
            sim.Obstacles.Add(new Obstacle(x, z, hx, hz, height));
            var obj = new GameObject("Camera obstacle"); obj.transform.SetParent(Root, false);
            obj.transform.position = new Vector3(x, height / 2, z); obj.layer = 8;
            var box = obj.AddComponent<BoxCollider>(); box.size = new Vector3(hx * 2, height, hz * 2);
        }
        void Sign(string caption, Vector3 pos, float yaw, Color color, float scale = .35f)
        {
            Transform r = Art.Group(caption, Root, pos); r.localRotation = Quaternion.Euler(0, yaw, 0);
            Art.Box("Signboard", r, Vector3.zero, new Vector3(7, 1.15f, .24f), color, .1f);
            Art.Tube("Sign post", r, new Vector3(-2.7f, -pos.y, 0), new Vector3(-2.7f, 0, 0), .09f, Art.White);
            Art.Tube("Sign post", r, new Vector3(2.7f, -pos.y, 0), new Vector3(2.7f, 0, 0), .09f, Art.White);
            WorldText(caption, r, new Vector3(0, 0, .16f), scale, Art.White, 6.2f);
        }
        void WorldText(string caption, Transform parent, Vector3 position, float scale, Color color, float maxWidth = 11)
        {
            Transform t = Art.Group("Readable sign", parent, position); t.localRotation = Quaternion.Euler(0, 180, 0);
            var text = t.gameObject.AddComponent<TextMeshPro>(); t = text.transform; text.text = caption;
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            text.fontSize = 10; text.fontStyle = FontStyles.Bold; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(100,10);
            text.fontSharedMaterial = new Material(text.font.material);
            text.fontSharedMaterial.SetFloat("_ZTestMode",4);
            text.ForceMeshUpdate();
            t.gameObject.AddComponent<FitWorldLabel>().MaxWidth = maxWidth;
        }
    }

    public sealed class FitWorldLabel : MonoBehaviour
    {
        public float MaxWidth = 11;
        IEnumerator Start()
        {
            yield return null;
            GetComponent<TMP_Text>().ForceMeshUpdate();
            var bounds = GetComponent<TMP_Text>().textBounds;
            float factor = Mathf.Min(MaxWidth / Mathf.Max(.01f, bounds.size.x), .67f / Mathf.Max(.01f, bounds.size.y));
            transform.localScale = Vector3.one * factor;
        }
        void OnDestroy() { var r=GetComponent<Renderer>();if(r!=null)Destroy(r.sharedMaterial); }
    }
}
