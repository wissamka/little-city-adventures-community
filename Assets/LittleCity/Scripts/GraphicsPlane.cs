using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed partial class GraphicsPlane:MonoBehaviour
    {
        VehicleModel model;Renderer[] low;bool built;
        readonly List<GameObject> parts=new List<GameObject>();readonly List<Material> owned=new List<Material>();
        Material paint,blue,red,dark,alloy,rubber,glass;
        public bool ShowingHigh{get;private set;}
        public static void Attach(VehicleModel m)
        {
            if(m.Mode!=Ride.Airplane)return;var g=m.Root.gameObject.AddComponent<GraphicsPlane>();g.model=m;g.low=m.Root.GetComponentsInChildren<Renderer>(true);
            if(GraphicsQuality.Current!=null)g.SetHigh(GraphicsQuality.Current.High);
        }
        public void SetHigh(bool high){if(high&&!built)Build();ShowingHigh=high&&built;foreach(var r in low)if(r!=null)r.enabled=!ShowingHigh;foreach(var p in parts)p.SetActive(ShowingHigh);}
        Material Finish(string c,float metal,float gloss){var m=GraphicsQuality.Current.Finish(Art.Hex(c),metal,gloss,7);owned.Add(m);return m;}
        Transform Part(string name,Transform parent){var r=Art.Group(name,parent);parts.Add(r.gameObject);return r;}
        GameObject Use(GameObject o,Material m){o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        void Box(string name,Transform r,Vector3 p,Vector3 size,Material m,float round=.01f)=>Use(Art.Box(name,r,p,size,Color.white,round),m);
        void Tube(string name,Transform r,Vector3 a,Vector3 b,float radius,Material m)=>Use(Art.Tube(name,r,a,b,radius,Color.white),m);
        void Ball(string name,Transform r,Vector3 p,Vector3 size,Material m)=>Use(Art.Ball(name,r,p,size,Color.white),m);
        void Mesh(string name,Transform r,List<Vector3> v,List<int> t,Material m)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(r,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        void Build()
        {
            if(GraphicsQuality.Current==null)return;
            paint=Finish("D3D7D8",.37f,.72f);blue=Finish("173F6A",.45f,.74f);red=Finish("A61D24",.32f,.67f);dark=Finish("252C30",.16f,.39f);alloy=Finish("858F96",.86f,.64f);rubber=Finish("151A1E",0,.18f);glass=GraphicsQuality.Current.Window();owned.Add(glass);
            var r=Part("Detailed light aircraft",transform);Fuselage(r);Wing(r,false);Wing(r,true);Fin(r);Cockpit(r);GearAndProp(r);
            foreach(int side in new[]{-1,1})
            {
                Tube("Main gear fairing",r,new Vector3(side*.54f,1,.9f),new Vector3(side*1.05f,.34f,1.03f),.045f,alloy);
                Tube("Hydraulic brake line",r,new Vector3(side*.57f,.99f,.94f),new Vector3(side*1.08f,.38f,1.04f),.009f,dark);
                Ball("Navigation light",r,new Vector3(side*4.58f,1.26f,.46f),new Vector3(.10f,.07f,.16f),side<0?red:Finish("2B865F",.2f,.7f));
                Tube("Wing trailing panel line",r,new Vector3(side*1.08f,1.08f,-.24f),new Vector3(side*4.25f,1.20f,-.18f),.006f,dark);
                Tube("Aileron hinge",r,new Vector3(side*2.92f,1.16f,-.05f),new Vector3(side*4.32f,1.20f,-.05f),.006f,dark);
                Tube("Wing fuel cap seam",r,new Vector3(side*1.28f,1.15f,.69f),new Vector3(side*1.44f,1.15f,.69f),.018f,alloy);
                for(int n=0;n<18;n++){float x=side*(1.05f+n*.18f);Ball("Flush wing fastener",r,new Vector3(x,1.18f+Mathf.Abs(x)*.022f,.89f),Vector3.one*.012f,alloy);}
                Tube("Cowl exhaust",r,new Vector3(side*.39f,1.0f,2.15f),new Vector3(side*.45f,.92f,2.04f),.038f,alloy);
            }
            Ball("Propeller spinner",r,new Vector3(0,1.38f,3.23f),new Vector3(.38f,.38f,.46f),paint);
            GraphicsCar.Combine(r);built=true;
        }
        void Fuselage(Transform r)
        {
            float[] z={-3.13f,-2.85f,-2.25f,-1.6f,-.9f,-.4f,.3f,1.1f,1.7f,2.15f,2.65f,3.13f};
            float[] width={.045f,.14f,.26f,.40f,.58f,.72f,.78f,.77f,.66f,.55f,.37f,.17f};
            var v=new List<Vector3>();var t=new List<int>();const int sides=40;
            for(int i=0;i<z.Length;i++)for(int n=0;n<=sides;n++){float a=n*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*width[i],1.38f+Mathf.Sin(a)*width[i]*.82f,z[i]));}
            for(int i=0;i<z.Length-1;i++)for(int n=0;n<sides;n++)
            {
                // The cockpit has a real opening under its glazing, so the pilot is
                // visible rather than buried inside an opaque ellipsoid.
                bool cockpit=z[i]>=-.4f&&z[i+1]<=1.7f&&n>=3&&n<17;if(cockpit)continue;
                int a=i*(sides+1)+n,b=a+sides+1;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});
            }
            Mesh("Lofted tapered fuselage",r,v,t,paint);
            foreach(int side in new[]{-1,1})
            {
                Tube("Longitudinal fuselage trim",r,new Vector3(side*.19f,1.42f,-2.6f),new Vector3(side*.72f,1.45f,-.45f),.022f,blue);
                Tube("Cowling accent",r,new Vector3(side*.70f,1.45f,1.45f),new Vector3(side*.41f,1.45f,2.56f),.026f,blue);
                Box("Cooling inlet",r,new Vector3(side*.24f,1.40f,2.98f),new Vector3(.17f,.13f,.04f),dark,.03f);
            }
        }
        void Cockpit(Transform r)
        {
            var v=new List<Vector3>();var t=new List<int>();const int arcs=20;
            float[] z={-.48f,-.23f,.25f,.84f,1.35f,1.72f,2.02f};float[] h={.05f,.75f,1.36f,1.49f,1.26f,.70f,.04f};float[] w={.49f,.59f,.64f,.64f,.57f,.43f,.29f};
            for(int i=0;i<z.Length;i++)for(int n=0;n<=arcs;n++){float a=n*Mathf.PI/arcs;v.Add(new Vector3(Mathf.Cos(a)*w[i],1.57f+Mathf.Sin(a)*h[i],z[i]));}
            for(int i=0;i<z.Length-1;i++)for(int n=0;n<arcs;n++){int a=i*(arcs+1)+n,b=a+arcs+1;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
            Mesh("Transparent shaped cockpit canopy",r,v,t,glass);
            foreach(int ring in new[]{1,4})for(int n=0;n<arcs;n++)Tube("Canopy arch frame",r,v[ring*(arcs+1)+n],v[ring*(arcs+1)+n+1],.017f,blue);
            foreach(int side in new[]{-1,1})for(int i=0;i<z.Length-1;i++)Tube("Canopy sill",r,new Vector3(side*w[i],1.57f,z[i]),new Vector3(side*w[i+1],1.57f,z[i+1]),.024f,blue);
            Box("Cockpit seat cushion",r,new Vector3(0,1.18f,.68f),new Vector3(.66f,.12f,.77f),dark,.055f);
            Box("Cockpit seat back",r,new Vector3(0,1.63f,.20f),new Vector3(.65f,.70f,.12f),dark,.05f);
            Box("Instrument panel",r,new Vector3(0,1.72f,1.52f),new Vector3(.82f,.30f,.10f),dark,.05f);
            foreach(float x in new[]{-.25f,0,.25f})Ball("Flight instrument face",r,new Vector3(x,1.76f,1.46f),new Vector3(.13f,.13f,.018f),alloy);
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
