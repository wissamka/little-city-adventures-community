using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class GraphicsOffroad:MonoBehaviour
    {
        VehicleModel model;Renderer[] low;bool built;
        readonly List<GameObject> parts=new List<GameObject>();readonly List<Material> owned=new List<Material>();
        public bool ShowingHigh{get;private set;}
        public static void Attach(VehicleModel m)
        {
            if(m.Mode!=Ride.RaceCar||m.CarStyle!=3)return;
            var g=m.Root.gameObject.AddComponent<GraphicsOffroad>();g.model=m;g.low=m.Root.GetComponentsInChildren<Renderer>(true);
            if(GraphicsQuality.Current!=null)g.SetHigh(GraphicsQuality.Current.High);
        }
        public void SetHigh(bool high)
        {
            if(high&&!built)Build();ShowingHigh=high&&built;
            foreach(var r in low)if(r!=null)r.enabled=!ShowingHigh;
            foreach(var p in parts)p.SetActive(ShowingHigh);
        }
        Material Finish(string color,float metal,float gloss,int surface=0){var m=GraphicsQuality.Current.Finish(Art.Hex(color),metal,gloss,surface);owned.Add(m);return m;}
        GameObject Use(GameObject o,Material m){o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        GameObject Box(string name,Transform r,Vector3 p,Vector3 size,Material m,float round=.02f)=>Use(Art.Box(name,r,p,size,Color.white,round),m);
        void Tube(string name,Transform r,Vector3 a,Vector3 b,float radius,Material m)=>Use(Art.Tube(name,r,a,b,radius,Color.white),m);
        void Ball(string name,Transform r,Vector3 p,Vector3 size,Material m)=>Use(Art.Ball(name,r,p,size,Color.white),m);
        void Ring(string name,Transform r,Vector3 p,float radius,float wall,float width,Material m){var o=Use(Art.Ring(name,r,radius,wall,width,Color.white),m);o.transform.localPosition=p;}
        Transform Part(string name,Transform parent){var r=Art.Group(name,parent);parts.Add(r.gameObject);return r;}
        void Pane(string name,Transform r,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material m)
        {
            var mesh=new Mesh{name=name};mesh.vertices=new[]{a,b,c,d};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(r,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=m;o.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        void Build()
        {
            if(GraphicsQuality.Current==null)return;
            var paint=Finish("AAA180",.43f,.68f,7);var trim=Finish("30372B",.16f,.32f);var rubber=Finish("171B1D",0,.19f,8);var alloy=Finish("777E82",.88f,.62f);var black=Finish("23282A",.2f,.38f);var leather=Finish("574632",0,.29f);var red=Finish("9F211C",.26f,.64f);var lamp=Finish("C9D9DC",.32f,.9f);
            var glass=GraphicsQuality.Current.Window();owned.Add(glass);
            var r=Part("Detailed safari body and cabin",transform);
            Box("Ladder chassis",r,new Vector3(0,.65f,0),new Vector3(1.9f,.22f,4.35f),black,.04f);
            Box("Pressed hood",r,new Vector3(0,1.43f,1.39f),new Vector3(1.95f,.16f,1.67f),paint,.07f);
            Box("Engine compartment",r,new Vector3(0,1.11f,1.45f),new Vector3(1.89f,.57f,1.58f),paint,.08f);
            Box("Rear cargo tub",r,new Vector3(0,1.08f,-1.68f),new Vector3(1.94f,.37f,.88f),paint,.04f);
            Box("Cabin floor",r,new Vector3(0,.94f,-.6f),new Vector3(1.9f,.14f,2.35f),rubber);
            Box("Grille surround",r,new Vector3(0,1.19f,2.29f),new Vector3(1.85f,.66f,.12f),paint,.07f);
            Box("Recessed radiator",r,new Vector3(0,1.16f,2.36f),new Vector3(.98f,.44f,.055f),black);
            for(int n=-3;n<=3;n++)Box("Grille vertical bar",r,new Vector3(n*.145f,1.16f,2.393f),new Vector3(.054f,.44f,.03f),paint,.02f);
            Box("Front bumper",r,new Vector3(0,.74f,2.43f),new Vector3(2.14f,.17f,.25f),trim,.035f);
            Box("Rear bumper",r,new Vector3(0,.74f,-2.23f),new Vector3(2.08f,.16f,.20f),trim);
            Box("Winch housing",r,new Vector3(0,.91f,2.43f),new Vector3(.58f,.21f,.24f),black,.04f);
            Tube("Winch cable drum",r,new Vector3(-.20f,.94f,2.56f),new Vector3(.20f,.94f,2.56f),.045f,alloy);
            Box("Dashboard",r,new Vector3(0,1.47f,.35f),new Vector3(1.74f,.26f,.20f),black,.04f);
            foreach(int side in new[]{-1,1})
            {
                Box("Door outer panel",r,new Vector3(side*.98f,1.20f,-.53f),new Vector3(.11f,.48f,1.54f),paint,.035f);
                Box("Door accent stripe",r,new Vector3(side*1.041f,1.29f,-.53f),new Vector3(.015f,.12f,1.49f),red,.005f);
                Box("Recessed door handle",r,new Vector3(side*1.056f,1.39f,-.99f),new Vector3(.025f,.07f,.19f),black,.02f);
                foreach(float z in new[]{-.05f,.16f})Box("External door hinge",r,new Vector3(side*1.057f,1.1f,z),new Vector3(.035f,.11f,.07f),alloy,.009f);
                Tube("Rock rail",r,new Vector3(side*1.08f,.73f,-1.07f),new Vector3(side*1.08f,.73f,.74f),.052f,trim);
                Box("Step tread",r,new Vector3(side*1.14f,.72f,-.20f),new Vector3(.30f,.045f,.66f),rubber,.015f);
                Box("Upholstered seat cushion",r,new Vector3(side*.45f,1.12f,-.70f),new Vector3(.66f,.19f,.73f),leather,.085f);
                Box("Contoured seat back",r,new Vector3(side*.45f,1.5f,-1.05f),new Vector3(.65f,.73f,.18f),leather,.075f);
                Box("Seat headrest",r,new Vector3(side*.45f,1.94f,-1.05f),new Vector3(.33f,.20f,.13f),leather,.05f);
                for(int seam=-1;seam<=1;seam++)Tube("Seat seam",r,new Vector3(side*.45f+seam*.13f,1.20f,-.4f),new Vector3(side*.45f+seam*.13f,1.20f,-.93f),.005f,trim);
                Tube("Rear roll bar",r,new Vector3(side*.89f,1,-1.55f),new Vector3(side*.89f,2.35f,-1.55f),.045f,trim);
                Tube("Upper roll bar",r,new Vector3(side*.89f,2.35f,-1.55f),new Vector3(side*.89f,2.35f,.40f),.045f,trim);
                Tube("Windshield pillar",r,new Vector3(side*.90f,1.49f,.64f),new Vector3(side*.87f,2.34f,.40f),.043f,paint);
                Tube("Mirror bracket",r,new Vector3(side*.97f,1.58f,.31f),new Vector3(side*1.18f,1.76f,.35f),.025f,black);
                Box("Mirror housing",r,new Vector3(side*1.21f,1.79f,.35f),new Vector3(.20f,.22f,.11f),black,.04f);
                Box("Mirror glass",r,new Vector3(side*1.21f,1.79f,.288f),new Vector3(.16f,.18f,.012f),alloy,.025f);
                Ball("Recessed headlight",r,new Vector3(side*.70f,1.24f,2.36f),new Vector3(.38f,.38f,.12f),black);
                Ball("Headlight lens",r,new Vector3(side*.70f,1.24f,2.413f),new Vector3(.30f,.30f,.045f),lamp);
                Box("Rear lamp housing",r,new Vector3(side*.76f,1.20f,-2.13f),new Vector3(.20f,.28f,.08f),black);
                Box("Rear brake lens",r,new Vector3(side*.76f,1.25f,-2.18f),new Vector3(.15f,.16f,.035f),red);
                foreach(int end in new[]{-1,1})
                {
                    float z=end*1.43f;
                    Box("Flared wheel arch top",r,new Vector3(side*1.08f,1.31f,z),new Vector3(.54f,.10f,1.22f),trim,.045f);
                    foreach(int edge in new[]{-1,1})Box("Wheel arch end",r,new Vector3(side*1.08f,1.15f,z+edge*.56f),new Vector3(.54f,.31f,.14f),trim,.04f);
                    Tube("Visible axle",r,new Vector3(0,.62f,z),new Vector3(side*1.1f,.62f,z),.075f,black);
                    for(int leaf=0;leaf<3;leaf++)Tube("Leaf spring",r,new Vector3(side*.71f,.55f+leaf*.027f,z-.44f),new Vector3(side*.71f,.55f+leaf*.027f,z+.44f),.012f,alloy);
                }
            }
            Pane("Clear windshield",r,new Vector3(-.86f,1.51f,.63f),new Vector3(.86f,1.51f,.63f),new Vector3(.84f,2.31f,.40f),new Vector3(-.84f,2.31f,.40f),glass);
            Tube("Windshield top",r,new Vector3(-.88f,2.35f,.39f),new Vector3(.88f,2.35f,.39f),.035f,paint);
            Tube("Roll bar cross brace",r,new Vector3(-.89f,2.35f,-1.55f),new Vector3(.89f,2.35f,-1.55f),.04f,trim);
            foreach(float x in new[]{-.43f,.38f})Tube("Windshield wiper",r,new Vector3(x,1.52f,.645f),new Vector3(x-.22f,1.81f,.569f),.012f,black);
            Tube("Steering column",r,new Vector3(-.43f,1.45f,.31f),new Vector3(-.43f,1.52f,-.38f),.024f,black);
            var steering=Art.Group("Steering wheel",r,new Vector3(-.43f,1.52f,-.38f));steering.localRotation=Quaternion.Euler(0,90,0);
            Ring("Steering rim",steering,Vector3.zero,.16f,.018f,.018f,rubber);for(int n=0;n<3;n++){float a=n*Mathf.PI*2/3;Tube("Steering spoke",steering,Vector3.zero,new Vector3(0,Mathf.Sin(a)*.15f,Mathf.Cos(a)*.15f),.012f,alloy);}
            foreach(var wheel in model.Wheels)
            {
                var w=Part("Detailed offroad rotating wheel",wheel);
                Ring("All terrain tire",w,Vector3.zero,.53f,.09f,.19f,rubber);
                foreach(int side in new[]{-1,1})
                {
                    Ring("Alloy wheel lip",w,new Vector3(side*.19f,0,0),.395f,.027f,.022f,alloy);
                    Tube("Wheel hub",w,Vector3.zero,new Vector3(side*.215f,0,0),.077f,black);
                    for(int n=0;n<6;n++){float a=n*Mathf.PI/3;Tube("Forged wheel spoke",w,new Vector3(side*.19f,Mathf.Sin(a)*.08f,Mathf.Cos(a)*.08f),new Vector3(side*.18f,Mathf.Sin(a+.1f)*.38f,Mathf.Cos(a+.1f)*.38f),.03f,alloy);Ball("Wheel bolt",w,new Vector3(side*.222f,Mathf.Sin(a)*.065f,Mathf.Cos(a)*.065f),Vector3.one*.022f,alloy);}
                }
                for(int n=0;n<48;n++)for(int row=-1;row<=1;row++)
                {float a=n*Mathf.PI/24+row*.018f;var block=Box("All terrain tread block",w,new Vector3(row*.11f,Mathf.Sin(a)*.603f,Mathf.Cos(a)*.603f),new Vector3(.10f,.055f,.033f),rubber,.005f);block.transform.localRotation=Quaternion.Euler(-a*Mathf.Rad2Deg,0,0);}
                GraphicsCar.Combine(w);
            }
            var spare=Art.Group("Rear spare wheel",r,new Vector3(.28f,1.34f,-2.3f));spare.localRotation=Quaternion.Euler(0,90,0);
            Ring("Spare all terrain tire",spare,Vector3.zero,.52f,.09f,.15f,rubber);Ring("Spare alloy rim",spare,new Vector3(.15f,0,0),.39f,.024f,.02f,alloy);
            Box("Expedition crate",r,new Vector3(-.53f,1.24f,-1.85f),new Vector3(.59f,.59f,.53f),trim,.035f);
            GraphicsCar.Combine(r);built=true;
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
