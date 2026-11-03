Shader "LittleCity/NightLamp" {
 Properties {_Color("Light color",Color)=(1,.75,.35,1)}
 SubShader {Tags {"RenderType"="Opaque"} Pass {CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;float4 vert(float4 p:POSITION):SV_POSITION{return UnityObjectToClipPos(p);}fixed4 frag():SV_Target{return _Color;}
 ENDCG}}
}
