using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace OneBitLab.FluidSim
{
    // This system handle subdivide phase of simulation
    // Once time for next subdivision run out we create two new neighboring particles and update current one
    // We also calculate time of next subdivision based on the new dispersion angle
    // The higher the dispersion angle the faster particles move away from each other
    
    [UpdateAfter( typeof(WaveSpawnSystem) )]
    public class WaveSubdivideSystem : SystemBase
    {
        //-------------------------------------------------------------
        private WaveSpawnSystem                        m_WaveSpawnSystem;
        private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

        //-------------------------------------------------------------
        protected override void OnCreate()
        {
            m_WaveSpawnSystem = World.GetOrCreateSystem<WaveSpawnSystem>();
            m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            EntityCommandBuffer.ParallelWriter createECB = m_EndSimECBSystem.CreateCommandBuffer().AsParallelWriter();
            EntityCommandBuffer.ParallelWriter deleteECB = m_EndSimECBSystem.CreateCommandBuffer().AsParallelWriter();
            EntityArchetype                    archetype = m_WaveSpawnSystem.WaveArchetype;

            float       dTime        = Time.DeltaTime;
            const float cWPRadius    = WaveSpawnSystem.c_WaveParticleRadius;
            const float cWPSpeed     = WaveSpawnSystem.c_WaveParticleSpeed;
            const float cWPMinHeight = WaveSpawnSystem.c_WaveParticleMinHeight;

            Entities
                .ForEach( ( Entity           entity,
                            int              entityInQueryIndex,
                            ref TimeToSubdiv tts,
                            ref WaveHeight   height,
                            ref DispersAngle angle,
                            in  WaveDir      waveDir,
                            in  WavePos      wavePos,
                            in  WaveOrigin   waveOrigin ) =>
                {
                    tts.Value -= dTime;

                    if( tts.Value > 0 )
                    {
                        // Particle is not ready for subdivision
                        return;
                    }

                    if( height.Value < cWPMinHeight )
                    {
                        // Particle reached min height, schedule it for deletion
                        deleteECB.DestroyEntity( entityInQueryIndex, entity );
                        return;
                    }

                    float newAngle  = angle.Value  / 3.0f;
                    float newHeight = height.Value / 3.0f;
                    // Particle need to be subdivided when gap between two particles become visible
                    // More details on Page 101: http://www.cemyuksel.com/research/waveparticles/cem_yuksel_dissertation.pdf
                    float totalSubdivDistance = cWPRadius / ( 3.0f * math.tan( newAngle * 0.5f ) );
                    float distanceTraveled    = math.distance( waveOrigin.Value, wavePos.Value );
                    float distanceToSubdiv    = totalSubdivDistance - distanceTraveled;
                    float timeToSubdiv        = distanceToSubdiv / cWPSpeed;

                    // Create left particle
                    float2 leftWaveDir = math.rotate(
                                                 quaternion.RotateY( -newAngle ),
                                                 new float3( waveDir.Value.x, 0, waveDir.Value.y ) )
                                             .xz;
                    var leftWavePos = new WavePos {Value = waveOrigin.Value + leftWaveDir * distanceTraveled};

                    Entity leftEntity = createECB.CreateEntity( entityInQueryIndex, archetype );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, leftWavePos );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, waveOrigin );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, new WaveDir {Value      = leftWaveDir} );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, new WaveHeight {Value   = newHeight} );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, new WaveSpeed {Value    = cWPSpeed} );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, new DispersAngle {Value = newAngle} );
                    createECB.SetComponent( entityInQueryIndex, leftEntity, new TimeToSubdiv {Value = timeToSubdiv} );

                    // Create right particle
                    float2 rightWaveDir = math.rotate(
                                                  quaternion.RotateY( newAngle ),
                                                  new float3( waveDir.Value.x, 0, waveDir.Value.y ) )
                                              .xz;
                    var rightWavePos = new WavePos {Value = waveOrigin.Value + rightWaveDir * distanceTraveled};

                    Entity rightEntity = createECB.CreateEntity( entityInQueryIndex, archetype );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, rightWavePos );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, waveOrigin );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, new WaveDir {Value      = rightWaveDir} );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, new WaveHeight {Value   = newHeight} );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, new WaveSpeed {Value    = cWPSpeed} );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, new DispersAngle {Value = newAngle} );
                    createECB.SetComponent( entityInQueryIndex, rightEntity, new TimeToSubdiv {Value = timeToSubdiv} );

                    // Modify current particle
                    height.Value = newHeight;
                    angle.Value  = newAngle;
                    tts.Value    = timeToSubdiv;
                } )
                .ScheduleParallel();

            m_EndSimECBSystem.AddJobHandleForProducer( Dependency );
        }

        //-------------------------------------------------------------
    }
}