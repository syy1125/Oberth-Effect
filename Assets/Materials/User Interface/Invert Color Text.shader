Shader "Custom/Invert Color Text"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Font Texture", 2D) = "white" {}
        [PerRendererData]
        _Color ("Text Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform fixed4 _Color;

            v2f vert(const appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(const v2f i) : SV_Target
            {
                fixed4 col = i.color;
                col *= tex2D(_MainTex, i.uv).a;
                return col;
            }
            ENDCG

            Blend OneMinusDstColor OneMinusSrcAlpha
        }
    }
}