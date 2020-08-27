using OneBitLab.Services;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace OneBitLab.FluidSim
{
    [UpdateAfter( typeof(WaveMoveSystem) )]
    public class HeightFieldSystem : SystemBase
    {
        //-------------------------------------------------------------
        private RenderTexture m_HeightFieldRT;
        private RenderTexture m_TmpHeightFieldRT;
        private Texture2D     m_HeightFieldTex;
        private Material      m_FilterMat;

        //-------------------------------------------------------------
        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            m_HeightFieldRT    = ResourceLocatorService.Instance.m_HeightFieldRT;
            m_TmpHeightFieldRT = new RenderTexture( m_HeightFieldRT );

            m_HeightFieldTex = new Texture2D( m_HeightFieldRT.width,
                                              m_HeightFieldRT.height,
                                              TextureFormat.RFloat,
                                              mipChain: false,
                                              linear: true );

            m_FilterMat = new Material( Shader.Find( "FluidSim/WaveFilter_02" ) );
            m_FilterMat.SetFloat( "_WaveParticleRadius", WaveSpawnSystem.c_WaveParticleRadius );
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            NativeArray<float> pixData = m_HeightFieldTex.GetRawTextureData<float>();

            // Clear texture color to black
            Job
                .WithCode( () =>
                {
                    for( int i = 0; i < pixData.Length; i++ )
                    {
                        pixData[ i ] = 0;
                    }
                } )
                .Schedule();

            // Bake data we want to capture in the job
            int   w      = m_HeightFieldRT.width;
            int   h      = m_HeightFieldRT.height;
            float border = 5.0f;
            float texelW = 2.0f * border / w;
            float texelH = 2.0f * border / h;
            // Project all wave particles to texture
            // TODO: split in two jobs, first to create list of all modification (can be parallel)
            Entities
                .ForEach( ( in WavePos wPos, in WaveHeight height ) =>
                {
                    // Filter all particles which are beyond the border
                    // Also filter particles in the first and last raws of hegihtmap, 
                    // that helps us reduce three "if" statements during anti-aliasing
                    if( math.abs( wPos.Value.x ) >= ( border - 5.0f * texelW ) ||
                        math.abs( wPos.Value.y ) >= ( border - 5.0f * texelH ) )
                    {
                        return;
                    }

                    // Make particle positions start from 0,0 coordinates
                    float2 pos = -wPos.Value + border;
                    // Pixel coordinates with fractional parts
                    float xF = pos.x / texelW;
                    float yF = pos.y / texelH;
                    // Texture pixel indices
                    int x = (int)xF;
                    int y = (int)yF;
                    // Interpolation coefficients between texture indices
                    float dX = xF - x;
                    float dY = yF - y;
                    // Indices 
                    int x0y0 = x         + y         * w;
                    int x1y0 = ( x + 1 ) + y         * w;
                    int x0y1 = x         + ( y + 1 ) * w;
                    int x1y1 = ( x + 1 ) + ( y + 1 ) * w;

                    //pixData[ x0y0 ] = ( byte )( pixData[ x0y0 ] + height.Value);
                    // Do manual anti-aliasing for the 2x2 pixel square
                    pixData[ x0y0 ] = pixData[ x0y0 ] + height.Value * ( 1.0f - dX ) * ( 1.0f - dY );
                    pixData[ x1y0 ] = pixData[ x1y0 ] + height.Value * dX            * ( 1.0f - dY );
                    pixData[ x0y1 ] = pixData[ x0y1 ] + height.Value * ( 1.0f - dX ) * dY;
                    pixData[ x1y1 ] = pixData[ x1y1 ] + height.Value * dX            * dY;
                } )
                .Schedule();

            // We have to wait for all jobs before applying changes to the texture
            Dependency.Complete();
            m_HeightFieldTex.Apply();

            // Horizontal filter pass
            Graphics.Blit( m_HeightFieldTex, m_TmpHeightFieldRT, m_FilterMat, pass: 0 );
            Graphics.Blit( m_TmpHeightFieldRT, m_HeightFieldRT, m_FilterMat, pass: 1 ); 
            // Graphics.Blit( m_HeightFieldTex, m_HeightFieldRT );
        }

        //-------------------------------------------------------------
        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            // Clear height field render texture
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = m_HeightFieldRT;
            GL.Clear( false, true, Color.clear );
            RenderTexture.active = currentRT;
        }

        //-------------------------------------------------------------
    }
}