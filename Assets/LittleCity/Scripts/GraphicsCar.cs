using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LittleCity
{
    public sealed class GraphicsCar : MonoBehaviour
    {
        VehicleModel vehicle;Renderer[] low;readonly List<GameObject> high=new List<GameObject>();readonly List<Material> owned=new List<Material>();
        Vector3 baseSeat;Vector3[] baseGrips;
        GameObject concept; readonly List<Transform> conceptWheels=new List<Transform>();
        public bool ShowingHigh {get;private set;}
        public bool ConceptVisible=>concept!=null&&concept.activeSelf;
        public int DetailVertices {get;private set;}
        public static void Attach(VehicleModel model)
        {
            if(model.Mode!=Ride.RaceCar || model.CarStyle==3)return;
            model.Root.gameObject.AddComponent<MovingScenery>();var c=model.Root.gameObject.AddComponent<GraphicsCar>();c.vehicle=model;c.baseSeat=model.Seat;c.baseGrips=(Vector3[])model.Grips.Clone();c.low=model.Root.GetComponentsInChildren<Renderer>(true);
            if(GraphicsQuality.Current!=null)c.SetHigh(GraphicsQuality.Current.High);
        }
        Material Own(Material m){owned.Add(m);return m;}
        public void SetHigh(bool enabled)
        {
            if(high.Count==0)Build();
            if(enabled&&concept==null)BuildConcept();
            foreach(var r in low)if(r!=null)r.enabled=false;
            foreach(var o in high)if(o!=null)o.SetActive(!enabled||concept==null);
            if(concept!=null)concept.SetActive(enabled);
            ShowingHigh=enabled;
            // Imported concept has a central forward driving seat, verified against source mesh bounds.
            bool centerDrive=enabled&&concept!=null;vehicle.Seat=centerDrive?new Vector3(-.005f,.51f,.26f):baseSeat;
            vehicle.DriverScale=centerDrive?.74f:.5f;vehicle.ToyDriverScale=centerDrive?.50f:.40f;vehicle.DriverLean=centerDrive?-14:0;
            for(int i=0;i<2;i++)
            {
                vehicle.Grips[i]=centerDrive?new Vector3(i==0?-.14f:.13f,.918f,.701f):baseGrips[i];
                if(vehicle.Pedals.Count<2)vehicle.Pedals.Add(Art.Group("Driver pedal contact "+i,transform));
                vehicle.Pedals[i].localPosition=centerDrive?new Vector3(i==0?-.0716f:.0613f,i==0?.4403f:.4261f,1.27f):new Vector3(baseSeat.x+(i==0?-.12f:.12f),.465f,.25f);
            }
            GetComponent<CarDamageVisual>()?.Refresh();
        }
        void BuildConcept()
        {
            var prefab=Resources.Load<GameObject>("Concept/Car");if(prefab==null)return;
            concept=Instantiate(prefab,transform,false);concept.name="Near-realistic concept sports car";Art.Box("Center driver cushion",concept.transform,new Vector3(-.005f,.405f,.26f),new Vector3(.40f,.12f,.48f),Art.Hex("34353B"),.06f);
            foreach(Transform child in concept.transform)if(child.name.StartsWith("Wheel"))conceptWheels.Add(child);
            foreach(var renderer in concept.GetComponentsInChildren<Renderer>())
            {
                var m=renderer.sharedMaterial;if(m==null||!renderer.name.Contains("|Paint"))continue;
                var paint=Own(new Material(m));
                paint.color=vehicle.CarStyle==1?Art.Hex("CF9D26"):vehicle.CarStyle==2?Art.Hex("266C4A"):Art.Hex("A9101D");
                renderer.sharedMaterial=paint;
            }
        }
        public void AnimateConcept(float speed,float dt)
        {foreach(var wheel in conceptWheels)wheel.Rotate(Vector3.right,speed*dt/.47f*Mathf.Rad2Deg,Space.Self);}
        GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material material,float round=.025f)
        {var o=Art.Box(name,parent,p,size,Color.white,round);o.GetComponent<Renderer>().sharedMaterial=material;return o;}
        void Tube(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
        {var o=Art.Tube(name,parent,a,b,radius,Color.white);o.GetComponent<Renderer>().sharedMaterial=material;}
        void Pane(string name,Transform parent,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material material)
        {MeshObject(name,parent,new[]{a,b,c,d},new[]{0,2,1,0,3,2},material);}
        static GameObject MeshObject(string name,Transform parent,Vector3[] v,int[] t,Material material)
        {
            var mesh=new Mesh{name=name};mesh.vertices=v;mesh.triangles=t;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=material;o.AddComponent<OwnedMesh>().Mesh=mesh;return o;
        }
        void Build()
        {
            var q=GraphicsQuality.Current;if(q==null)return;
            Color color=vehicle.CarStyle==0?Art.Hex("B90C18"):vehicle.CarStyle==1?Art.Hex("D9A817"):Art.Hex("388E35");
            var paint=Own(q.Finish(color,.63f,.91f,7));var carbon=Own(q.Finish(new Color(.025f,.031f,.038f),.25f,.45f));
            var rubber=Own(q.Finish(new Color(.038f,.04f,.042f),.02f,.22f,8));var chrome=Own(q.Finish(new Color(.62f,.67f,.72f),.96f,.87f));
            var red=Own(q.Finish(new Color(.88f,.025f,.012f),.25f,.9f));var lamp=Own(q.Finish(new Color(.82f,.94f,1),.3f,.95f));var window=Own(q.Window());
            var root=Art.Group("High Def sculpted sports car",transform);high.Add(root.gameObject);
            // A lofted shell forms shaped front/rear haunches, tapered nose, shoulders and hood.
            float[] zs={-2.12f,-1.92f,-1.55f,-1.23f,-.92f,-.45f,0,.65f,1.08f,1.3f,1.62f,1.98f,2.14f};
            float[] widths={.91f,1.12f,1.18f,1.2f,1.12f,1.05f,1.02f,1.06f,1.15f,1.18f,1.13f,1.02f,.91f};
            float[] tops={.78f,.97f,1.02f,1.1f,1.01f,.96f,.94f,.94f,1.04f,1.1f,.99f,.81f,.66f};
            var v=new List<Vector3>();var t=new List<int>();const int points=12;
            for(int i=0;i<zs.Length;i++)for(int k=0;k<points;k++)
            {
                float[] xs={0,.82f,1,1,.91f,.45f,0,-.45f,-.91f,-1,-1,-.82f};
                float[] ys={tops[i]-.06f,tops[i]+.02f,tops[i]-.06f,.58f,.40f,.36f,.36f,.36f,.40f,.58f,tops[i]-.06f,tops[i]+.02f};
                v.Add(new Vector3(xs[k]*widths[i],ys[k],zs[i]));
            }
            for(int i=0;i<zs.Length-1;i++)for(int k=0;k<points;k++){if(i>=3&&i<7&&(k==0||k==11))continue;int a=i*points+k,b=i*points+(k+1)%points,c=a+points,d=b+points;t.AddRange(new[]{a,c,b,b,c,d});}
            for(int k=1;k<points-1;k++){t.AddRange(new[]{0,k,k+1});int last=(zs.Length-1)*points;t.AddRange(new[]{last,last+k+1,last+k});}
            MeshObject("Continuous aerodynamic body panels",root,v.ToArray(),t.ToArray(),paint);
            // Cabin roof is a separate gently crowned panel with sloping front and rear glass.
            float roofZ=vehicle.CarStyle==1?-.35f:-.30f;
            Box("Crowned roof",root,new Vector3(0,1.55f,roofZ),new Vector3(1.5f,.095f,1.17f),vehicle.CarStyle==1?carbon:paint,.06f);
            Pane("Sloped windshield",root,new Vector3(-.87f,.98f,.8f),new Vector3(.87f,.98f,.8f),new Vector3(.72f,1.54f,.27f),new Vector3(-.72f,1.54f,.27f),window);
            Pane("Fastback rear glass",root,new Vector3(-.85f,1,-1.25f),new Vector3(-.73f,1.52f,-.90f),new Vector3(.73f,1.52f,-.90f),new Vector3(.85f,1,-1.25f),window);
            Box("Cockpit floor",root,new Vector3(0,.43f,-.2f),new Vector3(1.65f,.10f,1.9f),carbon);
            var steering=Art.Group("Cockpit steering wheel",root,new Vector3(-.43f,1.05f,.05f));steering.localRotation=Quaternion.Euler(0,90,0);
            var steeringRim=Art.Ring("Cockpit steering rim",steering,.16f,.018f,.018f,Art.Ink);steeringRim.GetComponent<Renderer>().sharedMaterial=carbon;
            Tube("Steering crossbar",steering,new Vector3(0,-.14f,0),new Vector3(0,.14f,0),.012f,chrome);
            for(int side=-1;side<=1;side+=2)Box("Driving pedal",root,new Vector3(-.43f+side*.12f,.485f,.25f),new Vector3(.12f,.03f,.2f),chrome);
            Box("Cockpit dashboard",root,new Vector3(0,.93f,.55f),new Vector3(1.55f,.12f,.30f),carbon);
            for(int side=-1;side<=1;side+=2)
            {
                Pane("Deep side glass",root,new Vector3(side*.88f,.98f,.7f),new Vector3(side*.86f,1,-1.16f),new Vector3(side*.74f,1.5f,-.89f),new Vector3(side*.73f,1.51f,.26f),window);
                Tube("Window A pillar",root,new Vector3(side*.88f,.98f,.81f),new Vector3(side*.74f,1.56f,.27f),.036f,paint);
                Tube("Fastback pillar",root,new Vector3(side*.86f,1,-1.26f),new Vector3(side*.74f,1.56f,-.90f),.043f,paint);
                Box("Sculpted lower sill",root,new Vector3(side*1.09f,.43f,-.13f),new Vector3(.14f,.12f,1.58f),carbon);
                Tube("Door shut line",root,new Vector3(side*1.071f,.50f,-.71f),new Vector3(side*1.076f,.92f,-.78f),.009f,carbon);
                Box("Flush handle",root,new Vector3(side*1.091f,.91f,-.52f),new Vector3(.025f,.035f,.18f),chrome,.01f);
                var mirror=Art.Ball("Sculpted side mirror",root,new Vector3(side*1.12f,1.19f,.45f),new Vector3(.27f,.14f,.27f),Color.white);mirror.GetComponent<Renderer>().sharedMaterial=paint;
                Box("Mirror glass",root,new Vector3(side*1.13f,1.19f,.32f),new Vector3(.20f,.085f,.02f),chrome);
                Box("Bucket seat",root,new Vector3(side*.43f,.69f,-.23f),new Vector3(.62f,.18f,.69f),carbon,.08f);
                Box("Bucket back",root,new Vector3(side*.43f,.98f,-.60f),new Vector3(.6f,.7f,.19f),carbon,.07f);
                Box("Leather headrest",root,new Vector3(side*.43f,1.28f,-.6f),new Vector3(.34f,.23f,.15f),carbon,.055f);
                var head=Box("Angular headlight housing",root,new Vector3(side*.78f,.83f,1.87f),new Vector3(.51f,.12f,.27f),carbon,.035f);head.transform.localRotation=Quaternion.Euler(0,side*-18,0);
                for(int n=0;n<3;n++)Box("LED headlight optic",root,new Vector3(side*(.65f+n*.115f),.854f,2.01f-n*.028f),new Vector3(.073f,.041f,.045f),lamp,.018f);
                Tube("Front LED signature",root,new Vector3(side*.57f,.89f,1.94f),new Vector3(side*.97f,.89f,1.80f),.018f,lamp);
                Box("Rear lamp black inset",root,new Vector3(side*.70f,.84f,-2.01f),new Vector3(.71f,.19f,.12f),carbon,.035f);
                for(int n=0;n<2;n++)Box("Red rear light segment",root,new Vector3(side*(.47f+n*.30f),.86f,-2.075f),new Vector3(.24f,.095f,.05f),red,.025f);
                Box("Rear wing upright",root,new Vector3(side*.77f,1.19f,-1.83f),new Vector3(.065f,.30f,.17f),carbon);
                for(int vent=0;vent<4;vent++)Box("Side heat extractor",root,new Vector3(side*1.12f,.83f,.66f+vent*.055f),new Vector3(.027f,.14f,.022f),carbon,.008f);
                foreach(float z in new[]{-1.27f,1.26f}){Art.WheelArch(root,new Vector3(side*1.105f,.48f,z),Color.white);root.GetChild(root.childCount-1).GetComponent<Renderer>().sharedMaterial=paint;}
            }
            Box("Rear carbon wing",root,new Vector3(0,1.36f,-1.86f),new Vector3(2.28f,.075f,.29f),carbon,.045f);
            Box("Rear center brake strip",root,new Vector3(0,1.40f,-1.99f),new Vector3(.72f,.025f,.023f),red,.01f);
            Box("Front splitter",root,new Vector3(0,.39f,1.98f),new Vector3(2.04f,.075f,.43f),carbon);
            Box("Deep front intake",root,new Vector3(0,.61f,2.105f),new Vector3(1.26f,.22f,.07f),carbon);
            Box("Rear diffuser",root,new Vector3(0,.44f,-2.04f),new Vector3(1.93f,.22f,.19f),carbon);
            for(int fin=-3;fin<=3;fin++)Box("Diffuser fin",root,new Vector3(fin*.23f,.35f,-2.03f),new Vector3(.035f,.20f,.36f),carbon,.008f);
            for(int tip=-1;tip<=1;tip+=2){Tube("Polished exhaust",root,new Vector3(tip*.3f,.47f,-2),new Vector3(tip*.3f,.47f,-2.19f),.095f,chrome);Tube("Exhaust opening",root,new Vector3(tip*.3f,.47f,-2.188f),new Vector3(tip*.3f,.47f,-2.202f),.072f,carbon);}
            Box("Rear registration plate",root,new Vector3(0,.73f,-2.10f),new Vector3(.40f,.11f,.02f),chrome);
            foreach(var wheel in vehicle.Wheels)
            {
                var w=Art.Group("High Def wheel tire brake and rim",wheel);high.Add(w.gameObject);
                var tire=Art.Ring("Performance tire",w,.405f,.072f,.18f,Color.white);tire.GetComponent<Renderer>().sharedMaterial=rubber;
                foreach(int side in new[]{-1,1})
                {
                    var rim=Art.Ring("Forged alloy outer rim",w,.343f,.022f,.020f,Color.white);rim.transform.localPosition=new Vector3(side*.176f,0,0);rim.GetComponent<Renderer>().sharedMaterial=chrome;
                    var disc=Art.Disk("Brake rotor",w,new Vector3(side*.146f,0,0),.29f,.02f,Color.white);disc.transform.localRotation=Quaternion.Euler(0,0,90);disc.GetComponent<Renderer>().sharedMaterial=chrome;
                    for(int n=0;n<10;n++){float a=n*Mathf.PI/5;Tube("Split alloy spoke",w,new Vector3(side*.19f,Mathf.Sin(a)*.07f,Mathf.Cos(a)*.07f),new Vector3(side*.18f,Mathf.Sin(a+.12f)*.34f,Mathf.Cos(a+.12f)*.34f),.019f,chrome);}
                }
                for(int n=0;n<40;n++){float a=n*Mathf.PI/20;var block=Box("Fine tire tread",w,new Vector3(0,Mathf.Sin(a)*.474f,Mathf.Cos(a)*.474f),new Vector3(.27f,.004f,.009f),rubber,.001f);block.transform.localRotation=Quaternion.Euler(-a*Mathf.Rad2Deg,0,0);}
                Combine(w);
            }
            Combine(root);
            foreach(var o in high)foreach(var f in o.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null)DetailVertices+=f.sharedMesh.vertexCount;
        }
        public static void Combine(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();var originals=new List<MeshRenderer>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<MeshRenderer>();if(r==null || f.sharedMesh==null)continue;var m=r.sharedMaterial;if(!groups.ContainsKey(m))groups[m]=new List<CombineInstance>();groups[m].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});originals.Add(r);
            }
            foreach(var entry in groups){var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name="High Def detail batch"};mesh.CombineMeshes(entry.Value.ToArray());var o=new GameObject("High Def material batch",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(root,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=entry.Key;o.AddComponent<OwnedMesh>().Mesh=mesh;}
            foreach(var r in originals)r.enabled=false;
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
