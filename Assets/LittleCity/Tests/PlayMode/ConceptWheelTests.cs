using NUnit.Framework;
using UnityEngine;
namespace LittleCity.Tests {
 public sealed class ConceptWheelTests {
  [Test] public void AllFourWheelsHaveStraightAxlesAndDoNotWobbleDuringSpin() {
   var root=Object.Instantiate(Resources.Load<GameObject>("Concept/Car"));int count=0;
   try {
    foreach(Transform wheel in root.transform) {
     if(!wheel.name.StartsWith("Wheel"))continue;count++;
     var start=BoundsInParent(wheel);
     Assert.That(start.size.x,Is.LessThan(start.size.y*.60f),wheel.name+" narrow axle direction");
     Assert.That(Mathf.Abs(start.size.y-start.size.z),Is.LessThan(.035f),wheel.name+" circular tire");
     for(int i=0;i<24;i++) {
      wheel.localRotation=Quaternion.Euler(i*15,0,0);var b=BoundsInParent(wheel);
      Assert.That(Mathf.Abs(b.size.x-start.size.x),Is.LessThan(.004f),wheel.name+" axle stays straight");
      Assert.That(Mathf.Abs(b.size.y-start.size.y),Is.LessThan(.035f),wheel.name+" no vertical wobble");
      Assert.That(Mathf.Abs(b.size.z-start.size.z),Is.LessThan(.035f),wheel.name+" no fore-aft wobble");
      Assert.That(Vector3.Distance(b.center,start.center),Is.LessThan(.025f),wheel.name+" hub stays centered");
     }
    }
    Assert.That(count,Is.EqualTo(4));
   } finally {Object.DestroyImmediate(root);}
  }
  static Bounds BoundsInParent(Transform wheel) {
   bool first=true;var b=new Bounds();
   foreach(var f in wheel.GetComponentsInChildren<MeshFilter>())foreach(var p in f.sharedMesh.vertices) {
    var v=wheel.parent.InverseTransformPoint(f.transform.TransformPoint(p));if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);
   }
   return b;
  }
 }
}
