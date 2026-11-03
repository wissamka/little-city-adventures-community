using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public sealed class ExpandedCity : MonoBehaviour
    {
        readonly List<Transform> districts=new List<Transform>();
        readonly List<Transform> aircraft=new List<Transform>();
        Simulation state;float timer;
        public int DistrictCount=>districts.Count;
        public int ActiveDistrictCount {get{int n=0;foreach(var d in districts)if(d.gameObject.activeSelf)n++;return n;}}
        public IReadOnlyList<Transform> Aircraft=>aircraft;
        public static void Build(Transform root,Simulation simulation)
        {
            var host=Art.Group("Greater Little City districts",root);var city=host.gameObject.AddComponent<ExpandedCity>();city.state=simulation;
            Color asphalt=Art.Hex("475D68"),curb=Art.Hex("A4ABA0");
            for(int i=-4;i<4;i++)
            {
                float p=112+i*224;
                Art.Box("Long city avenue",host,new Vector3(p,.008f,0),new Vector3(18,.055f,1570),asphalt,0);
                Art.Box("Long city avenue",host,new Vector3(0,.008f,p),new Vector3(1760,.055f,18),asphalt,0);
                for(int j=-38;j<=38;j++)
                {
                    Art.Box("Avenue lane stripe",host,new Vector3(p,.04f,j*20),new Vector3(.22f,.012f,6),Art.White,0);
                    Art.Box("Avenue lane stripe",host,new Vector3(j*22,.04f,p),new Vector3(6,.012f,.22f),Art.White,0);
                }
            }
            // The original four streets connect directly to the new avenue grid.
            foreach(int s in CityWorld.Streets)
            {
                Art.Box("Old city connector",host,new Vector3(s,.004f,0),new Vector3(12,.055f,238),asphalt,0);
                Art.Box("Old city connector",host,new Vector3(0,.004f,s),new Vector3(238,.055f,12),asphalt,0);
            }
            Art.CombineStatic(host);
            Color[] walls={Art.Hex("899D98"),Art.Hex("AF9981"),Art.Hex("A4A48E"),Art.Hex("879CA8")};
            for(int bx=-3;bx<=3;bx++)for(int bz=-3;bz<=3;bz++)
            {
                if(bz==0 && Mathf.Abs(bx)<=1)continue;
                var district=Art.Group("City district "+bx+", "+bz,host,new Vector3(bx*224,0,bz*224));city.districts.Add(district);
                Art.Box("District sidewalk",district,new Vector3(0,.025f,0),new Vector3(184,.07f,184),curb,0);
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                {
                    float px=x*48,pz=z*48,height=18+Mathf.Abs(bx*7+bz*13+x*3+z)%6*6;
                    // Keep both runway approaches and the sky-route gates clear of buildings.
                    // The expanded district at (224, +/-224) otherwise blocks the gates at 32 m.
                    if(Mathf.Abs(bx*224+px-172)<56 && Mathf.Abs(bz*224+pz)<244)continue;
                    Color color=walls[Mathf.Abs(bx+bz+x+z)%walls.Length];
                    Art.Box("Outer district masonry",district,new Vector3(px,height*.5f,pz),new Vector3(48,height,48),color,.12f);
                    Art.Box("Roof cap",district,new Vector3(px,height+.3f,pz),new Vector3(49,.6f,49),curb,.05f);
                    for(int floor=0;floor<height/6;floor++)foreach(int side in new[]{-1,1})
                    {
                        Art.Box("Recessed window strip",district,new Vector3(px+side*24.03f,3+floor*6,pz),new Vector3(.05f,2.6f,40),Art.Hex("546F7B"),0);
                        Art.Box("Recessed window strip",district,new Vector3(px,3+floor*6,pz+side*24.03f),new Vector3(40,2.6f,.05f),Art.Hex("546F7B"),0);
                    }
                    simulation.Obstacles.Add(new Obstacle(bx*224+px,bz*224+pz,24,24,height));
                    ContactShadows.Create(district,new Vector3(px,.075f,pz),new Vector2(57,57));
                }
                AdventureWorlds.Caption("GREATER LITTLE CITY",district,new Vector3(0,3,-80),180,18);
                Art.CombineStatic(district);
            }
            for(int i=0;i<5;i++)
            {
                var plane=Art.Group("Overhead aircraft "+(i+1),host);plane.gameObject.AddComponent<MovingScenery>();city.aircraft.Add(plane);
                Art.Ball("Airliner fuselage",plane,new Vector3(0,0,0),new Vector3(1.7f,1.55f,12),Art.White);
                Art.Box("Airliner wings",plane,new Vector3(0,-.1f,0),new Vector3(16,.22f,2.7f),walls[i%4],.10f);
                Art.Box("Tail wings",plane,new Vector3(0,.35f,-4.7f),new Vector3(6,.16f,1.6f),Art.Teal,.05f);
                Art.Box("Tail fin",plane,new Vector3(0,1.1f,-4.7f),new Vector3(.20f,2.2f,1.6f),Art.Teal,.08f);
                Art.Ball("Cockpit glass",plane,new Vector3(0,.30f,4.3f),new Vector3(1.25f,.8f,1.7f),Art.Ink);
            }
            city.RefreshVisibility();city.TickAircraft(0);
        }
        public void RefreshVisibility()
        {
            float range=Application.isMobilePlatform?300:390;
            var p=new Vector2(state.Position.X,state.Position.Z);
            foreach(var district in districts)district.gameObject.SetActive(Vector2.Distance(p,new Vector2(district.position.x,district.position.z))<range);
        }
        public void TickAircraft(float time)
        {
            for(int i=0;i<aircraft.Count;i++)
            {
                float a=time*(.030f+i*.004f)+i*Mathf.PI*2/5,radius=180+i*85;
                aircraft[i].localPosition=new Vector3(Mathf.Sin(a)*radius,57+i*8,Mathf.Cos(a)*radius);
                aircraft[i].localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg+90,8);
            }
        }
        void Update(){timer+=Time.unscaledDeltaTime;if(timer>.25f){timer=0;RefreshVisibility();}TickAircraft(Time.time);}
    }
}
