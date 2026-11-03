using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    // Detail is attached to the original moving pivots. Seats, grips, pedals and
    // physical dimensions remain owned by VehicleModel in both graphics modes.
    public sealed class GraphicsBike : MonoBehaviour
    {
        VehicleModel model;
        Renderer[] original;
        readonly List<GameObject> parts=new List<GameObject>();
        readonly List<Material> materials=new List<Material>();
        readonly Dictionary<Renderer,bool> visibility=new Dictionary<Renderer,bool>();
        public bool ShowingHigh {get;private set;}
        public int DetailVertices {get;private set;}
        bool built;
        Material paint,rubber,alloy,steel,dark,red;
        bool Motor=>model.Mode==Ride.Motorcycle || model.Mode==Ride.DirtBike;
        bool Offroad=>model.Mode==Ride.DirtBike || model.Mode==Ride.MountainBike;
        public static void Attach(VehicleModel m)
        {
            if(m.Mode!=Ride.Bicycle && m.Mode!=Ride.MountainBike && m.Mode!=Ride.MiniBike && m.Mode!=Ride.Motorcycle && m.Mode!=Ride.DirtBike)return;
            var b=m.Root.gameObject.AddComponent<GraphicsBike>();b.model=m;b.original=m.Root.GetComponentsInChildren<Renderer>(true);
            foreach(var r in b.original)b.visibility[r]=r.enabled;
            if(GraphicsQuality.Current!=null)b.SetHigh(GraphicsQuality.Current.High);
        }
        public void SetHigh(bool high)
        {
            if(high&&!built)Build();
            ShowingHigh=high&&built;
            foreach(var r in original)if(r!=null)r.enabled=ShowingHigh?false:visibility[r];
            foreach(var p in parts)p.SetActive(ShowingHigh);
        }
        Material Finish(Color c,float metal,float gloss,int surface=0)
        {var m=GraphicsQuality.Current.Finish(c,metal,gloss,surface);materials.Add(m);return m;}
        Transform Part(string name,Transform parent)
        {var p=Art.Group(name,parent);parts.Add(p.gameObject);return p;}
        GameObject Use(GameObject g,Material m){g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        GameObject Box(string name,Transform p,Vector3 pos,Vector3 size,Material m,float round=.01f)
            =>Use(Art.Box(name,p,pos,size,Color.white,round),m);
        void Tube(string name,Transform p,Vector3 a,Vector3 b,float radius,Material m)
            =>Use(Art.Tube(name,p,a,b,radius,Color.white),m);
        void Ball(string name,Transform p,Vector3 pos,Vector3 size,Material m)
            =>Use(Art.Ball(name,p,pos,size,Color.white),m);
        void Ring(string name,Transform p,Vector3 pos,float radius,float thickness,float width,Material m)
        {
            // More segments keep thin rims round in close gameplay views.
            const int segments=64,sides=12;var v=new List<Vector3>();var normals=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;var radial=new Vector3(0,Mathf.Sin(a),Mathf.Cos(a));
                v.Add(pos+radial*(radius+Mathf.Cos(b)*thickness)+Vector3.right*Mathf.Sin(b)*width);
                normals.Add((radial*Mathf.Cos(b)/thickness+Vector3.right*Mathf.Sin(b)/width).normalized);
                if(i==segments||j==sides)continue;int k=i*(sides+1)+j,l=k+sides+1;tris.AddRange(new[]{k,k+1,l,l,k+1,l+1});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetNormals(normals);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(p,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        void Cable(string name,Transform p,Vector3 a,Vector3 mid,Vector3 end,float radius,Material m)
        {Vector3 last=a;for(int i=1;i<=12;i++){float t=i/12f;Vector3 next=(1-t)*(1-t)*a+2*(1-t)*t*mid+t*t*end;Tube(name,p,last,next,radius,m);last=next;}}
        void Build()
        {
            if(GraphicsQuality.Current==null)return;
            paint=Finish(Art.Hex(model.Mode==Ride.DirtBike?"E29A20":model.Mode==Ride.Motorcycle?"9F1420":model.Mode==Ride.MountainBike?"C27B32":model.Mode==Ride.MiniBike?"7949A0":"1C6950"),.48f,.76f);
            rubber=Finish(new Color(.023f,.026f,.029f),0,.19f,8);alloy=Finish(new Color(.45f,.48f,.51f),.88f,.56f);steel=Finish(new Color(.22f,.25f,.27f),.83f,.43f);dark=Finish(new Color(.038f,.043f,.048f),.24f,.34f);red=Finish(Art.Hex("BD241D"),.3f,.56f);
            var body=Part("Detailed bike frame and components",transform);
            if(model.Mode==Ride.MiniBike)BuildMiniBmx(body);
            else
            {
            float radius=Motor?.47f:.53f;
            Vector3 rear=new Vector3(0,radius,-.93f),front=new Vector3(0,radius,1.02f),crank=new Vector3(0,.53f,-.1f),saddle=new Vector3(0,Motor?1.16f:1,-.43f),neck=new Vector3(0,1.28f,.62f);
            Tube("Hydroformed down tube",body,crank,neck,Motor?.06f:.039f,paint);
            Tube("Top tube",body,saddle,neck,Motor?.05f:.032f,paint);
            Tube("Seat tube",body,crank,saddle,.033f,paint);
            Tube("Adjustable seat post",body,saddle,new Vector3(0,Motor?1.39f:1.16f,-.43f),.023f,dark);
            Ball("Shaped saddle",body,new Vector3(0,Motor?1.43f:1.2f,-.44f),new Vector3(Motor?.46f:.32f,.14f,Motor?.86f:.46f),rubber);
            Tube("Headset",body,new Vector3(0,1.20f,.65f),new Vector3(0,1.42f,.58f),.05f,dark);
            Tube("Handlebar stem",body,neck,new Vector3(0,1.55f,.63f),.026f,alloy);
            Tube("Handlebar center",body,new Vector3(-.24f,1.55f,.68f),new Vector3(.24f,1.55f,.68f),.021f,dark);
            foreach(int side in new[]{-1,1})
            {
                var dropout=rear+Vector3.right*side*.085f;
                Tube("Seat stay",body,dropout,saddle+Vector3.right*side*.018f,.018f,paint);
                Tube("Chain stay",body,dropout,crank+Vector3.right*side*.065f,.023f,paint);
                var crown=neck+Vector3.right*side*.11f;var axle=front+Vector3.right*side*.11f;
                Tube("Fork stanchion",body,crown,Vector3.Lerp(crown,axle,.62f),.027f,alloy);
                Tube("Fork lower",body,Vector3.Lerp(crown,axle,.49f),axle,Offroad?.043f:.030f,Offroad?dark:paint);
                Tube("Swept handlebar",body,new Vector3(side*.24f,1.55f,.68f),new Vector3(side*.49f,1.55f,.63f),.021f,dark);
                Tube("Textured grip",body,new Vector3(side*.36f,1.55f,.63f),new Vector3(side*.51f,1.55f,.63f),.031f,rubber);
                Tube("Brake lever",body,new Vector3(side*.28f,1.56f,.68f),new Vector3(side*.45f,1.52f,.73f),.009f,alloy);
                Cable("Brake hose",body,new Vector3(side*.27f,1.54f,.68f),new Vector3(side*.3f,1.22f,1.15f),front+new Vector3(side*.15f,.14f,0),.006f,rubber);
            }
            // Brakes attach to the fork/frame so calipers never rotate with wheels.
            foreach(var wheel in model.Wheels)
            {
                BuildWheel(wheel,radius);
                Box("Fixed brake caliper",body,wheel.localPosition+new Vector3(-.15f,.17f,-.09f),new Vector3(.065f,.10f,.07f),red,.015f);
            }
            float chainX=Motor?.19f:.13f;
            Tube("Upper chain run",body,new Vector3(chainX,.64f,-.1f),new Vector3(chainX,radius+.07f,-.93f),.012f,steel);
            Tube("Lower chain run",body,new Vector3(chainX,.42f,-.1f),new Vector3(chainX,radius-.07f,-.93f),.012f,steel);
            if(!Motor)
            {
                var moving=Part("Detailed chainring and crank",model.Crank);
                Ring("Chainring",moving,new Vector3(chainX,0,0),.107f,.011f,.009f,steel);
                for(int n=0;n<32;n++){float a=n*Mathf.PI/16;Box("Chainring tooth",moving,new Vector3(chainX,Mathf.Sin(a)*.12f,Mathf.Cos(a)*.12f),new Vector3(.02f,.014f,.014f),alloy,.002f);}
                foreach(int side in new[]{-1,1})Tube("Crank arm",moving,new Vector3(side*.12f,0,0),new Vector3(side*.28f,side*.20f,0),.018f,alloy);
                foreach(var pedal in model.Pedals){var p=Part("Detailed pinned pedal",pedal);Box("Pedal cage",p,Vector3.zero,new Vector3(.23f,.045f,.19f),dark,.012f);for(int pin=-1;pin<=1;pin++)foreach(int s in new[]{-1,1})Box("Grip pin",p,new Vector3(pin*.075f,.029f,s*.068f),new Vector3(.011f,.015f,.011f),alloy,.001f);GraphicsCar.Combine(p);}
                GraphicsCar.Combine(moving);
                Box("Rear derailleur",body,new Vector3(.14f,radius-.12f,-.91f),new Vector3(.055f,.19f,.10f),dark,.02f);
            }
            else BuildMotor(body);
            foreach(var spring in model.Suspension)
            {
                var p=Part("Detailed moving shock",spring);Tube("Shock piston",p,Vector3.zero,new Vector3(0,.43f,0),.019f,alloy);
                Vector3 previous=new Vector3(.046f,.04f,0);
                for(int n=1;n<=72;n++){float a=n*Mathf.PI/6;var next=new Vector3(Mathf.Cos(a)*.046f,.04f+n*.0045f,Mathf.Sin(a)*.046f);Tube("Continuous spring coil",p,previous,next,.009f,dark);previous=next;}
                GraphicsCar.Combine(p);
            }
            }
            GraphicsCar.Combine(body);
            foreach(var p in parts)foreach(var f in p.GetComponentsInChildren<MeshFilter>())if(f.GetComponent<Renderer>().enabled)DetailVertices+=f.sharedMesh.vertexCount;
            built=true;
        }
        void BuildMiniBmx(Transform body)
        {
            // Detail follows the exact low-mode geometry and contact locations.
            Vector3 rear=model.Wheels[0].localPosition,front=model.Wheels[1].localPosition,crank=model.Crank.localPosition,saddle=new Vector3(0,.63f,-.24f),neck=new Vector3(0,.70f,.36f);
            foreach(var pair in new[]{new[]{rear,saddle},new[]{rear,crank},new[]{saddle,crank},new[]{saddle,neck},new[]{crank,neck}})Tube("Welded micro BMX frame",body,pair[0],pair[1],.032f,paint);
            Tube("Polished saddle post",body,saddle,new Vector3(0,.77f,-.24f),.026f,alloy);
            Ball("Small stitched BMX saddle",body,new Vector3(0,.80f,-.25f),new Vector3(.30f,.10f,.34f),rubber);
            Tube("Tall chrome BMX stem",body,neck,new Vector3(0,1.25f,.32f),.038f,alloy);
            Tube("BMX bar base",body,new Vector3(-.16f,1.25f,.32f),new Vector3(.16f,1.25f,.32f),.032f,alloy);
            Tube("BMX high crossbar",body,new Vector3(-.36f,2.07f,.20f),new Vector3(.36f,2.07f,.20f),.024f,alloy);
            foreach(int side in new[]{-1,1})
            {
                Tube("Short chromoly fork",body,neck+Vector3.right*side*.08f,front+Vector3.right*side*.08f,.032f,alloy);
                Tube("Oversized tall BMX bar",body,new Vector3(side*.16f,1.25f,.32f),new Vector3(side*.39f,2.45f,.18f),.032f,alloy);
                Tube("Tall BMX rubber grip",body,new Vector3(side*.39f,2.45f,.18f),new Vector3(side*.65f,2.45f,.08f),.048f,rubber);
                Tube("BMX brake lever",body,new Vector3(side*.42f,2.43f,.21f),new Vector3(side*.58f,2.40f,.19f),.009f,alloy);
                Cable("BMX long brake cable",body,new Vector3(side*.42f,2.43f,.21f),new Vector3(side*.35f,1.05f,.72f),front+new Vector3(side*.11f,.08f,0),.005f,rubber);
                Tube("BMX axle peg",body,front+Vector3.right*side*.08f,front+Vector3.right*side*.29f,.05f,alloy);
            }
            foreach(var wheel in model.Wheels)BuildWheel(wheel,.29f);
            var moving=Part("Mini BMX short crank",model.Crank);Ring("BMX single chainring",moving,new Vector3(.13f,0,0),.09f,.012f,.01f,steel);
            foreach(int side in new[]{-1,1})Tube("BMX short crank arm",moving,new Vector3(side*.12f,0,0),new Vector3(side*.24f,side*.14f,0),.018f,alloy);
            foreach(var pedal in model.Pedals){var part=Part("Mini BMX pinned pedal",pedal);Box("BMX pedal cage",part,Vector3.zero,new Vector3(.19f,.05f,.15f),dark,.01f);GraphicsCar.Combine(part);}
            Tube("BMX upper chain",body,crank+new Vector3(.13f,.09f,0),rear+new Vector3(.13f,.07f,0),.011f,steel);
            Tube("BMX lower chain",body,crank+new Vector3(.13f,-.09f,0),rear+new Vector3(.13f,-.07f,0),.011f,steel);
            GraphicsCar.Combine(moving);
        }

        void BuildWheel(Transform pivot,float radius)
        {
            var w=Part("Detailed rotating wheel",pivot);float tire=Motor?.065f:Offroad?.045f:.033f,width=Motor?.132f:Offroad?.068f:.05f;
            Ring("Pneumatic tire",w,Vector3.zero,radius-tire,tire,width,rubber);
            Ring("Double wall rim",w,Vector3.zero,radius-tire*2-.011f,.014f,width*.70f,dark);
            Tube("Sealed bearing hub",w,new Vector3(-width,0,0),new Vector3(width,0,0),.028f,alloy);
            for(int side=-1;side<=1;side+=2)for(int n=0;n<16;n++)
            {
                float a=(n+(side>0?.5f:0))*Mathf.PI/8,b=a+(n%2==0?.28f:-.28f);
                Tube("Cross laced spoke",w,new Vector3(side*width*.75f,Mathf.Sin(b)*.028f,Mathf.Cos(b)*.028f),new Vector3(side*width*.45f,Mathf.Sin(a)*(radius-2*tire),Mathf.Cos(a)*(radius-2*tire)),Motor?.0045f:.0035f,alloy);
            }
            Ring("Perforated brake disc",w,new Vector3(-.135f,0,0),.15f,.020f,.005f,steel);
            for(int n=0;n<6;n++){float a=n*Mathf.PI/3;Tube("Brake disc carrier",w,new Vector3(-.135f,0,0),new Vector3(-.135f,Mathf.Sin(a)*.14f,Mathf.Cos(a)*.14f),.009f,steel);}
            if(pivot.localPosition.z<0)for(int n=0;n<6;n++)Ring("Cassette sprocket",w,new Vector3(.07f+n*.01f,0,0),.077f-n*.006f,.008f,.004f,alloy);
            for(int n=0;n<48;n++)for(int s=-1;s<=1;s+=2)
            {float a=n*Mathf.PI/24+s*.024f;var b=Box(Offroad?"Knobby tire block":"Tire siping",w,new Vector3(s*width*.52f,Mathf.Cos(a)*(radius-.004f),Mathf.Sin(a)*(radius-.004f)),new Vector3(width*.68f,Offroad?.024f:.005f,Offroad?.043f:.012f),rubber,.002f);b.transform.localRotation=Quaternion.Euler(a*Mathf.Rad2Deg,0,0);}
            GraphicsCar.Combine(w);
        }
        void BuildMotor(Transform body)
        {
            Ball("Sculpted fuel tank",body,new Vector3(0,1.18f,.18f),new Vector3(.64f,.47f,.84f),paint);
            Box("Cylinder block",body,new Vector3(0,.75f,.06f),new Vector3(.46f,.4f,.39f),dark,.04f);
            for(int n=0;n<7;n++)Box("Cylinder cooling fin",body,new Vector3(0,.59f+n*.045f,.08f),new Vector3(.51f,.018f,.37f),steel,.007f);
            foreach(int side in new[]{-1,1}){Ball("Engine case",body,new Vector3(side*.24f,.58f,-.15f),new Vector3(.13f,.31f,.35f),steel);Tube("Footpeg",body,new Vector3(side*.19f,.53f,-.1f),new Vector3(side*.4f,.53f,-.1f),.028f,dark);}
            Cable("Curved exhaust header",body,new Vector3(.23f,.79f,.21f),new Vector3(.4f,.43f,.58f),new Vector3(.34f,.46f,-.54f),.038f,steel);
            Tube("Brushed muffler",body,new Vector3(.35f,.49f,-.49f),new Vector3(.35f,.53f,-1.16f),.071f,alloy);
            Tube("Exhaust outlet",body,new Vector3(.35f,.53f,-1.165f),new Vector3(.35f,.531f,-1.179f),.049f,rubber);
            if(Offroad)
            {
                Box("Raised front mudguard",body,new Vector3(0,1.14f,1.03f),new Vector3(.35f,.065f,.91f),paint,.03f);
                Box("Rear mudguard",body,new Vector3(0,1.31f,-1.09f),new Vector3(.39f,.075f,.65f),paint,.03f);
                var white=Finish(new Color(.81f,.84f,.83f),.05f,.4f);
                Box("Race number plate",body,new Vector3(0,1.57f,.76f),new Vector3(.42f,.40f,.055f),white,.035f);
                foreach(float x in new[]{-.065f,.065f})Box("Number eleven",body,new Vector3(x,1.59f,.792f),new Vector3(.035f,.25f,.012f),dark,.002f);
            }
            else
            {
                Box("Rear seat cowl",body,new Vector3(0,1.29f,-1.01f),new Vector3(.34f,.10f,.30f),paint,.035f);
                foreach(int side in new[]{-1,1})Tube("Rear cowl support",body,new Vector3(side*.13f,1.16f,-.43f),new Vector3(side*.13f,1.26f,-1.09f),.018f,steel);
                var lens=Finish(new Color(.72f,.83f,.87f),.35f,.92f);
                Ball("Headlamp housing",body,new Vector3(0,1.32f,.85f),new Vector3(.32f,.32f,.23f),dark);
                Ball("Headlamp lens",body,new Vector3(0,1.32f,.947f),new Vector3(.28f,.28f,.065f),lens);
                foreach(int side in new[]{-1,1}){Tube("Mirror stalk",body,new Vector3(side*.33f,1.55f,.65f),new Vector3(side*.50f,1.81f,.64f),.012f,steel);Ball("Mirror housing",body,new Vector3(side*.50f,1.83f,.64f),new Vector3(.20f,.10f,.05f),dark);Box("Mirror face",body,new Vector3(side*.50f,1.83f,.61f),new Vector3(.16f,.072f,.012f),alloy,.02f);}
            }
            Box("Rear light",body,new Vector3(0,1.29f,-1.13f),new Vector3(.20f,.055f,.04f),red,.02f);
        }
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);}
    }
}
