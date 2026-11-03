using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    // Visitors yield to the player; they never add collision walls or block a doorway.
    public sealed class AmbientPeople:MonoBehaviour
    {
        sealed class Visitor {public CharacterModel Model;public Vector3 Home,Target;public float Phase,Wait;public int Id;}
        CityGame game;readonly List<Visitor> people=new List<Visitor>();Vector3 center;string area="";float timer;int serial;
        public int Count=>people.Count;public int MovingCount {get;private set;}public int AvoidanceCount {get;private set;}
        public void Initialize(CityGame owner){game=owner;}
        static Vector3 V(Point p)=>new Vector3(p.X,p.Y,p.Z);
        bool Free(Vector3 p)
        {
            if(game.State.Indoors)
            {if(Mathf.Abs(p.x-game.State.RoomX)>game.State.RoomHalfX-1.6f||Mathf.Abs(p.z-game.State.RoomZ)>game.State.RoomHalfZ-2.5f)return false;if(Mathf.Abs(p.y-game.State.Position.Y)>1)return false;}
            else if(game.State.IsOcean(new Point(p.x,p.y,p.z)))return false;
            foreach(var o in game.State.Indoors?game.State.InteriorObstacles:game.State.Obstacles)
            {
                if(p.y>=o.Height+.05f||p.y+1.5f<o.Bottom)continue;Vector2 d=new Vector2(p.x-o.X,p.z-o.Z);float c=Mathf.Cos(o.Yaw),s=Mathf.Sin(o.Yaw);Vector2 local=new Vector2(d.x*c-d.y*s,d.x*s+d.y*c);
                if(Mathf.Abs(local.x)<o.HalfX+.7f&&Mathf.Abs(local.y)<o.HalfZ+.7f)return false;
            }
            return true;
        }
        Vector3 Ground(Vector3 p){p.y=game.State.GroundHeight(new Point(p.x,game.State.Position.Y,p.z));return p;}
        void Rebuild(string key)
        {
            foreach(var p in people)Destroy(p.Model.Root.gameObject);people.Clear();area=key;center=V(game.State.Position);serial=0;
            bool inside=game.State.Indoors;if(inside&&(game.State.RoomName=="Home"||game.Camper.Inside))return;
            int wanted=inside?3:Application.isMobilePlatform?12:24;
            if(!inside&&game.State.WorldIndex==1&&IslandLayout.At(game.State.Position)>=0)wanted=Application.isMobilePlatform?10:18;
            for(int attempt=0;attempt<300&&people.Count<wanted;attempt++)
            {
                float angle=attempt*2.399963f,rad=inside?2+attempt%7:15+attempt%11*5;
                Vector3 p=Ground(center+new Vector3(Mathf.Sin(angle)*rad,0,Mathf.Cos(angle)*rad));
                if(!Free(p)||Vector3.Distance(p,center)<(inside?2:10))continue;
                bool overlaps=false;foreach(var other in people)if(Vector3.Distance(other.Home,p)<3)overlaps=true;if(overlaps)continue;
                int id=serial++;var model=Models.Character(transform,id%3);model.Root.name="Island and city visitor "+id;model.Root.position=p;model.Root.localScale=Vector3.one*(.57f+(id%4)*.055f);
                var tint=new MaterialPropertyBlock();tint.SetColor("_Color",Color.HSVToRGB((id*.137f+.12f)%1,.55f,.85f));
                foreach(var renderer in model.Root.GetComponentsInChildren<Renderer>())if(renderer.name.ToLowerInvariant().Contains("hood")||renderer.name.ToLowerInvariant().Contains("sleeve")||renderer.name.ToLowerInvariant().Contains("beanie")||renderer.name.StartsWith("Pocket"))renderer.SetPropertyBlock(tint);
                people.Add(new Visitor{Id=id,Model=model,Home=p,Target=p,Phase=id*.53f,Wait=id%3*.6f});
            }
        }
        void Update()
        {
            if(game==null||!game.Ready||game.Worlds.Busy)return;
            string key=game.State.WorldIndex+":"+(game.State.Indoors?game.State.RoomName:game.State.WorldIndex==1?IslandLayout.At(game.State.Position).ToString():"outside");
            Vector3 player=V(game.State.Position);timer+=Time.deltaTime;
            if(area!=key||!game.State.Indoors&&Vector3.Distance(player,center)>90){Rebuild(key);}
            MovingCount=0;if(game.Hud.MenuOpen)return;float dt=Mathf.Min(.05f,Time.deltaTime);
            foreach(var p in people)
            {
                Vector3 here=p.Model.Root.position;bool visible=Vector3.Distance(here,player)<110;p.Model.Root.gameObject.SetActive(visible);if(!visible)continue;
                p.Wait-=dt;if(p.Wait<=0&&Vector3.Distance(here,p.Target)<.5f)
                {p.Phase+=1.618f;p.Target=Ground(p.Home+new Vector3(Mathf.Sin(p.Phase)*4,0,Mathf.Cos(p.Phase)*4));p.Wait=1.2f+p.Id%4*.6f;}
                Vector3 delta=p.Target-here;delta.y=0;float speed=0;
                if(p.Wait<=0&&delta.magnitude>.1f)
                {
                    Vector3 next=Ground(here+delta.normalized*dt*(.65f+p.Id%3*.15f));bool safe=Free(next)&&Vector3.Distance(next,player)>(game.State.Mode==Ride.Walk?1.8f:5);
                    foreach(var other in people)if(other!=p&&Vector3.Distance(other.Model.Root.position,next)<.8f)safe=false;
                    if(safe){p.Model.Root.position=next;p.Model.Root.rotation=Quaternion.Slerp(p.Model.Root.rotation,Quaternion.LookRotation(delta),dt*5);speed=1;MovingCount++;}
                    else{AvoidanceCount++;p.Target=here;p.Wait=.6f;}
                }
                p.Model.Pose(timer+p.Phase,speed,false,false);p.Model.Head.localRotation=Quaternion.Euler(0,speed==0?Mathf.Sin(timer*.4f+p.Id)*12:0,0);
            }
        }
    }
}
