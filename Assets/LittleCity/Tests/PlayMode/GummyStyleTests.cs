using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleCity.Tests
{
 public sealed class GummyStyleTests
 {
  [UnityTest] public IEnumerator AllCharactersChangeSilhouetteAndRestoreTheirOriginalRenderers()
  {
   int saved=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference),character=PlayerPrefs.GetInt("LittleCity.Character");
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.enabled=false;
    for(int i=0;i<6;i++)
    {
     g.SelectCharacter(i);g.SelectRide(Ride.Walk);g.Graphics.SetCozy(true);var model=g.Explorer;var soft=model.Gummy;
     Assert.That(soft.Active,Is.True);Assert.That(soft.Originals.All(r=>r.forceRenderingOff),Is.True,"Original renderers hidden for "+i);
     Assert.That(soft.Visual.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.shader.isSupported),Is.True);
     var mesh=soft.ElasticBody.GetComponentInChildren<MeshFilter>().sharedMesh;Assert.That(mesh.bounds.size.x,Is.GreaterThan(1.3f),"Wide continuous blob silhouette");
     Assert.That(soft.Visual.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True,"No added gameplay collisions");
     model.SetSleeping(true);soft.Draw(0);Assert.That(soft.Originals.All(r=>r.forceRenderingOff),Is.True,"Sleeping cannot reveal original skin");model.SetSleeping(false);
     g.Graphics.SetCozy(false);Assert.That(soft.Active,Is.False);Assert.That(soft.Originals.All(r=>!r.forceRenderingOff),Is.True);
     Assert.That(model.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true), Is.Empty);
    }
    g.Graphics.SetCozy(true);yield return null;Assert.That(g.People.GetComponentsInChildren<GummyCharacter>().All(c=>c.Active),Is.True,"Neighborhood people share style");
    LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,saved);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.Save();}
  }
  [UnityTest] public IEnumerator LandingSquashesReboundsAndSettlesWithoutMovingTheContactRig()
  {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.enabled=false;
   var model=g.Explorer;var soft=model.Gummy;soft.enabled=false;soft.SetStyle(true);model.Pose(0,0,false,false);var root=model.Root.position;var feet=model.Ends.Select(t=>t.position).ToArray();
   soft.Motion(false,-6,false,false);soft.Draw(1f/60);soft.Motion(true,0,false,false);
   float min=1,max=1;
   for(int i=0;i<150;i++){soft.Draw(1f/60);if(i<40)min=Mathf.Min(min,soft.Stretch);if(i>10)max=Mathf.Max(max,soft.Stretch);}
   Assert.That(min,Is.LessThan(.8f),"Visible landing squash");Assert.That(max,Is.GreaterThan(1.04f),"Elastic rebound");Assert.That(soft.Stretch,Is.EqualTo(1).Within(.002f),"Spring settles");
   Assert.That(model.Root.position,Is.EqualTo(root));for(int i=0;i<4;i++)Assert.That(model.Ends[i].position,Is.EqualTo(feet[i]));
   foreach(float dt in new[]{1f/120,1f/30,.1f})
   {soft.SetStyle(true);soft.Motion(false,-12,false,false);soft.Draw(dt);soft.Motion(true,0,false,false);for(int i=0;i<100;i++){soft.Draw(dt);Assert.That(soft.Stretch,Is.InRange(.7f,1.25f));}}
   LogAssert.NoUnexpectedReceived();
  }
  [UnityTest] public IEnumerator BubblyHandsAndFeetStayOnVehicleControls()
  {
   int saved=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference),character=PlayerPrefs.GetInt("LittleCity.Character");
   try
   {
    yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;g.Hud.BeginExploring();g.enabled=false;g.Graphics.SetCozy(true);
    for(int i=0;i<6;i++)foreach(var ride in new[]{Ride.RaceCar,Ride.Bicycle,Ride.Motorcycle,Ride.Camper})
    {
     g.SelectCharacter(i);g.SelectRide(ride);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();var model=g.Explorer;model.Gummy.Draw(0);
     for(int side=0;side<2;side++){Assert.That(Vector3.Distance(model.Gummy.Hands[side].position,model.Ends[side+2].position),Is.LessThan(.001f),i+" "+ride+" hand");Assert.That(Vector3.Distance(model.Gummy.Feet[side].position,model.Ends[side].position),Is.LessThan(.001f),i+" "+ride+" foot");}
    }
    LogAssert.NoUnexpectedReceived();
   }
   finally{PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,saved);PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.Save();}
  }
  [UnityTest] public IEnumerator RenderActualBubblyCharactersAndMotion()
  {
   yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
   yield return GummyValidation.Capture(g,System.IO.Path.Combine(Application.dataPath,"../Documentation/GelValidation"),false);
   LogAssert.NoUnexpectedReceived();
  }
 }
}
