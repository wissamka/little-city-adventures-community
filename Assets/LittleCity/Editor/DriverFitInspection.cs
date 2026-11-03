using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace LittleCity.Editor
{
 public static class DriverFitInspection
 {
  public static void Inspect()
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LittleCity/ConceptSource/CarConcept.fbx"));var renderers=root.GetComponentsInChildren<MeshRenderer>();var all=renderers[0].bounds;foreach(var r in renderers)all.Encapsulate(r.bounds);
   var hood=Array.Find(renderers,r=>r.name=="BodyHood");bool flip=hood!=null&&hood.bounds.center.z<all.center.z;
   var normalize=Matrix4x4.Scale(new Vector3(2.40f/all.size.x,1.48f/all.size.y,4.40f/all.size.z))*Matrix4x4.Rotate(Quaternion.Euler(0,flip?180:0,0))*Matrix4x4.Translate(-new Vector3(all.center.x,all.min.y,all.center.z));
   var lines=new List<string>();var seatPoints=new List<string>();
   foreach(var r in renderers)
   {
    var f=r.GetComponent<MeshFilter>();var m=normalize*f.transform.localToWorldMatrix;var b=new Bounds();bool first=true;
    foreach(var v in f.sharedMesh.vertices){var p=m.MultiplyPoint3x4(v);if(r.name.StartsWith("InteriorSeats"))seatPoints.Add(r.name+","+p.x+","+p.y+","+p.z);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
    lines.Add(r.name+" min="+b.min.ToString("F4")+" max="+b.max.ToString("F4")+" center="+b.center.ToString("F4"));
   }
   Directory.CreateDirectory("Documentation/DriverFit");File.WriteAllLines("Documentation/DriverFit/source-part-bounds.txt",lines);File.WriteAllLines("Documentation/DriverFit/seat-vertices.csv",seatPoints);UnityEngine.Object.DestroyImmediate(root);Debug.Log("DRIVER_GEOMETRY_INSPECTED");
  }
 }
}
