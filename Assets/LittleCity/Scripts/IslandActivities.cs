using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class IslandActivity
    {public string Label,Kind;public Vector3 Point;public Transform Moving;public Transform[] Ropes;public int Island,Value;}
    public sealed class IslandActivities
    {
        readonly CityGame game;readonly List<IslandActivity> activities=new List<IslandActivity>();readonly List<Transform> coins=new List<Transform>();
        public IReadOnlyList<IslandActivity> Activities=>activities;public int Coins {get;private set;}public int ShoppingMask {get;private set;}public int CompletedActivities {get;private set;}
        IslandActivity active;float time;int lastWorld=-1;Transform scarf;
        public IslandActivities(CityGame g,WorldPlace world){game=g;for(int i=0;i<3;i++)Build(world,i);}
        IslandActivity Nearest
        {
            get{if(game.State.WorldIndex!=1||game.State.Mode!=Ride.Walk||game.State.Indoors)return null;var p=V(game.State.Position);IslandActivity best=null;float d=3;
                foreach(var a in activities){float n=Vector3.Distance(p,a.Point);if(n<d){d=n;best=a;}}return best;}
        }
        public string Context=>active!=null?"F • Finish playing":Nearest!=null?"F • "+Nearest.Label:"";
        public bool Use()
        {
            if(active!=null){Finish();return true;}var a=Nearest;if(a==null)return false;
            if(a.Kind=="swing"||a.Kind=="slide"||a.Kind=="roundabout")
            {active=a;time=0;game.State.BeginRest(RestPose.Sitting,P(a.Point),0);Tick(0);game.RefreshPlayer();return true;}
            if(a.Kind=="bounce"){game.State.LaunchIslandBounce(12);game.State.Say("Boing! Jump again when you land.");CompletedActivities++;return true;}
            if(a.Kind=="pipe"){var target=IslandLayout.Centers[1]+new Point(25,0,20);game.State.Place(target);game.RefreshPlayer();game.State.Say("Whoosh! The green pipe leads to the castle garden.");CompletedActivities++;return true;}
            if(a.Kind=="flag"){game.State.Say(Coins>=8?"Course complete! Eight golden coins and the castle flag are yours.":"Find all 8 coins on the steps and in the garden, then return to the flag. "+Coins+" / 8 collected.");if(Coins>=8)CompletedActivities++;return true;}
            if(a.Kind=="item"){ShoppingMask|=1<<a.Value;game.State.Say("Found "+(a.Value==0?"the sneakers":a.Value==1?"the jacket":"the backpack")+"! Find all three and visit checkout.");return true;}
            if(a.Kind=="checkout"){game.State.Say(ShoppingMask==7?"Shopping challenge complete! Your pretend outfit is ready—no real purchase.":"Shopping list: sneakers, jacket and backpack. Explore the racks to find all three.");if(ShoppingMask==7)CompletedActivities++;return true;}
            if(a.Kind=="scarf"){PlayerPrefs.SetInt("LittleCity.IslandScarf",a.Value);PlayerPrefs.Save();ApplyAccessory();game.State.Say("Trying on the "+(a.Value==1?"blue":"yellow")+" scarf. Your choice is saved.");return true;}
            return false;
        }
        public void ApplyAccessory()
        {
            if(scarf!=null)Object.Destroy(scarf.gameObject);int look=PlayerPrefs.GetInt("LittleCity.IslandScarf",0);if(look==0||game.Explorer==null)return;
            var parent=game.Explorer.Head;float y=-.12f;
            if(game.Explorer.Gummy!=null && game.Explorer.Gummy.Active){parent=game.Explorer.Gummy.ElasticBody;y=1.02f;}
            scarf=Art.Group("Island souvenir scarf",parent,new Vector3(0,y,0));Art.Ball("Soft scarf",scarf,Vector3.zero,new Vector3(.24f,.10f,.23f),look==1?Art.Blue:Art.Yellow);
            if(game.Explorer.Gummy!=null && game.Explorer.Gummy.Active)scarf.localScale=new Vector3(4,1.5f,4.3f);
        }
        public void Tick(float dt)
        {
            if(game.State.WorldIndex!=1||game.State.Indoors){if(active!=null)Finish();return;}
            if(lastWorld!=1){lastWorld=1;ApplyAccessory();}
            if(active!=null)
            {
                if(game.State.Resting==RestPose.None){ResetSeat(active);active=null;return;}time+=dt;Vector3 seat=active.Point;float yaw=0;
                if(active.Kind=="swing")
                {
                    float angle=.5f*Mathf.Sin(time*1.5f);
                    seat+=new Vector3(0,5-Mathf.Cos(angle)*3.75f,Mathf.Sin(angle)*3.75f);
                    MoveRopes(active,seat);
                }
                if(active.Kind=="roundabout"){float a=time*1.6f;seat+=new Vector3(Mathf.Sin(a)*1.4f,1.1f,Mathf.Cos(a)*1.4f);yaw=a;}
                if(active.Kind=="slide"){float t=Mathf.Clamp01(time/3);seat=active.Point+new Vector3(0,4*(1-t)+.5f,1+10*t);if(t>=1){var end=active.Point+new Vector3(0,0,12);Finish();game.State.Place(P(end));game.RefreshPlayer();return;}}
                // Sitting pose places the hips above the actor origin; anchor the hips to the seat.
                game.State.Place(P(seat-Vector3.up*(1.08f*game.State.WalkingScale+.04f)),yaw);
                if(active.Moving!=null){active.Moving.position=seat-Vector3.up*.09f;active.Moving.rotation=Quaternion.Euler(0,yaw*Mathf.Rad2Deg,0);}

            }
            foreach(var coin in coins)
            {
                if(!coin.gameObject.activeSelf)continue;coin.Rotate(0,70*dt,0);
                if(game.State.Mode==Ride.Walk&&Vector3.Distance(V(game.State.Position)+Vector3.up*.8f,coin.position)<1.6f){coin.gameObject.SetActive(false);Coins++;game.State.Say("Golden coin "+Coins+" / 8");}
            }
        }
        void Finish(){if(active==null)return;var a=active;active=null;game.State.EndRest();ResetSeat(a);CompletedActivities++;game.RefreshPlayer();}
        static void MoveRopes(IslandActivity a,Vector3 seat)
        {
            if(a.Ropes==null)return;
            for(int i=0;i<a.Ropes.Length;i++)
            {
                float x=i==0?-.7f:.7f;Vector3 top=a.Point+new Vector3(x,5,0),end=seat+Vector3.right*x,delta=top-end;
                a.Ropes[i].position=(top+end)*.5f;a.Ropes[i].rotation=Quaternion.FromToRotation(Vector3.up,delta);a.Ropes[i].localScale=new Vector3(.06f,delta.magnitude*.5f,.06f);
            }
        }
        static void ResetSeat(IslandActivity a)
        {
            if(a.Moving==null)return;var seat=a.Point+(a.Kind=="swing"?new Vector3(0,1.25f,0):new Vector3(0,1.1f,1.4f));a.Moving.position=seat-Vector3.up*.09f;a.Moving.rotation=Quaternion.identity;MoveRopes(a,seat);
        }
        void Add(int island,string kind,string label,Vector3 point,Transform moving=null,int value=0){activities.Add(new IslandActivity{Island=island,Kind=kind,Label=label,Point=point,Moving=moving,Value=value});}
        static Vector3 V(Point p)=>new Vector3(p.X,p.Y,p.Z);static Point P(Vector3 p)=>new Point(p.x,p.y,p.z);
        void Build(WorldPlace world,int i)
        {
            Vector3 c=V(IslandLayout.Centers[i]);var r=Art.Group(IslandLayout.Names[i],world.Root,c);Color color=i==0?Art.Teal:i==1?Art.Red:Art.Blue;
            Art.Disk("Island sandy shoreline",r,new Vector3(0,-.16f,0),82,.32f,Art.Hex("E8C890"));Art.Disk("Island grass",r,new Vector3(0,-.01f,0),76,.035f,Art.Hex("7AB968"));
            Art.Box("Arrival boardwalk",r,new Vector3(0,-.08f,-98),new Vector3(8,.16f,52),YachtDesign.Teak,0);
            for(int z=-120;z<-72;z+=6)foreach(int x in new[]{-1,1}){Art.Tube("Dock bollard",r,new Vector3(x*3.7f,0,z),new Vector3(x*3.7f,.8f,z),.12f,Art.White);}
            var ring=Art.Ring("Mooring approach ring",r,18,.18f,.18f,color);ring.transform.localPosition=V(IslandLayout.Berth(i))-c+new Vector3(0,1.10f,0);ring.transform.localRotation=Quaternion.Euler(0,0,90);
            Art.Tube("Island beacon",r,new Vector3(0,0,-72),new Vector3(0,24,-72),.28f,color);Art.Ball("Beacon lantern",r,new Vector3(0,25,-72),Vector3.one*3.5f,Art.Yellow);
            AdventureWorlds.Caption(IslandLayout.Names[i].ToUpperInvariant(),r,new Vector3(0,27,-72),180,38);
            AdventureWorlds.Caption("STOP IN THE RING · F TO DOCK",r,new Vector3(0,2,-116),180,14);
            for(int j=0;j<14;j++){float a=j*Mathf.PI*2/14;Vector3 p=new Vector3(Mathf.Sin(a)*65,0,Mathf.Cos(a)*65);if(p.z<-45)continue;Art.Tube("Island tree trunk",r,p,p+Vector3.up*4,.4f,YachtDesign.Teak);Art.Ball("Leaf crown",r,p+Vector3.up*5,new Vector3(7,6,7),j%2==0?Art.Hex("45865D"):Art.Hex("5BA476"));}
            if(i==0)
            {
                var swing=Art.Group("Swing playground",r,new Vector3(-20,0,-12));foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Art.Tube("Swing A-frame",swing,new Vector3(x*3,0,z*2),new Vector3(x*3,5,0),.14f,Art.Teal);
                Art.Tube("Swing top beam",swing,new Vector3(-3,5,0),new Vector3(3,5,0),.16f,Art.Yellow);
                var seat=Art.Box("Moving swing seat",swing,new Vector3(0,1.16f,0),new Vector3(1.8f,.18f,.7f),Art.Red,.08f).transform;
                var ropes=new Transform[2];for(int side=0;side<2;side++){float x=side==0?-.7f:.7f;ropes[side]=Art.Tube("Swing rope",swing,new Vector3(x,1.25f,0),new Vector3(x,5,0),.03f,Art.White).transform;}
                Add(i,"swing","Ride the swing",c+new Vector3(-20,0,-12),seat);activities[activities.Count-1].Ropes=ropes;
                var slide=Art.Group("Rainbow slide",r,new Vector3(18,0,-16));Art.Box("Slide tower",slide,new Vector3(0,2,0),new Vector3(5,4,5),Art.Teal,.2f);
                var ramp=Art.Box("Smooth slide chute",slide,new Vector3(0,2.3f,6),new Vector3(2,.18f,11),Art.Yellow,.08f);ramp.transform.localRotation=Quaternion.Euler(22,0,0);
                for(int side=-1;side<=1;side+=2)Art.Tube("Slide side wall",slide,new Vector3(side*1.1f,4.3f,1),new Vector3(side*1.1f,.3f,11),.2f,Art.Red);
                Add(i,"slide","Ride the tall slide",c+new Vector3(18,0,-16));
                Art.Disk("Roundabout",r,new Vector3(-20,.15f,24),3.5f,.3f,Art.Blue);Art.Tube("Roundabout center",r,new Vector3(-20,.3f,24),new Vector3(-20,1.6f,24),.14f,Art.Yellow);var spinSeat=Art.Box("Roundabout bench",r,new Vector3(-20,1.01f,25.4f),new Vector3(1.8f,.18f,.7f),Art.Red,.08f).transform;Art.Tube("Roundabout bench support",spinSeat,new Vector3(0,-.09f,0),new Vector3(0,-.71f,0),.14f,Art.Yellow);Add(i,"roundabout","Spin on the roundabout",c+new Vector3(-20,0,24),spinSeat);
                Art.Disk("Bouncy trampoline",r,new Vector3(22,.08f,24),4,.16f,Art.Ink);var bounceRing=Art.Ring("Trampoline padded rim",r,4,.3f,.3f,Art.Red);bounceRing.transform.localPosition=new Vector3(22,.25f,24);bounceRing.transform.localRotation=Quaternion.Euler(0,0,90);Add(i,"bounce","Bounce on the trampoline",c+new Vector3(22,0,24));
            }
            if(i==1)
            {
                for(int j=0;j<9;j++){float h=j*.45f;Art.Box("Coin course step",r,new Vector3(-25,h/2,-27+j*6),new Vector3(6,Mathf.Max(.05f,h),6),j%2==0?Art.Hex("B97948"):Art.Hex("D49655"),0);}
                for(int j=0;j<8;j++){Vector3 p=j<6?new Vector3(-25,.9f+j*.45f,-27+j*6):new Vector3(10+(j-6)*12,.9f,20);var coin=Art.Disk("Collectible golden coin",r,p,.55f,.14f,Art.Yellow).transform;coin.localRotation=Quaternion.Euler(90,0,0);coins.Add(coin);}
                Art.Disk("Green warp pipe",r,new Vector3(20,1.1f,-24),2.4f,2.2f,Art.Teal);Art.Disk("Pipe rim",r,new Vector3(20,2.2f,-24),2.8f,.45f,Art.Teal);Art.Disk("Pipe opening",r,new Vector3(20,2.44f,-24),2,.03f,Art.Ink);Add(i,"pipe","Travel through the green pipe",c+new Vector3(20,0,-28));
                foreach(int x in new[]{-1,1}){Art.Box("Castle tower",r,new Vector3(x*14,6,44),new Vector3(7,12,7),Art.Hex("E6D6BB"),.1f);Art.Ball("Red castle roof",r,new Vector3(x*14,13,44),new Vector3(9,5,9),Art.Red);}
                Art.Box("Castle gate wall",r,new Vector3(0,4,45),new Vector3(24,8,3),Art.Hex("E6D6BB"),0);Art.Box("Castle door",r,new Vector3(0,2.7f,43.3f),new Vector3(5,5.4f,.2f),YachtDesign.Teak,.3f);
                Art.Tube("Victory flagpole",r,new Vector3(8,0,28),new Vector3(8,9,28),.12f,Art.White);Art.Box("Victory flag",r,new Vector3(10,8,28),new Vector3(4,2,.10f),Art.Red,0);Add(i,"flag","Raise the castle flag",c+new Vector3(8,0,28));
                for(int j=0;j<5;j++){Vector3 p=new Vector3(-50+j*24,0,55);Art.Tube("Giant mushroom stalk",r,p,p+Vector3.up*2,.7f,Art.White);Art.Ball("Red mushroom cap",r,p+Vector3.up*2.5f,new Vector3(5,2,5),Art.Red);Art.Ball("Mushroom spot",r,p+new Vector3(0,3.4f,0),new Vector3(1,.15f,1),Art.White);}
                AdventureWorlds.Caption("COIN COURSE · 8 COINS + CASTLE FLAG",r,new Vector3(-15,3,-39),180,25);
            }
            if(i==2)
            {
                Art.Box("Shopping plaza",r,new Vector3(0,.03f,0),new Vector3(82,.06f,86),Art.Hex("D7D9D3"),0);
                Art.Box("Store back wall",r,new Vector3(0,5,35),new Vector3(70,10,.5f),Art.White,0);foreach(int x in new[]{-1,1})Art.Box("Store side wall",r,new Vector3(x*35,5,8),new Vector3(.5f,10,54),Art.White,0);
                Art.Box("Front canopy",r,new Vector3(0,9,-19),new Vector3(72,.35f,5),Art.Ink,0);AdventureWorlds.Caption("NORDSTROM RACK",r,new Vector3(0,7.4f,-19.3f),180,40);
                string[] kinds={"Sneakers","Jackets","Backpacks"};for(int j=0;j<3;j++)
                {
                    float x=-23+j*23;var rack=Art.Group(kinds[j]+" display",r,new Vector3(x,0,5));Art.Box("Display base",rack,new Vector3(0,.6f,0),new Vector3(10,1.2f,4),Art.White,.15f);
                    Art.Tube("Rack rail",rack,new Vector3(-4,2.8f,0),new Vector3(4,2.8f,0),.06f,Art.Ink);
                    for(int k=0;k<5;k++){Color tint=k%2==0?Art.Red:Art.Blue;Vector3 p=new Vector3(-3.2f+k*1.6f,1.8f,0);if(j==0)Art.Ball("Sneaker pair",rack,p,new Vector3(1,.5f,1.6f),tint);else Art.Box(j==1?"Colorful jacket":"Backpack",rack,p,new Vector3(1.2f,j==1?1.7f:1.1f,.45f),tint,.15f);}
                    AdventureWorlds.Caption(kinds[j],rack,new Vector3(0,3.6f,0),180,9);Add(i,"item","Find the "+kinds[j].ToLowerInvariant(),c+new Vector3(x,0,1),null,j);
                }
                Art.Box("Checkout counter",r,new Vector3(-20,.65f,25),new Vector3(12,1.3f,4),Art.Blue,.1f);Art.Box("Register",r,new Vector3(-20,1.8f,25),new Vector3(1.2f,1,.7f),Art.Ink,.06f);Add(i,"checkout","Complete the shopping challenge",c+new Vector3(-20,0,21));
                foreach(int j in new[]{1,2}){float x=12+j*7;Art.Box("Try-on mirror",r,new Vector3(x,2.3f,32),new Vector3(4,4.6f,.14f),Art.Hex("A8D9E4"),.12f);Add(i,"scarf","Try on a "+(j==1?"blue":"yellow")+" scarf",c+new Vector3(x,0,29),null,j);}
                AdventureWorlds.Caption("FIND SNEAKERS, A JACKET + A BACKPACK",r,new Vector3(0,3,-29),180,33);
                world.Collisions.Add(new Obstacle(c.x,c.z+35,35,.3f,10));foreach(int side in new[]{-1,1})world.Collisions.Add(new Obstacle(c.x+side*35,c.z+8,.3f,27,10));
            }
        }
    }
}
