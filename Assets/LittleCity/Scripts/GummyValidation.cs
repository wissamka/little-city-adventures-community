using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LittleCity
{
 public sealed class GummyValidation:MonoBehaviour
 {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-gummy")>=0 || Array.IndexOf(Environment.GetCommandLineArgs(),"--validate-glass")>=0)new GameObject("Bubbly validation").AddComponent<GummyValidation>();}
  float began;bool finished;
  void Awake(){began=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;}
  void Update(){if(!finished && Time.realtimeSinceStartup-began>180){finished=true;Debug.LogError("GUMMY_RUNTIME_FAILED timeout");Application.Quit(2);}}
  void Log(string message,string stack,LogType type){if(type==LogType.Exception && !finished){finished=true;Debug.LogError("GUMMY_RUNTIME_FAILED "+message);Application.Quit(2);}}
  void OnDestroy(){Application.logMessageReceived-=Log;}
  IEnumerator Start()
  {
   var g=FindFirstObjectByType<CityGame>();while(!g.Ready||g.Graphics.Busy)yield return null;
   var args=Environment.GetCommandLineArgs();bool living=Array.IndexOf(args,"--validate-glass")>=0;yield return Capture(g,args[Array.IndexOf(args,living?"--glass-output":"--gummy-output")+1],true,living);
   finished=true;Debug.Log("GUMMY_RUNTIME_PASSED");Application.Quit(0);
  }
  public static IEnumerator Capture(CityGame g,string folder,bool animation,bool livingGlass=false)
  {
   Directory.CreateDirectory(folder);g.enabled=false;g.People.enabled=false;
   int sunMask=RenderSettings.sun.cullingMask;RenderSettings.sun.cullingMask &= ~(1<<9);
   var studio=new GameObject("Bubbly preview studio");var cam=new GameObject("Bubbly studio camera").AddComponent<Camera>();cam.transform.SetParent(studio.transform);cam.enabled=false;cam.cullingMask=1<<9;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Art.Hex("F2EBDD");cam.orthographic=true;cam.orthographicSize=3.25f;cam.nearClipPlane=.1f;cam.farClipPlane=40;
   cam.transform.position=new Vector3(1000,4.2f,14);cam.transform.LookAt(new Vector3(1000,1.35f,0));
   var light=new GameObject("Rubber studio softbox").AddComponent<Light>();light.transform.SetParent(studio.transform);light.type=LightType.Directional;light.intensity=1.1f;light.color=new Color(1,.94f,.85f);light.cullingMask=1<<9;light.transform.rotation=Quaternion.Euler(35,210,0);light.shadows=LightShadows.Soft;
   var floor=Art.Box("Studio floor",studio.transform,Vector3.zero,new Vector3(24,.06f,12),Art.Hex("E8E0D3"),0);floor.transform.position=new Vector3(1000,-.07f,0);floor.layer=9;var floorMat=new Material(Resources.Load<Shader>("GummyRubber")){color=Art.Hex("D4C8B3")};floorMat.SetFloat("_Glossiness",.2f);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
   var models=new List<CharacterModel>();
   for(int i=0;i<6;i++)
   {
    var model=Models.Character(studio.transform,i);model.Root.position=new Vector3(1000+(i-2.5f)*2.15f,0,0);model.Root.rotation=Quaternion.Euler(0,-8,0);model.Gummy.enabled=false;
    model.Root.localScale=Vector3.one*(i==4?1.18f:i==3?1.07f:1);
    foreach(var t in model.Root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;models.Add(model);
   }
   bool fog=RenderSettings.fog;RenderSettings.fog=false;
   foreach(var model in models){model.Pose(0,0,false,false);model.Gummy.SetStyle(false);}
   yield return null;Save(cam,folder,"lineup-original",1440,700);
   foreach(var model in models){model.Gummy.SetLivingGlass(livingGlass);model.Gummy.SetStyle(true);model.Gummy.Draw(0);}
   yield return null;Save(cam,folder,"lineup-bubbly",1440,700);
   for(int i=0;i<6;i++)
   {
    foreach(var m in models)m.Root.gameObject.SetActive(m==models[i]);var model=models[i];
    cam.orthographicSize=1.58f;cam.transform.position=model.Root.position+new Vector3(2.2f,2.2f,6);cam.transform.LookAt(model.Root.position+Vector3.up*1.2f);
    model.Gummy.Draw(0);Save(cam,folder,Simulation.CharacterNames[i]+"-bubbly",800,900);
   }
   yield return CheckGel(models,cam,light,floor,folder);
   foreach(var m in models)m.Root.gameObject.SetActive(false);
   var hero=models[5];hero.Root.gameObject.SetActive(true);cam.orthographicSize=2.25f;var origin=hero.Root.position;cam.transform.position=origin+new Vector3(2.5f,2.6f,7);cam.transform.LookAt(origin+Vector3.up*1.8f);
   if(animation)Directory.CreateDirectory(Path.Combine(folder,"frames"));
   float min=1,max=1;const float dt=1f/30;
   for(int i=0;i<135;i++)
   {
    float t=i*dt,jump=t-1.4f,height=jump>0&&jump<.86f?6*jump-7*jump*jump:0,vertical=height>0?6-14*jump:0;
    hero.Root.position=origin+Vector3.up*Mathf.Max(0,height)*.66f;hero.Pose(t,t<1.4f?2.7f:0,false,false);hero.Gummy.Motion(height<=0,vertical,false,false);hero.Gummy.Draw(dt);
    if(t>2.25f&&t<3){min=Mathf.Min(min,hero.Gummy.Stretch);max=Mathf.Max(max,hero.Gummy.Stretch);}
    if(animation)Save(cam,Path.Combine(folder,"frames"),i.ToString("D4"),720,720);
    if(i==48||i==71||i==77)Save(cam,folder,"motion-"+i,720,720);
    if(i%10==0)yield return null;
   }
   if(min>.85f || max<1.03f)throw new Exception("Bubbly landing did not visibly squash and rebound: "+min+" / "+max);
   File.WriteAllText(Path.Combine(folder,"motion-results.txt"),"All six characters rendered in Original and Bubbly styles.\nLanding stretch min="+min+" rebound max="+max+"\n");
   RenderSettings.fog=fog;RenderSettings.sun.cullingMask=sunMask;Destroy(floorMat);Destroy(studio);yield return null;
  }
  static IEnumerator CheckGel(List<CharacterModel> models,Camera cam,Light light,GameObject floor,string folder)
  {
   var hero=models[5];foreach(var m in models)m.Root.gameObject.SetActive(m==hero);
   hero.Pose(0,0,false,false);hero.Gummy.Draw(0);cam.orthographicSize=1.58f;cam.transform.position=hero.Root.position+new Vector3(0,1.3f,6);cam.transform.LookAt(hero.Root.position+Vector3.up*1.3f);
   var mats=hero.Gummy.Visual.GetComponentsInChildren<Renderer>().Select(r=>r.sharedMaterial).Where(m=>m.shader.name=="LittleCity/Translucent Gummy" || m.shader.name=="LittleCity/Living Glass").Distinct().ToArray();
   if(mats.Length==0 || mats.Any(m=>!m.shader.isSupported))throw new Exception("Translucent gummy shader missing or unsupported");
   floor.SetActive(false);var gel=Resources.Load<Shader>(hero.Gummy.LivingGlass?"GummyLivingGlass":"GummyGel");float gloss=mats[0].GetFloat("_Glossiness");var opaque=Resources.Load<Shader>("GummyRubber");
   var point=cam.WorldToViewportPoint(hero.Root.TransformPoint(new Vector3(0,.9f,0)));
   cam.backgroundColor=new Color(.85f,.2f,.1f);yield return null;var warm=Save(cam,folder,"gel-warm-background",800,900);
   cam.backgroundColor=new Color(.08f,.35f,.85f);yield return null;var cool=Save(cam,folder,"gel-cool-background",800,900);
   float transmitted=SampleDifference(warm,cool,point,800,900);
   foreach(var m in mats){m.shader=opaque;m.renderQueue=2020;}
   cam.backgroundColor=new Color(.85f,.2f,.1f);yield return null;warm=Save(cam,folder,"opaque-warm-background",800,900);
   cam.backgroundColor=new Color(.08f,.35f,.85f);yield return null;cool=Save(cam,folder,"opaque-cool-background",800,900);
   float solid=SampleDifference(warm,cool,point,800,900);
   if(transmitted<.035f || solid>.01f)throw new Exception("Transmission pixel check failed: gel="+transmitted+" opaque="+solid);
   foreach(var m in mats){m.shader=gel;m.renderQueue=-1;}
   floor.SetActive(true);cam.backgroundColor=Art.Hex("F2EBDD");
   var backdrop=new GameObject("Transmission stripe backdrop");backdrop.transform.SetParent(floor.transform.parent);
   for(int i=0;i<9;i++)
   {
    var stripe=Art.Box("Transmission stripe",backdrop.transform,Vector3.zero,new Vector3(.45f,4,.08f),i%2==0?Art.Hex("436D83"):Art.Hex("F3DCB5"),0);stripe.transform.position=hero.Root.position+new Vector3((i-4)*.45f,1.5f,-1.2f);stripe.layer=9;
   }
   yield return null;Save(cam,folder,"gel-refraction-day",800,900);
   foreach(var m in mats){m.shader=opaque;m.renderQueue=2020;m.SetFloat("_Glossiness",.6f);}
   yield return null;Save(cam,folder,"rubber-before",800,900);
   foreach(var m in mats){m.shader=gel;m.renderQueue=-1;m.SetFloat("_Glossiness",gloss);}
   var ambient=RenderSettings.ambientLight;float reflection=RenderSettings.reflectionIntensity;var rotation=light.transform.rotation;float intensity=light.intensity;
   backdrop.SetActive(false);RenderSettings.ambientLight=new Color(.16f,.14f,.12f);light.transform.rotation=Quaternion.Euler(18,10,0);light.intensity=1.5f;
   yield return null;Save(cam,folder,"gel-backlit",800,900);
   light.transform.rotation=rotation;light.intensity=.16f;RenderSettings.ambientLight=new Color(.23f,.25f,.36f);RenderSettings.reflectionIntensity=.3f;cam.backgroundColor=new Color(.11f,.12f,.20f);
   yield return null;Save(cam,folder,"gel-night",800,900);
   if(hero.Gummy.LivingGlass)
   {
    hero.Gummy.SetLivingGlass(false);yield return null;Save(cam,folder,"bubbly-night-comparison",800,900);
    hero.Gummy.SetLivingGlass(true);
   }
   light.intensity=intensity;RenderSettings.ambientLight=ambient;RenderSettings.reflectionIntensity=reflection;cam.backgroundColor=Art.Hex("F2EBDD");
   if(hero.Gummy.LivingGlass)
   {
    hero.Gummy.SetLivingGlass(false);yield return null;Save(cam,folder,"bubbly-day-comparison",800,900);
    hero.Gummy.SetLivingGlass(true);yield return null;Save(cam,folder,"living-glass-day",800,900);
   }
   hero.Root.localRotation=Quaternion.Euler(0,172,0);yield return null;Save(cam,folder,"gel-rear",800,900);hero.Root.localRotation=Quaternion.Euler(0,-8,0);
   Destroy(backdrop);yield return null;
   File.WriteAllText(Path.Combine(folder,"gel-results.txt"),"PASS Supported translucent shader on body and limbs\nPASS Background transmission changes belly pixels: "+transmitted+"\nPASS Opaque control remains unchanged: "+solid+"\nDay, night, backlit, rear and matte/translucent comparison views rendered.\n");
  }
  static float SampleDifference(Color32[] a,Color32[] b,Vector3 point,int width,int height)
  {
   float delta=0;int cx=Mathf.RoundToInt(point.x*width),cy=Mathf.RoundToInt(point.y*height);
   for(int y=cy-3;y<=cy+3;y++)for(int x=cx-3;x<=cx+3;x++){int i=y*width+x;delta+=(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b))/765f;}
   return delta/49;
  }
  static Color32[] Save(Camera camera,string folder,string name,int width,int height)
  {
   camera.aspect=width/(float)height;
   var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};rt.Create();var before=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());var pixels=t.GetPixels32();camera.targetTexture=null;RenderTexture.active=before;Destroy(t);rt.Release();Destroy(rt);return pixels;
  }
 }
}
