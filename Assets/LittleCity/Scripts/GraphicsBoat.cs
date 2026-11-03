using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class GraphicsBoat:MonoBehaviour
    {
        VehicleModel model;Renderer[] low;bool built;
        readonly List<GameObject> parts=new List<GameObject>();readonly List<Material> owned=new List<Material>();
        Material white,paint,teak,chrome,rubber,dark,glass;Transform propeller;Vector3 previous;
        public bool ShowingHigh{get;private set;}
        public static void Attach(VehicleModel m)
        {
            if(m.Mode!=Ride.Motorboat&&m.Mode!=Ride.Speedboat)return;var g=m.Root.gameObject.AddComponent<GraphicsBoat>();g.model=m;g.low=m.Root.GetComponentsInChildren<Renderer>(true);g.previous=m.Root.position;
            if(GraphicsQuality.Current!=null)g.SetHigh(GraphicsQuality.Current.High);
        }
        public void SetHigh(bool high){if(high&&!built)Build();ShowingHigh=high&&built;foreach(var r in low)if(r!=null)r.enabled=!ShowingHigh;foreach(var p in parts)p.SetActive(ShowingHigh);}
        Material Finish(string c,float metal,float gloss,int surface=0){var m=GraphicsQuality.Current.Finish(Art.Hex(c),metal,gloss,surface);owned.Add(m);return m;}
        GameObject Use(GameObject o,Material m){o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        void Box(string name,Transform r,Vector3 p,Vector3 size,Material m,float round=.02f)=>Use(Art.Box(name,r,p,size,Color.white,round),m);
        void Tube(string name,Transform r,Vector3 a,Vector3 b,float radius,Material m)=>Use(Art.Tube(name,r,a,b,radius,Color.white),m);
        void Ball(string name,Transform r,Vector3 p,Vector3 size,Material m)=>Use(Art.Ball(name,r,p,size,Color.white),m);
        Transform Part(string name,Transform parent){var p=Art.Group(name,parent);parts.Add(p.gameObject);return p;}
        void Mesh(string name,Transform r,List<Vector3> v,List<int> t,Material m){var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(r,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<OwnedMesh>().Mesh=mesh;}
        void Build()
        {
            if(GraphicsQuality.Current==null)return;white=Finish("D8DFDF",.25f,.72f,7);paint=Finish(model.Mode==Ride.Speedboat?"183C67":"276B68",.42f,.76f,7);teak=Finish("99764B",0,.3f,3);chrome=Finish("929CA2",.87f,.67f);rubber=Finish("20292C",0,.24f);dark=Finish("343E41",.3f,.46f);glass=GraphicsQuality.Current.Window();owned.Add(glass);
            var r=Part("Detailed open cockpit boat",transform);Hull(r);
            Box("Teak cockpit sole",r,new Vector3(0,.86f,-.55f),new Vector3(2.47f,.13f,3.58f),teak,.025f);
            for(int n=-7;n<=7;n++)Tube("Teak plank caulk",r,new Vector3(n*.16f,.929f,-2.28f),new Vector3(n*.16f,.929f,1.20f),.005f,rubber);
            Box("Helm seat cushion",r,new Vector3(0,1.27f,-.85f),new Vector3(1.06f,.25f,.98f),white,.11f);
            Box("Helm seat back",r,new Vector3(0,1.64f,-1.32f),new Vector3(1.06f,.70f,.17f),paint,.08f);
            Box("Aft bench cushion",r,new Vector3(0,1.24f,-2.19f),new Vector3(2.12f,.20f,.56f),white,.08f);
            foreach(int side in new[]{-1,1})Tube("Seat stitched seam",r,new Vector3(side*.40f,1.40f,-.44f),new Vector3(side*.40f,1.40f,-1.24f),.005f,paint);
            Box("Helm console",r,new Vector3(0,1.42f,.05f),new Vector3(1.35f,.85f,.54f),white,.08f);
            Box("Instrument binnacle",r,new Vector3(0,1.83f,-.04f),new Vector3(1.03f,.13f,.30f),dark,.03f);
            foreach(float x in new[]{-.3f,0,.3f})Ball("Marine instrument",r,new Vector3(x,1.86f,-.20f),new Vector3(.16f,.13f,.027f),chrome);
            var wheel=Art.Group("Helm wheel",r,new Vector3(0,1.91f,-.30f));wheel.localRotation=Quaternion.Euler(0,90,0);
            Use(Art.Ring("Wrapped helm rim",wheel,.29f,.025f,.025f,Color.white),rubber);for(int n=0;n<3;n++){float a=n*Mathf.PI*2/3;Tube("Helm spoke",wheel,Vector3.zero,new Vector3(0,Mathf.Sin(a)*.27f,Mathf.Cos(a)*.27f),.016f,chrome);}
            Tube("Throttle lever",r,new Vector3(.64f,1.76f,-.12f),new Vector3(.64f,1.94f,-.20f),.018f,chrome);Ball("Throttle grip",r,new Vector3(.64f,1.94f,-.20f),new Vector3(.08f,.065f,.06f),rubber);
            Mesh("Raked windscreen",r,new List<Vector3>{new Vector3(-1.1f,1.25f,.91f),new Vector3(1.1f,1.25f,.91f),new Vector3(1.0f,2.01f,.55f),new Vector3(-1.0f,2.01f,.55f)},new List<int>{0,1,2,0,2,3},glass);
            Tube("Windscreen top frame",r,new Vector3(-1,2.01f,.55f),new Vector3(1,2.01f,.55f),.022f,chrome);
            foreach(int side in new[]{-1,1})
            {
                Tube("Windscreen side frame",r,new Vector3(side*1.1f,1.25f,.91f),new Vector3(side,2.01f,.55f),.022f,chrome);
                Tube("Bow rail",r,new Vector3(side*1.43f,1.58f,1.02f),new Vector3(side*.22f,1.64f,3.22f),.022f,chrome);
                foreach(float z in new[]{1.12f,2.14f}){float x=z<2?1.39f:.80f;Tube("Bow rail stanchion",r,new Vector3(side*x,1.25f,z),new Vector3(side*x,1.60f,z),.015f,chrome);}
                foreach(float z in new[]{-2.1f,.92f}){Box("Mooring cleat base",r,new Vector3(side*1.36f,1.26f,z),new Vector3(.13f,.045f,.22f),chrome);Tube("Mooring cleat horns",r,new Vector3(side*1.36f,1.32f,z-.14f),new Vector3(side*1.36f,1.32f,z+.14f),.02f,chrome);}
                Ball("Navigation light",r,new Vector3(side*.66f,1.32f,2.32f),new Vector3(.11f,.055f,.16f),Finish(side<0?"A51C27":"248554",.2f,.72f));
            }
            Box("Outboard cowling",r,new Vector3(0,.51f,-3.26f),new Vector3(.68f,1.03f,.68f),dark,.13f);
            Box("Outboard lower leg",r,new Vector3(0,-.27f,-3.20f),new Vector3(.19f,.84f,.34f),dark,.045f);
            Tube("Outboard tilt bracket",r,new Vector3(-.23f,.48f,-2.95f),new Vector3(.23f,.48f,-2.95f),.07f,chrome);
            propeller=Part("Detailed outboard propeller",transform);propeller.localPosition=new Vector3(0,-.57f,-3.42f);
            for(int n=0;n<3;n++){float a=n*Mathf.PI*2/3;var blade=Art.Group("Propeller blade",propeller);blade.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);Ball("Curved propeller paddle",blade,new Vector3(.035f,.16f,0),new Vector3(.12f,.28f,.035f),chrome);}GraphicsCar.Combine(propeller);
            if(model.Mode==Ride.Motorboat)Canopy(r);
            GraphicsCar.Combine(r);built=true;
        }
        void Hull(Transform r)
        {
            float[] z={-3.30f,-2.8f,-1.8f,-.5f,.7f,1.7f,2.6f,3.3f,3.5f};float[] width={1.20f,1.50f,1.60f,1.64f,1.55f,1.25f,.78f,.23f,.02f};var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<z.Length;i++){float w=width[i];v.AddRange(new[]{new Vector3(-w,1.18f,z[i]),new Vector3(-w*.98f,.35f,z[i]),new Vector3(-w*.62f,-.13f,z[i]),new Vector3(0,-.42f,z[i]),new Vector3(w*.62f,-.13f,z[i]),new Vector3(w*.98f,.35f,z[i]),new Vector3(w,1.18f,z[i])});}
            for(int i=0;i<z.Length-1;i++)for(int n=0;n<6;n++){int a=i*7+n,b=a+7;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            for(int n=1;n<6;n++)t.AddRange(new[]{0,n+1,n});Mesh("Tapered chine hull",r,v,t,white);
            var deckV=new List<Vector3>();var deckT=new List<int>();for(int i=4;i<z.Length;i++){int k=deckV.Count;deckV.Add(new Vector3(-width[i],1.21f,z[i]));deckV.Add(new Vector3(width[i],1.21f,z[i]));if(i<z.Length-1)deckT.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}Mesh("Shaped bow deck",r,deckV,deckT,paint);
            foreach(int side in new[]{-1,1})for(int i=0;i<z.Length-1;i++)
            {Tube("Gunwale rub rail",r,new Vector3(side*width[i],1.20f,z[i]),new Vector3(side*width[i+1],1.20f,z[i+1]),.042f,chrome);Tube("Hull accent stripe",r,new Vector3(side*width[i]*.995f,.86f,z[i]),new Vector3(side*width[i+1]*.995f,.86f,z[i+1]),.034f,paint);}
        }
        void Canopy(Transform r)
        {
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-1.95f,.45f})Tube("Bimini support",r,new Vector3(side*1.35f,1.22f,z),new Vector3(side*1.35f,2.95f,z),.025f,chrome);
            var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<=12;i++){float x=-1.5f+i*.25f,y=3.07f-.13f*Mathf.Pow(x/1.5f,2);v.Add(new Vector3(x,y,-2.1f));v.Add(new Vector3(x,y,.60f));if(i<12){int k=i*2;t.AddRange(new[]{k,k+1,k+2,k+2,k+1,k+3});}}
            int front=v.Count;var back=t.ToArray();for(int i=0;i<front;i++)v.Add(v[i]-Vector3.up*.025f);for(int i=0;i<back.Length;i+=3)t.AddRange(new[]{back[i]+front,back[i+2]+front,back[i+1]+front});
            Mesh("Arched fabric sun canopy",r,v,t,paint);
        }
        void LateUpdate(){float speed=Vector3.Distance(transform.position,previous)/Mathf.Max(.001f,Time.deltaTime);previous=transform.position;if(propeller!=null&&ShowingHigh)propeller.Rotate(Vector3.forward,Mathf.Min(speed,25)*120*Time.deltaTime,Space.Self);}
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
