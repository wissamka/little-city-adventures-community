using UnityEngine;
using UnityEngine.Rendering;

namespace LittleCity
{
    // Low-cost soft grounding complements directional shadows without a screen-space AO pass.
    public static class ContactShadows
    {
        static Mesh mesh;
        static Material material;
        public static Transform Create(Transform parent,Vector3 position,Vector2 size)
        {
            if(mesh==null)
            {
                mesh=new Mesh{name="Soft contact footprint"};
                mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
                mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
                material=new Material(Resources.Load<Shader>("ContactShadow")){name="Soft blue contact shade"};
            }
            var obj=new GameObject("Soft contact shade",typeof(MeshFilter),typeof(MeshRenderer));
            obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=new Vector3(size.x,1,size.y);
            obj.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return obj.transform;
        }
        public static void Release(){if(mesh!=null)Object.Destroy(mesh);if(material!=null)Object.Destroy(material);mesh=null;material=null;}
    }
}
