Shader "FluidSim/WaveFilter_02"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WaveParticleRadius ("Wave Particle Radius", Float) = 0.15
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
        uniform float _WaveParticleRadius;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            return o;
        }

        static float TEXTURE_SIZE = 1024.0f;
        static float PI = 3.14f;
        //static float WAVE_PARTICLE_RADIUS = 0.15f;
        static float TEXEL_WIDTH_IN_WORLD = 10.0f / TEXTURE_SIZE;
        static float TEXEL_WIDTH = 1.0f / TEXTURE_SIZE;

        // X-axis filtering of the particles texture 
        float4 frag_horizontal(v2f i) : SV_Target
        {
            float4 div = float4( 0, tex2D( _MainTex, i.uv ).r, 0, 1);
            div.z = div.y;
            float texOffset = 0;
            
            for( float idx = TEXEL_WIDTH_IN_WORLD; idx < _WaveParticleRadius; idx += TEXEL_WIDTH_IN_WORLD ) {
                texOffset += TEXEL_WIDTH;
                
                float ampL = tex2D(_MainTex, float2( i.uv.x + texOffset, i.uv.y ) );
                float ampR = tex2D(_MainTex, float2( i.uv.x - texOffset, i.uv.y ) );
                
                //div.x += (ampL - ampR) * -sqrt(2.0)/2.0 * sin( PI * idx / WAVE_PARTICLE_RADIUS ) * ( cos( PI * idx / WAVE_PARTICLE_RADIUS) + 1.0f );
                div.x += (ampL - ampR) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f ) * 2.0 * -0.5 * sin( PI * idx / _WaveParticleRadius );
                div.y += (ampL + ampR) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f );
                div.z += (ampL + ampR) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f ) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f );
            }
            
            return div;
        }

        // Y-axis filtering of the particles texture 
        float4 frag_vertical(v2f i) : SV_Target
        {
            float4 d = tex2D( _MainTex, i.uv );
            float4 div = float4( d.x, d.y, 0, 1);
            float texOffset = 0;
            
            for( float idx = TEXEL_WIDTH_IN_WORLD; idx < _WaveParticleRadius; idx += TEXEL_WIDTH_IN_WORLD ) {
                texOffset += TEXEL_WIDTH;
                
                float4 ampB = tex2D( _MainTex, float2( i.uv.x, i.uv.y + texOffset ) );
                float4 ampT = tex2D( _MainTex, float2( i.uv.x, i.uv.y - texOffset ) );
                
                div.x += (ampB.x + ampT.x) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f ) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f );
                div.y += (ampB.y + ampT.y) * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f );
                div.z += (ampB.z - ampT.z) * 2.0f * 0.5f * ( cos( PI * idx / _WaveParticleRadius) + 1.0f ) * -0.5 * sin( PI * idx / _WaveParticleRadius );
            }
            
            // Scale XY offset of the wave
            div.xz *= 0.1f;
            
            return div;
        }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Performs two pass filtering by X & Y axis 
        // Output: texture with x, y & x offsets for the vertex of the wave plane
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
