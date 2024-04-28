using Unity.Mathematics;

namespace Voon.Obb
{
    public struct NativeOrientedBox3D
    {
        public float3 Min;
        public float3 Max;
        public float3x3 Rotation;
    }
}
