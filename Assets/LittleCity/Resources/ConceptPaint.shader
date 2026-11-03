Shader "LittleCity/Concept PBR" {
 Properties {
  _Color("Color",Color)=(1,1,1,1)
  _MainTex("Albedo",2D)="white"{}
  _BumpMap("Normal",2D)="bump"{}
  _BumpScale("Normal strength",Float)=1
  _MetalRough("Metallic roughness",2D)="white"{}
  _Metallic("Metallic",Range(0,1))=0
  _Roughness("Roughness",Range(0,1))=.4
  _Occlusion("Occlusion UV2",2D)="white"{}
  _OcclusionStrength("Occlusion strength",Range(0,1))=1
  _EmissionColor("Emission",Color)=(0,0,0,0)
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 300
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  #include "UnityStandardUtils.cginc"
  #include "Cozy.cginc"
  sampler2D _MainTex,_BumpMap,_MetalRough,_Occlusion;
  fixed4 _Color,_EmissionColor;half _Metallic,_Roughness,_BumpScale,_OcclusionStrength;float _CityNight;
  struct Input {float2 uv_MainTex;float2 uv_BumpMap;float2 uv_MetalRough;float2 uv2_Occlusion;};
  void surf(Input IN,inout SurfaceOutputStandard o) {
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
   o.Normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
   fixed4 mr=tex2D(_MetalRough,IN.uv_MetalRough);
   o.Metallic=mr.b*_Metallic;o.Smoothness=1-mr.g*_Roughness;
   o.Occlusion=lerp(1,tex2D(_Occlusion,IN.uv2_Occlusion).r,_OcclusionStrength);
   o.Albedo=CozyPigment(o.Albedo);
   o.Normal=normalize(lerp(o.Normal,float3(0,0,1),_CozyStyle*.7));
   o.Metallic*=lerp(1,.42,_CozyStyle);o.Smoothness=lerp(o.Smoothness,.43,_CozyStyle*.8);
   o.Emission=_EmissionColor.rgb*(1+_CityNight*2);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
