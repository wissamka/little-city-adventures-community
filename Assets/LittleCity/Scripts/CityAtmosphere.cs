using System.Collections.Generic;
using UnityEngine;
namespace LittleCity {
 public sealed class CityAtmosphere:MonoBehaviour {
  public static CityAtmosphere Current{get;private set;}public bool Night{get;private set;}public int Glow{get;private set;}
  public string GlowName=>new[]{"off","cyan","pink","amber"}[Glow];
  CityGame game;Transform world,lightsRoot,carRoot,effects;readonly List<Light> street=new List<Light>();Light under;GameObject glowPlane;Material glowMaterial,lampMaterial,tailMaterial;Light[] headlights;GameObject jets;float scan;
  public void Initialize(CityGame owner){Current=this;game=owner;Night=PlayerPrefs.GetInt("LittleCity.Night",0)==1;Glow=PlayerPrefs.GetInt("LittleCity.Underglow",0)%4;game.State.NitroInstalled=PlayerPrefs.GetInt("LittleCity.NOS",0)==1;}
  public void ToggleNight(){Night=!Night;PlayerPrefs.SetInt("LittleCity.Night",Night?1:0);PlayerPrefs.Save();game.Graphics.RefreshLighting();game.State.Say(Night?"Night drive · city lights are on":"Daylight");}
  public void CycleGlow(){Glow=(Glow+1)%4;PlayerPrefs.SetInt("LittleCity.Underglow",Glow);PlayerPrefs.Save();}
  public void ToggleNitro(){game.State.NitroInstalled=!game.State.NitroInstalled;game.State.CancelNitro();PlayerPrefs.SetInt("LittleCity.NOS",game.State.NitroInstalled?1:0);PlayerPrefs.Save();game.State.Say(game.State.NitroInstalled?"NOS fitted · hold Option / Alt or NOS while accelerating":"NOS removed");}
  public void Apply() {
   Shader.SetGlobalFloat("_CityNight",Night?1:0);QualitySettings.pixelLightCount=Night?4:1;
   if(Night){RenderSettings.ambientLight=new Color(.16f,.20f,.31f);RenderSettings.sun.intensity=.12f;RenderSettings.sun.color=new Color(.62f,.74f,1);RenderSettings.fogColor=new Color(.035f,.06f,.12f);RenderSettings.reflectionIntensity=.35f;}
   else if(game!=null)RenderSettings.fogColor=Art.Hex(game.State.WorldIndex==0?"7FADB7":game.State.WorldIndex==1?"A2DCE5":game.State.WorldIndex==2?"CADFEF":"ADD8E8");
   if(lightsRoot!=null)lightsRoot.gameObject.SetActive(Night);
   game?.Graphics?.ApplyCozyLighting();
  }
  void BuildStreetLights() {
   if(lightsRoot!=null)Destroy(lightsRoot.gameObject);street.Clear();world=game.Worlds.ActivePlace.Root;
   lightsRoot=Art.Group("Night street lights",world);lightsRoot.gameObject.AddComponent<MovingScenery>();
   if(lampMaterial==null){lampMaterial=new Material(Resources.Load<Shader>("NightLamp")){color=new Color(1,.75f,.36f)};tailMaterial=new Material(Resources.Load<Shader>("NightLamp")){color=new Color(1,.025f,.012f)};}
   var locations=new List<Vector3>();foreach(var t in world.GetComponentsInChildren<Transform>(true))if(t.name=="Lamp globe")locations.Add(t.position);
   if(game.State.WorldIndex==3){var tag=world.GetComponent<GeneratedWorldTag>();if(tag!=null&&tag.Theme==(int)WorldTheme.NewYork)for(int x=-170;x<=100;x+=45)for(int z=-120;z<=135;z+=102){var p=new Vector3(x+8,5.3f,z+8);locations.Add(p);Art.Tube("Night avenue lamp post",lightsRoot,new Vector3(p.x,0,p.z),p,.085f,Art.Ink);}}
   foreach(var p in locations){var globe=Art.Ball("Warm lamp",lightsRoot,p,Vector3.one*.72f,Color.white);globe.GetComponent<Renderer>().sharedMaterial=lampMaterial;var light=globe.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.69f,.31f);light.range=17;light.intensity=2.1f;light.shadows=LightShadows.None;street.Add(light);}
   lightsRoot.gameObject.SetActive(Night);Apply();
  }
  void BuildCarEffects(Transform root) {
   if(effects!=null)Destroy(effects.gameObject);carRoot=root;if(root==null)return;effects=Art.Group("Car lights and NOS",root);
   headlights=new Light[2];for(int i=0;i<2;i++){var o=new GameObject("Working headlight");o.transform.SetParent(effects,false);o.transform.localPosition=new Vector3(i==0?-.7f:.7f,.76f,2.05f);o.transform.localRotation=Quaternion.Euler(6,0,0);var l=o.AddComponent<Light>();l.type=LightType.Spot;l.range=32;l.spotAngle=58;l.intensity=3;l.color=new Color(.82f,.92f,1);l.shadows=LightShadows.None;headlights[i]=l;var tail=Art.Ball("Night tail light",effects,new Vector3(i==0?-.7f:.7f,.78f,-2.16f),new Vector3(.26f,.07f,.04f),Color.red);tail.GetComponent<Renderer>().sharedMaterial=tailMaterial;}
   glowPlane=GameObject.CreatePrimitive(PrimitiveType.Quad);glowPlane.name="Soft ground underglow";Destroy(glowPlane.GetComponent<Collider>());glowPlane.transform.SetParent(effects,false);glowPlane.transform.localPosition=new Vector3(0,.095f,0);glowPlane.transform.localRotation=Quaternion.Euler(90,0,0);glowPlane.transform.localScale=new Vector3(3.4f,5.4f,1);
   if(glowMaterial==null)glowMaterial=new Material(Resources.Load<Shader>("Underglow"));glowPlane.GetComponent<Renderer>().sharedMaterial=glowMaterial;
   var u=new GameObject("Undercar light");u.transform.SetParent(effects,false);u.transform.localPosition=new Vector3(0,.2f,0);under=u.AddComponent<Light>();under.type=LightType.Point;under.range=4;under.intensity=1.7f;under.shadows=LightShadows.None;
   jets=Art.Group("Blue NOS exhaust",effects).gameObject;for(int i=-1;i<=1;i+=2)Art.Ball("Boost flame",jets.transform,new Vector3(i*.3f,.45f,-2.45f),new Vector3(.17f,.16f,.6f),Art.Hex("53DFFF"));
  }
  void LateUpdate() {
   if(game==null||!game.Ready||game.Worlds==null||game.Worlds.ActivePlace==null||game.Worlds.ActivePlace.Root==null||Camera.main==null)return;
   if(world!=game.Worlds.ActivePlace.Root)BuildStreetLights();
   var v=game.State.Mode==Ride.RaceCar?game.CurrentVehicle:null;var root=v==null?null:v.Root;if(root!=carRoot)BuildCarEffects(root);
   scan-=Time.unscaledDeltaTime;if(scan<=0){scan=.5f;var p=Camera.main.transform.position;street.Sort((a,b)=>(a.transform.position-p).sqrMagnitude.CompareTo((b.transform.position-p).sqrMagnitude));for(int i=0;i<street.Count;i++)street[i].enabled=Night&&i<8;}
   if(effects==null)return;
   for(int i=0;i<2;i++)headlights[i].enabled=Night;
   var color=Glow==2?new Color(1,.07f,.47f,.7f):Glow==3?new Color(1,.48f,.03f,.7f):new Color(.02f,.78f,1,.7f);
   glowMaterial.color=color;under.color=color;under.enabled=Glow>0;glowPlane.SetActive(Glow>0);
   jets.SetActive(game.State.NitroActive&&!game.Hud.MenuOpen&&Application.isFocused);
  }
  void OnDestroy(){if(Current==this){Current=null;Shader.SetGlobalFloat("_CityNight",0);}if(glowMaterial!=null)Destroy(glowMaterial);if(lampMaterial!=null)Destroy(lampMaterial);if(tailMaterial!=null)Destroy(tailMaterial);}
 }
}
