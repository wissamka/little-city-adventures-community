Shader "LittleCity/HighGlass"
{
 Properties { _Color("Tint",Color)=(.20,.35,.4,.28) }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard alpha:fade
  #pragma target 3.0
  #include "Cozy.cginc"
  fixed4 _Color;float _CityNight;
  struct Input {float3 viewDir;};
  void surf(Input IN,inout SurfaceOutputStandard o)
  {
   float fresnel=pow(1-saturate(abs(normalize(IN.viewDir).z)),4);
   o.Albedo=CozyPigment(_Color.rgb);o.Metallic=lerp(.18,.06,_CozyStyle);o.Smoothness=lerp(.96,.73,_CozyStyle);o.Alpha=.16+fresnel*.58;o.Emission=_Color.rgb*_CityNight*.8;
  }
  ENDCG
 }
 FallBack "Transparent/Diffuse"
}
