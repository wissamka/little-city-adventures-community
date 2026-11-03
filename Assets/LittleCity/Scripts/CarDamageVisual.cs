using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
    // Clone body meshes per car; tyres, steering and shared imported assets stay untouched.
    public sealed class CarDamageVisual:MonoBehaviour
    {
        sealed class Panel {public MeshFilter Filter;public Mesh Mesh;public Vector3[] Original,Normals;public Vector4[] Tangents;}
        readonly List<Panel> panels=new List<Panel>();readonly List<Point> dents=new List<Point>();
        public int DentCount=>dents.Count;
        public float MaximumDeformation {get;private set;}
        public static void Attach(VehicleModel model){if(model.Mode==Ride.RaceCar)model.Root.gameObject.AddComponent<CarDamageVisual>();}
        public void Set(IReadOnlyList<Point> source)
        {
            bool same=source.Count==dents.Count;if(same)for(int i=0;i<source.Count;i++)if(source[i].X!=dents[i].X||source[i].Y!=dents[i].Y||source[i].Z!=dents[i].Z){same=false;break;}
            if(same)return;dents.Clear();for(int i=0;i<source.Count;i++)dents.Add(source[i]);Refresh();
        }
        public void Refresh()
        {
            var filters=GetComponentsInChildren<MeshFilter>(true);
            foreach(var f in filters)
            {
                if(f.sharedMesh==null||!f.sharedMesh.isReadable)continue;bool wheel=false;
                for(var parent=f.transform;parent!=transform;parent=parent.parent)if(parent.name.ToLowerInvariant().Contains("wheel")||parent.name.ToLowerInvariant().Contains("steering")){wheel=true;break;}
                if(wheel)continue;
                bool body=(f.name.StartsWith("Body|")&&!f.name.StartsWith("Body|Interior")&&f.name!="Body|Floormat"&&f.name!="Body|Glass"&&f.name!="Body|Dashboard")||f.name=="Sculpted hood"||f.name=="Rear deck"||f.name=="Nose"||f.name=="Roof"||f.name=="Rounded side body"||f.name=="Continuous aerodynamic body panels"||f.name=="Crowned roof"||f.name=="Safari hood"||f.name=="Jeep body";
                if(f.name=="High Def material batch"&&f.transform.parent.name=="High Def sculpted sports car")body=true;
                if(!body)continue;bool known=false;foreach(var p in panels)if(p.Filter==f){known=true;break;}if(known)continue;
                var clone=Instantiate(f.sharedMesh);clone.name=f.sharedMesh.name+" • individual dents";f.sharedMesh=clone;panels.Add(new Panel{Filter=f,Mesh=clone,Original=clone.vertices,Normals=clone.normals,Tangents=clone.tangents});
            }
            MaximumDeformation=0;
            foreach(var p in panels)
            {
                if(p.Filter==null)continue;var vertices=new Vector3[p.Original.Length];var toRoot=transform.worldToLocalMatrix*p.Filter.transform.localToWorldMatrix;var fromRoot=toRoot.inverse;
                for(int i=0;i<vertices.Length;i++)
                {
                    Vector3 original=toRoot.MultiplyPoint3x4(p.Original[i]),move=Vector3.zero;
                    foreach(var d in dents)
                    {
                        var normal=new Vector3(d.X,0,d.Z).normalized;
                        var center=new Vector3(normal.x*1.13f,.84f,normal.z*2.02f);
                        var offset=original-center;offset.y*=.7f;float distance=offset.magnitude;
                        float weight=Mathf.Pow(Mathf.Clamp01(1-distance/1.4f),2);
                        move+=(-normal-Vector3.up*.28f)*(.052f*d.Y*weight);
                    }
                    move=Vector3.ClampMagnitude(move,.48f);MaximumDeformation=Mathf.Max(MaximumDeformation,move.magnitude);
                    vertices[i]=fromRoot.MultiplyPoint3x4(original+move);
                }
                p.Mesh.vertices=vertices;if(dents.Count==0){p.Mesh.normals=p.Normals;p.Mesh.tangents=p.Tangents;}else{p.Mesh.RecalculateNormals();if(p.Mesh.uv.Length==vertices.Length)p.Mesh.RecalculateTangents();}p.Mesh.RecalculateBounds();
            }
        }
        void OnDestroy(){foreach(var p in panels)if(p.Mesh!=null)Destroy(p.Mesh);}
    }
}
