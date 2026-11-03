using NUnit.Framework;
using UnityEngine;
namespace LittleCity.Tests
{
    public sealed class CanopyTests
    {
        [Test]public void BroadleafFacesHaveValidOpposingNormalsAndLeafScale()
        {
            var root=new GameObject("Canopy test");var material=new Material(Shader.Find("Standard"));
            try
            {
                typeof(HighScenery).GetMethod("LeafTree",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{root.transform,Vector3.zero,18f,material,material,20});
                Mesh mesh=null;foreach(var f in root.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh.name=="Dense individual broadleaf canopy")mesh=f.sharedMesh;
                Assert.That(mesh,Is.Not.Null);Assert.That(mesh.indexFormat,Is.EqualTo(UnityEngine.Rendering.IndexFormat.UInt32));
                var v=mesh.vertices;var n=mesh.normals;Assert.That(v.Length,Is.GreaterThan(65535));
                for(int i=0;i<v.Length;i+=18)
                {
                    for(int j=0;j<9;j++){Assert.That(n[i+j].magnitude,Is.EqualTo(1).Within(.001f));Assert.That(Vector3.Dot(n[i+j],n[i+j+9]),Is.LessThan(-.99f),"Front/back leaf normals must not cancel");}
                    Assert.That(Vector3.Distance(v[i+1],v[i+5]),Is.InRange(.23f,.39f),"Believable leaf size instead of giant diamond cards");
                }
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
        }
    }
}
