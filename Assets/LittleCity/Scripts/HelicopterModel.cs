using UnityEngine;
namespace LittleCity
{
    public static partial class Models
    {
        static void Helicopter(VehicleModel m)
        {
            var r=Art.Group("Helicopter cabin and tail",m.Root);Color paint=Art.Hex("D6A335"),ink=Art.Hex("23353D"),metal=Art.Hex("B7C6CA");
            Art.Ball("Streamlined engine cabin",r,new Vector3(0,1.8f,-1.27f),new Vector3(2.4f,2.1f,2.0f),paint);
            Art.Box("Cockpit lower shell",r,new Vector3(0,1.07f,.65f),new Vector3(2.3f,.46f,2.25f),paint,.22f);
            Art.Box("Cockpit interior floor",r,new Vector3(0,1.30f,.50f),new Vector3(2.08f,.08f,1.96f),ink,.035f);
            // The cockpit is open geometry, closed by transparent curved glazing.
            const int arcs=10;
            for(int i=0;i<arcs;i++)
            {
                float a=-Mathf.PI/2+i*Mathf.PI/arcs,b=-Mathf.PI/2+(i+1)*Mathf.PI/arcs;
                Vector3 a0=new Vector3(Mathf.Sin(a)*1.15f,1.31f,.45f+Mathf.Cos(a)*1.52f),b0=new Vector3(Mathf.Sin(b)*1.15f,1.31f,.45f+Mathf.Cos(b)*1.52f);
                Vector3 a1=new Vector3(Mathf.Sin(a)*1.04f,2.65f,.32f+Mathf.Cos(a)*1.20f),b1=new Vector3(Mathf.Sin(b)*1.04f,2.65f,.32f+Mathf.Cos(b)*1.20f);
                Art.GlassPane("Clear curved cockpit glazing",r,a0,b0,b1,a1);
                Vector3 a2=new Vector3(Mathf.Sin(a)*.20f,2.92f,.1f+Mathf.Cos(a)*.28f),b2=new Vector3(Mathf.Sin(b)*.20f,2.92f,.1f+Mathf.Cos(b)*.28f);
                Art.GlassPane("Clear upper cockpit dome",r,a1,b1,b2,a2);
                if(i==0||i==5)Art.Tube("Cockpit windshield frame",r,a0,a1,.035f,ink);
                Art.Tube("Upper canopy frame",r,a1,b1,.03f,ink);
            }
            Art.GlassPane("Upper rear cockpit glazing",r,new Vector3(-1.04f,2.65f,-.18f),new Vector3(1.04f,2.65f,-.18f),new Vector3(.20f,2.92f,.10f),new Vector3(-.20f,2.92f,.10f));
            foreach(int side in new[]{-1,1})
            {
                CamperFurnishings.Seat(r,new Vector3(side*.48f,1.45f,.27f),ink);
                Art.Tube("Landing skid",r,new Vector3(side*1.18f,.08f,-1.7f),new Vector3(side*1.18f,.08f,1.7f),.075f,metal);
                Art.Tube("Upturned skid toe",r,new Vector3(side*1.18f,.08f,1.7f),new Vector3(side*1.18f,.27f,2.05f),.075f,metal);
                foreach(float z in new[]{-.9f,.9f})Art.Tube("Landing skid strut",r,new Vector3(side*.70f,1,z),new Vector3(side*1.18f,.08f,z),.065f,metal);
                Art.Box("Navigation lamp",r,new Vector3(side*1.26f,1.65f,-.5f),new Vector3(.12f,.10f,.10f),side<0?Art.Red:Art.Teal,.035f);
            }
            Art.Tube("Tapered tail boom",r,new Vector3(0,1.87f,-1.45f),new Vector3(0,2.65f,-5.4f),.20f,paint);
            Art.Box("Horizontal tail stabilizer",r,new Vector3(0,2.32f,-4.40f),new Vector3(2.35f,.11f,.56f),paint,.07f);
            var fin=Art.Box("Vertical tail fin",r,new Vector3(0,3.01f,-5.28f),new Vector3(.13f,1.35f,.81f),paint,.10f);fin.transform.localRotation=Quaternion.Euler(-12,0,0);
            Art.Box("Dark tail cap",r,new Vector3(0,3.62f,-5.4f),new Vector3(.15f,.12f,.63f),ink,.04f);
            Art.Ball("Rotor transmission fairing",r,new Vector3(0,2.9f,-.52f),new Vector3(1.15f,.65f,1.36f),paint);
            Art.Tube("Main rotor mast",r,new Vector3(0,3.1f,-.48f),new Vector3(0,3.72f,-.48f),.09f,metal);
            Art.Box("Flight instrument dashboard",r,new Vector3(0,1.56f,1.08f),new Vector3(1.65f,.25f,.38f),ink,.07f);
            m.Seat=new Vector3(-.48f,1.59f,.27f);m.Grips[0]=m.Seat+new Vector3(-.20f,.33f,.39f);m.Grips[1]=m.Seat+new Vector3(.20f,.33f,.39f);
            foreach(int side in new[]{-1,1})
            {
                m.Pedals.Add(Art.Group("Helicopter foot pedal",m.Root,m.Seat+new Vector3(side*.14f,-.45f,.43f)));
                Art.Tube("Cyclic hand control",r,m.Seat+new Vector3(side*.20f,-.10f,.39f),m.Seat+new Vector3(side*.20f,.33f,.39f),.025f,ink);
            }
            GraphicsCar.Combine(r);
            var main=Art.Group("Animated main rotor",m.Root,new Vector3(0,3.73f,-.48f));
            foreach(int angle in new[]{0,90,180,270})
            {
                var blade=Art.Group("Rotor blade",main);blade.localRotation=Quaternion.Euler(0,angle,0);
                Art.Box("Carbon rotor blade",blade,new Vector3(2.75f,0,0),new Vector3(4.35f,.055f,.26f),ink,.025f);
                Art.Box("Yellow blade tip",blade,new Vector3(4.88f,.002f,0),new Vector3(.26f,.06f,.27f),Art.Yellow,.02f);
            }
            Art.Ball("Main rotor hub",main,Vector3.zero,new Vector3(.46f,.22f,.46f),metal);GraphicsCar.Combine(main);
            var tail=Art.Group("Animated tail rotor",m.Root,new Vector3(.30f,2.80f,-5.3f));
            foreach(int angle in new[]{0,120,240})
            {
                var blade=Art.Group("Tail rotor blade",tail);blade.localRotation=Quaternion.Euler(angle,0,0);
                Art.Box("Tail rotor foil",blade,new Vector3(0,.45f,0),new Vector3(.05f,.84f,.13f),ink,.015f);
                Art.Box("Tail rotor tip",blade,new Vector3(0,.84f,0),new Vector3(.06f,.14f,.14f),Art.Yellow,.01f);
            }
            GraphicsCar.Combine(tail);
            var details=Art.Group("High Def helicopter details",m.Root);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<5;i++)Art.Box("Engine cooling vent",details,new Vector3(side*1.12f,2.13f,-.65f-i*.16f),new Vector3(.06f,.27f,.07f),ink,.012f);
                Art.Tube("Rotor pitch link",details,new Vector3(side*.30f,3.18f,-.50f),new Vector3(side*.48f,3.68f,-.50f),.019f,metal);
                Art.Box("Cabin step",details,new Vector3(side*1.25f,.70f,.30f),new Vector3(.23f,.08f,.68f),metal,.025f);
                Art.Box("Door handle",details,new Vector3(side*1.10f,1.78f,-.05f),new Vector3(.055f,.05f,.20f),metal,.01f);
            }
            for(int i=0;i<3;i++)Art.Box("Flight display",details,new Vector3(-.50f+i*.45f,1.70f,1.07f),new Vector3(.31f,.03f,.22f),Art.Teal,.015f);
            Art.Tube("Tail antenna",details,new Vector3(0,3,-4.4f),new Vector3(0,3.8f,-4.7f),.012f,ink);GraphicsCar.Combine(details);
            var rig=m.Root.gameObject.AddComponent<HelicopterRig>();rig.MainRotor=main;rig.TailRotor=tail;rig.Details=details.gameObject;rig.SetHigh(GraphicsQuality.Current!=null&&GraphicsQuality.Current.High);
        }
    }
    public sealed class HelicopterRig:MonoBehaviour
    {
        public Transform MainRotor,TailRotor;public GameObject Details;public bool ShowingHigh{get;private set;}
        public void SetHigh(bool high){ShowingHigh=high;Details.SetActive(high);}
        public void Animate(float dt,float throttle){MainRotor.Rotate(Vector3.up,dt*(180+throttle*1700),Space.Self);TailRotor.Rotate(Vector3.right,dt*(420+throttle*2600),Space.Self);}
    }
}
