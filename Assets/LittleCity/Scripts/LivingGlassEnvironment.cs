using UnityEngine;

namespace LittleCity
{
    // A shared HDR look-development environment adds broad window/softbox
    // reflections. The scene's ordinary reflection probes still contribute.
    public static class LivingGlassEnvironment
    {
        static Cubemap texture;
        public static Cubemap Texture
        {
            get
            {
                if(texture!=null)return texture;
                const int size=128;
                texture=new Cubemap(size,TextureFormat.RGBAHalf,true){name="Living glass window reflections",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
                for(int face=0;face<6;face++)
                {
                    var pixels=new Color[size*size];
                    for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                    {
                        float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;
                        Vector3 d=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);d.Normalize();
                        Color c=d.y>0?Color.Lerp(new Color(.32f,.38f,.44f),new Color(.13f,.22f,.36f),d.y):Color.Lerp(new Color(.19f,.16f,.12f),new Color(.07f,.09f,.11f),-d.y);
                        c+=new Color(1,.96f,.87f)*Panel(d,new Vector3(.54f,.18f,.82f),.10f,.58f)*7;
                        c+=new Color(.82f,.92f,1)*Panel(d,new Vector3(-.65f,.3f,.7f),.065f,.48f)*5;
                        c+=new Color(1,.88f,.65f)*Panel(d,new Vector3(-.2f,.22f,-1),.16f,.5f)*3;
                        pixels[y*size+x]=c;
                    }
                    texture.SetPixels(pixels,(CubemapFace)face);
                }
                texture.Apply(true,false);return texture;
            }
        }
        static float Panel(Vector3 direction,Vector3 center,float width,float height)
        {
            center.Normalize();float facing=Vector3.Dot(direction,center);if(facing<=.2f)return 0;
            var right=Vector3.Cross(Vector3.up,center).normalized;var up=Vector3.Cross(center,right);
            float x=Mathf.Abs(Vector3.Dot(direction,right)/facing)/width,y=Mathf.Abs(Vector3.Dot(direction,up)/facing)/height;
            return (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1.08f,x)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1.06f,y)));
        }
    }
}
