using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleCity
{
    public sealed class GraphicsQuality : MonoBehaviour
    {
        public static GraphicsQuality Current {get;private set;}
        public bool High {get;private set;}
        public bool Cozy {get;private set;}
        public bool LivingGlass {get;private set;}
        public const string GlassPreference="LittleCity.LivingGlass";
        public const string CozyPreference="LittleCity.CozyStyle";
        public bool Busy {get;private set;}
        bool initialized;
        public bool Available=>lit!=null && glass!=null && lit.isSupported && glass.isSupported;
        CityGame game;Camera camera;Shader lit,glass;Texture2D detail;Cubemap sky;
        Material skyMaterial,originalSky;CameraClearFlags originalClear;
        readonly Dictionary<Material,Material> originals=new Dictionary<Material,Material>();
        readonly Dictionary<Material,int> surfaces=new Dictionary<Material,int>();
        Color ambient,sunColor;float sunIntensity,shadowDistance,shadowStrength;int aa;float oldFogStart,oldFogEnd;
        Transform detailRoot;int world=-1;ReflectionProbe probe;
        public int ConvertedMaterials=>originals.Count;
        public void Initialize(CityGame owner,Camera cam)
        {
            Current=this;game=owner;camera=cam;lit=Resources.Load<Shader>("HighDefinition");glass=Resources.Load<Shader>("HighGlass");
            bool launchGlass=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--living-glass")>=0;
            Cozy=PlayerPrefs.GetInt(CozyPreference,0)==1 || System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--cozy")>=0 || launchGlass;
            LivingGlass=Cozy && (PlayerPrefs.GetInt(GlassPreference,0)==1 || launchGlass);
            if(LivingGlass)PlayerPrefs.SetInt(GlassPreference,1);
            if(Cozy){PlayerPrefs.SetInt(CozyPreference,1);PlayerPrefs.Save();}
            Shader.SetGlobalFloat("_CozyStyle",Cozy?1:0);
            ambient=RenderSettings.ambientLight;sunColor=RenderSettings.sun.color;sunIntensity=RenderSettings.sun.intensity;shadowStrength=RenderSettings.sun.shadowStrength;
            shadowDistance=QualitySettings.shadowDistance;aa=QualitySettings.antiAliasing;oldFogStart=RenderSettings.fogStartDistance;oldFogEnd=RenderSettings.fogEndDistance;
            detail=Noise();sky=SkyReflection();
            originalSky=RenderSettings.skybox;originalClear=camera.clearFlags;skyMaterial=new Material(Resources.Load<Shader>("HighSky"));
            // Version 2: Low Def preserves the former High Def finish. New High is opt-in.
            Busy=true;StartCoroutine(Initial());
        }
        IEnumerator Initial(){yield return null;Busy=false;SetHigh(PlayerPrefs.GetInt("LittleCity.GraphicsModeV2",0)==1);}
        public void SetHigh(bool high){if(Busy || (initialized&&high==High))return;if(!Available){game.State.Say("Detailed graphics are unavailable on this graphics device.");return;}StartCoroutine(Switch(high));}
        IEnumerator Switch(bool high)
        {
            Busy=true;game.Hud.ClearInput();game.State.Say(high?"Loading High Def…":"Restoring Low Def…");yield return null;
            High=high;initialized=true;
            CaptureMaterials();
            foreach(var pair in originals)if(pair.Key!=null)ApplyMaterial(pair.Key,pair.Value);
            foreach(var car in FindObjectsByType<GraphicsCar>(FindObjectsInactive.Include,FindObjectsSortMode.None))car.SetHigh(high);
            foreach(var bike in FindObjectsByType<GraphicsBike>(FindObjectsInactive.Include,FindObjectsSortMode.None))bike.SetHigh(high);
            foreach(var safari in FindObjectsByType<GraphicsOffroad>(FindObjectsInactive.Include,FindObjectsSortMode.None))safari.SetHigh(high);
            foreach(var plane in FindObjectsByType<GraphicsPlane>(FindObjectsInactive.Include,FindObjectsSortMode.None))plane.SetHigh(high);
            foreach(var boat in FindObjectsByType<GraphicsBoat>(FindObjectsInactive.Include,FindObjectsSortMode.None))boat.SetHigh(high);
            foreach(var board in FindObjectsByType<GraphicsBoard>(FindObjectsInactive.Include,FindObjectsSortMode.None))board.SetHigh(high);
            foreach(var camper in FindObjectsByType<GraphicsCamper>(FindObjectsInactive.Include,FindObjectsSortMode.None))camper.SetHigh(high);
            foreach(var helicopter in FindObjectsByType<HelicopterRig>(FindObjectsInactive.Include,FindObjectsSortMode.None))helicopter.SetHigh(high);
            if(detailRoot!=null){Destroy(detailRoot.gameObject);detailRoot=null;}
            Lighting();UpdateScenery();game.RefreshDriverPose();
            PlayerPrefs.SetInt("LittleCity.GraphicsModeV2",high?1:0);PlayerPrefs.Save();
            yield return null;Busy=false;game.State.Say(high?"High Def":"Low Def");
        }
        public void SetCozy(bool cozy){SetStyle(cozy,false);}
        public void SetLivingGlass(bool enabled){SetStyle(true,enabled);}
        void SetStyle(bool cozy,bool livingGlass)
        {
            if(Busy || (Cozy==cozy && LivingGlass==livingGlass))return;
            Cozy=cozy;LivingGlass=cozy && livingGlass;
            Shader.SetGlobalFloat("_CozyStyle",cozy?1:0);CozyShape.SetAll(cozy);GummyCharacter.SetAll(cozy);game.RefreshPortraitStyle();game.Worlds?.Islands?.ApplyAccessory();
            PlayerPrefs.SetInt(CozyPreference,cozy?1:0);PlayerPrefs.SetInt(GlassPreference,LivingGlass?1:0);PlayerPrefs.Save();
            game.Hud.ClearInput();Lighting();game.State.Say(LivingGlass?"Living Glass":cozy?"Bubbly":"Original style");game.Hud.Refresh();
        }
        void CaptureMaterials()
        {
            foreach(var r in game.GetComponentsInChildren<MeshRenderer>(true))
                foreach(var m in r.sharedMaterials)
                    if(m!=null && (m.shader.name=="LittleCity/Sunshine" || m.shader.name=="LittleCity/Glass") && !originals.ContainsKey(m))
                    {originals.Add(m,new Material(m));surfaces[m]=m.HasProperty("_Surface")?(int)m.GetFloat("_Surface"):0;}
        }
        void ApplyMaterial(Material m,Material low)
        {
            if(low.shader.name=="LittleCity/Glass"){m.shader=glass;m.color=new Color(.22f,.36f,.4f,.25f);return;}
            int surface=surfaces[m];Color c=low.color;
            m.shader=lit;m.color=c;m.SetTexture("_Detail",detail);m.SetFloat("_Surface",surface);
            m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",surface==4?.27f:surface==0?.36f:.16f);
        }
        public Material Finish(Color color,float metallic,float smooth,int surface=0)
        {
            var m=new Material(lit){color=color,enableInstancing=true};m.SetTexture("_Detail",detail);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);m.SetFloat("_Surface",surface);return m;
        }
        public Material Window(){return new Material(glass){color=new Color(.09f,.17f,.22f,.32f)};}
        void Lighting()
        {
            CozyShape.SetAll(Cozy);GummyCharacter.SetAll(Cozy);
            RenderSettings.ambientLight=new Color(.40f,.47f,.55f);
            RenderSettings.sun.color=new Color(1,.91f,.79f);RenderSettings.sun.intensity=1.4f;RenderSettings.sun.shadowStrength=.86f;RenderSettings.sun.shadows=LightShadows.Soft;
            QualitySettings.shadowDistance=Application.isMobilePlatform?65:170;
            QualitySettings.antiAliasing=Application.isMobilePlatform?2:4;
            RenderSettings.fogStartDistance=100;RenderSettings.fogEndDistance=600;
            camera.farClipPlane=900;
            RenderSettings.skybox=skyMaterial;camera.clearFlags=CameraClearFlags.Skybox;
            RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture=sky;RenderSettings.reflectionIntensity=1.05f;
            if(probe!=null)probe.gameObject.SetActive(true);
            if(CityAtmosphere.Current!=null)CityAtmosphere.Current.Apply();else ApplyCozyLighting();
        }
        public void ApplyCozyLighting()
        {
            if(!Cozy)return;
            bool night=CityAtmosphere.Current!=null && CityAtmosphere.Current.Night;
            RenderSettings.ambientLight=night?new Color(.23f,.25f,.36f):new Color(.64f,.62f,.59f);
            RenderSettings.sun.color=night?new Color(.73f,.79f,1):new Color(1,.91f,.80f);
            RenderSettings.sun.intensity=night?.16f:1.12f;
            RenderSettings.sun.shadowStrength=night?.50f:.63f;
            RenderSettings.reflectionIntensity=night?.30f:.64f;
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,night?new Color(.11f,.12f,.20f):new Color(.82f,.82f,.74f),.32f);
        }
        public void RefreshLighting(){Lighting();UpdateScenery();game.RefreshDriverPose();}
        void UpdateScenery()
        {
            if(detailRoot!=null)detailRoot.gameObject.SetActive(world==game.State.WorldIndex);
            if(world!=game.State.WorldIndex || detailRoot==null)
            {
                if(detailRoot!=null)Destroy(detailRoot.gameObject);world=game.State.WorldIndex;
                detailRoot=HighScenery.Build(game,this);detailRoot.gameObject.SetActive(true);
            }
            if(probe==null)
            {
                var o=new GameObject("High Def local reflections");o.transform.SetParent(transform);probe=o.AddComponent<ReflectionProbe>();
                probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.resolution=128;
                probe.size=new Vector3(95,60,95);probe.nearClipPlane=2;probe.farClipPlane=160;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.32f,.48f,.61f);probe.cullingMask=1;
            }
            var p=game.State.Position;probe.transform.position=new Vector3(p.X,3,p.Z);probe.RenderProbe();
        }
        float refresh;
        void LateUpdate()
        {
            if(!initialized || Busy || game==null || !game.Ready)return;
            refresh-=Time.unscaledDeltaTime;if(refresh>0)return;refresh=.5f;
            int n=originals.Count;CaptureMaterials();if(originals.Count!=n)foreach(var pair in originals)if(pair.Key!=null && pair.Key.shader==pair.Value.shader)ApplyMaterial(pair.Key,pair.Value);
            if(world!=game.State.WorldIndex) {Lighting();UpdateScenery();game.RefreshDriverPose();}
        }
        static Texture2D Noise()
        {
            var t=new Texture2D(256,256,TextureFormat.RGB24,true,true){name="High Def mineral grain",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};var pixels=new Color[256*256];var random=new System.Random(147);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++){float n=(float)random.NextDouble();float broad=Mathf.PerlinNoise(x*.065f,y*.065f);pixels[y*256+x]=new Color(n*.6f+broad*.4f,n,broad);}
            t.SetPixels(pixels);t.Apply(true,false);return t;
        }
        static Cubemap SkyReflection()
        {
            const int size=64;var cube=new Cubemap(size,TextureFormat.RGB24,true){name="High Def sky and treeline reflection"};
            for(int face=0;face<6;face++)
            {
                var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float u=(x+.5f)/size*2-1,v=(y+.5f)/size*2-1;Vector3 d=face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1);d.Normalize();
                    Color c=d.y>0?Color.Lerp(new Color(.72f,.79f,.81f),new Color(.20f,.39f,.63f),Mathf.Sqrt(d.y)):Color.Lerp(new Color(.16f,.22f,.17f),new Color(.28f,.26f,.22f),-d.y);
                    float horizon=.05f+.10f*Mathf.PerlinNoise(d.x*16+19,d.z*16+19);if(d.y>0 && d.y<horizon)c=Color.Lerp(c,new Color(.09f,.17f,.10f),.8f);
                    float sun=Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,new Vector3(-.35f,.55f,.76f).normalized)),160);pixels[y*size+x]=c+new Color(1,.86f,.65f)*sun;
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true,false);return cube;
        }
        void OnDestroy(){if(Current==this){Current=null;Shader.SetGlobalFloat("_CozyStyle",0);}foreach(var m in originals.Values)if(m!=null)Destroy(m);if(detail!=null)Destroy(detail);if(sky!=null)Destroy(sky);if(skyMaterial!=null)Destroy(skyMaterial);}
    }
}
