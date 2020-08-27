using Unity.Entities;

namespace OneBitLab.FluidSim
{
    [UpdateBefore( typeof(WaveSubdivideSystem) )]
    public class WaveMoveSystem : SystemBase
    {
        //-------------------------------------------------------------
        protected override void OnUpdate()
        {
            float dTime = Time.DeltaTime;

            Entities
                .ForEach( ( ref WavePos   wPos,
                            in  WaveDir   wDir,
                            in  WaveSpeed wSpeed ) =>
                {
                    wPos.Value = wPos.Value + dTime * wSpeed.Value * wDir.Value;
                } )
                .ScheduleParallel();
        }

        //-------------------------------------------------------------
    }
}