using g3;
using Unity.Mathematics;

public struct OrientedBox3d
{
    public Vector3d Min;
    public Vector3d Max;
    public Matrix3d Rotation;
}

namespace Voon.Obb
{
    public struct NativeOrientedBox3D
    {
        public float3 Min;
        public float3 Max;
        public float3x3 Rotation;
    }
}
