using OneBitLab.Services;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace OneBitLab.FluidSim
{
    public struct WaveDebugData
    {
        public float2 Pos;
        public float2 Dir;
        public float2 Origin;
        public float  TimeToReflect;
    }

    [UpdateAfter( typeof(WaveReflectSystem) )]
    public class WaveDebugSystem : SystemBase
    {
        //-------------------------------------------------------------
        public readonly NativeQueue<WaveDebugData>
            m_DebugQueue = new NativeQueue<WaveDebugData>( Allocator.Persistent );

        //-------------------------------------------------------------
        protected override void OnDestroy()
        {
            m_DebugQueue.Dispose();
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            if( !Input.GetMouseButton( 1 ) )
            {
                // Only do work if RMB is pressed
                return;
            }

            Ray    ray        = ResourceLocatorService.Instance.m_MainCam.ScreenPointToRay( Input.mousePosition );
            float  dist       = math.abs( ray.origin.y / ray.direction.y );
            float3 hitPoint   = ray.GetPoint( dist );
            float  radiusSq   = 0.5f * 0.5f;
            var    debugQueue = m_DebugQueue;

            Entities
                .ForEach( ( in WavePos wPos, in WaveDir wDir, in WaveOrigin wOrigin, in TimeToReflect ttr ) =>
                {
                    if( math.distancesq( wPos.Value, hitPoint.xz ) > radiusSq )
                    {
                        return;
                    }

                    debugQueue.Enqueue( new WaveDebugData
                    {
                        Pos = wPos.Value, Dir = wDir.Value, Origin = wOrigin.Value, TimeToReflect = ttr.Value
                    } );
                } )
                .Run();
        }

        //-------------------------------------------------------------
    }
}