using UnityEngine;
using static System.MathF;

namespace LittleCity
{
    // Original procedural Foley: separate engaged chain, overrunning hub and tire
    // beds. Two-second cyclic buffers avoid downloads and per-frame allocations.
    public static class BicycleAudio
    {
        public const int SampleRate=48000;
        public static AudioClip Create(string name,int kind)
        {
            var data=Samples(kind);
            var clip=AudioClip.Create(name,data.Length,1,SampleRate,false);clip.SetData(data,0);return clip;
        }
        public static float[] Samples(int kind)
        {
            const int rate=SampleRate,n=rate*2;
            var samples=new float[n];uint random=81917;float low=0,mid=0;
            for(int i=-2000;i<n;i++)
            {
                random=random*1664525+1013904223;
                float noise=((random>>8)&65535)/32767.5f-1;
                low+=.028f*(noise-low);mid+=.21f*(noise-mid);
                if(i<0)continue;
                float t=(float)i/rate,v;
                if(kind==0)
                {
                    // Two loaded strokes per revolution, with many small chain
                    // contacts: a rounded mechanical purr, not an engine tone.
                    float load=.63f+.37f*Pow(.5f+.5f*Cos(2*PI*2.5f*t),2);
                    float tooth=t-Floor(t*80)/80;
                    float tick=(1-Exp(-tooth*2100))*Exp(-tooth*630);
                    v=load*((mid-low)*.46f+low*.48f+tick*(.15f*Sin(2*PI*1650*tooth)+.09f*Sin(2*PI*920*tooth)));
                }
                else if(kind==1)
                {
                    // Dense, springy pawl clicks as the wheel overruns the cranks.
                    float tooth=t-Floor(t*48)/48;
                    float click=(1-Exp(-tooth*3600))*Exp(-tooth*500);
                    v=click*(.44f*Sin(2*PI*1350*tooth)+.18f*Sin(2*PI*2750*tooth)+(mid-low)*.5f);
                }
                else
                {
                    float tread=.8f+.2f*Cos(2*PI*36*t);
                    v=low*1.25f*tread+(mid-low)*.075f;
                }
                samples[i]=v;
            }
            // Short crossfade seam: retain the continuous bed while removing
            // the discontinuity between the final and first noise samples.
            const int seam=384;
            for(int i=0;i<seam;i++)
            {
                float t=i/(float)(seam-1);
                samples[n-seam+i]=samples[n-seam+i]*(1-t)+samples[i]*t;
            }
            var data=new float[n-seam];System.Array.Copy(samples,seam,data,0,data.Length);
            return data;
        }
    }
}
