using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleCity.Tests
{
    public sealed class BikeScaleTests
    {
        [Test] public void BicycleLayersAreDistinctContinuousAndHaveHeadroom()
        {
            var clips=new AudioClip[3];var data=new float[3][];
            try
            {
                for(int k=0;k<3;k++)
                {
                    clips[k]=BicycleAudio.Create("probe",k);data[k]=new float[clips[k].samples];clips[k].GetData(data[k],0);
                    double sum=0;float peak=0;foreach(float v in data[k]){sum+=v*v;peak=Mathf.Max(peak,Mathf.Abs(v));}
                    Assert.That(System.Math.Sqrt(sum/data[k].Length),Is.InRange(.02,.25),"Audible bed with headroom");
                    Assert.That(peak,Is.LessThan(.85f));
                    Assert.That(Mathf.Abs(data[k][0]-data[k][data[k].Length-1]),Is.LessThan(.10f),"No large loop seam");
                }
                for(int k=1;k<3;k++){double diff=0;for(int i=0;i<data[0].Length;i++)diff+=System.Math.Abs(data[0][i]-data[k][i]);Assert.That(diff/data[0].Length,Is.GreaterThan(.035));}
            }
            finally{foreach(var c in clips)if(c!=null)Object.DestroyImmediate(c);}
        }

        [UnityTest] public IEnumerator TallRiderAndMiniBmxKeepContactsAndStyleSwitching()
        {
            int character=PlayerPrefs.GetInt("LittleCity.Character"),cozy=PlayerPrefs.GetInt(GraphicsQuality.CozyPreference),glass=PlayerPrefs.GetInt(GraphicsQuality.GlassPreference);
            try
            {
                yield return SceneManager.LoadSceneAsync("LittleCity");var g=Object.FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
                g.Hud.BeginExploring();g.enabled=false;g.SelectCharacter(3);g.State.Place(new Point(-300,0,-300));g.RefreshPlayer();
                Assert.That(g.State.WalkingScale,Is.EqualTo(1.65f));
                foreach(int style in new[]{0,1,2})
                {
                    if(style==2)g.Graphics.SetLivingGlass(true);else g.Graphics.SetCozy(style==1);
                    Assert.That(g.Explorer.Root.localScale.y,Is.EqualTo(1.65f));
                    g.SelectRide(Ride.MiniBike);yield return null;var m=g.CurrentVehicle;
                    Assert.That(m.Wheels[0].position.y-g.Explorer.Root.parent.position.y,Is.EqualTo(.29f*.52f).Within(.02f));
                    Assert.That(m.Grips[0].y-m.Seat.y,Is.GreaterThan(1.5f),"Very tall handlebars above a low saddle");
                    Assert.That(m.Wheels[1].localPosition.z-m.Wheels[0].localPosition.z,Is.LessThan(1.2f),"Short frame");
                    foreach(bool high in new[]{false,true})
                    {
                        g.Graphics.SetHigh(high);while(g.Graphics.Busy)yield return null;
                        foreach(bool standing in new[]{false,true})for(int frame=0;frame<30;frame++)
                        {
                            m.Animate(6,.033f,0);g.Explorer.FitRide(m,standing);g.Explorer.Gummy.Draw(0);
                            Assert.That(g.Explorer.HandError,Is.LessThan(.035f));Assert.That(g.Explorer.FootError,Is.LessThan(.035f));
                        }
                    }
                    g.SelectRide(Ride.Walk);yield return null;Assert.That(g.Explorer.Root.localScale.y,Is.EqualTo(1.65f),"Towering height returns after riding");
                }
                g.State.Place(new Point(13,0,-42.2f));g.Interact();yield return null;g.RefreshPlayer();
                Assert.That(g.State.Indoors,Is.True);Assert.That(g.Explorer.Root.localScale.y,Is.LessThan(1),"Room clearance");
                g.Buildings.Leave();g.RefreshPlayer();Assert.That(g.Explorer.Root.localScale.y,Is.EqualTo(1.65f));
                yield return BikeScaleValidation.Capture(g,System.IO.Path.Combine(Application.dataPath,"../Documentation/BikeScaleValidation/Editor"));
                LogAssert.NoUnexpectedReceived();
            }
            finally{PlayerPrefs.SetInt("LittleCity.Character",character);PlayerPrefs.SetInt(GraphicsQuality.CozyPreference,cozy);PlayerPrefs.SetInt(GraphicsQuality.GlassPreference,glass);PlayerPrefs.Save();}
        }
    }
}
