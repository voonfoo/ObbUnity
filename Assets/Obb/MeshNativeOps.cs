using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Voon.Obb
{
    public static class MeshNativeOps
    {
        public static unsafe NativeArray<float3> GetNativeArray(Vector3[] source)
        {
            NativeArray<float3> verts = new NativeArray<float3>(source.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            fixed (void* vertexBufferPointer = source)
            {
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(verts),
                    vertexBufferPointer, source.Length * (long) UnsafeUtility.SizeOf<float3>());
            }

            return verts;
        }

        public static unsafe NativeArray<int> GetNativeArray(int[] source)
        {
            NativeArray<int> verts =
                new NativeArray<int>(source.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            fixed (void* vertexBufferPointer = source)
            {
                UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(verts),
                    vertexBufferPointer, source.Length * (long) UnsafeUtility.SizeOf<int>());
            }

            return verts;
        }
    }
}
