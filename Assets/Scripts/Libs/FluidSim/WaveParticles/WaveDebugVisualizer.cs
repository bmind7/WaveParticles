using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace OneBitLab.FluidSim
{
    public class WaveDebugVisualizer : MonoBehaviour
    {
        //-------------------------------------------------------------
        private NativeQueue<WaveDebugData> m_DebugQueue;

        //-------------------------------------------------------------
        private void Awake()
        {
            m_DebugQueue = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<WaveDebugSystem>().m_DebugQueue;
        }

        //-------------------------------------------------------------
        private void OnDrawGizmos()
        {
            if( !m_DebugQueue.IsCreated ) return;

            while( m_DebugQueue.TryDequeue( out WaveDebugData data ) )
            {
                // Debug.Log( time );
                var     center       = new Vector3( data.Pos.x, 0, data.Pos.y );
                var     origin       = new Vector3( data.Origin.x, 0, data.Origin.y );
                var     dir          = new Vector3( data.Dir.x, 0, data.Dir.y );
                Vector3 reflectPoint = center + WaveSpawnSystem.c_WaveParticleSpeed * data.TimeToReflect * dir;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere( center, 0.1f );
                Gizmos.color = Color.red;
                Gizmos.DrawLine( center, reflectPoint );
                Gizmos.DrawWireSphere( reflectPoint, 0.02f );
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere( origin, 0.02f );
                Gizmos.DrawLine( origin, center );
            }
        }

        //-------------------------------------------------------------
    }
}