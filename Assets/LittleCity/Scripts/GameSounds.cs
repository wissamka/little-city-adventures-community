using UnityEngine;

namespace LittleCity
{
    // Small synthesized clips avoid downloads, streaming stalls and overlapping engine loops.
    public sealed class GameSounds : MonoBehaviour
    {
        CityGame game;
        AudioSource engine, shoes,boost,bikeHub,bikeTires;
        AudioClip car, plane, foot, freewheel,chain,tireRoll,boostClip,motorcycle,scooter,helicopter;
        Ride lastMode;
        int lastWorld;
        float stepDistance;
        bool focused=true;
        public int StepsPlayed {get;private set;}
        public bool EnginePlaying=>(engine!=null && engine.isPlaying)||(bikeHub!=null && bikeHub.isPlaying);
        public float PedalingVolume=>engine!=null && engine.isPlaying && engine.clip==chain?engine.volume:0;
        public float CoastingVolume=>bikeHub==null?0:bikeHub.volume;
        public bool BicyclePlaying=>(engine!=null && engine.clip==chain && engine.isPlaying)||(bikeHub!=null && bikeHub.isPlaying)||(bikeTires!=null && bikeTires.isPlaying);
        public bool BoostPlaying=>boost!=null&&boost.isPlaying;
        public float EnginePitch=>engine==null?0:engine.pitch;
        public string EngineSound=>engine==null||engine.clip==null?"":engine.clip.name;
        public static float Volume=>Mathf.Clamp01(PlayerPrefs.GetFloat("LittleCity.SoundVolume",.7f));
        public static void CycleVolume(){float v=Volume;PlayerPrefs.SetFloat("LittleCity.SoundVolume",v>.8f?0:v>.2f?1:.7f);PlayerPrefs.Save();}
        public void Initialize(CityGame owner,Camera camera)
        {
            game=owner;
            if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();
            engine=gameObject.AddComponent<AudioSource>();engine.playOnAwake=false;engine.loop=true;engine.spatialBlend=0;
            shoes=gameObject.AddComponent<AudioSource>();shoes.playOnAwake=false;shoes.spatialBlend=0;
            boost=gameObject.AddComponent<AudioSource>();boost.playOnAwake=false;boost.loop=true;boost.spatialBlend=0;boostClip=Clip("NOS whoosh",1,4);boost.clip=boostClip;
            car=Clip("Race engine",1,0);plane=Clip("Propeller flight",1,1);foot=Clip("Silly shoe puff",.19f,2);freewheel=BicycleAudio.Create("Mountain bike freehub",1);chain=BicycleAudio.Create("Mountain bike chain drive",0);tireRoll=BicycleAudio.Create("Mountain bike tire roll",2);
            bikeHub=gameObject.AddComponent<AudioSource>();bikeHub.playOnAwake=false;bikeHub.loop=true;bikeHub.spatialBlend=0;bikeHub.clip=freewheel;
            bikeTires=gameObject.AddComponent<AudioSource>();bikeTires.playOnAwake=false;bikeTires.loop=true;bikeTires.spatialBlend=0;bikeTires.clip=tireRoll;
            motorcycle=Clip("Motorcycle engine",1,5);scooter=Clip("Electric scooter whine",1,6);helicopter=Clip("Helicopter rotor",1,7);
            lastMode=game.State.Mode;lastWorld=game.State.WorldIndex;
        }
        static AudioClip Clip(string name,float seconds,int kind)
        {
            const int rate=24000;int n=Mathf.RoundToInt(seconds*rate);var samples=new float[n];float phase=0,softNoise=0;uint random=173;
            for(int i=0;i<n;i++)
            {
                float t=(float)i/rate;random=random*1664525+1013904223;float noise=((random>>8)&65535)/32767.5f-1;
                softNoise=Mathf.Lerp(softNoise,noise,.17f);
                if(kind==7)
                {
                    float beat=.48f+.52f*Mathf.Pow(.5f+.5f*Mathf.Sin(2*Mathf.PI*24*t),3);
                    samples[i]=beat*(.28f*Mathf.Sin(2*Mathf.PI*84*t)+.10f*Mathf.Sin(2*Mathf.PI*168*t)+.06f*Mathf.Sin(2*Mathf.PI*252*t)+softNoise*.035f);
                }
                else if(kind==6)
                {
                    float carrier=2*Mathf.PI*1900*t+.28f*Mathf.Sin(2*Mathf.PI*90*t);
                    samples[i]=(.72f+.08f*Mathf.Sin(2*Mathf.PI*60*t))*(.19f*Mathf.Sin(carrier)+.04f*Mathf.Sin(2*carrier));
                }
                else if(kind==5)
                {
                    float firing=.52f+.48f*Mathf.Pow(.5f+.5f*Mathf.Sin(2*Mathf.PI*24*t),3);
                    samples[i]=firing*(.37f*Mathf.Sin(2*Mathf.PI*96*t)+.16f*Mathf.Sin(2*Mathf.PI*192*t)+.08f*Mathf.Sin(2*Mathf.PI*288*t))+softNoise*.022f;
                }
                else if(kind==4)samples[i]=noise*.24f*(.7f+.3f*Mathf.Sin(t*2*Mathf.PI));
                else if(kind==3)
                {
                    float pulse=t%.2f,envelope=(1-Mathf.Exp(-pulse*850))*Mathf.Exp(-pulse*155);
                    samples[i]=pulse<.065f?envelope*(.30f*Mathf.Sin(2*Mathf.PI*1450*pulse)+.09f*Mathf.Sin(2*Mathf.PI*2300*pulse)+softNoise*.16f):0;
                }
                else if(kind==2)
                {
                    float envelope=Mathf.Sin(Mathf.PI*t/seconds);phase+=2*Mathf.PI*(150-105*t/seconds)/rate;
                    float buzz=Mathf.Sin(phase)+.35f*Mathf.Sin(2*phase)+.2f*Mathf.Sin(3*phase);
                    samples[i]=envelope*envelope*(buzz*.45f+noise*.045f);
                }
                else
                {
                    float hz=kind==0?70:90;float pulse=.7f+.3f*Mathf.Sin(2*Mathf.PI*(kind==0?35:18)*t);
                    samples[i]=pulse*(Mathf.Sin(2*Mathf.PI*hz*t)*.32f+Mathf.Sin(2*Mathf.PI*hz*2*t)*.14f+Mathf.Sin(2*Mathf.PI*hz*3*t)*.07f)+noise*.018f;
                    // Keep random-noise boundary quiet while periodic engine components meet exactly.
                    if(i<120 || i>n-120)samples[i]*=Mathf.Min(i,n-1-i)/120f;
                }
            }
            var clip=AudioClip.Create(name,n,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void LateUpdate()
        {
            if(game==null || !game.Ready)return;
            var s=game.State;float volume=Volume;
            if(!focused || game.Hud.MenuOpen || volume<=0){Silence();return;}
            if(lastMode!=s.Mode || lastWorld!=s.WorldIndex){Silence();lastMode=s.Mode;lastWorld=s.WorldIndex;}
            if(s.NitroActive){boost.volume=volume*.22f;if(!boost.isPlaying)boost.Play();}else if(boost.isPlaying)boost.Stop();
            bool racing=s.IsRoadCar && Mathf.Abs(s.ActualSpeed)>.2f;
            bool flying=s.Mode==Ride.Airplane && s.Airborne,rotor=s.Mode==Ride.Helicopter;
            bool cycling=s.IsBicycle && Mathf.Abs(s.ActualSpeed)>.2f;
            bool motor=s.Mode==Ride.Motorcycle || s.Mode==Ride.DirtBike;
            bool electric=s.Mode==Ride.Scooter && Mathf.Abs(s.ActualSpeed)>.2f;
            if(cycling)
            {
                float speed=Mathf.Abs(s.ActualSpeed),effort=Mathf.Clamp01(s.PedalEffort);
                float cadence=.72f+Mathf.Min(.85f,speed/22);
                BikeLayer(engine,chain,volume*.52f*effort,cadence);
                BikeLayer(bikeHub,freewheel,volume*.35f*(1-effort),.65f+Mathf.Min(1.4f,speed/12));
                BikeLayer(bikeTires,tireRoll,s.Grounded?volume*(.08f+Mathf.Min(.14f,speed*.008f)):0,.78f+Mathf.Min(.65f,speed/28));
            }
            else if(racing || flying || motor || electric || rotor)
            {
                var clip=rotor?helicopter:motor?motorcycle:electric?scooter:flying?plane:car;
                float speed=Mathf.Abs(s.Speed),targetPitch=rotor?.75f+s.Throttle*.4f:motor?.80f+Mathf.Min(1.75f,speed/12):electric?1+Mathf.Min(.55f,speed/30):flying?.8f+s.Throttle*.7f:.65f+speed/16;
                if(engine.clip!=clip){engine.Stop();engine.clip=clip;engine.pitch=targetPitch;engine.volume=0;}
                engine.pitch=Mathf.MoveTowards(engine.pitch,targetPitch,Time.unscaledDeltaTime*(motor?1.7f:2.5f));
                float gain=rotor?.24f:motor?.11f+Mathf.Min(.16f,speed*.012f):electric?.14f:flying?.27f:.30f;
                engine.volume=Mathf.MoveTowards(engine.volume,volume*gain,Time.unscaledDeltaTime*.6f);
                if(!engine.isPlaying)engine.Play();
            }
            else if(engine.isPlaying)engine.Stop();
            if(!cycling){bikeHub.Stop();bikeHub.volume=0;bikeTires.Stop();bikeTires.volume=0;}
            if(s.Mode==Ride.Walk && s.Grounded && !s.Swimming && s.Resting==RestPose.None && s.ActualSpeed>.1f)
            {
                stepDistance+=s.ActualSpeed*Mathf.Min(Time.unscaledDeltaTime,.1f);
                if(stepDistance>=.9f){stepDistance%=.9f;shoes.pitch=StepsPlayed%2==0?1:.87f;shoes.volume=volume*.7f;shoes.clip=foot;shoes.Play();StepsPlayed++;}
            }
            else{stepDistance=0;if(shoes.isPlaying)shoes.Stop();}
        }
        static void BikeLayer(AudioSource source,AudioClip clip,float gain,float pitch)
        {
            if(source.clip!=clip){source.Stop();source.clip=clip;source.volume=0;source.pitch=pitch;}
            source.pitch=Mathf.MoveTowards(source.pitch,pitch,Time.unscaledDeltaTime*2);
            source.volume=Mathf.MoveTowards(source.volume,gain,Time.unscaledDeltaTime*1.5f);
            if(source.volume>.0001f){if(!source.isPlaying)source.Play();}else source.Stop();
        }
        public void Silence(){if(bikeHub!=null){bikeHub.Stop();bikeHub.volume=0;}if(bikeTires!=null){bikeTires.Stop();bikeTires.volume=0;}if(boost!=null)boost.Stop();if(engine!=null)engine.Stop();if(shoes!=null)shoes.Stop();stepDistance=0;}
        void OnApplicationFocus(bool value){focused=value;if(!value)Silence();}
        void OnApplicationPause(bool paused){focused=!paused;if(paused)Silence();}
        void OnDisable(){Silence();}
        void OnDestroy(){if(helicopter!=null)Destroy(helicopter);if(motorcycle!=null)Destroy(motorcycle);if(scooter!=null)Destroy(scooter);if(boostClip!=null)Destroy(boostClip);if(car!=null)Destroy(car);if(plane!=null)Destroy(plane);if(foot!=null)Destroy(foot);if(freewheel!=null)Destroy(freewheel);if(chain!=null)Destroy(chain);if(tireRoll!=null)Destroy(tireRoll);}
    }
}
