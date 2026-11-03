Shader "LittleCity/Translucent Gummy"
{
 Properties
 {
  _Color("Candy pigment",Color)=(.9,.48,.12,1)
  _Glossiness("Wet polished surface",Range(0,1))=.88
  _Density("Pigment density",Range(.1,4))=1.65
  _Transmission("Light transmission",Range(0,1))=.92
  _Refraction("Soft refraction",Range(0,.05))=.03
  _Scatter("Internal light scattering",Range(0,2))=.85
  _ThicknessScale("Relative candy thickness",Range(.1,2))=1
 }
 SubShader
 {
  // Composite an absorbed/refracted scene color, then write ordinary depth. This
  // keeps overlapping limbs stable and prevents hidden original avatars showing
  // through. One named scene capture is shared by the whole crowd per camera.
  Tags { "Queue"="Geometry+20" "RenderType"="Opaque" }
  GrabPass { "_GummySceneColor" }
  CGPROGRAM
  #pragma surface surf Gummy fullforwardshadows addshadow vertex:vert
  #pragma target 3.0
  #include "UnityPBSLighting.cginc"
  sampler2D _GummySceneColor;
  float4 _GummySceneColor_TexelSize;
  fixed4 _Color;
  half _Glossiness,_Density,_Transmission,_Refraction,_Scatter,_ThicknessScale;
  struct Input { float4 grabPos; float3 worldNormal; float3 worldPos; };
  void vert(inout appdata_full v,out Input o)
  { UNITY_INITIALIZE_OUTPUT(Input,o);o.grabPos=ComputeGrabScreenPos(UnityObjectToClipPos(v.vertex)); }

  half4 LightingGummy(SurfaceOutputStandard s,half3 viewDir,UnityGI gi)
  {
   half4 color=LightingStandard(s,viewDir,gi);
   // Light travelling toward the viewer lights the interior of the candy. gi's
   // light color already carries the light's distance and shadow attenuation.
   half back=pow(saturate(dot(viewDir,-gi.light.dir+s.Normal*.28)),3);
   half wrapped=pow(saturate(dot(-s.Normal,gi.light.dir)*.65+.35),2);
   color.rgb+=_Color.rgb*gi.light.color*(back*.75+wrapped*.25)*_Scatter;
   return color;
  }
  void LightingGummy_GI(SurfaceOutputStandard s,UnityGIInput data,inout UnityGI gi)
  { LightingStandard_GI(s,data,gi); }

  void surf(Input IN,inout SurfaceOutputStandard o)
  {
   float3 normal=normalize(IN.worldNormal);
   float3 view=normalize(UnityWorldSpaceViewDir(IN.worldPos));
   half facing=saturate(dot(normal,view));
   half thickness=_ThicknessScale*lerp(.12,1.25,pow(facing,.62));
   half3 absorption=exp(-(1-saturate(_Color.rgb))*_Density*thickness);
   half transmit=_Transmission*exp(-thickness*.70);
   float2 uv=IN.grabPos.xy/IN.grabPos.w;
   float3 viewNormal=mul((float3x3)UNITY_MATRIX_V,normal);
   float2 bend=viewNormal.xy*_Refraction*thickness;
   float2 blur=abs(_GummySceneColor_TexelSize.xy)*(1+thickness*.7);
   float2 safeMin=abs(_GummySceneColor_TexelSize.xy)*2;
   uv=clamp(uv+bend,safeMin,1-safeMin);
   half3 behind=tex2D(_GummySceneColor,uv).rgb*.4;
   behind+=tex2D(_GummySceneColor,clamp(uv+blur,safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv-blur,safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv+float2(blur.x,-blur.y),safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv+float2(-blur.x,blur.y),safeMin,1-safeMin)).rgb*.15;
   o.Albedo=_Color.rgb*(1-transmit)*.83;
   o.Metallic=0;o.Smoothness=_Glossiness;o.Occlusion=1;o.Alpha=1;
   o.Emission=behind*absorption*transmit;
  }
  ENDCG
 }
 FallBack "LittleCity/Gummy Rubber"
}
