Shader "LittleCity/SoftContactShadow"
{
    Properties { _Color ("Shadow tint", Color) = (.025,.055,.065,.32) }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION;float2 uv:TEXCOORD0; };
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv*2-1;return o;}
            fixed4 frag(v2f i):SV_Target
            {float r=dot(i.uv,i.uv);return fixed4(_Color.rgb,_Color.a*(1-smoothstep(.08,1,r)));}
            ENDCG
        }
    }
}
