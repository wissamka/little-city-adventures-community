using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public static partial class Models
    {
        static void RoadCamper(VehicleModel m)
        {
            bool rv=m.Mode==Ride.Camper;float half=rv?1.6f:1.4f,front=rv?4:3.3f;
            Color paint=Art.Hex(rv?"E5DFCF":"416D86"),ink=Art.Hex("28343B"),metal=Art.Hex("A8B5BC");
            var body=Art.Group("Open glazed road vehicle body",m.Root);
            void Box(string name,Vector3 p,Vector3 size,Color c,float round=.04f)
            {var o=Art.Box(name,body,p,size,c,round);if(name.Contains("pillar"))o.GetComponent<Renderer>().sharedMaterial=Art.Mat(c,0);}
            Box("Lower chassis",new Vector3(0,.53f,0),new Vector3(half*2,.24f,front*2),ink,.10f);
            Box("Interior oak floor",new Vector3(0,.72f,0),new Vector3(half*2-.15f,.16f,front*2-.2f),Art.Hex("AF926D"));
            Box("Sculpted short bonnet",new Vector3(0,1.23f,front-.43f),new Vector3(half*2,.85f,.86f),paint,.18f);
            Box("Front bumper",new Vector3(0,.7f,front),new Vector3(half*2+.04f,.3f,.20f),ink,.07f);
            Box("Rear bumper",new Vector3(0,.7f,-front),new Vector3(half*2+.04f,.24f,.20f),ink,.07f);
            Box("Front grille",new Vector3(0,1.2f,front+.006f),new Vector3(half*1.2f,.35f,.05f),ink);
            Art.GlassPane("Transparent front windshield",body,new Vector3(-half+.08f,1.65f,front-.12f),new Vector3(half-.08f,1.65f,front-.12f),new Vector3(half-.18f,2.83f,front-.91f),new Vector3(-half+.18f,2.83f,front-.91f));
            foreach(int side in new[]{-1,1})
            {
                Box("Continuous door sill",new Vector3(side*(half-.07f),1.17f,-.3f),new Vector3(.14f,.75f,front*2-.7f),paint);
                Box("Roof edge above side windows",new Vector3(side*(half-.07f),2.98f,-.4f),new Vector3(.14f,.3f,front*2-.8f),paint);
                for(int i=0;i<3;i++)
                {
                    float z0=-front+.12f+i*(front*2-1.2f)/3,z1=-front+.12f+(i+1)*(front*2-1.2f)/3;
                    Art.GlassPane("Transparent side window",body,new Vector3(side*(half-.07f),1.56f,z0+.06f),new Vector3(side*(half-.07f),1.56f,z1-.06f),new Vector3(side*(half-.07f),2.83f,z1-.06f),new Vector3(side*(half-.07f),2.83f,z0+.06f));
                    Box("Slim window pillar",new Vector3(side*(half-.07f),2.18f,z0),new Vector3(.15f,1.3f,.11f),paint);
                }
                Art.Tube("Sloping windshield frame",body,new Vector3(side*(half-.04f),1.60f,front-.10f),new Vector3(side*(half-.14f),2.90f,front-.90f),.065f,paint);
                Box("LED headlight",new Vector3(side*half*.74f,1.35f,front+.007f),new Vector3(.43f,.18f,.07f),Art.Hex("FFF2C7"));
                Box("Tail lamp",new Vector3(side*(half-.20f),1.42f,-front-.015f),new Vector3(.16f,.48f,.06f),Art.Red);
                Art.Tube("Mirror arm",body,new Vector3(side*half,2.05f,front-1.1f),new Vector3(side*(half+.23f),2.05f,front-.98f),.03f,ink);
                Box("Side mirror",new Vector3(side*(half+.23f),2.09f,front-.96f),new Vector3(.13f,.27f,.16f),metal);
                foreach(int end in new[]{-1,1})
                {
                    float axle=end*(rv?2.52f:2.0f);Wheel(m,new Vector3(side*(half-.05f),.62f,axle),.62f,.35f,true);
                    Box("Wheel arch top",new Vector3(side*(half-.01f),1.29f,axle),new Vector3(.30f,.12f,1.43f),ink);
                }
                Box("Horizontal touring stripe",new Vector3(side*(half+.008f),1.37f,-.7f),new Vector3(.025f,.16f,front*2-1.5f),rv?Art.Teal:Art.White,.01f);
            }
            Box("Rear lower panel",new Vector3(0,1.17f,-front+.02f),new Vector3(half*2,.75f,.14f),paint);
            Box("Rear upper panel",new Vector3(0,2.99f,-front+.02f),new Vector3(half*2,.30f,.14f),paint);
            Art.GlassPane("Transparent rear window",body,new Vector3(-half+.10f,1.56f,-front+.02f),new Vector3(half-.10f,1.56f,-front+.02f),new Vector3(half-.10f,2.83f,-front+.02f),new Vector3(-half+.10f,2.83f,-front+.02f));
            Box("Rounded touring roof",new Vector3(0,3.16f,-.44f),new Vector3(half*2,.18f,front*2-.85f),paint,.08f);
            Box("Dashboard",new Vector3(0,1.58f,front-.86f),new Vector3(half*2-.2f,.25f,.47f),ink,.065f);
            float driverZ=front-1.52f;CamperFurnishings.Seat(body,new Vector3(-.70f,1.04f,driverZ),ink);CamperFurnishings.Seat(body,new Vector3(.70f,1.04f,driverZ),ink);
            m.Seat=new Vector3(-.70f,1.19f,driverZ);
            m.Grips[0]=m.Seat+new Vector3(-.19f,.35f,.40f);m.Grips[1]=m.Seat+new Vector3(.19f,.35f,.40f);
            foreach(int side in new[]{-1,1})m.Pedals.Add(Art.Group("Driver pedal contact",m.Root,m.Seat+new Vector3(side*.14f,-.47f,.43f)));
            var steering=Art.Group("Driver steering wheel",body,m.Seat+new Vector3(0,.35f,.40f));steering.localRotation=Quaternion.Euler(0,90,0);Art.Ring("Steering rim",steering,.21f,.025f,.025f,ink);
            if(rv)
            {
                CamperFurnishings.Build(body,.80f,null);
                Box("Side entry door lower panel",new Vector3(half+.015f,1.17f,1.20f),new Vector3(.035f,.75f,1.15f),Art.White);
                Box("RV door handle",new Vector3(half+.06f,1.65f,.83f),new Vector3(.045f,.08f,.20f),ink,.02f);
                Box("RV boarding step",new Vector3(half+.18f,.47f,1.20f),new Vector3(.36f,.12f,1.13f),metal);
            }
            else for(int row=0;row<2;row++)foreach(int side in new[]{-1,1})CamperFurnishings.Seat(body,new Vector3(side*.70f,1.04f,-.30f-row*1.35f),Art.Hex("65757A"));
            GraphicsCar.Combine(body);foreach(var wheel in m.Wheels)GraphicsCar.Combine(wheel);
            var details=Art.Group("High Def touring details",m.Root);
            for(int i=-4;i<=4;i++)Art.Box("Fine grille slat",details,new Vector3(i*.15f,1.2f,front+.042f),new Vector3(.055f,.27f,.02f),metal,.004f);
            for(int i=0;i<3;i++)Art.Box("Roof skylight",details,new Vector3(0,3.28f,-front+1.0f+i*1.4f),new Vector3(.80f,.07f,.65f),Art.Hex("567C87"),.055f);
            foreach(int side in new[]{-1,1})Art.Tube("Roof luggage rail",details,new Vector3(side*(half-.25f),3.34f,-front+.25f),new Vector3(side*(half-.25f),3.34f,front-1.2f),.035f,metal);
            for(int i=0;i<4;i++)Art.Box("Instrument display",details,new Vector3(-.90f+i*.15f,1.728f,front-.99f),new Vector3(.10f,.025f,.14f),i==0?Art.Teal:ink,.015f);
            GraphicsCar.Combine(details);var g=m.Root.gameObject.AddComponent<GraphicsCamper>();g.Details=details.gameObject;g.SetHigh(GraphicsQuality.Current!=null&&GraphicsQuality.Current.High);
        }
    }
    public sealed class GraphicsCamper:MonoBehaviour
    {
        public GameObject Details;public bool ShowingHigh{get;private set;}
        public void SetHigh(bool high){ShowingHigh=high;if(Details!=null)Details.SetActive(high);}
    }
    public static class CamperFurnishings
    {
        public static void Seat(Transform r,Vector3 p,Color color)
        {
            Art.Box("Upholstered seat cushion",r,p,new Vector3(.76f,.25f,.76f),color,.10f);
            Art.Box("Upholstered seat back",r,p+new Vector3(0,.4f,-.35f),new Vector3(.76f,.8f,.16f),color,.08f);
            Art.Box("Headrest",r,p+new Vector3(0,.86f,-.34f),new Vector3(.46f,.25f,.18f),color,.07f);
        }
        public static void Build(Transform r,float floor,List<Obstacle> solids)
        {
            Color linen=Art.Hex("E9E0C8"),wood=Art.Hex("B59B76"),blue=Art.Hex("658D92");
            void Box(string name,Vector3 p,Vector3 size,Color color,bool solid=false)
            {p.y+=floor;var o=Art.Box(name,r,p,size,color,.05f);if(name.Contains("cabinets"))o.GetComponent<Renderer>().sharedMaterial=Art.Mat(color,0);if(solid&&solids!=null)solids.Add(new Obstacle(p.x,p.z,size.x/2,size.z/2,p.y+size.y/2));}
            Box("Camper bed frame",new Vector3(0,.23f,-2.9f),new Vector3(2.86f,.46f,1.8f),wood,true);
            Box("Camper mattress",new Vector3(0,.56f,-2.9f),new Vector3(2.8f,.22f,1.75f),linen);
            Box("Folded teal duvet",new Vector3(.42f,.73f,-2.9f),new Vector3(1.91f,.13f,1.72f),blue);
            Box("Camper pillow",new Vector3(-1.0f,.80f,-2.9f),new Vector3(.60f,.21f,1.28f),Art.White);
            Box("Kitchen base cabinets",new Vector3(1.16f,.49f,-.37f),new Vector3(.76f,.98f,2.02f),wood,true);
            Box("Kitchen stone worktop",new Vector3(1.16f,1.03f,-.37f),new Vector3(.79f,.10f,2.04f),linen);
            Box("Stainless kitchen sink",new Vector3(1.16f,1.09f,.08f),new Vector3(.51f,.025f,.53f),Art.Hex("67777D"));
            Art.Tube("Kitchen faucet",r,new Vector3(1.44f,floor+1.1f,.1f),new Vector3(1.44f,floor+1.37f,.1f),.023f,Art.White);
            Art.Tube("Kitchen faucet spout",r,new Vector3(1.44f,floor+1.37f,.1f),new Vector3(1.20f,floor+1.37f,.1f),.023f,Art.White);
            foreach(float z in new[]{-.69f,-1.02f})Art.Disk("Cooktop ring",r,new Vector3(1.16f,floor+1.1f,z),.13f,.015f,Art.Ink);
            Box("Refrigerator door",new Vector3(.766f,.54f,-.85f),new Vector3(.025f,.79f,.78f),Art.White);
            Box("Fridge handle",new Vector3(.736f,.78f,-.55f),new Vector3(.025f,.19f,.035f),Art.Ink);
            Box("Dinette bench",new Vector3(-1.15f,.38f,-.68f),new Vector3(.74f,.76f,1.05f),blue,true);
            Box("Dinette back cushion",new Vector3(-1.45f,.98f,-.68f),new Vector3(.15f,.64f,1.05f),blue);
            Box("Small dining table",new Vector3(-1.14f,.98f,.35f),new Vector3(.73f,.10f,.70f),wood,true);
            Box("Table pedestal",new Vector3(-1.14f,.45f,.35f),new Vector3(.1f,.9f,.1f),Art.White);
            Box("Travel book",new Vector3(-1.14f,1.06f,.35f),new Vector3(.35f,.05f,.36f),blue);
        }
    }
}
