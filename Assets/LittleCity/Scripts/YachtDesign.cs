using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public static class YachtDesign
    {
        public static readonly Color Pearl=Art.Hex("F0F1EC"),Teak=Art.Hex("A99172"),Ink=Art.Hex("20343E"),Linen=Art.Hex("DDD8CB"),Metal=Art.Hex("9AA6AB");
        public static GameObject Panel(string name,Transform p,Vector3 pos,Vector3 size,Color color,float round=.06f)
        {var o=Art.Box(name,p,pos,size,color,round);o.GetComponent<Renderer>().sharedMaterial=Art.Mat(color,0);return o;}
        static void Mesh(string name,Transform p,List<Vector3> v,List<int> t,Color c,int surface=0)
        {var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(p,false);o.GetComponent<MeshFilter>().sharedMesh=m;o.GetComponent<MeshRenderer>().sharedMaterial=Art.Mat(c,surface);o.AddComponent<OwnedMesh>().Mesh=m;}
        static float Beam(float u)
        {
            // A full, usable aft terrace flows into a long fine bow.
            float[] at={0,.06f,.18f,.45f,.64f,.76f,.86f,.94f,1};float[] w={7.7f,9.1f,10,10.1f,9.8f,8.8f,6.7f,3.7f,.08f};
            for(int i=1;i<at.Length;i++)if(u<=at[i])return Mathf.Lerp(w[i-1],w[i],Mathf.SmoothStep(0,1,(u-at[i-1])/(at[i]-at[i-1])));return .08f;
        }
        public static void Exterior(Transform parent,Vector3 position,string name,float length,Color accent)
        {
            var r=Art.Group(name+" superyacht",parent,position);const int sections=80,ring=9;var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<=sections;i++)
            {
                float u=(float)i/sections,w=Beam(u),z=(u-.5f)*length,top=3.45f+Mathf.Pow(Mathf.Max(0,(u-.7f)/.3f),2)*1.1f,keel=-3.2f+Mathf.Pow(Mathf.Abs(u-.45f)*1.7f,3)*2;
                v.AddRange(new[]{new Vector3(-w,top,z),new Vector3(-w*.99f,top-1,z),new Vector3(-w*.86f,.05f,z),new Vector3(-w*.55f,keel+.45f,z),new Vector3(0,keel,z),new Vector3(w*.55f,keel+.45f,z),new Vector3(w*.86f,.05f,z),new Vector3(w*.99f,top-1,z),new Vector3(w,top,z)});
                if(i==sections)continue;for(int j=0;j<ring;j++){int a=i*ring+j,b=i*ring+(j+1)%ring,c=a+ring,d=b+ring;t.AddRange(new[]{a,b,c,b,d,c});}
            }
            for(int j=1;j<ring-1;j++){t.AddRange(new[]{0,j+1,j});int q=sections*ring;t.AddRange(new[]{q,q+j,q+j+1});}
            Mesh("Flowing displacement hull",r,v,t,Pearl);
            var deckV=new List<Vector3>{new Vector3(0,3.48f,0)};var outline=new List<Vector3>();
            for(int side=-1;side<=1;side+=2)for(int i=0;i<=sections;i++){float u=side<0?(float)i/sections:1-(float)i/sections;outline.Add(new Vector3(side*Mathf.Max(.02f,Beam(u)-.28f),3.49f+Mathf.Pow(Mathf.Max(0,(u-.7f)/.3f),2)*1.1f,(u-.5f)*length));}
            deckV.AddRange(outline);var dt=new List<int>();for(int i=0;i<outline.Count;i++)dt.AddRange(new[]{0,i+1,(i+1)%outline.Count+1});Mesh("Fitted teak foredeck",r,deckV,dt,Teak,3);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<sections;i++)
                {
                    float u=(float)i/sections,u2=(float)(i+1)/sections;Vector3 a=new Vector3(side*(Beam(u)-.12f),3.55f+Mathf.Pow(Mathf.Max(0,(u-.7f)/.3f),2)*1.1f,(u-.5f)*length),b=new Vector3(side*(Beam(u2)-.12f),3.55f+Mathf.Pow(Mathf.Max(0,(u2-.7f)/.3f),2)*1.1f,(u2-.5f)*length);
                    Art.Tube("Continuous stainless guardrail",r,a+Vector3.up,b+Vector3.up,.038f,Metal);
                    Art.Tube("Slim hull sheer line",r,a-Vector3.up*.35f,b-Vector3.up*.35f,.055f,Ink);
                    if(i%5==0)Art.Tube("Guardrail stanchion",r,a,a+Vector3.up,.035f,Metal);
                }
                for(int i=0;i<6;i++)Panel("Hull suite glazing",r,new Vector3(side*9.92f,1.72f,-length*.23f+i*4.4f),new Vector3(.055f,.65f,3.3f),Ink,.18f);
            }
            float aft=-length*.5f;
            Panel("Teak swim platform",r,new Vector3(0,.55f,aft-.75f),new Vector3(14,.22f,3),Teak,.2f);
            for(int i=0;i<8;i++)Panel("Terrace access step",r,new Vector3(0,.7f+i*.37f,aft+.1f+i*.4f),new Vector3(4,.18f,.44f),Teak,.035f);
            // Two coherent decks; the main saloon is glass with a thin continuous roof.
            float cabinStart=-length*.28f,cabinEnd=length*.23f;
            Panel("Saloon plinth",r,new Vector3(0,3.8f,(cabinStart+cabinEnd)/2),new Vector3(16,.6f,cabinEnd-cabinStart),Pearl,.16f);
            foreach(int side in new[]{-1,1})
            {
                Art.GlassPane("Continuous saloon glass",r,new Vector3(side*8,4.1f,cabinStart),new Vector3(side*8,4.1f,cabinEnd),new Vector3(side*7.55f,8.45f,cabinEnd-2),new Vector3(side*7.55f,8.45f,cabinStart+1));
                for(float z=cabinStart+2;z<cabinEnd;z+=7)Art.Tube("Fine window mullion",r,new Vector3(side*8,4.1f,z),new Vector3(side*7.55f,8.45f,z),.055f,Ink);
                Art.Tube("Raked saloon pillar",r,new Vector3(side*8,3.9f,cabinEnd),new Vector3(side*7.55f,8.6f,cabinEnd-2),.13f,Pearl);
                Panel("Saloon interior sofa",r,new Vector3(side*5,4.05f,-7),new Vector3(2,.8f,12),Linen,.25f);
            }
            Art.GlassPane("Saloon front windshield",r,new Vector3(-8,4.1f,cabinEnd),new Vector3(8,4.1f,cabinEnd),new Vector3(7.55f,8.45f,cabinEnd-2),new Vector3(-7.55f,8.45f,cabinEnd-2));
            Panel("Floating upper deck edge",r,new Vector3(0,8.7f,-2),new Vector3(17,.28f,length*.68f),Pearl,.13f);
            Panel("Upper terrace teak",r,new Vector3(0,8.86f,-2),new Vector3(16.45f,.035f,length*.675f),Teak,.015f);
            foreach(int side in new[]{-1,1})
            {
                float z0=-2-length*.34f,z1=-2+length*.34f;
                Art.Tube("Upper deck handrail",r,new Vector3(side*8.15f,9.86f,z0),new Vector3(side*8.15f,9.86f,z1),.04f,Metal);
                for(float z=z0;z<z1;z+=4)Art.Tube("Upper rail post",r,new Vector3(side*8.15f,8.88f,z),new Vector3(side*8.15f,9.86f,z),.034f,Metal);
                for(int i=0;i<3;i++){Vector3 p=new Vector3(side*5.3f,9.05f,-length*.25f+i*4);Panel("Low lounge frame",r,p,new Vector3(1.8f,.24f,3.1f),Teak,.10f);Panel("Tailored deck cushion",r,p+Vector3.up*.21f,new Vector3(1.72f,.22f,3),Linen,.10f);}
            }
            float bridgeZ=length*.15f;
            Panel("Bridge lower fascia",r,new Vector3(0,9.22f,bridgeZ),new Vector3(12,.7f,16),Pearl,.15f);
            foreach(int side in new[]{-1,1})Art.GlassPane("Bridge side glazing",r,new Vector3(side*6,9.55f,bridgeZ-8),new Vector3(side*6,9.55f,bridgeZ+8),new Vector3(side*5.5f,12.1f,bridgeZ+6),new Vector3(side*5.5f,12.1f,bridgeZ-8));
            Art.GlassPane("Swept bridge windshield",r,new Vector3(-6,9.55f,bridgeZ+8),new Vector3(6,9.55f,bridgeZ+8),new Vector3(5.5f,12.1f,bridgeZ+6),new Vector3(-5.5f,12.1f,bridgeZ+6));
            Panel("Slim bridge canopy",r,new Vector3(0,12.3f,bridgeZ-1),new Vector3(12.5f,.22f,17),Pearl,.10f);
            Art.Tube("Swept radar arch",r,new Vector3(-4,12.4f,bridgeZ-3),new Vector3(-2,13.95f,bridgeZ-4),.18f,Pearl);Art.Tube("Swept radar arch",r,new Vector3(4,12.4f,bridgeZ-3),new Vector3(2,13.95f,bridgeZ-4),.18f,Pearl);
            Panel("Radar crossbar",r,new Vector3(0,13.95f,bridgeZ-4),new Vector3(4.2f,.22f,1),Pearl,.1f);Art.Ball("Compact radar dome",r,new Vector3(0,14.4f,bridgeZ-4),new Vector3(1.3f,.75f,1.3f),Pearl);
            Panel("Aft terrace dining top",r,new Vector3(0,4.45f,aft+8),new Vector3(5,.2f,3),Teak,.1f);foreach(int side in new[]{-1,1})Panel("Aft bench upholstery",r,new Vector3(side*4,3.9f,aft+8),new Vector3(1.8f,.55f,4),Linen,.18f);
            AdventureWorlds.Caption(name,r,new Vector3(0,2.5f,aft-.05f),180,5);
            Art.CombineStatic(r);r.gameObject.AddComponent<MovingScenery>();
        }
    }
}
