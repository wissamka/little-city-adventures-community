Shader "LittleCity/HighSky"
{
 SubShader
 {
  Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
  Cull Off ZWrite Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "Cozy.cginc"
   float _CityNight;struct appdata {float4 vertex:POSITION;};struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
   v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float3 d=normalize(i.dir);float h=saturate(d.y);
    float3 c=lerp(float3(.68,.77,.80),float3(.16,.35,.60),sqrt(h));
    float clouds=sin(d.x*19+d.z*4)*sin(d.z*13-d.x*8)*.5+.5;clouds=smoothstep(.68,.87,clouds)*smoothstep(.08,.35,h)*(1-smoothstep(.55,.8,h));
    c=lerp(c,float3(.87,.90,.91),clouds*.6);
    float3 cozySky=lerp(float3(.88,.84,.75),float3(.48,.70,.79),sqrt(h));
    cozySky=lerp(cozySky,float3(.96,.94,.88),clouds*.3);c=lerp(c,cozySky,_CozyStyle);
    float sun=pow(saturate(dot(d,normalize(float3(-.35,.55,.76)))),600);
    float2 cell=floor(d.xz/max(.05,d.y)*280);float stars=step(.998,frac(sin(dot(cell,float2(12.98,78.23)))*43758.54))*smoothstep(.1,.4,h);
    float moon=pow(saturate(dot(d,normalize(float3(.25,.65,-.7)))),2200);
    float3 night=lerp(float3(.035,.055,.11),float3(.003,.009,.027),sqrt(h))+stars*.5+moon*float3(.65,.74,1);
    night=lerp(night,night*1.2+float3(.017,.018,.032),_CozyStyle);
    return fixed4(lerp(c+sun*float3(1,.86,.65),night,_CityNight),1);
   }
   ENDCG
  }
 }
}
