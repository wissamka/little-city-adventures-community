Shader "LittleCity/HighDefinition"
{
 Properties
 {
  _Color("Color",Color)=(1,1,1,1)
  _Surface("Surface",Float)=0
  _Detail("Detail",2D)="gray"{}
  _Metallic("Metal",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.3
 }
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  LOD 300
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  #include "Cozy.cginc"
  sampler2D _Detail;fixed4 _Color;half _Surface,_Metallic,_Glossiness;
  struct Input {float3 worldPos;float3 worldNormal;float3 viewDir; INTERNAL_DATA};
  void surf(Input IN,inout SurfaceOutputStandard o)
  {
   float3 p=IN.worldPos;
   float3 wn=WorldNormalVector(IN,float3(0,0,1));
   float3 weights=pow(abs(wn),4);weights/=max(.001,weights.x+weights.y+weights.z);
   float scale=_Surface==3?1.3:3.5;
   float grain=tex2D(_Detail,p.yz*scale).r*weights.x+tex2D(_Detail,p.xz*scale).r*weights.y+tex2D(_Detail,p.xy*scale).r*weights.z;
   float micro=tex2D(_Detail,p.xz*38).g;
   float variation=.90+grain*.20;
   if(_Surface==1)
   {
    float row=floor(p.y/.24);float2 brick=frac(float2((abs(wn.x)>.5?p.z:p.x)/.55+fmod(row,2)*.5,p.y/.24));
    float mortar=step(.032,brick.x)*step(.065,brick.y);variation=lerp(.61,variation,mortar);
   }
   if(_Surface==3)variation*=.84+.16*sin(p.y*13+grain*10);
   if(_Surface==4)variation=.77+grain*.34;
   if(_Surface==5)variation=.72+grain*.45+micro*.1;
   if(_Surface==8)variation=.55+grain*.8+micro*.12;
   if(_Surface==9)
   {
    float broad=tex2D(_Detail,p.xz*.035).b;
    float wear=tex2D(_Detail,p.xz*.23).r;
    float2 cell=floor(p.xz/4.7);
    float crackLine=abs(frac(p.z*.213+sin(p.x*.74+cell.y)*.024)-.5);
    float crack=(1-smoothstep(.001,.006,crackLine))*step(.69,frac(sin(dot(cell,float2(12.98,78.23)))*43758.54));
    variation=(.67+grain*.3+micro*.15)*( .83+broad*.4+smoothstep(.48,.68,wear)*.13)*(1-crack*.55);
   }
   if(_Surface==7)variation=.97+micro*.06;
   if(_CozyStyle>.5)
   {
    // Quiet, continuous finishes instead of noisy stone and high-contrast mortar.
    variation=.97+grain*.06;
    if(_Surface==3)
    {
     float2 uv=abs(wn.y)>.6?p.xz:(abs(wn.x)>.5?p.zy:p.xy);
     float wood=sin(uv.x*35+sin(uv.y*1.7)*2+grain*1.7);
     float fine=sin(uv.x*107+grain*3);
     variation=.96+wood*.033+fine*.012;
    }
    if(_Surface==6)variation=.985+grain*.03;
    if(_Surface==5 || _Surface==4)variation=.96+grain*.08;
   }
   o.Albedo=CozyPigment(_Color.rgb)*variation;
   o.Metallic=lerp(_Metallic,_Metallic*.38,_CozyStyle);
   float satin=_Surface==6?.16:_Surface==3?.34:_Surface==4?.28:_Surface==5?.18:.43;
   o.Smoothness=lerp(saturate(_Glossiness+(grain-.5)*.13),satin,_CozyStyle);
   o.Occlusion=.84+grain*.16;
   // Fine texture relief in the surface's tangent frame; paint stays smooth.
   float relief=(_Surface==7?0:_Surface==4?.035:.11)*lerp(1,.16,_CozyStyle);
   o.Normal=normalize(float3((grain-.5)*relief,(micro-.5)*relief,1));
  }
  ENDCG
 }
 FallBack "Diffuse"
}
