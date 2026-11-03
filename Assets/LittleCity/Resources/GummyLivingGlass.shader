Shader "LittleCity/Living Glass"
{
 Properties
 {
  _Color("Candy pigment",Color)=(.9,.48,.12,1)
  _Glossiness("Wet polished surface",Range(0,1))=.965
  _Density("Pigment density",Range(.1,4))=2.2
  _Transmission("Light transmission",Range(0,1))=.98
  _Refraction("Soft refraction",Range(0,.05))=.042
  _Scatter("Internal light scattering",Range(0,2))=1.2
  _ThicknessScale("Relative candy thickness",Range(.1,2))=1
  _InnerGlow("Living inner light",Range(0,3))=1.4
  _GlassEnvironment("Glass reflection environment",Cube)="black"{}
  _ReflectionStrength("Polished glass reflections",Range(0,4))=1.3
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
  samplerCUBE _GlassEnvironment;
  float4 _GummySceneColor_TexelSize;
  fixed4 _Color;
  half _Glossiness,_Density,_Transmission,_Refraction,_Scatter,_ThicknessScale,_InnerGlow,_ReflectionStrength;
  struct Input { float4 grabPos; float3 worldNormal; float3 worldPos;float3 localPos; };
  void vert(inout appdata_full v,out Input o)
  { UNITY_INITIALIZE_OUTPUT(Input,o);o.localPos=v.vertex.xyz;o.grabPos=ComputeGrabScreenPos(UnityObjectToClipPos(v.vertex)); }

  half4 LightingGummy(SurfaceOutputStandard s,half3 viewDir,UnityGI gi)
  {
   gi.indirect.specular*=_ReflectionStrength;
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
   half transmit=_Transmission*exp(-thickness*.48);
   float2 uv=IN.grabPos.xy/IN.grabPos.w;
   float3 viewNormal=mul((float3x3)UNITY_MATRIX_V,normal);
   float handBlown=1+.045*sin(IN.localPos.y*5+sin(IN.localPos.x*7))*sin(IN.localPos.z*4+1);
   float2 bend=viewNormal.xy*_Refraction*thickness*handBlown;
   float2 blur=abs(_GummySceneColor_TexelSize.xy)*(1+thickness*.7);
   float2 safeMin=abs(_GummySceneColor_TexelSize.xy)*2;
   uv=clamp(uv+bend,safeMin,1-safeMin);
   half3 behind=tex2D(_GummySceneColor,uv).rgb*.4;
   behind+=tex2D(_GummySceneColor,clamp(uv+blur,safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv-blur,safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv+float2(blur.x,-blur.y),safeMin,1-safeMin)).rgb*.15;
   behind+=tex2D(_GummySceneColor,clamp(uv+float2(-blur.x,blur.y),safeMin,1-safeMin)).rgb*.15;
   o.Albedo=_Color.rgb*(1-transmit)*.24;
   o.Metallic=0;o.Smoothness=_Glossiness;o.Occlusion=1;o.Alpha=1;
   half depth=1-exp(-thickness*1.6);
   half edge=pow(1-facing,2);
   half breathe=1+.035*sin(_Time.y*2.1+unity_ObjectToWorld._m03*.37);
   half3 inner=_Color.rgb*_InnerGlow*breathe*(depth*.30+edge*.20);
   half fresnel=.045+.955*pow(1-facing,5);
   half3 windows=texCUBE(_GlassEnvironment,reflect(-view,normal)).rgb*fresnel*_ReflectionStrength;
   o.Emission=behind*absorption*transmit+inner+windows;
   // A few tiny trapped air bubbles add depth to the thicker blown-glass body.
   if(_ThicknessScale>.75)
   {
    float3 cameraLocal=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;
    float3 ray=normalize(IN.localPos-cameraLocal);
    for(int i=0;i<3;i++)
    {
     float3 center=i==0?float3(.20,.92,.12):i==1?float3(-.18,.64,.08):float3(.10,1.19,.02);
     float radius=i==1?.021:.028;float3 delta=center-IN.localPos;float along=dot(delta,ray);
     float distance=length(delta-ray*along)/radius;
     float ring=smoothstep(.58,.87,distance)*(1-smoothstep(.87,1.12,distance));
     o.Emission+=half3(.65,.82,1)*ring*.10*step(0,along)*step(along,1.2);
    }
   }
  }
  ENDCG
 }
 FallBack "LittleCity/Gummy Rubber"
}
