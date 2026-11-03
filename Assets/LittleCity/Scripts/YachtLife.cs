using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class YachtSpot
    {
        public string Label;
        public Vector3 Near,Anchor;
        public float Yaw;
        public RestPose Pose;
        public int Deck;
        public bool Stairs,Helm;
    }
    public sealed class YachtRoom
    {
        public Transform Root;
        public readonly Transform[] Decks=new Transform[2];
        public readonly List<Obstacle>[] Solids={new List<Obstacle>(),new List<Obstacle>()};
        public readonly List<YachtSpot> Spots=new List<YachtSpot>();
    }
    public sealed class YachtLife
    {
        readonly CityGame game;
        readonly YachtRoom[] rooms=new YachtRoom[3];
        readonly string[] names={"Aurora","Sea Star","Sunbeam"};
        readonly Vector3[] boarding={new Vector3(-65,0,60),new Vector3(-130,0,65),new Vector3(-5,0,65)};
        public bool Aboard { get; private set; }
        public int YachtIndex { get; private set; }
        public int Deck => Aboard && game.State.Position.Y>4.8f?1:0;
        public IReadOnlyList<YachtSpot> Spots => rooms[YachtIndex].Spots;
        public YachtLife(CityGame owner)
        {
            game=owner;for(int i=0;i<3;i++){rooms[i]=Build(i);rooms[i].Root.gameObject.SetActive(false);}
        }
        YachtSpot Nearest
        {
            get
            {
                if(!Aboard)return null;
                Vector3 p=new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z);YachtSpot best=null;float d=2.1f;
                foreach(var spot in rooms[YachtIndex].Spots)
                {
                    if(spot.Deck!=Deck)continue;
                    float n=Vector3.Distance(p,spot.Near);if(n<d){d=n;best=spot;}
                }
                return best;
            }
        }
        int Boardable
        {
            get
            {
                if(game.State.WorldIndex!=1 || game.State.Mode!=Ride.Walk || game.State.Indoors)return -1;
                var p=new Vector3(game.State.Position.X,game.State.Position.Y,game.State.Position.Z);
                for(int i=0;i<3;i++)if(Vector3.Distance(p,boarding[i])<3)return i;return -1;
            }
        }
        public string Context
        {
            get
            {
                if(game.State.Resting!=RestPose.None)return game.State.Resting==RestPose.Sleeping?"F • Wake up":"F • Stand up";
                if(!Aboard)return Boardable>=0?"F • Board "+names[Boardable]+" yacht":"";
                if(Deck==0 && game.State.Position.Z < -25 && Mathf.Abs(game.State.Position.X)<2.2f)return "F • Leave yacht for the marina";
                return Nearest!=null && !Nearest.Stairs?"F • "+Nearest.Label:names[YachtIndex]+" • Explore the lounge, cabins and sun deck";
            }
        }
        public bool Use()
        {
            if(game.State.Resting!=RestPose.None){game.State.EndRest();game.RefreshPlayer();return true;}
            if(!Aboard)
            {
                int index=Boardable;if(index<0)return false;
                Board(index);return true;
            }
            if(Deck==0 && game.State.Position.Z < -25 && Mathf.Abs(game.State.Position.X)<2.2f){if(!game.Worlds.Voyages.LeaveCabin())Leave();return true;}
            var spot=Nearest;if(spot==null)return true;
            if(spot.Helm){game.Worlds.Voyages.Begin(YachtIndex);return true;}
            if(spot.Stairs){game.State.Say("Walk along the staircase to change decks.");return true;}
            else game.State.BeginRest(spot.Pose,new Point(spot.Anchor.x,spot.Anchor.y,spot.Anchor.z),spot.Yaw);
            game.RefreshPlayer();return true;
        }
        public void Board(int index)
        {
            YachtIndex=index;Aboard=true;game.Worlds.SetOutdoorVisible(false);rooms[index].Root.gameObject.SetActive(true);
            game.State.EnterRoom(new Point(0,0,-27),10.7f,29.5f,names[index]+" · Main deck");game.State.InteriorGround=YachtStairs.FloorAt;game.State.Say("Welcome aboard "+names[index]+". Upstairs, F at the captain’s chair starts sailing.");ApplyDeck();game.RefreshPlayer();
        }
        void ApplyDeck()
        {
            rooms[YachtIndex].Decks[0].gameObject.SetActive(true);rooms[YachtIndex].Decks[1].gameObject.SetActive(true);
            game.State.InteriorObstacles.Clear();foreach(var solids in rooms[YachtIndex].Solids)game.State.InteriorObstacles.AddRange(solids);
        }
        public void Tick()
        {
            if(!Aboard)return;
            game.State.RoomName=names[YachtIndex]+(game.State.Position.Y>.1f && game.State.Position.Y<5.3f?" · Stairs":Deck==1?" · Sun deck":" · Main deck");
        }
        public void Leave()
        {
            if(!Aboard)return;game.State.EndRest();rooms[YachtIndex].Root.gameObject.SetActive(false);Aboard=false;
            game.State.ExitInterior();game.Worlds.SetOutdoorVisible(true);game.RefreshPlayer();
        }
        YachtRoom Build(int index)
        {
            var room=new YachtRoom{Root=Art.Group(names[index]+" furnished yacht",game.transform)};
            Color accent=Art.Hex(index==0?"7B9296":index==1?"A2ADA0":"C3AD83"),wood=YachtDesign.Teak;
            Art.Box("Sea through the panoramic windows",room.Root,new Vector3(0,-4,0),new Vector3(1000,2,1000),Art.Hex("269CB9"),0);
            for(int deck=0;deck<2;deck++)
            {
                float y=deck*5.4f;var r=Art.Group(deck==0?"Furnished main deck":"Open sun deck",room.Root);room.Decks[deck]=r;
                if(deck==0)Art.Box("Teak floor",r,new Vector3(0,y-.12f,0),new Vector3(22,.24f,60),wood,0);
                else FloorWithOpening(r,y-.12f,.24f,wood);
                for(int x=-10;x<=10;x++)
                {
                    if(deck==1 && x>=5 && x<=7){Art.Box("Deck plank seam",r,new Vector3(x,y+.012f,-29),new Vector3(.022f,.015f,2),Art.Hex("B78F61"),0);Art.Box("Deck plank seam",r,new Vector3(x,y+.012f,5),new Vector3(.022f,.015f,50),Art.Hex("B78F61"),0);}
                    else Art.Box("Deck plank seam",r,new Vector3(x,y+.012f,0),new Vector3(.022f,.015f,60),Art.Hex("B78F61"),0);
                }
                foreach(int side in new[]{-1,1})
                {
                    Wall(room,deck,new Vector3(side*11,y+.55f,0),new Vector3(.25f,1.1f,60),Art.White);
                    for(int z=-27;z<=27;z+=6)
                    {
                        if(deck==0)
                        {
                            YachtDesign.Panel("Window post",r,new Vector3(side*10.95f,y+2.5f,z),new Vector3(.09f,3,.08f),YachtDesign.Ink);
                            Art.GlassPane("Panoramic sea window",r,new Vector3(side*10.94f,y+1.1f,z-2.8f),new Vector3(side*10.94f,y+1.1f,z+2.8f),new Vector3(side*10.94f,y+3.9f,z+2.8f),new Vector3(side*10.94f,y+3.9f,z-2.8f));
                        }
                        else Art.Tube("Sun deck safety post",r,new Vector3(side*10.9f,y+.5f,z),new Vector3(side*10.9f,y+1.1f,z),.035f,YachtDesign.Metal);
                    }
                    if(deck==1)Art.Tube("Continuous sun deck rail",r,new Vector3(side*10.9f,y+1.12f,-30),new Vector3(side*10.9f,y+1.12f,30),.04f,YachtDesign.Metal);
                    else YachtDesign.Panel("Concealed warm ceiling strip",r,new Vector3(side*8.3f,3.18f,0),new Vector3(.06f,.025f,59),Art.Hex("FFE6B4"),0);
                }
                Wall(room,deck,new Vector3(0,y+1,-30),new Vector3(22,2,.25f),Art.White);
                Wall(room,deck,new Vector3(0,y+1,30),new Vector3(22,2,.25f),Art.White);
                if(deck==0)
                {
                    FloorWithOpening(r,4.1f,.2f,Art.White);
                    for(int z=-24;z<=24;z+=12)
                    {
                        Art.Disk("Recessed ceiling light",r,new Vector3(0,3.97f,z),.16f,.025f,YachtDesign.Pearl);
                        var lamp=new GameObject("Warm cabin light").AddComponent<Light>();lamp.transform.SetParent(r,false);lamp.transform.localPosition=new Vector3(0,3.3f,z);lamp.type=LightType.Point;lamp.range=14;lamp.intensity=1.6f;lamp.shadows=LightShadows.None;lamp.color=Art.Hex("FFF1D4");
                    }
                }
                room.Spots.Add(new YachtSpot{Deck=deck,Near=new Vector3(6,y,deck==0?-28.7f:-19.3f),Label=deck==0?"Stairs to sun deck":"Stairs to cabins",Stairs=true});
                if(deck==0)
                {
                    const int steps=25;float rise=YachtStairs.Height/steps,run=(YachtStairs.End-YachtStairs.Start)/steps;
                    for(int step=0;step<steps;step++){Art.Box("Walkable yacht stair tread",r,new Vector3(6,(step+.5f)*rise,-28+(step+.5f)*run),new Vector3(3.1f,rise,run+.01f),wood,0);YachtDesign.Panel("Stair nosing",r,new Vector3(6,(step+1)*rise+.008f,-28+step*run+.025f),new Vector3(3.04f,.015f,.04f),YachtDesign.Pearl,0);}
                    foreach(int side in new[]{-1,1})Art.Tube("Yacht stair handrail",r,new Vector3(6+side*1.58f,1,-28),new Vector3(6+side*1.58f,6.4f,-20),.04f,YachtDesign.Metal);
                    AdventureWorlds.Caption("SUN DECK ↑",r,new Vector3(6,1.8f,-28.6f),0,2);
                }
            }
            var main=room.Decks[0];
            AdventureWorlds.Caption(names[index].ToUpperInvariant()+" • WELCOME ABOARD",main,new Vector3(0,2.5f,-28),0,13);
            Art.Box("Welcome mat",main,new Vector3(0,.02f,-27),new Vector3(3,.025f,2),accent);
            foreach(int side in new[]{-1,1})
            {
                float sofaZ=side<0?-20:-13;Vector3 p=new Vector3(side*5,.45f,sofaZ);
                Furniture(room,0,"Lounge sofa",p,new Vector3(4,.9f,2),YachtDesign.Linen);
                YachtDesign.Panel("Tailored sofa back",main,p+new Vector3(0,.55f,side<0?-.7f:.7f),new Vector3(4,.65f,.30f),YachtDesign.Linen,.13f);
                for(int cushion=0;cushion<3;cushion++)YachtDesign.Panel("Separate linen seat cushion",main,p+new Vector3(-1.30f+cushion*1.3f,.47f,0),new Vector3(1.25f,.16f,1.5f),YachtDesign.Linen,.065f);
                YachtDesign.Panel("Accent throw pillow",main,p+new Vector3(1.35f,.70f,side<0?-.45f:.45f),new Vector3(.65f,.55f,.24f),accent,.10f);
                room.Spots.Add(new YachtSpot{Deck=0,Near=new Vector3(side*5,0,sofaZ+2),Anchor=new Vector3(side*5,.42f,sofaZ+(side<0?.55f:-.55f)),Yaw=side<0?0:Mathf.PI,Pose=RestPose.Sitting,Label="Sit on the lounge sofa"});
            }
            Furniture(room,0,"Coffee table",new Vector3(0,.43f,-19),new Vector3(3.3f,.86f,2),Art.White);
            Art.Box("Travel book",main,new Vector3(-.7f,.9f,-19),new Vector3(.6f,.08f,.5f),accent);
            Art.Ball("Fruit bowl",main,new Vector3(.5f,1,-19),new Vector3(.8f,.22f,.8f),Art.White);
            for(int i=0;i<3;i++)Art.Ball("Fruit",main,new Vector3(.3f+i*.18f,1.2f,-19),Vector3.one*.22f,i==0?Art.Red:Art.Yellow);
            Art.Box("Lounge screen",main,new Vector3(-8.7f,1.8f,-13),new Vector3(.1f,1.8f,3),Art.Ink);
            Art.Box("Screen seascape",main,new Vector3(-8.62f,1.8f,-13),new Vector3(.03f,1.5f,2.7f),Art.Blue);
            Furniture(room,0,"Dining table",new Vector3(0,.55f,0),new Vector3(3,1.1f,7),wood);
            for(int i=-1;i<=1;i++)foreach(int side in new[]{-1,1})
            {
                Vector3 chair=new Vector3(side*2.8f,.4f,i*2.2f);
                Furniture(room,0,"Dining chair",chair,new Vector3(.9f,.8f,.9f),accent);
                Art.Box("Dining chair back",main,chair+new Vector3(side*.36f,.65f,0),new Vector3(.16f,1,.9f),accent);
                room.Spots.Add(new YachtSpot{Deck=0,Near=new Vector3(side*4,0,i*2.2f),Anchor=new Vector3(side*2.8f,.10f,i*2.2f),Yaw=side<0?Mathf.PI/2:-Mathf.PI/2,Pose=RestPose.Sitting,Label="Sit at the dining table"});
                Art.Disk("Dining plate",main,new Vector3(side*.95f,1.13f,i*2.2f),.38f,.025f,Art.White);
            }
            Furniture(room,0,"Kitchen cabinet",new Vector3(7,.6f,1),new Vector3(2,1.2f,10),Art.White);
            Art.Box("Galley worktop",main,new Vector3(7,1.24f,1),new Vector3(2.1f,.12f,10.1f),Art.Ink);
            Art.Box("Kitchen sink",main,new Vector3(7,1.32f,-1),new Vector3(1.3f,.05f,1.2f),Art.Hex("94B5C2"));
            Art.Tube("Tap",main,new Vector3(7.6f,1.3f,-1),new Vector3(7.6f,1.7f,-1),.035f,Art.White);
            Furniture(room,0,"Fridge",new Vector3(7.3f,1.1f,8),new Vector3(1.4f,2.2f,1.5f),Art.White);
            foreach(int side in new[]{-1,1})Wall(room,0,new Vector3(side*5,1.6f,11),new Vector3(7.3f,3.2f,.18f),Art.White);
            AdventureWorlds.Caption("CABINS",main,new Vector3(0,2.6f,10.8f),180,4);
            foreach(int side in new[]{-1,1})
            {
                Furniture(room,0,"Cabin bed frame",new Vector3(side*4.7f,.3f,20),new Vector3(3.4f,.6f,4.4f),wood);
                Art.Box("Soft mattress",main,new Vector3(side*4.7f,.73f,20),new Vector3(3.3f,.3f,4.3f),Art.White,.14f);
                Art.Box("Duvet",main,new Vector3(side*4.7f,.94f,19.6f),new Vector3(3.25f,.16f,3.3f),accent,.07f);
                Art.Box("Pillow",main,new Vector3(side*4.7f,1.03f,22f),new Vector3(2.4f,.25f,.8f),Art.White,.12f);
                Furniture(room,0,"Bedside cabinet",new Vector3(side*7.3f,.5f,21.3f),new Vector3(1,1,1),wood);
                Art.Ball("Bedside lamp",main,new Vector3(side*7.3f,1.4f,21.3f),new Vector3(.65f,.55f,.65f),Art.White);
                room.Spots.Add(new YachtSpot{Deck=0,Near=new Vector3(side*4.7f,0,16.7f),Anchor=new Vector3(side*4.7f,1.07f,19.65f),Yaw=Mathf.PI,Pose=RestPose.Sleeping,Label="Sleep in the cabin bed"});
            }
            Furniture(room,0,"Wardrobe",new Vector3(-7.5f,1.2f,27.8f),new Vector3(2,2.4f,1.2f),Art.White);
            Furniture(room,0,"Bathroom vanity",new Vector3(5,.5f,27.8f),new Vector3(3,1,1.4f),Art.White);
            Art.Box("Mirror",main,new Vector3(5,1.9f,29.7f),new Vector3(2.8f,1.5f,.07f),Art.Hex("A1D7E0"));
            Art.Ball("Porcelain basin",main,new Vector3(5,1.1f,27.8f),new Vector3(1.4f,.3f,.85f),Art.White);
            var top=room.Decks[1];
            foreach(int side in new[]{-1,1})for(int i=0;i<3;i++)
            {
                Vector3 p=new Vector3(side*5,5.78f,-13+i*5);
                Furniture(room,1,"Sun lounger",p,new Vector3(1.5f,.75f,3),Art.White);
                Art.Box("Lounger cushion",top,p+Vector3.up*.42f,new Vector3(1.4f,.18f,2.9f),accent);
                room.Spots.Add(new YachtSpot{Deck=1,Near=new Vector3(side*3,5.4f,p.z),Anchor=new Vector3(side*5,5.63f,p.z+1.15f),Yaw=0,Pose=RestPose.Sitting,Label="Relax on a sun lounger"});
            }
            Furniture(room,1,"Sun deck table",new Vector3(0,5.9f,0),new Vector3(3,1,3),wood);
            Art.Box("Bridge cabin",top,new Vector3(0,6.8f,21),new Vector3(12,2.8f,9),Art.White,.4f);
            // An open doorway gives access to the furnished helm without opaque walls in the room.
            top.Find("Bridge cabin").gameObject.SetActive(false);
            foreach(int side in new[]{-1,1})Wall(room,1,new Vector3(side*6,6.8f,21),new Vector3(.2f,2.8f,9),Art.White);
            Art.GlassPane("Bridge windshield",top,new Vector3(-6,5,25.5f),new Vector3(6,5,25.5f),new Vector3(6,8.2f,25.5f),new Vector3(-6,8.2f,25.5f));
            Furniture(room,1,"Captain dashboard",new Vector3(0,6.2f,24),new Vector3(9,1.6f,1.4f),Art.Ink);
            foreach(int side in new[]{-1,1})
            {
                Art.Box("Navigation screen",top,new Vector3(side*2,7.07f,23.8f),new Vector3(1.3f,.7f,.09f),Art.Teal);
                Furniture(room,1,"Captain chair",new Vector3(side*2,5.85f,21),new Vector3(1.1f,.9f,1.1f),accent);
                room.Spots.Add(new YachtSpot{Deck=1,Near=new Vector3(side*2,5.4f,19),Anchor=new Vector3(side*2,5.68f,21),Yaw=0,Pose=RestPose.Sitting,Helm=true,Label="Take the helm · sail to the islands"});
            }
            for(int deck=0;deck<2;deck++)Art.CombineStatic(room.Decks[deck]);return room;
        }
        static void FloorWithOpening(Transform parent,float y,float thickness,Color color)
        {
            Art.Box("Deck beside open stairwell",parent,new Vector3(-3.3f,y,0),new Vector3(15.4f,thickness,60),color,0);
            Art.Box("Deck edge beside stairwell",parent,new Vector3(9.3f,y,0),new Vector3(3.4f,thickness,60),color,0);
            Art.Box("Aft stair landing",parent,new Vector3(6,y,-29),new Vector3(3.2f,thickness,2),color,0);
            Art.Box("Forward stair landing",parent,new Vector3(6,y,5),new Vector3(3.2f,thickness,50),color,0);
        }
        static void Furniture(YachtRoom room,int deck,string name,Vector3 p,Vector3 size,Color color)
        {
            var r=room.Decks[deck];
            if(name.Contains("table"))
            {
                YachtDesign.Panel(name,r,p+Vector3.up*(size.y/2-.09f),new Vector3(size.x,.18f,size.z),color,.08f);
                foreach(int side in new[]{-1,1})YachtDesign.Panel("Slim table pedestal",r,p+new Vector3(0,-.06f,side*size.z*.27f),new Vector3(.20f,size.y-.14f,size.z*.12f),YachtDesign.Ink,.035f);
            }
            else if(name.Contains("chair") || name=="Sun lounger" || name=="Lounge sofa")
            {
                YachtDesign.Panel(name,r,p+Vector3.up*(size.y/2-.17f),new Vector3(size.x,.34f,size.z),color,.14f);
                foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})YachtDesign.Panel("Furniture foot",r,p+new Vector3(x*size.x*.38f,-.12f,z*size.z*.34f),new Vector3(.09f,Mathf.Max(.2f,size.y-.25f),.09f),YachtDesign.Ink,.025f);
            }
            else YachtDesign.Panel(name,r,p,size,name=="Yacht wall"?YachtDesign.Pearl:color,.06f);
            room.Solids[deck].Add(new Obstacle(p.x,p.z,size.x/2,size.z/2,p.y+size.y/2,deck==0?-1000:p.y-size.y/2));
        }
        static void Wall(YachtRoom room,int deck,Vector3 p,Vector3 size,Color color)
        {
            Furniture(room,deck,"Yacht wall",p,size,color);var obj=new GameObject("Yacht camera barrier",typeof(BoxCollider));obj.transform.SetParent(room.Decks[deck],false);obj.transform.localPosition=p;obj.layer=8;obj.GetComponent<BoxCollider>().size=size;
        }
    }
}
