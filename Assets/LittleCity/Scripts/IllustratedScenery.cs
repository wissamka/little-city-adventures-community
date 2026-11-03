using System.Collections.Generic;
using UnityEngine;

namespace LittleCity
{
    public static class IllustratedScenery
    {
        static readonly Dictionary<string,Mesh> Meshes=new Dictionary<string,Mesh>();
        public static int CachedVertices { get {int n=0;foreach(var m in Meshes.Values)n+=m.vertexCount;return n;} }
        static readonly Color Pine=Art.Hex("52756B"),Sage=Art.Hex("78957A"),Stone=Art.Hex("99A69C"),Bark=Art.Hex("796759");
        public static int Surface(string name)
        {
            string n=name.ToLowerInvariant();
            if(n.Contains("leaf")||n.Contains("foliage")||n.Contains("frond")||n.Contains("pine branches")||n.Contains("crown")||n.Contains("hedge"))return 4;
            if(n.Contains("grass")||n.Contains("lawn"))return 5;
            if(n.Contains("masonry")||n.Contains("wall")||n.Contains("pillar")||n.Contains("parapet")||n.Contains("brick"))return 1;
            if(n.Contains("stone")||n.Contains("path")||n.Contains("pav")||n.Contains("sidewalk")||n.Contains("fountain"))return 2;
            if(n.Contains("trunk")||n.Contains("branch")||n.Contains("plank")||n.Contains("slat")||n.Contains("floor")||n.Contains("cabinet")||n.Contains("door frame"))return 3;
            if(n.Contains("shirt")||n.Contains("sleeve")||n.Contains("jacket")||n.Contains("trouser")||n.Contains("duvet")||n.Contains("sofa")||n.Contains("rug"))return 6;
            return 0;
        }
        static float Range(System.Random r,float a,float b)=>Mathf.Lerp(a,b,(float)r.NextDouble());
        sealed class Geometry
        {
            public readonly List<Vector3> v=new List<Vector3>(),n=new List<Vector3>();
            public readonly List<Vector3> uv=new List<Vector3>();
            public readonly List<Color> c=new List<Color>();public readonly List<int> t=new List<int>();
            public void Triangle(Vector3 a,Vector3 b,Vector3 d,Vector3 normal,Color color)
            {int i=v.Count;v.Add(a);v.Add(b);v.Add(d);for(int k=0;k<3;k++){n.Add(normal);c.Add(color);uv.Add(Vector3.zero);}t.Add(i);t.Add(i+1);t.Add(i+2);}
            public void Leaf(Vector3 p,Vector3 normal,float length,float width,Color color)
            {
                Vector3 u=Vector3.Cross(normal,Vector3.up);if(u.sqrMagnitude<.01f)u=Vector3.right;u.Normalize();Vector3 w=Vector3.Cross(normal,u).normalized;
                // A rounded six-point silhouette avoids the large diamond spikes of the first pass.
                Vector3[] ring={p+w*length*.5f,p+w*length*.24f+u*width*.5f,
                    p-w*length*.28f+u*width*.43f,p-w*length*.5f,
                    p-w*length*.28f-u*width*.43f,p+w*length*.24f-u*width*.5f};
                Vector3 mid=p+normal*.018f;
                for(int k=0;k<6;k++)
                {
                    Triangle(mid,ring[(k+1)%6],ring[k],normal,color);
                    uv[uv.Count-2]=new Vector3(0,0,1);uv[uv.Count-1]=new Vector3(0,0,1);
                    Triangle(mid,ring[k],ring[(k+1)%6],-normal,color*.86f);
                    uv[uv.Count-2]=new Vector3(0,0,1);uv[uv.Count-1]=new Vector3(0,0,1);
                }
            }
            public Mesh Finish(string name){var m=new Mesh{name=name};m.SetVertices(v);m.SetNormals(n);m.SetColors(c);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateBounds();return m;}
        }
        static Mesh Foliage(bool hedge)
        {
            string key=hedge?"Clipped hedge leaves":"Layered fruit-tree leaves";
            if(Meshes.TryGetValue(key,out var result))return result;
            var g=new Geometry();var rng=new System.Random(hedge?102:813);
            int clusters=hedge?1:7,leaves=hedge?420:105;
            for(int cluster=0;cluster<clusters;cluster++)
            {
                float angle=cluster*Mathf.PI*2/6;
                Vector3 center=hedge?new Vector3(0,.7f,0):cluster==6?new Vector3(0,5.6f,0):new Vector3(Mathf.Sin(angle)*1.1f,4.4f+cluster%3*.32f,Mathf.Cos(angle)*.85f);
                Vector3 radius=hedge?new Vector3(1.12f,.62f,.73f):new Vector3(1.25f,1.13f,1.1f);
                for(int j=0;j<leaves;j++)
                {
                    float y=Range(rng,-1,1),a=Range(rng,0,Mathf.PI*2),r=Mathf.Sqrt(1-y*y);
                    Vector3 normal=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);
                    Vector3 shell=normal;
                    if(hedge)shell=new Vector3(Mathf.Sign(normal.x)*Mathf.Pow(Mathf.Abs(normal.x),.55f),Mathf.Sign(normal.y)*Mathf.Pow(Mathf.Abs(normal.y),.7f),Mathf.Sign(normal.z)*Mathf.Pow(Mathf.Abs(normal.z),.55f));
                    Vector3 pos=center+Vector3.Scale(shell,radius)*Range(rng,.94f,1);
                    g.Leaf(pos,normal,hedge?Range(rng,.19f,.28f):Range(rng,.48f,.68f),hedge?.17f:.39f,Color.white*Range(rng,.87f,1.23f));
                }
            }
            return Meshes[key]=g.Finish(key);
        }
        static GameObject MeshObject(string name,Transform parent,Mesh mesh,Vector3 p,Vector3 scale,Color color,int surface)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;
            o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=Art.Mat(color,surface);return o;
        }
        public static Transform Tree(Transform parent,Vector3 p,float scale,bool fruit=true,Color? leafColor=null)
        {
            var root=Art.Group("Illustrated branching tree",parent,p);root.localScale=Vector3.one*scale;root.localRotation=Quaternion.Euler(0,Mathf.Repeat(p.x*13+p.z*7,360),0);
            Art.Tube("Weathered trunk",root,Vector3.zero,new Vector3(.12f,3.9f,.08f),.24f,Bark);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                Art.Tube("Branch",root,new Vector3(.05f,2.3f+i*.18f,0),new Vector3(Mathf.Sin(a)*1.4f,4.6f+i%2*.6f,Mathf.Cos(a)*1.2f),.09f,Bark);
            }
            MeshObject("Layered illustrated foliage",root,Foliage(false),Vector3.zero,Vector3.one,leafColor??Pine,4);
            if(fruit)for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;Art.Ball("Orchard fruit",root,new Vector3(Mathf.Sin(a)*1.8f,4.2f+i%3*.3f,Mathf.Cos(a)*1.55f),Vector3.one*.22f,i%2==0?Art.Hex("C59852"):Art.Hex("A66658"));
            }
            ContactShadows.Create(root,new Vector3(0,.05f,0),new Vector2(5.2f,4.5f));
            return root;
        }
        public static void Hedge(Transform parent,Vector3 p,Vector3 scale)
        {MeshObject("Clipped leafy hedge",parent,Foliage(true),p,scale,Sage,4);ContactShadows.Create(parent,p+Vector3.up*.045f,new Vector2(scale.x*2.55f,scale.z*1.85f));}
        static Mesh Flagstone(int variant)
        {
            string key="Hand cut stone "+variant;if(Meshes.TryGetValue(key,out var m))return m;
            var g=new Geometry();var rng=new System.Random(500+variant);var ring=new Vector3[6];
            Vector2[] corners={new Vector2(-.48f,-.52f),new Vector2(.30f,-.52f),new Vector2(.51f,-.28f),new Vector2(.49f,.52f),new Vector2(-.31f,.52f),new Vector2(-.52f,.24f)};
            for(int i=0;i<6;i++)ring[i]=new Vector3(corners[i].x+Range(rng,-.025f,.025f),.016f,corners[i].y+Range(rng,-.025f,.025f));
            float stoneTone=Range(rng,.89f,1.08f);
            for(int i=0;i<6;i++)
            {
                var a=ring[i];var b=ring[(i+1)%6];g.Triangle(new Vector3(0,.025f,0),b,a,Vector3.up,Color.white*stoneTone);
                g.uv[g.uv.Count-2]=new Vector3(0,0,1);g.uv[g.uv.Count-1]=new Vector3(0,0,1);
                var lowA=new Vector3(a.x,-.03f,a.z);var lowB=new Vector3(b.x,-.03f,b.z);Vector3 normal=new Vector3((a.x+b.x)*.5f,0,(a.z+b.z)*.5f).normalized;
                g.Triangle(a,b,lowA,normal,Color.white*.73f);g.Triangle(b,lowB,lowA,normal,Color.white*.73f);
            }
            return Meshes[key]=g.Finish(key);
        }
        static Mesh Grass()
        {
            const string key="Tufts of grass";if(Meshes.TryGetValue(key,out var m))return m;
            var g=new Geometry();var rng=new System.Random(81);
            for(int i=0;i<20;i++)
            {
                Vector3 p=new Vector3(Range(rng,-.5f,.5f),0,Range(rng,-.5f,.5f));float a=Range(rng,0,6.28f);Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.045f;
                Vector3 tip=p+new Vector3(Range(rng,-.1f,.1f),Range(rng,.18f,.45f),Range(rng,-.1f,.1f));Vector3 n=Vector3.Cross(side,tip-p).normalized;
                g.Triangle(p-side,tip,p+side,n,Color.white*Range(rng,.7f,1.1f));g.Triangle(p+side,tip,p-side,-n,Color.white);
            }
            return Meshes[key]=g.Finish(key);
        }
        static List<Vector2> ClipCell(List<Vector2> polygon,Vector2 middle,Vector2 normal)
        {
            var clipped=new List<Vector2>();
            for(int i=0;i<polygon.Count;i++)
            {
                Vector2 a=polygon[i],b=polygon[(i+1)%polygon.Count];float da=Vector2.Dot(a-middle,normal),db=Vector2.Dot(b-middle,normal);
                if(da<=0)clipped.Add(a);
                if((da<=0)!=(db<=0))clipped.Add(Vector2.Lerp(a,b,da/(da-db)));
            }
            return clipped;
        }
        static void Paving(Transform root,Rect area,int seed)
        {
            var rng=new System.Random(seed);int cols=Mathf.CeilToInt(area.width/.66f),rows=Mathf.CeilToInt(area.height/.80f);
            float cw=area.width/cols,ch=area.height/rows;var sites=new Vector2[cols,rows];
            for(int x=0;x<cols;x++)for(int y=0;y<rows;y++)sites[x,y]=new Vector2(area.xMin+(x+.5f+Range(rng,-.24f,.24f))*cw,area.yMin+(y+.5f+Range(rng,-.24f,.24f))*ch);
            var g=new Geometry();
            for(int x=0;x<cols;x++)for(int y=0;y<rows;y++)
            {
                Vector2 site=sites[x,y];var cell=new List<Vector2>{new Vector2(area.xMin,area.yMin),new Vector2(area.xMax,area.yMin),new Vector2(area.xMax,area.yMax),new Vector2(area.xMin,area.yMax)};
                for(int nx=Mathf.Max(0,x-2);nx<=Mathf.Min(cols-1,x+2);nx++)for(int ny=Mathf.Max(0,y-2);ny<=Mathf.Min(rows-1,y+2);ny++)
                    if(nx!=x||ny!=y)cell=ClipCell(cell,(site+sites[nx,ny])*.5f,sites[nx,ny]-site);
                // Offset each edge inward by 12 mm: neighboring stones have a consistent 24 mm joint.
                var inset=new List<Vector2>(cell);
                for(int k=0;k<cell.Count;k++)
                {
                    Vector2 a=cell[k],b=cell[(k+1)%cell.Count],edge=(b-a).normalized;
                    Vector2 outward=new Vector2(edge.y,-edge.x);
                    inset=ClipCell(inset,(a+b)*.5f-outward*.012f,outward);
                }
                if(inset.Count<3)continue;
                Color tone=Color.white*Range(rng,.80f,1.12f);float h=.082f+Range(rng,-.003f,.003f);
                Vector3 center=new Vector3(site.x,h,site.y);
                for(int k=0;k<inset.Count;k++)
                {
                    Vector2 a=inset[k],b=inset[(k+1)%inset.Count];Vector3 va=new Vector3(a.x,h,a.y),vb=new Vector3(b.x,h,b.y);
                    g.Triangle(center,vb,va,Vector3.up,tone);
                    Vector3 normal=new Vector3(b.y-a.y,0,a.x-b.x).normalized;
                    g.Triangle(va,vb,va-Vector3.up*.025f,normal,tone*.75f);
                    g.Triangle(vb,vb-Vector3.up*.025f,va-Vector3.up*.025f,normal,tone*.75f);
                }
            }
            string name="Individual flagstones "+seed;var mesh=g.Finish(name);Meshes[name]=mesh;
            MeshObject(name,root,mesh,Vector3.zero,Vector3.one,Stone,2);
        }
        public static void ParkDetails(Transform root)
        {
            Paving(root,new Rect(-2.45f,-23.8f,4.9f,47.6f),91);
            Paving(root,new Rect(-23.8f,-6.65f,21.35f,3.3f),121);
            Paving(root,new Rect(2.45f,-6.65f,21.35f,3.3f),151);
            foreach(int side in new[]{-1,1})for(int i=0;i<8;i++)
            {
                float z=-19+i*5.3f;if(Mathf.Abs(z+5)<3)continue;
                Hedge(root,new Vector3(side*3.35f,.05f,z),new Vector3(.65f,.83f,1.12f));
                for(int j=0;j<3;j++)MeshObject("Wild grass",root,Grass(),new Vector3(side*4.5f,.06f,z+j*.6f),Vector3.one,Art.Hex("6B8F76"),5);
            }
            foreach(int side in new[]{-1,1})
            {
                Art.Box("Brick garden entrance pier",root,new Vector3(side*3.4f,1.15f,-23.7f),new Vector3(.72f,2.3f,.72f),Art.Hex("A09281"),.03f);
                Art.Box("Stone pier cap",root,new Vector3(side*3.4f,2.37f,-23.7f),new Vector3(.92f,.15f,.92f),Stone,.035f);
                Art.Tube("Iron arch upright",root,new Vector3(side*3.4f,2.35f,-23.7f),new Vector3(side*3.4f,3.25f,-23.7f),.05f,Art.Ink);
            }
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                Art.Tube("Garden iron arch",root,new Vector3(Mathf.Cos(a)*3.4f,3.25f+Mathf.Sin(a)*1.2f,-23.7f),new Vector3(Mathf.Cos(b)*3.4f,3.25f+Mathf.Sin(b)*1.2f,-23.7f),.055f,Art.Ink);
            }
        }
        public static void WindowBox(Transform parent,float x,float y,float z)
        {
            Art.Box("Wooden window planter",parent,new Vector3(x,y,z),new Vector3(1.5f,.32f,.55f),Bark,.04f);
            Hedge(parent,new Vector3(x,y+.15f,z),new Vector3(.63f,.45f,.35f));
        }
        public static void Release(){foreach(var m in Meshes.Values)if(m!=null)Object.Destroy(m);Meshes.Clear();}
    }
}
