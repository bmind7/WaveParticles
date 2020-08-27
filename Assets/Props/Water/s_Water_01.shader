Shader "FluidSim/Water_01"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float4 div = tex2Dlod(_MainTex, float4(o.uv, 0, 0));
                float4 vertexPos = v.vertex + div;
                //float4 vertexPos = v.vertex;
                //vertexPos.y += div.r;
                o.vertex = UnityObjectToClipPos(vertexPos);

                return o;
            }

            fixed4 _Color;
            fixed4 frag(v2f i) : SV_Target
            {
                float eps = 0.001;
                float hL = tex2D(_MainTex, float2(i.uv.x + eps, i.uv.y)).y;
                float hR = tex2D(_MainTex, float2(i.uv.x - eps, i.uv.y)).y;
                float hT = tex2D(_MainTex, float2(i.uv.x, i.uv.y - eps)).y;
                float hB = tex2D(_MainTex, float2(i.uv.x, i.uv.y + eps)).y;

                float3 norm = normalize( float3( hL - hR, 2 * eps * 10, hB - hT ) );

                //return 2 * 15 * tex2D(_MainTex, i.uv) * 15 * tex2D(_MainTex, i.uv) * 15 * tex2D(_MainTex, i.uv);
                //return 5 * tex2D(_MainTex, i.uv);
                //float diff = 30 * (abs(hL-hR) + abs(hT-hB));
                //return fixed4(diff, diff, diff,1 );
                //return fixed4(norm, 1);
                //return fixed4(_WorldSpaceLightPos0.xyz, 1);
                return _Color * saturate(dot(norm, _WorldSpaceLightPos0)) + unity_AmbientSky;
            }
            ENDCG
        }
    }
}
