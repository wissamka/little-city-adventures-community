Shader "LittleCity/Sunshine"
{
    Properties
    {
        _Color ("Pigment", Color) = (1,1,1,1)
        _Surface ("Surface: plain brick stone wood leaves grass cloth", Float) = 0
        _OutlinePixels ("Ink contour", Range(0,3)) = 1.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 150
        Pass
        {
            Name "INK"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex inkVert
            #pragma fragment inkFrag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float _OutlinePixels;float _Surface;float _CozyStyle;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; UNITY_FOG_COORDS(0) };
            v2f inkVert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex);
                float3 n=mul((float3x3)UNITY_MATRIX_V,UnityObjectToWorldNormal(v.normal));
                float2 inkDirection=n.xy/max(length(n.xy),.15);
                o.pos.xy+=inkDirection*_OutlinePixels*2/_ScreenParams.xy*o.pos.w;
                UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 inkFrag(v2f i):SV_Target
            { if(_CozyStyle>.5)discard; if(_Surface>3.5 && _Surface<4.5)discard; fixed4 c=fixed4(.008,.017,.021,1);UNITY_APPLY_FOG(i.fogCoord,c);return c; }
            ENDCG
        }
        CGPROGRAM
        #pragma surface surf Painted fullforwardshadows addshadow vertex:paintVert
        #pragma target 3.0
        #include "../Resources/Cozy.cginc"
        fixed4 _Color;
        half _Surface;
        struct Input { float3 worldPos; float3 worldNormal; float3 viewDir; float4 pigment:COLOR;float leafEdge; };
        void paintVert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.pigment=v.color;o.leafEdge=v.texcoord.z;}
        float hash21(float2 p){float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
        float paperNoise(float2 p)
        {
            float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);
        }
        half4 LightingPainted(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
        {
            half light=saturate(dot(s.Normal,lightDir)*.6+.4);
            half band=.32+.28*smoothstep(.15,.38,light)+.32*smoothstep(.58,.85,light);
            half shadow=lerp(.18,1,atten);
            half rim=pow(1-saturate(dot(s.Normal,viewDir)),3)*.045;
            half3 cool=half3(.34,.48,.51);
            half3 tint=lerp(cool,_LightColor0.rgb,saturate(band*shadow));
            return half4(s.Albedo*tint*(.30+.75*band*shadow)+s.Albedo*rim,1);
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            float3 n=abs(IN.worldNormal);
            float2 p=n.y>.6?IN.worldPos.xz:n.x>n.z?IN.worldPos.zy:IN.worldPos.xy;
            float grain=paperNoise(p*5.5);
            fixed3 pigment=_Color.rgb*IN.pigment.rgb;
            float grey=dot(pigment,float3(.22,.65,.13));
            pigment=lerp(pigment,grey.xxx,.13);
            pigment*=.93+grain*.13;
            if(_Surface>.5 && _Surface<1.5)
            {
                float2 q=p*float2(1.25,3.3);q.x+=floor(q.y)*.5;
                float2 f=frac(q),edge=min(f,1-f),aa=max(fwidth(q),.004);
                float mortar=smoothstep(.025,.025+aa.x*1.3,edge.x)*smoothstep(.04,.04+aa.y*1.3,edge.y);
                float mottling=.82+.24*hash21(floor(q));
                pigment=lerp(pigment*.52,pigment*mottling,mortar);
            }
            if(_Surface>1.5 && _Surface<2.5)
            {
                pigment*=.85+.25*paperNoise(p*2.4);
            }
            if(_Surface>2.5 && _Surface<3.5)
            {
                float grainLine=sin(p.y*31+paperNoise(p*float2(3,.7))*7);
                pigment*=.91+.09*grainLine;
                float seam=min(frac(p.x*2.2),1-frac(p.x*2.2));
                pigment*=lerp(.6,1,smoothstep(.012,.035,seam));
            }
            if(_Surface>3.5 && _Surface<4.5)
            {
                float veins=abs(sin(p.x*9+paperNoise(p*4)*3));
                pigment*=.90+.18*grain;
                float edgeWidth=max(fwidth(IN.leafEdge)*1.25,.045);
                pigment=lerp(pigment,pigment*.40,smoothstep(1-edgeWidth,1,IN.leafEdge));
                pigment*=lerp(.72,1,smoothstep(.06,.17,veins));
            }
            if(_Surface>4.5 && _Surface<5.5) pigment*=.84+.22*paperNoise(p*1.5);
            if(_Surface>5.5)
            {
                float weave=sin(p.x*95)*sin(p.y*95);
                float fade=1-saturate(max(fwidth(p.x),fwidth(p.y))*100);
                pigment*=1+weave*.04*fade;
            }
            float hatch=abs(sin((p.x+p.y)*43));
            float hatchFade=1-saturate(length(fwidth(p))*35);
            pigment*=1-(1-smoothstep(.025,.12,hatch))*.045*hatchFade;
            pigment=lerp(pigment,CozyPigment(_Color.rgb)*(.97+grain*.06),_CozyStyle);
            o.Albedo= pigment; o.Alpha=1;o.Emission=pigment*.055;
        }
        ENDCG
    }
    Fallback "Mobile/Diffuse"
}
