using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    public sealed class HighScenery : MonoBehaviour
    {
        readonly List<Material> owned=new List<Material>();
        Material Own(Material m){owned.Add(m);return m;}
        static void Material(GameObject o,Material m){o.GetComponent<Renderer>().sharedMaterial=m;}
        public static Transform Build(CityGame game,GraphicsQuality q)
        {
            var root=Art.Group("High Def roadside and landscape detail",game.transform);var owner=root.gameObject.AddComponent<HighScenery>();
            var asphalt=owner.Own(q.Finish(Art.Hex("3B4043"),.03f,.22f,8));var bark=owner.Own(q.Finish(Art.Hex("4B382A"),0,.17f,3));
            if(q.High){asphalt.color=Art.Hex("30373C");asphalt.SetFloat("_Surface",9);}
            var stone=owner.Own(q.Finish(Art.Hex("6A6D64"),0,.19f,2));var foliage=owner.Own(q.Finish(Art.Hex("234B2C"),0,.24f,4));var lightLeaves=owner.Own(q.Finish(Art.Hex("4D6735"),0,.26f,4));
            if(game.State.WorldIndex==0)
            {
                foreach(int s in CityWorld.Streets)
                {
                    Material(Art.Box("Textured asphalt street surface",root,new Vector3(s,.030f,0),new Vector3(11.9f,.001f,218),Color.white,0),asphalt);
                    Material(Art.Box("Textured asphalt street surface",root,new Vector3(0,.030f,s),new Vector3(218,.001f,11.9f),Color.white,0),asphalt);
                }
                foreach(int side in new[]{-1,1})for(int i=0;i<10;i++)
                {
                    float x=-155+i*29,z=side*(122+i%3*6);Pine(root,new Vector3(x,0,z),12+i%4*3,bark,foliage,lightLeaves,i+30);
                    Rock(root,new Vector3(x+6,.3f,z+side*4),new Vector3(4+i%3,1.8f+i%2,3),stone,i);
                }
                // Small road-edge plantings frame the existing park road without occupying it.
                for(int i=0;i<6;i++)
                {
                    float z=-22+i*9;Pine(root,new Vector3(-22,0,z),8+i%2*2,bark,foliage,lightLeaves,50+i);
                    Rock(root,new Vector3(-20,.12f,z+2),new Vector3(1.3f,.6f,1.1f),stone,71+i);
                }
                for(int i=0;i<12;i++)
                {
                    var chunk=Art.Group("Detailed roadside planting",root,new Vector3(-153+i*27,0,-118));
                    for(int n=0;n<7;n++){float x=n*.27f;Material(Art.Tube("Wild grass blade",chunk,new Vector3(x,0,0),new Vector3(x+.11f,.35f+n%3*.12f,.10f),.014f,Color.white),lightLeaves);}
                    GraphicsCar.Combine(chunk);
                }
            }
            else if(game.State.WorldIndex==4)
            {
                foreach(int side in new[]{-1,1})for(int i=0;i<5;i++)Pine(root,new Vector3(side*197,0,-122+i*61),13+i%3*3,bark,foliage,lightLeaves,i+side*10);
            }
            GraphicsCar.Combine(root);return root;
        }
        static void Rock(Transform root,Vector3 p,Vector3 size,Material mat,int seed)
        {
            var o=Art.Ball("Weathered roadside rock",root,p,size,Color.white);var f=o.GetComponent<MeshFilter>();var mesh=Object.Instantiate(f.sharedMesh);mesh.name="Irregular layered stone";var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++){var v=vertices[i];float n=Mathf.PerlinNoise(v.x*7+seed+20,v.z*7+v.y*3+30);vertices[i]=v*(.86f+n*.25f);}
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();f.sharedMesh=mesh;o.AddComponent<OwnedMesh>().Mesh=mesh;Material(o,mat);
        }
        static void Pine(Transform parent,Vector3 p,float height,Material bark,Material leaf,Material tip,int seed)
        {
            if(GraphicsQuality.Current!=null&&GraphicsQuality.Current.High){LeafTree(parent,p,height,bark,leaf,seed);return;}
            var root=Art.Group("Detailed tall pine",parent,p);Material(Art.Tube("Textured tapered trunk",root,Vector3.zero,new Vector3(0,height,0),height*.028f,Color.white),bark);
            var random=new System.Random(seed+100);var v=new List<Vector3>();var t=new List<int>();
            for(int level=0;level<9;level++)
            {
                float y=height*(.2f+level*.079f),radius=height*.24f*(1-level*.085f);
                for(int branch=0;branch<7;branch++)
                {
                    float a=branch*Mathf.PI*2/7+level*.8f;Vector3 end=new Vector3(Mathf.Sin(a)*radius,y-.15f,Mathf.Cos(a)*radius);
                    Material(Art.Tube("Visible pine branch",root,new Vector3(0,y+.35f,0),end,.027f,Color.white),bark);
                    for(int n=0;n<30;n++)
                    {
                        Vector3 c=Vector3.Lerp(new Vector3(0,y+.35f,0),end,.15f+n*.028f)+new Vector3((float)random.NextDouble()*.55f-.27f,(float)random.NextDouble()*.35f,(float)random.NextDouble()*.55f-.27f);
                        float length=.22f+(float)random.NextDouble()*.28f;Vector3 right=new Vector3(Mathf.Cos(a),.1f,-Mathf.Sin(a))*.12f,along=new Vector3(Mathf.Sin(a),.35f,Mathf.Cos(a))*length;
                        int k=v.Count;v.Add(c-right);v.Add(c+along);v.Add(c+right);v.Add(c-along*.3f);v.Add(c-right);v.Add(c+along);v.Add(c+right);v.Add(c-along*.3f);t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3,k+6,k+5,k+4,k+7,k+6,k+4});
                    }
                }
            }
            var mesh=new Mesh{name="Individual pine needle sprays"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var needles=new GameObject("Dense detailed needles",typeof(MeshFilter),typeof(MeshRenderer));needles.transform.SetParent(root,false);needles.GetComponent<MeshFilter>().sharedMesh=mesh;needles.GetComponent<MeshRenderer>().sharedMaterial=leaf;needles.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        static void LeafTree(Transform parent,Vector3 p,float height,Material bark,Material leaf,int seed)
        {
            var root=Art.Group("Layered broadleaf tree",parent,p);var random=new System.Random(seed+200);
            Material(Art.Tube("Bark trunk",root,Vector3.zero,new Vector3(.12f,height*.68f,0),height*.025f,Color.white),bark);
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            for(int branch=0;branch<9;branch++)
            {
                float angle=branch*2.4f;float y=height*(.4f+branch*.047f);float radius=height*(.23f-branch*.01f);
                Vector3 center=new Vector3(Mathf.Sin(angle)*radius,y+height*.13f,Mathf.Cos(angle)*radius);
                Material(Art.Tube("Forked limb",root,new Vector3(0,y,0),center,height*.010f,Color.white),bark);
                for(int n=0;n<450;n++)
                {
                    float theta=(float)random.NextDouble()*Mathf.PI*2,u=(float)random.NextDouble()*2-1,r=Mathf.Pow((float)random.NextDouble(),.333f);
                    Vector3 c=center+new Vector3(Mathf.Cos(theta)*Mathf.Sqrt(1-u*u),u*.7f,Mathf.Sin(theta)*Mathf.Sqrt(1-u*u))*r*height*.19f;
                    var rotation=Quaternion.Euler((float)random.NextDouble()*140-70,(float)random.NextDouble()*360,0);
                    float length=.12f+(float)random.NextDouble()*.07f,width=length*.48f;
                    Vector3 x=rotation*Vector3.right*width,z=rotation*Vector3.forward*length,fold=rotation*Vector3.up*.012f;
                    // Distinct vertices for the two faces are essential: opposing
                    // triangles sharing vertices cancel normals and turn leaves black.
                    var shape=new[]{c+fold,c-z,c-x*.62f-z*.64f,c-x,c-x*.62f+z*.64f,c+z,c+x*.62f+z*.64f,c+x,c+x*.62f-z*.64f};
                    int k=vertices.Count;vertices.AddRange(shape);vertices.AddRange(shape);
                    for(int edge=0;edge<8;edge++){int a=k+1+edge,b=k+1+(edge+1)%8;triangles.AddRange(new[]{k,a,b,k+9,b+9,a+9});}
                    float shade=.72f+(float)random.NextDouble()*.4f;for(int t=0;t<18;t++)colors.Add(new Color(shade,shade,shade,1));
                }
            }
            var mesh=new Mesh{name="Dense individual broadleaf canopy",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var canopy=new GameObject("Broadleaf canopy",typeof(MeshFilter),typeof(MeshRenderer));canopy.transform.SetParent(root,false);canopy.GetComponent<MeshFilter>().sharedMesh=mesh;canopy.GetComponent<MeshRenderer>().sharedMaterial=leaf;canopy.AddComponent<OwnedMesh>().Mesh=mesh;
        }
        void OnDestroy(){foreach(var m in owned)if(m!=null)Destroy(m);}
    }
}
