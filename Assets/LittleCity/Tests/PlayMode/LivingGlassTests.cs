using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LittleCity.Tests
{
 public sealed class LivingGlassTests
 {
  [Test] public void GlassEnvironmentHasBrightWindowsAndDarkContrast()
  {
   var pixels=LivingGlassEnvironment.Texture.GetPixels(CubemapFace.PositiveZ);Assert.That(pixels.Max(c=>c.maxColorComponent),Is.GreaterThan(4));Assert.That(pixels.Min(c=>c.maxColorComponent),Is.LessThan(.5f));
  }
  [UnityTest] public IEnumerator ThirdModePreservesBubblyAndPersistsIndependentlyOfDetail()
  {
   int cozy=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference),glass=PlayerPrefs.GetInt(GraphicsQuality.GlassPreference),detail=PlayerPrefs.GetInt("LittleCity.GraphicsModeV2");
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.enabled=false;g.Hud.BeginExploring();g.Hud.ShowControls(true);
    var buttons=g.Hud.GetComponentsInChildren<Button>(true);
    var bubbly=buttons.Single(b=>b.name=="Style: Cozy button");var original=buttons.Single(b=>b.name=="Style: Original button");var living=buttons.Single(b=>b.name=="Style: Living Glass button");
    bubbly.onClick.Invoke();var body=g.Explorer.Gummy.ElasticBody.GetComponentInChildren<MeshRenderer>();var baseline=body.sharedMaterial;var pigment=baseline.color;
    var position=g.State.Position;living.onClick.Invoke();Assert.That(g.Graphics.LivingGlass,Is.True);Assert.That(g.Graphics.Cozy,Is.True);Assert.That(g.Explorer.Gummy.LivingGlass,Is.True);
    Assert.That(body.sharedMaterial.shader.name,Is.EqualTo("LittleCity/Living Glass"));Assert.That(body.sharedMaterial.shader.isSupported,Is.True);Assert.That(g.Explorer.Gummy.GlowLight.gameObject.activeInHierarchy,Is.True);
    Assert.That(g.State.Position,Is.EqualTo(position));
    foreach(bool high in new[]{true,false}){g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;Assert.That(g.Graphics.LivingGlass,Is.True);}
    bubbly.onClick.Invoke();Assert.That(g.Graphics.LivingGlass,Is.False);Assert.That(body.sharedMaterial,Is.SameAs(baseline));Assert.That(body.sharedMaterial.color,Is.EqualTo(pigment));Assert.That(g.Explorer.Gummy.GlowLight.gameObject.activeInHierarchy,Is.False);
    original.onClick.Invoke();Assert.That(g.Explorer.Gummy.Active,Is.False);Assert.That(g.Explorer.Gummy.Originals.All(r=>!r.forceRenderingOff),Is.True);
    living.onClick.Invoke();yield return SceneManager.LoadSceneAsync("LittleCity");g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
    Assert.That(g.Graphics.LivingGlass,Is.True);Assert.That(g.Explorer.Gummy.LivingGlass,Is.True);Assert.That(g.Graphics.High,Is.False);
    yield return null;Assert.That(g.People.GetComponentsInChildren<GummyCharacter>().All(c=>c.LivingGlass),Is.True);LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,cozy);PlayerPrefs.SetInt(GraphicsQuality.GlassPreference,glass);PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",detail);PlayerPrefs.Save();}
  }
  [UnityTest] public IEnumerator GlassKeepsTheExactBubblyElasticity()
  {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.enabled=false;
   var model=g.Explorer;var soft=model.Gummy;soft.enabled=false;var curves=new float[2][];
   for(int mode=0;mode<2;mode++)
   {
    soft.SetLivingGlass(mode==1);soft.Motion(true,0,false,false);soft.SetStyle(true);model.Pose(0,0,false,false);soft.Motion(false,-6,false,false);soft.Draw(1f/60);soft.Motion(true,0,false,false);curves[mode]=new float[90];
    for(int i=0;i<90;i++){soft.Draw(1f/60);curves[mode][i]=soft.Stretch;}
   }
   CollectionAssert.AreEqual(curves[0],curves[1]);LogAssert.NoUnexpectedReceived();
  }
  [UnityTest] public IEnumerator RenderLivingGlassAndVerifyTransmission()
  {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
   yield return GummyValidation.Capture(g,System.IO.Path.Combine(Application.dataPath,"../Documentation/LivingGlassValidation"),false,true);LogAssert.NoUnexpectedReceived();
  }
 }
}
