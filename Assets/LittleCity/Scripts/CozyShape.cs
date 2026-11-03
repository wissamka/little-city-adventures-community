using System.Collections.Generic;
using UnityEngine;
namespace LittleCity
{
 // Visual-only alternatives retain the same object bounds and collision model.
 // Static scenery combines both versions into the same material batches.
 public sealed class CozyShape:MonoBehaviour
 {
  static readonly HashSet<CozyShape> shapes=new HashSet<CozyShape>();
  MeshFilter filter;Mesh original;public Mesh Soft{get;private set;}bool ownsSoft;
  public static int Count=>shapes.Count;
  public void Initialize(Mesh normal,Mesh soft,bool owns=false)
  {
   filter=GetComponent<MeshFilter>();original=normal;Soft=soft;ownsSoft=owns;shapes.Add(this);
   Apply(GraphicsQuality.Current!=null&&GraphicsQuality.Current.Cozy);
  }
  public static void SetAll(bool cozy){foreach(var shape in shapes)if(shape!=null)shape.Apply(cozy);}
  public void Apply(bool cozy){if(filter!=null)filter.sharedMesh=cozy?Soft:original;}
  public Mesh Original=>original;
  public static bool Furniture(string name)
  {
   string n=name.ToLowerInvariant();return n.Contains("sofa")||n.Contains("pillow")||n.Contains("duvet")||n.Contains("bed frame")||n.Contains("coffee table")||n.Contains("dining table")||n.Contains("dining chair")||n.Contains("cabinet")||n.Contains("bookcase")||n.Contains("linen seat")||n.Contains("picnic bench");
  }
  void OnDestroy(){shapes.Remove(this);if(ownsSoft&&Soft!=null)Destroy(Soft);}
 }
}
