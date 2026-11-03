using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class CamperLife
    {
        readonly CityGame game;readonly Transform room;readonly List<Obstacle> solids=new List<Obstacle>();
        public bool Inside{get;private set;}
        public CamperLife(CityGame g){game=g;room=Build();room.gameObject.SetActive(false);}
        public static Vector3 Door(ParkedRide ride)=>ride.Model.Root.TransformPoint(new Vector3(2.42f,0,1.2f));
        public ParkedRide Boardable
        {
            get
            {
                var s=game.State;if(Inside||s.Indoors||s.Mode!=Ride.Walk||s.Resting!=RestPose.None)return null;
                foreach(var ride in game.Parking.Rides)
                {
                    if(ride.World!=s.WorldIndex||ride.Model.Mode!=Ride.Camper||!ride.Model.Root.gameObject.activeInHierarchy)continue;
                    if(Vector3.Distance(Door(ride),new Vector3(s.Position.X,s.Position.Y,s.Position.Z))<1.25f)return ride;
                }
                return null;
            }
        }
        public string Context
        {
            get
            {
                if(!Inside)return Boardable!=null?"F • Explore the RV cabin":"";
                if(game.State.Resting!=RestPose.None)return game.State.Resting==RestPose.Sleeping?"F • Wake up":"F • Stand up";
                var p=game.State.Position;
                if(p.Z> .76f && p.Z<1.95f)return "F • Leave the RV";
                if(p.Z< -1.15f)return "F • Rest in the camper bed";
                if(p.Z< -.20f)return "F • Sit at the dinette";
                return "Camper RV · Kitchen, dining and bed";
            }
        }
        public bool Use()
        {
            if(!Inside)
            {
                var parked=Boardable;if(parked==null)return false;
                Inside=true;game.Worlds.SetOutdoorVisible(false);room.gameObject.SetActive(true);
                game.State.EnterRoom(new Point(0,0,1.28f),1.55f,3.88f,"Camper RV · Living cabin");
                game.State.InteriorObstacles.Clear();game.State.InteriorObstacles.AddRange(solids);
                game.State.Place(new Point(0,0,1.28f),Mathf.PI);game.State.Say("Your home on wheels. Walk down the aisle to the bed; F uses the furniture or side door.");game.RefreshPlayer();return true;
            }
            if(game.State.Resting!=RestPose.None){game.State.EndRest();game.RefreshPlayer();return true;}
            var p=game.State.Position;
            if(p.Z>.76f&&p.Z<1.95f){Leave();return true;}
            if(p.Z< -1.15f)game.State.BeginRest(RestPose.Sleeping,new Point(.92f,.83f,-2.9f),Mathf.PI/2);
            else if(p.Z< -.20f)game.State.BeginRest(RestPose.Sitting,new Point(-1.12f,.02f,-.68f),Mathf.PI/2);
            game.RefreshPlayer();return true;
        }
        public void Leave()
        {
            if(!Inside)return;game.State.EndRest();Inside=false;room.gameObject.SetActive(false);game.State.ExitInterior();game.State.InteriorObstacles.Clear();game.Worlds.SetOutdoorVisible(true);game.RefreshPlayer();
        }
        public bool FrameCamera(Camera camera)
        {
            if(!Inside)return false;
            // A roof-off view keeps the narrow interior and full character readable.
            var p=game.State.Position;Vector3 target=new Vector3(0,.70f,Mathf.Clamp(p.Z,-2.0f,1.3f));
            camera.transform.position=target+new Vector3(0,4.4f,3.3f);camera.transform.LookAt(target);camera.fieldOfView=65;return true;
        }
        Transform Build()
        {
            var r=Art.Group("Explorable camper cabin",game.transform);
            Art.Box("Countryside beyond the clear windows",r,new Vector3(0,-.8f,0),new Vector3(1000,1,1000),Art.Hex("789B72"),0);
            Art.Box("Camper oak floor",r,new Vector3(0,-.10f,0),new Vector3(3.2f,.2f,8),Art.Hex("B59B76"),.015f);
            for(int i=-7;i<=7;i++)Art.Box("Oak plank seam",r,new Vector3(i*.20f,.008f,0),new Vector3(.008f,.008f,7.9f),Art.Hex("937B5B"),0);
            CamperFurnishings.Build(r,0,solids);
            // The far wall and real glazing remain visible; the near wall/roof lift
            // away only for the interior camera. Physical room boundaries stay solid.
            YachtDesign.Panel("Camper far wall below glazing",r,new Vector3(-1.57f,.40f,0),new Vector3(.12f,.8f,8),Art.White,.02f);
            YachtDesign.Panel("Camper far wall above glazing",r,new Vector3(-1.57f,2.18f,0),new Vector3(.12f,.34f,8),Art.White,.02f);
            for(int i=0;i<4;i++)
            {
                float z=-3.9f+i*1.94f;
                Art.GlassPane("Transparent living cabin window",r,new Vector3(-1.57f,.82f,z),new Vector3(-1.57f,.82f,z+1.84f),new Vector3(-1.57f,2.01f,z+1.84f),new Vector3(-1.57f,2.01f,z));
                Art.Box("Thin window mullion",r,new Vector3(-1.57f,1.4f,z),new Vector3(.13f,1.2f,.065f),Art.Ink,.015f);
            }
            foreach(int side in new[]{-1,1})
            {
                CamperFurnishings.Seat(r,new Vector3(side*.70f,.24f,2.48f),Art.Hex("28343B"));solids.Add(new Obstacle(side*.70f,2.45f,.38f,.45f,1.2f));
            }
            Art.Box("Cab dashboard",r,new Vector3(0,.78f,3.18f),new Vector3(2.94f,.25f,.47f),Art.Ink,.06f);solids.Add(new Obstacle(0,3.18f,1.47f,.235f,.91f));
            Art.Box("Side door mat",r,new Vector3(.72f,.02f,1.28f),new Vector3(1.4f,.025f,1.1f),Art.Hex("648F94"),.03f);
            var lamp=new GameObject("Warm camper cabin light").AddComponent<Light>();lamp.transform.SetParent(r,false);lamp.transform.localPosition=new Vector3(0,2.2f,-.4f);lamp.type=LightType.Point;lamp.range=8;lamp.intensity=1.5f;lamp.shadows=LightShadows.None;lamp.color=Art.Hex("FFF0DA");
            GraphicsCar.Combine(r);return r;
        }
    }
}
