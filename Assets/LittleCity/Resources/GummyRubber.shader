Shader "LittleCity/Gummy Rubber"
{
 Properties { _Color("Rubber color",Color)=(.5,.8,.4,1) _Glossiness("Soft gloss",Range(0,1))=.6 }
 SubShader
 {
  Tags { "RenderType"="Opaque" } LOD 250
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  fixed4 _Color; half _Glossiness;
  struct Input { float3 viewDir; };
  void surf(Input IN, inout SurfaceOutputStandard o)
  {
   o.Albedo=_Color.rgb; o.Metallic=0; o.Smoothness=_Glossiness;
   // A restrained soft edge glow suggests pigmented rubber without transparency
   // sorting or a costly screen-space effect on iPad.
   half rim=pow(1-saturate(dot(normalize(IN.viewDir),float3(0,0,1))),3);
   o.Emission=_Color.rgb*rim*.075; o.Occlusion=1; o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Diffuse"
}
