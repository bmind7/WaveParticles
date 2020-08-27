using Unity.Entities;
using Unity.Mathematics;

namespace OneBitLab.FluidSim
{
    //-------------------------------------------------------------
    public struct WaveOrigin : IComponentData
    {
        public float2 Value;
    }
    //-------------------------------------------------------------
    public struct WavePos : IComponentData
    {
        public float2 Value;
    }
    //-------------------------------------------------------------
    public struct WaveDir : IComponentData
    {
        public float2 Value;
    }
    //-------------------------------------------------------------
    public struct WaveHeight : IComponentData
    {
        // We use "byte" because this is raw data format type 
        // when we write directly to Texture2D memory
        public float Value;
    }
    //-------------------------------------------------------------
    public struct WaveSpeed : IComponentData
    {
        public float Value;
    }
    //-------------------------------------------------------------
    public struct DispersAngle : IComponentData
    {
        public float Value;
    }
    //-------------------------------------------------------------
    public struct TimeToReflect : IComponentData
    {
        public float Value;
    }
    //-------------------------------------------------------------
    public struct TimeToSubdiv : IComponentData
    {
        public float Value;
    }
    //-------------------------------------------------------------
}