using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed partial class GraphicsPlane
    {
        void Wing(Transform root,bool tail)
        {
            float[] span=tail?new[]{-1.76f,-1.55f,-.35f,.35f,1.55f,1.76f}:new[]{-4.7f,-4.35f,-1f,0,1f,4.35f,4.7f};
            var v=new List<Vector3>();var t=new List<int>();var tips=new List<int>();const int sections=40;
            for(int i=0;i<span.Length;i++)
            {
                float chord=tail?(Mathf.Abs(span[i])>1.6f?.52f:.85f):(Mathf.Abs(span[i])>4.5f?.78f:Mathf.Abs(span[i])>4?1.23f:1.58f);
                for(int n=0;n<=sections;n++)
                {
                    float a=n*Mathf.PI*2/sections,along=(1-Mathf.Cos(a))*.5f;
                    float thickness=Mathf.Sin(a)*(tail?.055f:.094f)*(1-along*.45f);
                    v.Add(new Vector3(span[i],(tail?1.51f:1.06f)+Mathf.Abs(span[i])*(tail?.013f:.027f)+thickness,(tail?-2.49f:.45f)+chord*(.5f-along)));
                }
            }
            for(int i=0;i<span.Length-1;i++)for(int n=0;n<sections;n++)
            {int a=i*(sections+1)+n,b=a+sections+1;var list=!tail&&(i==0||i==span.Length-2)?tips:t;list.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            for(int n=1;n<sections-1;n++){int end=(span.Length-1)*(sections+1);var cap=tail?t:tips;cap.AddRange(new[]{0,n+1,n,end,end+n,end+n+1});}
            Mesh(tail?"Airfoil horizontal stabilizer":"Curved airfoil main wing",root,v,t,tail?blue:paint);
            if(tips.Count>0)Mesh("Painted rounded wing tips",root,v,tips,red);
        }
        void Fin(Transform root)
        {
            Vector2[] outline={new Vector2(1.51f,-1.98f),new Vector2(2.52f,-2.42f),new Vector2(2.89f,-2.77f),new Vector2(2.88f,-2.95f),new Vector2(1.51f,-2.98f)};
            var v=new List<Vector3>();var t=new List<int>();foreach(int side in new[]{-1,1})foreach(var p in outline)v.Add(new Vector3(side*.056f,p.x,p.y));
            for(int n=1;n<outline.Length-1;n++)t.AddRange(new[]{0,n,n+1,5,5+n+1,5+n});
            for(int n=0;n<outline.Length;n++){int next=(n+1)%outline.Length;t.AddRange(new[]{n,n+5,next,next,n+5,next+5});}
            Mesh("Swept vertical stabilizer",root,v,t,blue);
            foreach(int side in new[]{-1,1}){Tube("Rudder hinge line",root,new Vector3(side*.061f,1.62f,-2.75f),new Vector3(side*.061f,2.76f,-2.78f),.006f,dark);Tube("Tail identity flash",root,new Vector3(side*.061f,2.32f,-2.39f),new Vector3(side*.061f,2.47f,-2.93f),.023f,red);}
        }
        void GearAndProp(Transform body)
        {
            foreach(var pivot in model.Wheels)
            {
                var w=Part("Detailed aircraft rotating wheel",pivot);float radius=pivot.localPosition.z<0?.20f:.32f,width=radius*.31f;
                var tire=Use(Art.Ring("Aircraft tire",w,radius*.86f,radius*.14f,width,Color.white),rubber);
                foreach(int side in new[]{-1,1})
                {
                    var rim=Use(Art.Ring("Aircraft alloy hub",w,radius*.65f,.023f,.025f,Color.white),alloy);rim.transform.localPosition=new Vector3(side*width*.85f,0,0);
                    Tube("Bearing cap",w,Vector3.zero,new Vector3(side*width,0,0),radius*.19f,alloy);
                    for(int n=0;n<6;n++){float a=n*Mathf.PI/3;Tube("Hub spoke",w,new Vector3(side*width*.7f,0,0),new Vector3(side*width*.7f,Mathf.Sin(a)*radius*.61f,Mathf.Cos(a)*radius*.61f),.014f,alloy);}
                }
                GraphicsCar.Combine(w);
            }
            var prop=Part("Detailed spinning propeller",model.Propeller);
            foreach(int side in new[]{-1,1}){Ball("Tapered composite blade",prop,new Vector3(.025f*side,.63f*side,0),new Vector3(.17f,1.23f,.052f),dark);Ball("Propeller safety tip",prop,new Vector3(.025f*side,1.18f*side,.002f),new Vector3(.115f,.15f,.054f),red);}
            GraphicsCar.Combine(prop);
            Tube("Tailwheel spring",body,new Vector3(0,1.2f,-2.55f),new Vector3(0,.26f,-2.46f),.026f,alloy);
        }
    }
}
