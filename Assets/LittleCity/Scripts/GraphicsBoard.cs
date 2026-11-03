using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class GraphicsBoard:MonoBehaviour
    {
        VehicleModel model;Renderer[] low;bool built;readonly List<GameObject> parts=new List<GameObject>();readonly List<Material> owned=new List<Material>();
        public bool ShowingHigh{get;private set;}
        public static void Attach(VehicleModel m){if(m.Mode!=Ride.Skateboard&&m.Mode!=Ride.Scooter)return;var g=m.Root.gameObject.AddComponent<GraphicsBoard>();g.model=m;g.low=m.Root.GetComponentsInChildren<Renderer>(true);if(GraphicsQuality.Current!=null)g.SetHigh(GraphicsQuality.Current.High);}
        public void SetHigh(bool high){if(high&&!built)Build();ShowingHigh=high&&built;foreach(var r in low)if(r!=null)r.enabled=!ShowingHigh;foreach(var p in parts)p.SetActive(ShowingHigh);}
        Material Finish(string c,float metal,float gloss,int surface=0){var m=GraphicsQuality.Current.Finish(Art.Hex(c),metal,gloss,surface);owned.Add(m);return m;}
        GameObject Use(GameObject o,Material m){o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        void Box(string name,Transform p,Vector3 pos,Vector3 size,Material m,float round=.01f)=>Use(Art.Box(name,p,pos,size,Color.white,round),m);
        void Tube(string name,Transform p,Vector3 a,Vector3 b,float radius,Material m)=>Use(Art.Tube(name,p,a,b,radius,Color.white),m);
        Transform Part(string name,Transform parent){var r=Art.Group(name,parent);parts.Add(r.gameObject);return r;}
        void Deck(Transform r,Material wood,Material tape,bool scooter)
        {
            var v=new List<Vector3>();var t=new List<int>();const int steps=24;
            for(int i=0;i<=steps;i++)
            {
                float z=-.625f+i*1.25f/steps,tip=Mathf.Clamp01((Mathf.Abs(z)-.43f)/.195f),width=.23f*(1-.48f*tip*tip),y=.267f+(scooter?0:tip*tip*.08f);
                v.AddRange(new[]{new Vector3(-width,y-.06f,z),new Vector3(-width,y,z),new Vector3(width,y,z),new Vector3(width,y-.06f,z)});
            }
            for(int i=0;i<steps;i++)for(int j=0;j<4;j++){int a=i*4+j,b=i*4+(j+1)%4;t.AddRange(new[]{a,a+4,b,b,a+4,b+4});}
            int end=steps*4;t.AddRange(new[]{0,1,2,0,2,3,end,end+3,end+2,end,end+2,end+1});
            Mesh("Shaped laminated deck",r,v,t,wood);
            var tv=new List<Vector3>();var tt=new List<int>();for(int i=1;i<steps;i++){var left=v[i*4+1]+new Vector3(.018f,.004f,0);var right=v[i*4+2]+new Vector3(-.018f,.004f,0);int k=tv.Count;tv.Add(left);tv.Add(right);if(i<steps-1)tt.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}Mesh("Fine grip surface",r,tv,tt,tape);
        }
        void Mesh(string name,Transform r,List<Vector3> v,List<int> t,Material m){var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(r,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<OwnedMesh>().Mesh=mesh;}
        void Build()
        {
            if(GraphicsQuality.Current==null)return;bool scooter=model.Mode==Ride.Scooter;var alloy=Finish("929A9F",.83f,.6f);var paint=Finish(scooter?"BC7932":"326F8C",.42f,.68f,7);var tape=Finish("252A2C",0,.16f,8);var wood=Finish("AC8350",0,.31f,3);var urethane=Finish("D3C8A7",.02f,.32f);
            var r=Part("Detailed scooter or skateboard",transform);Deck(r,scooter?alloy:wood,tape,scooter);
            if(scooter)
            {
                Tube("Aluminium steering tube",r,new Vector3(0,.20f,.60f),new Vector3(0,1.25f,.48f),.029f,alloy);
                foreach(int side in new[]{-1,1})Tube("Twin front fork",r,new Vector3(side*.047f,.32f,.59f),new Vector3(side*.047f,.14f,.64f),.018f,paint);
                Box("Forged head clamp",r,new Vector3(0,.36f,.584f),new Vector3(.10f,.09f,.10f),paint,.025f);
                Tube("Riser handlebar",r,new Vector3(-.32f,1.25f,.48f),new Vector3(.32f,1.25f,.48f),.022f,alloy);
                foreach(int side in new[]{-1,1})Tube("Ribbed rubber handgrip",r,new Vector3(side*.22f,1.25f,.48f),new Vector3(side*.36f,1.25f,.48f),.034f,tape);
                Box("Rear foot brake",r,new Vector3(0,.30f,-.53f),new Vector3(.14f,.045f,.28f),paint,.025f);
            }
            else foreach(int end in new[]{-1,1})
            {
                Box("Truck base plate",r,new Vector3(0,.198f,end*.43f),new Vector3(.22f,.025f,.16f),alloy);
                Tube("Forged truck hanger",r,new Vector3(-.245f,.105f,end*.43f),new Vector3(.245f,.105f,end*.43f),.026f,alloy);
                Tube("Truck kingpin",r,new Vector3(0,.19f,end*.42f),new Vector3(0,.12f,end*.43f),.019f,alloy);
                Box("Polyurethane bushing",r,new Vector3(0,.155f,end*.43f),new Vector3(.07f,.045f,.07f),paint,.025f);
                foreach(int side in new[]{-1,1})foreach(float z in new[]{-.05f,.05f})Box("Recessed truck bolt",r,new Vector3(side*.073f,.274f,end*.43f+z),new Vector3(.015f,.004f,.015f),alloy,.002f);
            }
            foreach(var pivot in model.Wheels)
            {
                float radius=scooter?.14f:.105f,width=scooter?.036f:.04f;var w=Part("Detailed rotating polyurethane wheel",pivot);
                Use(Art.Ring("Polyurethane tire",w,radius*.73f,radius*.27f,width,Color.white),scooter?tape:urethane);
                foreach(int side in new[]{-1,1}){var hub=Use(Art.Ring("Sealed bearing wheel hub",w,radius*.47f,.012f,.012f,Color.white),alloy);hub.transform.localPosition=new Vector3(side*width*.8f,0,0);Tube("Axle nut",w,Vector3.zero,new Vector3(side*width,0,0),.017f,alloy);}
                GraphicsCar.Combine(w);
            }
            GraphicsCar.Combine(r);built=true;
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
