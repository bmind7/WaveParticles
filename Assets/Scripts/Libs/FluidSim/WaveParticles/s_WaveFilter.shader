Shader "FluidSim/WaveFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
        #include "UnityCG.cginc"

            struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        sampler2D _MainTex;
        float4 _MainTex_ST;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            return o;
        }

        fixed4 frag_horizontal(v2f i) : SV_Target
        {
            float width = 10.0f;
            float waveRadius = 0.1f;
            float r = waveRadius / width;
            float step = r / 3.0f;

            fixed col1 = tex2D(_MainTex, i.uv - 3 * step * float2(1, 0));
            fixed col2 = tex2D(_MainTex, i.uv - 2 * step * float2(1, 0));
            fixed col3 = tex2D(_MainTex, i.uv - step * float2(1, 0));
            fixed col4 = tex2D(_MainTex, i.uv);
            fixed col5 = tex2D(_MainTex, i.uv + step * float2(1, 0));
            fixed col6 = tex2D(_MainTex, i.uv + 2 * step * float2(1, 0));
            fixed col7 = tex2D(_MainTex, i.uv + 3 * step * float2(1, 0));

            float pi = 3.14f;
            fixed col =
                col1 * (cos(pi * 3 * step / r) + 1) / 2 +
                col2 * (cos(pi * 2 * step / r) + 1) / 2 +
                col3 * (cos(pi * 1 * step / r) + 1) / 2 +
                col4 * (cos(pi * 0 * step / r) + 1) / 2 +
                col5 * (cos(pi * 1 * step / r) + 1) / 2 +
                col6 * (cos(pi * 2 * step / r) + 1) / 2 +
                col7 * (cos(pi * 3 * step / r) + 1) / 2;

            return float4(col, col, col, 1);
        }

        fixed4 frag_vertical(v2f i) : SV_Target
        {
            float width = 10.0f;
            float waveRadius = 0.1f;
            float r = waveRadius / width;
            float step = r / 3.0f;

            fixed col1 = tex2D(_MainTex, i.uv - 3 * step * float2(0, 1));
            fixed col2 = tex2D(_MainTex, i.uv - 2 * step * float2(0, 1));
            fixed col3 = tex2D(_MainTex, i.uv - step * float2(0, 1));
            fixed col4 = tex2D(_MainTex, i.uv);
            fixed col5 = tex2D(_MainTex, i.uv + step * float2(0, 1));
            fixed col6 = tex2D(_MainTex, i.uv + 2 * step * float2(0, 1));
            fixed col7 = tex2D(_MainTex, i.uv + 3 * step * float2(0, 1));

            float pi = 3.14f;
            fixed col =
                col1 * (cos(pi * 3 * step / r) + 1) / 2 +
                col2 * (cos(pi * 2 * step / r) + 1) / 2 +
                col3 * (cos(pi * 1 * step / r) + 1) / 2 +
                col4 * (cos(pi * 0 * step / r) + 1) / 2 +
                col5 * (cos(pi * 1 * step / r) + 1) / 2 +
                col6 * (cos(pi * 2 * step / r) + 1) / 2 +
                col7 * (cos(pi * 3 * step / r) + 1) / 2;

            return float4(col, col, col, 1);
        }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_horizontal
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag_vertical
            ENDCG
        }
    }
}
