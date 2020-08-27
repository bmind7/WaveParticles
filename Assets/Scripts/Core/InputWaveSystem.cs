using OneBitLab.FluidSim;
using OneBitLab.Services;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using RaycastHit = Unity.Physics.RaycastHit;

namespace OneBitLab.StormRider.Core
{
    [UpdateAfter( typeof(ExportPhysicsWorld) )]
    [UpdateBefore( typeof(WaveSpawnSystem) )]
    [UpdateBefore( typeof(EndFramePhysicsSystem) )]
    public class InputWaveSystem : SystemBase
    {
        //-------------------------------------------------------------
        private const float c_RaycastDist = 100.0f;

        //-------------------------------------------------------------
        private BuildPhysicsWorld     m_BuildPhysicsWorld;
        private EndFramePhysicsSystem m_EndFramePhysicsSystem;
        private ExportPhysicsWorld    m_ExportPhysicsWorld;
        private WaveSpawnSystem       m_WaveSpawnSystem;

        //-------------------------------------------------------------
        protected override void OnStartRunning()
        {
            m_BuildPhysicsWorld     = World.GetExistingSystem<BuildPhysicsWorld>();
            m_EndFramePhysicsSystem = World.GetExistingSystem<EndFramePhysicsSystem>();
            m_ExportPhysicsWorld    = World.GetExistingSystem<ExportPhysicsWorld>();
            m_WaveSpawnSystem       = World.GetOrCreateSystem<WaveSpawnSystem>();
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            // Handle Keyboard and update spawn rate if needed
            if( Input.GetKeyDown( KeyCode.Alpha1 ) )
            {
                m_WaveSpawnSystem.DropsPerSecond = 1;
            }
            if( Input.GetKeyDown( KeyCode.Alpha2 ) )
            {
                m_WaveSpawnSystem.DropsPerSecond = 10;
            }
            if( Input.GetKeyDown( KeyCode.Alpha3 ) )
            {
                m_WaveSpawnSystem.DropsPerSecond = 30;
            }
            if( Input.GetKeyDown( KeyCode.Alpha4 ) )
            {
                m_WaveSpawnSystem.DropsPerSecond = 100;
            }
            if( Input.GetKeyDown( KeyCode.Alpha0 ) )
            {
                m_WaveSpawnSystem.DropsPerSecond = 0;
            }
            
            // Handle mouse input
            if( !Input.GetMouseButtonDown( 0 ) )
            {
                // Handle only left mouse button clicks
                return;
            }

            UnityEngine.Ray ray = ResourceLocatorService.Instance.m_MainCam.ScreenPointToRay( Input.mousePosition );

            var input = new RaycastInput()
            {
                Start = ray.origin,
                End   = ray.GetPoint( c_RaycastDist ),
                Filter = new CollisionFilter
                {
                    BelongsTo    = ~0u,
                    CollidesWith = 1u << 0, // Water group
                    GroupIndex   = 0
                }
            };
            PhysicsWorld physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;

            NativeQueue<ParticleSpawnMessage>.ParallelWriter messageQueue =
                MessageService.Instance.GetOrCreateMessageQueue<ParticleSpawnMessage>().AsParallelWriter();

            Dependency = Job
                         .WithReadOnly( physicsWorld )
                         .WithCode( () =>
                         {
                             if( physicsWorld.CollisionWorld.CastRay( input, out RaycastHit hit ) )
                             {
                                 // Debug.Log( $"hit: {hit.Position}" );
                                 messageQueue.Enqueue( new ParticleSpawnMessage {Pos = hit.Position} );
                             }
                         } )
                         .Schedule(
                             JobHandle.CombineDependencies( Dependency, m_ExportPhysicsWorld.GetOutputDependency() )
                         );

            // Dependency = new MouseRaycastJob
            // {
            //     physicsWorld = m_BuildPhysicsWorld.PhysicsWorld,
            //     raycastInput = input,
            //     messageQueue = MessageService.Instance.GetOrCreateMessageQueue<ParticleSpawnMessage>()
            //                                  .AsParallelWriter()
            // }.Schedule(
            //     JobHandle.CombineDependencies( Dependency, m_ExportPhysicsWorld.GetOutputDependency() )
            // );

            m_WaveSpawnSystem.AddExternalDependency( Dependency );
            m_EndFramePhysicsSystem.AddInputDependency( Dependency );
        }

        //-------------------------------------------------------------
        // private struct MouseRaycastJob : IJob
        // {
        //     [ReadOnly]
        //     public PhysicsWorld physicsWorld;
        //     [ReadOnly]
        //     public RaycastInput raycastInput;
        //
        //     public NativeQueue<ParticleSpawnMessage>.ParallelWriter messageQueue;
        //
        //     public void Execute()
        //     {
        //         if( physicsWorld.CollisionWorld.CastRay( raycastInput, out RaycastHit hit ) )
        //         {
        //             // Debug.Log( $"hit: {hit.Position}" );
        //             messageQueue.Enqueue( new ParticleSpawnMessage {Pos = hit.Position} );
        //         }
        //     }
        // }

        //-------------------------------------------------------------
    }
}