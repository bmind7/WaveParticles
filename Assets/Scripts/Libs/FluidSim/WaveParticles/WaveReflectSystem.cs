using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace OneBitLab.FluidSim
{
    // Reflection phase of simulation
    // When time to the reflection reaches zero we make sure we really hit the obstacle
    // Then we calculate new direction of particle and dispersion angle
    [UpdateAfter( typeof(ExportPhysicsWorld) )]
    [UpdateAfter( typeof(WaveMoveSystem) )]
    [UpdateAfter( typeof(WaveSubdivideSystem) )]
    [UpdateBefore( typeof(EndFramePhysicsSystem) )]
    public class WaveReflectSystem : SystemBase
    {
        //-------------------------------------------------------------
        private const float c_RayDistance = 100.0f;

        private BuildPhysicsWorld     m_BuildPhysicsWorld;
        private EndFramePhysicsSystem m_EndFramePhysicsSystem;
        private ExportPhysicsWorld    m_ExportPhysicsWorld;

        //-------------------------------------------------------------
        protected override void OnCreate()
        {
            m_BuildPhysicsWorld     = World.GetExistingSystem<BuildPhysicsWorld>();
            m_EndFramePhysicsSystem = World.GetExistingSystem<EndFramePhysicsSystem>();
            m_ExportPhysicsWorld    = World.GetExistingSystem<ExportPhysicsWorld>();
        }

        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            float        dTime        = Time.DeltaTime;
            PhysicsWorld physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;

            const float cWPRadius = WaveSpawnSystem.c_WaveParticleRadius;
            const float cWPSpeed  = WaveSpawnSystem.c_WaveParticleSpeed;
            const float cRayDist  = c_RayDistance;

            Dependency = Entities
                         .WithReadOnly( physicsWorld )
                         .ForEach(
                             ( ref TimeToReflect ttr,
                               ref WaveOrigin    wOrigin,
                               ref WaveDir       wDir,
                               ref DispersAngle  dispAngle,
                               in  WavePos       wPos,
                               in  TimeToSubdiv  tts ) =>
                             {
                                 ttr.Value -= dTime;

                                 if( ttr.Value > 0 )
                                 {
                                     // Particle has not reached any obstacle
                                     return;
                                 }

                                 // TODO: kill particles which is way outside of the bounds

                                 // Raycast from the origin of the particle to find out the reflection point
                                 var start = new float3( wOrigin.Value.x, 0, wOrigin.Value.y );
                                 var dir   = new float3( wDir.Value.x, 0, wDir.Value.y );
                                 var rcInput = new RaycastInput
                                 {
                                     Start = start,
                                     End   = start + cRayDist * dir,
                                     Filter = new CollisionFilter
                                     {
                                         BelongsTo    = ~0u,
                                         CollidesWith = 1u << 1, // Target WaveObstacles
                                         GroupIndex   = 0
                                     }
                                 };

                                 if( !physicsWorld.CollisionWorld.CastRay( rcInput, out RaycastHit hit ) )
                                 {
                                     // No collision, so set TimeToReflect equivalent to traveling max ray distance
                                     // Debug.Log( "No Collision found"  );
                                     ttr.Value = cRayDist / cWPSpeed;
                                     return;
                                 }

                                 if( math.distance( hit.Position.xz, wPos.Value ) > 2.0 * dTime * cWPSpeed )
                                 {
                                     // We are not close to hit point, that means we are procession fresh reflection 
                                     // or newly spawned particle, so just update TimeToReflect and that's it
                                     ttr.Value = math.distance( hit.Position.xz, wPos.Value ) / cWPSpeed;
                                     return;
                                 }

                                 // Particle has reached the reflection point
                                 // Set TimeToReflect to 0 to update it properly next frame
                                 ttr.Value = 0;

                                 // Other value can be correctly calculated right now
                                 // rollback particle a bit to not overshoot the bounds
                                 // wOrigin.Value = wPos.Value - cWPSpeed * dTime * wDir.Value;
                                 wOrigin.Value = hit.Position.xz - cWPSpeed * dTime * wDir.Value;
                                 wDir.Value    = math.reflect( wDir.Value, hit.SurfaceNormal.xz );

                                 // Calculate new dispersion angle
                                 // Considering that waveOrigin is moved we need to recalculate new angle
                                 // to avoid particle discretization. New dispersion angle is equal to 
                                 // the angle between traveled distance and 1/3 of particle radius 
                                 // at the point of subdivision
                                 dispAngle.Value = 2 * math.atan2( cWPRadius / 3.0f, tts.Value * cWPSpeed );
                             } )
                         .ScheduleParallel(
                             JobHandle.CombineDependencies( Dependency, m_ExportPhysicsWorld.GetOutputDependency() )
                         );

            m_EndFramePhysicsSystem.AddInputDependency( Dependency );
        }

        //-------------------------------------------------------------
    }
}