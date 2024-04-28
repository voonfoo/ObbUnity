using Unity.Mathematics;

namespace Voon.Obb
{
    public struct NativeAxisAlignedBox3D
    {
        public float3 Min;
        public float3 Max;

        public NativeAxisAlignedBox3D(float3 min, float3 max)
        {
            Min = new float3(math.min(min.x, max.x), math.min(min.y, max.y), math.min(min.z, max.z));
            Max = new float3(math.max(min.x, max.x), math.max(min.y, max.y), math.max(min.z, max.z));
        }
    }
}