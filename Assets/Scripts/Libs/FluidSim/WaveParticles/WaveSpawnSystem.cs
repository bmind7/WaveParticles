using OneBitLab.Services;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace OneBitLab.FluidSim
{
    [AlwaysUpdateSystem]
    public class WaveSpawnSystem : SystemBase
    {
        //-------------------------------------------------------------
        public const float c_WaveParticleRadius = 0.15f;
        public const float c_WaveParticleHeight = 0.05f;
        public const float c_WaveParticleSpeed  = 0.5f;

        private const int c_StartEntitiesCount = 0;

        public static float s_WaveParticleMinHeight = c_WaveParticleHeight;

        public float DropsPerSecond
        {
            set
            {
                // TODO: change for math.EPSILON
                // avoid division by 0
                m_DropsInterval = 1.0f / math.max( value, 0.0000001f );
                m_TimeToDrop    = m_DropsInterval;
            }
        }

        public EntityArchetype WaveArchetype => m_Archetype;

        private JobHandle                              m_ExternalDependency;
        private EntityArchetype                        m_Archetype;
        private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;
        private float                                  m_DropsInterval;
        private float                                  m_TimeToDrop = 1000000.0f;
        private Random                                 m_Rnd        = new Random( seed: 1234 );
        private EntityQuery                            m_AllEntitiesQuery;

        //-------------------------------------------------------------
        public void AddExternalDependency( JobHandle newDependency )
        {
            m_ExternalDependency = JobHandle.CombineDependencies( m_ExternalDependency, newDependency );
        }

        //-------------------------------------------------------------
        protected override void OnCreate()
        {
            m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            m_Archetype = EntityManager.CreateArchetype(
                typeof(WaveOrigin),
                typeof(WavePos),
                typeof(WaveDir),
                typeof(WaveHeight),
                typeof(WaveSpeed),
                typeof(DispersAngle),
                typeof(TimeToReflect),
                typeof(TimeToSubdiv)
            );

            var entities = new NativeArray<Entity>( c_StartEntitiesCount, Allocator.Temp );
            EntityManager.CreateEntity( m_Archetype, entities );

            for( int i = 0; i < c_StartEntitiesCount; i++ )
            {
                EntityManager.SetComponentData( entities[ i ], new WavePos {Value = m_Rnd.NextFloat2( -5.0f, 5.0f )} );
                EntityManager.SetComponentData( entities[ i ], new WaveHeight {Value = c_WaveParticleHeight} );
                EntityManager.SetComponentData( entities[ i ], new WaveSpeed {Value = c_WaveParticleSpeed} );
                EntityManager.SetComponentData( entities[ i ],
                                                new WaveDir
                                                {
                                                    Value = math.normalizesafe( m_Rnd.NextFloat2( -1.0f, 1.0f ) )
                                                } );
            }

            entities.Dispose();

            m_AllEntitiesQuery = GetEntityQuery( ComponentType.ReadOnly<WaveHeight>() );
        }

        //-------------------------------------------------------------
        private void UpdateMinParticleHeight()
        {
            int particleCount = m_AllEntitiesQuery.CalculateEntityCountWithoutFiltering();

            // When number of particles is small we can let them live longer
            int subDivNumber = particleCount < 50_000 ? 4 : 3;

            s_WaveParticleMinHeight = c_WaveParticleHeight / math.pow( 3, subDivNumber );
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            UpdateMinParticleHeight();

            m_ExternalDependency.Complete();

            var messageQueue = MessageService.Instance.GetOrCreateMessageQueue<ParticleSpawnMessage>();

            // Check if need to add drops to the spawning queue
            m_TimeToDrop -= Time.DeltaTime;
            // Continue until we have available drops to spawn
            while( m_TimeToDrop < 0 )
            {
                messageQueue.Enqueue( new ParticleSpawnMessage {Pos = m_Rnd.NextFloat3( -5.0f, 5.0f )} );
                // We don't just reset m_TimeToDrop to the m_DropsInterval value,
                // it's done to avoid edge cases when deltaTime grows very big and we miss drops spawning,
                // for that scenario we just increase TimeToDrop and if we still below 0 then spawn agian
                m_TimeToDrop += m_DropsInterval;
            }

            EntityCommandBuffer ecb       = m_EndSimECBSystem.CreateCommandBuffer();
            EntityArchetype     archetype = m_Archetype;

            Dependency = Job
                         .WithCode( () =>
                         {
                             while( messageQueue.TryDequeue( out ParticleSpawnMessage message ) )
                             {
                                 // TODO: change 0.01 for math.EPSILON in future update
                                 for( float rot = 0; rot < 2.0f * math.PI - 0.01f; rot += math.PI / 3.0f )
                                 {
                                     var waveDir = new WaveDir
                                     {
                                         Value = math
                                                 .rotate( quaternion.RotateY( rot ),
                                                          new float3( 1.0f, 0.0f, 0.0f ) )
                                                 .xz
                                     };

                                     float dispersionAngle = math.PI / 3.0f;

                                     // Particle need to be subdivided when gap between two particles become visible
                                     // More details on Page 101: http://www.cemyuksel.com/research/waveparticles/cem_yuksel_dissertation.pdf
                                     float timeToSubdivide =
                                         c_WaveParticleRadius /
                                         ( 2.0f * math.tan( dispersionAngle * 0.5f ) * c_WaveParticleSpeed );

                                     Entity entity = ecb.CreateEntity( archetype );
                                     ecb.SetComponent( entity, new WaveOrigin {Value = message.Pos.xz} );
                                     ecb.SetComponent( entity, new WavePos {Value    = message.Pos.xz} );
                                     ecb.SetComponent( entity, waveDir );
                                     ecb.SetComponent( entity, new WaveHeight {Value   = c_WaveParticleHeight} );
                                     ecb.SetComponent( entity, new WaveSpeed {Value    = c_WaveParticleSpeed} );
                                     ecb.SetComponent( entity, new DispersAngle {Value = dispersionAngle} );
                                     ecb.SetComponent( entity, new TimeToSubdiv {Value = timeToSubdivide} );
                                 }
                             }
                         } )
                         .Schedule( JobHandle.CombineDependencies( Dependency, m_ExternalDependency ) );

            m_EndSimECBSystem.AddJobHandleForProducer( Dependency );
        }

        //-------------------------------------------------------------
    }
}