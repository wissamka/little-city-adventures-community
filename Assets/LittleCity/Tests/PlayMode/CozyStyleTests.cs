using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace LittleCity.Tests
{
 public sealed class CozyStyleTests
 {
  [UnityTest] public IEnumerator StyleButtonsPersistAcrossReloadAndKeepDetailIndependent()
  {
   int saved=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference,0),detail=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0);
   try
   {
    bool background=Application.runInBackground;PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,0);
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;
    yield return null;Assert.That(Application.runInBackground,Is.EqualTo(background),"Loading restores normal background policy");var before=g.State.Position;g.Hud.ShowControls(true);
    var cozy=g.Hud.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Style: Cozy button");
    var original=g.Hud.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Style: Original button");
    cozy.onClick.Invoke();Assert.That(g.Graphics.Cozy,Is.True);Assert.That(Shader.GetGlobalFloat("_CozyStyle"),Is.EqualTo(1));
    Assert.That(g.State.Position.X,Is.EqualTo(before.X));Assert.That(g.State.Position.Z,Is.EqualTo(before.Z));
    foreach(bool high in new[]{true,false}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Assert.That(g.Graphics.Cozy,Is.True);Assert.That(g.Graphics.High,Is.EqualTo(high));}
    original.onClick.Invoke();Assert.That(g.Graphics.Cozy,Is.False);Assert.That(Shader.GetGlobalFloat("_CozyStyle"),Is.Zero);
    cozy.onClick.Invoke();yield return SceneManager.LoadSceneAsync("LittleCity");g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
    Assert.That(g.Graphics.Cozy,Is.True);Assert.That(g.Graphics.High,Is.False);Assert.That(PlayerPrefs.GetInt(GraphicsQuality.CozyPreference),Is.EqualTo(1));LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,saved);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",detail);PlayerPrefs.Save();}
  }
  [UnityTest] public IEnumerator FurnitureRoundsInsideStaticBatchesAndRestoresWithoutExtraDraws()
  {
   int saved=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference,0);
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.enabled=false;g.Graphics.SetCozy(false);
    var root=Art.Group("Cozy geometry check",g.transform);Art.Box("Sofa seat",root,Vector3.zero,new Vector3(3,.8f,1.4f),Art.Teal,0);Art.CombineStatic(root);yield return null;
    var renderer=root.GetComponentsInChildren<MeshRenderer>().Single();var filter=renderer.GetComponent<MeshFilter>();var original=filter.sharedMesh;var bounds=original.bounds;int draws=root.GetComponentsInChildren<MeshRenderer>().Length;
    g.Graphics.SetCozy(true);Assert.That(filter.sharedMesh,Is.Not.SameAs(original));Assert.That(filter.sharedMesh.vertexCount,Is.GreaterThan(original.vertexCount));Assert.That(Vector3.Distance(filter.sharedMesh.bounds.size,bounds.size),Is.LessThan(.001f));Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(draws));
    Assert.That(filter.sharedMesh.normals.Any(n=>Mathf.Abs(n.x)>.1f && Mathf.Abs(n.y)>.1f),Is.True,"Smooth curved edge normals");
    g.Graphics.SetCozy(false);Assert.That(filter.sharedMesh,Is.SameAs(original));Object.Destroy(root.gameObject);yield return null;LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,saved);PlayerPrefs.Save();}
  }
  [UnityTest] public IEnumerator CozyCoversTravelNewMaterialsVehiclesAndDayNightWithoutChangingPlay()
  {
   int saved=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference,0),character=PlayerPrefs.GetInt("LittleCity.Character",0),night=PlayerPrefs.GetInt("LittleCity.Night",0);
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;g.Graphics.SetCozy(true);
    foreach(int world in new[]{0,1,2,4})
    {
     g.Worlds.VisitWorld(world);yield return new WaitForSecondsRealtime(.65f);Assert.That(g.Graphics.Cozy,Is.True);Assert.That(Shader.GetGlobalFloat("_CozyStyle"),Is.EqualTo(1));
     Assert.That(g.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).All(m=>m.shader.isSupported),Is.True,"Shaders supported in world "+world);
     var temp=Art.Box("New cozy wood cabinet",g.transform,Vector3.zero,Vector3.one,Art.Red);yield return new WaitForSecondsRealtime(.65f);
     Assert.That(temp.GetComponent<Renderer>().sharedMaterial.shader.name,Is.EqualTo("LittleCity/HighDefinition"));Object.Destroy(temp);
    }
    g.Worlds.VisitWorld(0);g.SelectCharacter(5);
    foreach(var ride in new[]{Ride.RaceCar,Ride.Bicycle,Ride.Camper,Ride.Airplane,Ride.Helicopter})
    {
     g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();Assert.That(g.State.Mode,Is.EqualTo(ride));Assert.That(g.Graphics.Cozy,Is.True);
     var head=g.Explorer.Head.position;float error=g.Explorer.HandError;g.Graphics.SetCozy(false);g.Graphics.SetCozy(true);Assert.That(g.Explorer.Head.position,Is.EqualTo(head),ride+" pose unchanged");Assert.That(g.Explorer.HandError,Is.EqualTo(error),ride+" contacts unchanged");
    }
    if(CityAtmosphere.Current.Night)CityAtmosphere.Current.ToggleNight();float day=RenderSettings.sun.intensity;
    CityAtmosphere.Current.ToggleNight();Assert.That(RenderSettings.sun.intensity,Is.LessThan(day));Assert.That(g.Graphics.Cozy,Is.True);
    g.Graphics.SetCozy(false);Assert.That(RenderSettings.sun.intensity,Is.EqualTo(.12f).Within(.001f));CityAtmosphere.Current.ToggleNight();Assert.That(RenderSettings.sun.intensity,Is.EqualTo(1.4f).Within(.001f));LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,saved);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.SetInt("LittleCity.Night",night);PlayerPrefs.Save();}
  }
 }
}
