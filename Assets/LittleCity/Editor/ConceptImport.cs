using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace LittleCity.Editor {
 public static class ConceptImport {
  const string Source="Assets/LittleCity/ConceptSource/",Output="Assets/LittleCity/Resources/Concept/";
  [Serializable] class Map {public int index=-1;public float scale=1,strength=1;}
  [Serializable] class Pbr {public float[] baseColorFactor;public float metallicFactor=1,roughnessFactor=1;public Map baseColorTexture,metallicRoughnessTexture;}
  [Serializable] class Mat {public string name;public Pbr pbrMetallicRoughness;public Map normalTexture,occlusionTexture;public float[] emissiveFactor;}
  [Serializable] class Tex {public int source;}
  [Serializable] class Data {public Mat[] materials;public Tex[] textures;}
  static Texture2D Texture(Data data,Map map,bool normal=false) {
   if(map==null||map.index<0)return null;
   string path=Source+"Textures/texture_"+data.textures[map.index].source+".png";
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
   importer.sRGBTexture=!normal&&data.textures[map.index].source==9;
   importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Color ColorOf(float[] c,Color fallback){return c==null||c.Length<3?fallback:new Color(c[0],c[1],c[2],c.Length>3?c[3]:1);}
  [MenuItem("Little City/Prepare detailed concept car")]
  public static void Prepare() {
   if(AssetDatabase.IsValidFolder(Output.TrimEnd('/')))AssetDatabase.DeleteAsset(Output.TrimEnd('/'));
   Directory.CreateDirectory(Output);AssetDatabase.Refresh();
   var data=JsonUtility.FromJson<Data>(File.ReadAllText(Source+"materials-source.json"));
   var materials=new Dictionary<string,Material>();
   for(int i=0;i<data.materials.Length;i++) {
    var s=data.materials[i];string name=s.name??"Material_2";var p=s.pbrMetallicRoughness??new Pbr();
    var m=new Material(Shader.Find("LittleCity/Concept PBR")){name=name};
    m.color=ColorOf(p.baseColorFactor,Color.white).gamma;
    m.SetFloat("_Metallic",p.metallicFactor);m.SetFloat("_Roughness",p.roughnessFactor);
    m.SetTexture("_MainTex",Texture(data,p.baseColorTexture));m.SetTexture("_MetalRough",Texture(data,p.metallicRoughnessTexture));
    m.SetTexture("_BumpMap",Texture(data,s.normalTexture,true));m.SetFloat("_BumpScale",s.normalTexture==null?0:s.normalTexture.scale);
    m.SetTexture("_Occlusion",Texture(data,s.occlusionTexture));m.SetFloat("_OcclusionStrength",s.occlusionTexture==null?0:s.occlusionTexture.strength);
    m.SetColor("_EmissionColor",ColorOf(s.emissiveFactor,Color.black)*.3f);
    if(name.StartsWith("Paint")){m.SetFloat("_BumpScale",.035f);m.SetTextureScale("_BumpMap",new Vector2(30,30));}
    if(name=="Interior 1")m.SetTextureScale("_BumpMap",new Vector2(200,400));
    if(name=="Interior 2")m.SetTextureScale("_BumpMap",new Vector2(100,100));
    if(name=="Floormat")m.SetTextureScale("_BumpMap",new Vector2(2,3));
    if(name=="Disc")m.SetTextureScale("_BumpMap",new Vector2(20,10));
    if(name=="Tiretread")m.SetTextureScale("_BumpMap",new Vector2(1,4));
    if(name=="Tireside"){m.SetTexture("_MainTex",null);m.SetTexture("_BumpMap",null);m.SetFloat("_BumpScale",0);m.color=new Color(.075f,.085f,.095f);}
    if(name=="Glass") {
     m.shader=Shader.Find("Standard");m.color=new Color(.16f,.25f,.31f,.60f);m.SetFloat("_Metallic",.12f);m.SetFloat("_Glossiness",.93f);
     m.SetFloat("_Mode",3);m.SetInt("_SrcBlend",(int)BlendMode.One);m.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);m.SetInt("_ZWrite",0);m.EnableKeyword("_ALPHAPREMULTIPLY_ON");m.renderQueue=3000;
    }
    string path=Output+"Material"+i+".mat";AssetDatabase.CreateAsset(m,path);materials[name]=m;
   }
   var imported=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"CarConcept.fbx"));
   var renderers=imported.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   var hood=Array.Find(renderers,r=>r.name=="BodyHood");bool flip=hood!=null&&hood.bounds.center.z<bounds.center.z;
   var rotation=Quaternion.Euler(0,flip?180:0,0);var scale=new Vector3(2.40f/bounds.size.x,1.48f/bounds.size.y,4.40f/bounds.size.z);
   var normalize=Matrix4x4.Scale(scale)*Matrix4x4.Rotate(rotation)*Matrix4x4.Translate(-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
   var root=new GameObject("Detailed Concept Car");var groups=new Dictionary<string,List<CombineInstance>>();var mats=new Dictionary<string,Material>();var pivots=new Dictionary<string,Vector3>();
   foreach(var r in renderers) {
    if(r.name=="License Plate"||r.name=="InteriorSteeringEmblem")continue;
    var mesh=r.GetComponent<MeshFilter>().sharedMesh;string part="Body";Matrix4x4 placement=normalize*r.transform.localToWorldMatrix;
    for(Transform a=r.transform;a!=null;a=a.parent)if(a.name=="WheelFrontL"||a.name=="WheelFrontR"||a.name=="WheelRearL"||a.name=="WheelRearR")
    {
     part=a.name;pivots[part]=normalize.MultiplyPoint3x4(a.position);
     // The source is a posed showroom model: its front axles are turned 30 degrees.
     // Bake neutral steering first, then keep a circular wheel cross-section for spin.
     Vector3 axle=a.TransformDirection(Vector3.right).normalized;
     var neutral=Quaternion.FromToRotation(axle,Vector3.Dot(axle,Vector3.right)>=0?Vector3.right:Vector3.left);
     placement=Matrix4x4.Translate(pivots[part])*Matrix4x4.Scale(new Vector3(scale.x,scale.y,scale.y))*Matrix4x4.Rotate(rotation*neutral)*Matrix4x4.Translate(-a.position)*r.transform.localToWorldMatrix;
     Debug.Log("CONCEPT_NEUTRAL_AXLE "+part+" source="+axle+" neutral="+(neutral*axle));
     break;
    }
    for(int s=0;s<mesh.subMeshCount;s++) {
     string materialName=r.sharedMaterials[Mathf.Min(s,r.sharedMaterials.Length-1)].name.Replace(" (Instance)","");
     if(!materials.TryGetValue(materialName,out var mat))mat=materials["Mechanical"];
     string key=part+"|"+materialName;if(!groups.ContainsKey(key)){groups[key]=new List<CombineInstance>();mats[key]=mat;}
     groups[key].Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=placement});
    }
   }
   var parents=new Dictionary<string,Transform>();int index=0;
   foreach(var group in groups) {
    string part=group.Key.Split('|')[0];if(!parents.ContainsKey(part)){var p=new GameObject(part);p.transform.SetParent(root.transform,false);if(pivots.ContainsKey(part))p.transform.localPosition=pivots[part];parents[part]=p.transform;}
    var list=group.Value;for(int i=0;i<list.Count;i++){var c=list[i];c.transform=Matrix4x4.Translate(-parents[part].localPosition)*c.transform;list[i]=c;}
    var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name=group.Key};mesh.CombineMeshes(list.ToArray());mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Output+"Mesh"+(index++)+".asset");
    var obj=new GameObject(group.Key,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parents[part],false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=mats[group.Key];
   }
   PrefabUtility.SaveAsPrefabAsset(root,Output+"Car.prefab");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(imported);AssetDatabase.SaveAssets();
   Debug.Log("CONCEPT_PREFAB_READY bounds="+bounds+" flipped="+flip+" batches="+index);
  }
 }
}
