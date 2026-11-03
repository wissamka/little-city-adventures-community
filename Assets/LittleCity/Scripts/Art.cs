using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleCity
{
    public static class Art
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> SoftBoxes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Mesh> Boxes = new Dictionary<string, Mesh>();
        public static int CachedBoxCount=>Boxes.Count;
        public static int CachedMaterialCount=>Materials.Count;
        public static Shader ToonShader;
        public static Shader GlassShader;
        public static readonly Color Ink = Hex("223B53"), White = Hex("FFF5D9"), Blue = Hex("237CD9"),
            Teal = Hex("24AAA4"), Yellow = Hex("FFD058"), Red = Hex("EF5B4C"), Green = Hex("73B941");

        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
        public static Material Mat(Color color, int surface = 0)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + ":" + surface;
            if (!Materials.TryGetValue(key, out Material mat) || mat == null)
            {
                Shader shader = ToonShader != null ? ToonShader : Shader.Find("LittleCity/Sunshine");
                if (shader == null) shader = Shader.Find("Mobile/Diffuse");
                mat = new Material(shader) { name = "Candy " + key, color = color, enableInstancing = true };
                mat.SetFloat("_Surface", surface);
                mat.SetFloat("_OutlinePixels", surface == 5 ? 0 : surface == 4 ? .75f : 1.05f);
                Materials[key] = mat;
            }
            return mat;
        }

        public static Material SignMaterial(Material source)
        {
            if(!Materials.TryGetValue("World sign",out Material mat) || mat==null){mat=new Material(source);mat.SetFloat("_CullMode",2);Materials["World sign"]=mat;}
            return mat;
        }
        public static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false); t.localPosition = position; return t;
        }

        public static void GlassPane(string name, Transform parent, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            if (!Materials.TryGetValue("Glass", out Material material))
            {
                material = new Material(GlassShader != null ? GlassShader : Shader.Find("LittleCity/Glass"));
                material.color = new Color(.48f, .82f, .92f, .17f); Materials["Glass"] = material;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { a, b, c, d }; mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            obj.AddComponent<OwnedMesh>().Mesh = mesh;
        }

        public static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name; obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos; obj.transform.localScale = size;
            var collider = obj.GetComponent<Collider>();
            // link.xml preserves CreatePrimitive's native dependencies in IL2CPP.
            // A missing optional collider must not prevent assigning the artwork.
            if (collider != null) { collider.enabled = false; Object.Destroy(collider); }
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color, IllustratedScenery.Surface(name));
            return obj;
        }
        public static GameObject Ball(string name, Transform parent, Vector3 pos, Vector3 size, Color color)
            => Shape(name, PrimitiveType.Sphere, parent, pos, size, color);
        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Color color, float round = .08f)
        {
            if (round <= 0)
            {
                var cube=Shape(name,PrimitiveType.Cube,parent,pos,size,color);
                AddCozyShape(cube,name,size,true);return cube;
            }
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(parent, false); obj.transform.localPosition = pos;
            string key = size.ToString("F3") + ":" + round.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            if (!Boxes.TryGetValue(key, out Mesh mesh)) { mesh = RoundBox(size, round); Boxes[key] = mesh; }
            obj.GetComponent<MeshFilter>().sharedMesh = mesh;
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color, IllustratedScenery.Surface(name));
            AddCozyShape(obj,name,size,false);return obj;
        }
        public static GameObject Tube(string name, Transform parent, Vector3 start, Vector3 end, float radius, Color color)
        {
            var obj = Shape(name, PrimitiveType.Cylinder, parent, (start + end) / 2,
                new Vector3(radius * 2, (end - start).magnitude / 2, radius * 2), color);
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
            return obj;
        }
        public static GameObject Disk(string name, Transform parent, Vector3 pos, float radius, float thickness, Color color)
            => Shape(name, PrimitiveType.Cylinder, parent, pos, new Vector3(radius * 2, thickness / 2, radius * 2), color);

        public static GameObject Ring(string name, Transform parent, float radius, float radialThickness, float width, Color color)
        {
            // A real open-center wheel, with its axle on X.
            const int segments = 28, sides = 8;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var indices = new List<int>();
            for (int i = 0; i <= segments; i++) for (int j = 0; j <= sides; j++)
            {
                float angle = i * Mathf.PI * 2 / segments, tube = j * Mathf.PI * 2 / sides;
                Vector3 radial = new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle));
                vertices.Add(radial * (radius + Mathf.Cos(tube) * radialThickness) + Vector3.right * Mathf.Sin(tube) * width);
                normals.Add((radial * Mathf.Cos(tube) + Vector3.right * Mathf.Sin(tube)).normalized);
                if (i == segments || j == sides) continue;
                int a = i * (sides + 1) + j, b = a + sides + 1;
                indices.Add(a); indices.Add(a + 1); indices.Add(b); indices.Add(b); indices.Add(a + 1); indices.Add(b + 1);
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); obj.transform.SetParent(parent, false);
            obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = Mat(color);
            obj.AddComponent<OwnedMesh>().Mesh = mesh;
            return obj;
        }

        public static void WheelArch(Transform parent,Vector3 center,Color color)
        {
            const int segments=24,sides=8;var v=new List<Vector3>();var n=new List<Vector3>();var idx=new List<int>();
            for(int i=0;i<=segments;i++)for(int j=0;j<=sides;j++)
            {
                float a=i*Mathf.PI/segments,b=j*Mathf.PI*2/sides;
                Vector3 radial=new Vector3(0,Mathf.Sin(a),Mathf.Cos(a));
                v.Add(center+radial*(.50f+Mathf.Cos(b)*.075f)+Vector3.right*Mathf.Sin(b)*.13f);
                n.Add((radial*Mathf.Cos(b)+Vector3.right*Mathf.Sin(b)).normalized);
                if(i==segments||j==sides)continue;int k=i*(sides+1)+j,l=k+sides+1;
                idx.Add(k);idx.Add(k+1);idx.Add(l);idx.Add(l);idx.Add(k+1);idx.Add(l+1);
            }
            var mesh=new Mesh{name="Sculpted curved wheel arch"};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(idx,0);mesh.RecalculateBounds();
            var obj=new GameObject("Wheel arch",typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=Mat(color);obj.AddComponent<OwnedMesh>().Mesh=mesh;
        }

        static void AddCozyShape(GameObject obj,string name,Vector3 size,bool normalized)
        {
            if(!CozyShape.Furniture(name) || Mathf.Min(size.x,size.y,size.z)<.03f)return;
            string key=size.ToString("F4")+":"+normalized;
            if(!SoftBoxes.TryGetValue(key,out var mesh))
            {
                mesh=RoundBox(size,Mathf.Min(.28f,Mathf.Min(size.x,size.y,size.z)*.40f));mesh.name="Soft rounded furniture";
                if(normalized)
                {
                    var v=mesh.vertices;var n=mesh.normals;
                    for(int i=0;i<v.Length;i++){v[i]=new Vector3(v[i].x/size.x,v[i].y/size.y,v[i].z/size.z);n[i]=Vector3.Scale(n[i],size).normalized;}
                    mesh.vertices=v;mesh.normals=n;mesh.RecalculateBounds();
                }
                SoftBoxes[key]=mesh;
            }
            obj.AddComponent<CozyShape>().Initialize(obj.GetComponent<MeshFilter>().sharedMesh,mesh);
        }

        static Mesh RoundBox(Vector3 size, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .48f);
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>();
            Vector3 h = size * .5f, inner = h - Vector3.one * radius;
            Vector3[] ns = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            const int steps = 6;
            foreach (Vector3 n in ns)
            {
                Vector3 u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int offset = vertices.Count;
                for (int y = 0; y <= steps; y++) for (int x = 0; x <= steps; x++)
                {
                    Vector3 raw = Vector3.Scale(n + u * (x * 2f / steps - 1) + v * (y * 2f / steps - 1), h);
                    Vector3 core = new Vector3(Mathf.Clamp(raw.x, -inner.x, inner.x), Mathf.Clamp(raw.y, -inner.y, inner.y), Mathf.Clamp(raw.z, -inner.z, inner.z));
                    Vector3 normal = (raw - core).normalized;
                    vertices.Add(core + normal * radius); normals.Add(normal);
                    if (x == steps || y == steps) continue;
                    int a = offset + y * (steps + 1) + x, b = a + 1, c = a + steps + 1, d = c + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(c);
                    triangles.Add(b); triangles.Add(d); triangles.Add(c);
                }
            }
            var mesh = new Mesh { name = "Rounded toy form" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        public static void CombineStatic(Transform root)
        {
            // Spatial chunks retain frustum culling. One renderer per material per chunk.
            var batches = new Dictionary<string, List<CombineInstance>>();
            var mats = new Dictionary<string, Material>();
            var softBatches = new Dictionary<string,List<CombineInstance>>();var softKeys=new HashSet<string>();
            var originals = new List<GameObject>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || filter.GetComponentInParent<MovingScenery>() != null || filter.GetComponentInParent<TMPro.TMP_Text>() != null) continue;
                Vector3 p = filter.transform.position;
                string key = Mathf.FloorToInt(p.x / 48) + ":" + Mathf.FloorToInt(p.z / 48) + ":" + renderer.sharedMaterial.GetInstanceID();
                if (!batches.ContainsKey(key)) { batches[key] = new List<CombineInstance>();softBatches[key]=new List<CombineInstance>(); mats[key] = renderer.sharedMaterial; }
                var shape=filter.GetComponent<CozyShape>();var matrix=root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                batches[key].Add(new CombineInstance { mesh = shape!=null?shape.Original:filter.sharedMesh, transform = matrix });
                softBatches[key].Add(new CombineInstance { mesh = shape!=null?shape.Soft:filter.sharedMesh, transform = matrix });
                if(shape!=null)softKeys.Add(key);
                originals.Add(filter.gameObject); renderer.enabled = false;
            }
            foreach (var item in batches)
            {
                var obj = new GameObject("Scenery " + item.Key, typeof(MeshFilter), typeof(MeshRenderer));
                obj.transform.SetParent(root, false);
                var mesh = new Mesh { name = "Scenery chunk", indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(item.Value.ToArray(), true, true);
                obj.GetComponent<MeshFilter>().sharedMesh = mesh; obj.GetComponent<MeshRenderer>().sharedMaterial = mats[item.Key];
                obj.AddComponent<OwnedMesh>().Mesh = mesh;
                if(softKeys.Contains(item.Key))
                {
                    var soft=new Mesh{name="Soft scenery chunk",indexFormat=IndexFormat.UInt32};soft.CombineMeshes(softBatches[item.Key].ToArray(),true,true);
                    obj.AddComponent<CozyShape>().Initialize(mesh,soft,true);
                }
                obj.isStatic = true;
            }
            foreach (GameObject obj in originals){Object.Destroy(obj.GetComponent<MeshRenderer>());var shape=obj.GetComponent<CozyShape>();if(shape!=null)Object.Destroy(shape);}
        }

        public static void Release()
        {
            foreach (var mat in Materials.Values) if (mat != null) Object.Destroy(mat);
            foreach (var mesh in Boxes.Values) if (mesh != null) Object.Destroy(mesh);
            foreach(var mesh in SoftBoxes.Values)if(mesh!=null)Object.Destroy(mesh);SoftBoxes.Clear();
            Materials.Clear(); Boxes.Clear(); IllustratedScenery.Release();ContactShadows.Release();
        }
    }

    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh Mesh;
        void OnDestroy() { if (Mesh != null) Destroy(Mesh); }
    }
}
