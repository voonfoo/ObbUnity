using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voon.Obb
{
    [BurstCompile]
    public static class NativeObbTree
    {
        [BurstCompile]
        public static void Build(ref NativeArray<float3> vertices, ref NativeArray<int> indices, ref NativeOrientedBox3D bounds)
        {
            float3 weightedMean = new float3();
            float areaSum = 0;
            float c00 = 0;
            float c01 = 0;
            float c02 = 0;
            float c11 = 0;
            float c12 = 0;
            float c22 = 0;

            for (int i = 0; i < indices.Length; i += 3)
            {
                float3 p = new float3();
                float3 vertex = vertices[indices[i]];
                p[0] = vertex.x;
                p[1] = vertex.y;
                p[2] = vertex.z;

                float3 q = new float3();
                vertex = vertices[indices[i + 1]];
                q[0] = vertex.x;
                q[1] = vertex.y;
                q[2] = vertex.z;

                float3 r = new float3();
                vertex = vertices[indices[i + 2]];
                r[0] = vertex.x;
                r[1] = vertex.y;
                r[2] = vertex.z;

                float3 mean = (p + q + r) / 3.0f;
                float area = math.length(math.cross((q - p), (r - p))) / 2.0f;
                weightedMean += mean * area;
                areaSum += area;
                c00 += (9.0f * mean[0] * mean[0] + p[0] * p[0] + q[0] * q[0] + r[0] * r[0]) * (area / 12.0f);
                c01 += (9.0f * mean[0] * mean[1] + p[0] * p[1] + q[0] * q[1] + r[0] * r[1]) * (area / 12.0f);
                c02 += (9.0f * mean[0] * mean[2] + p[0] * p[2] + q[0] * q[2] + r[0] * r[2]) * (area / 12.0f);
                c11 += (9.0f * mean[1] * mean[1] + p[1] * p[1] + q[1] * q[1] + r[1] * r[1]) * (area / 12.0f);
                c12 += (9.0f * mean[1] * mean[2] + p[1] * p[2] + q[1] * q[2] + r[1] * r[2]) * (area / 12.0f);
            }

            weightedMean /= areaSum;
            c00 /= areaSum;
            c01 /= areaSum;
            c02 /= areaSum;
            c11 /= areaSum;
            c12 /= areaSum;
            c22 /= areaSum;

            c00 -= weightedMean[0] * weightedMean[0];
            c01 -= weightedMean[0] * weightedMean[1];
            c02 -= weightedMean[0] * weightedMean[2];
            c11 -= weightedMean[1] * weightedMean[1];
            c12 -= weightedMean[1] * weightedMean[2];
            c22 -= weightedMean[2] * weightedMean[2];

            float3x3 covarianceMatrix = new float3x3(c00, c01, c02, c01, c11, c12, c02, 0, c22);
            BuildFromCovarianceMatrix(ref vertices, ref indices, ref covarianceMatrix, ref bounds);
        }

        [BurstCompile]
        private static void BuildFromCovarianceMatrix(ref NativeArray<float3> vertices,
            ref NativeArray<int> indices,
            ref float3x3 covarianceMatrix, ref NativeOrientedBox3D bounds)
        {
            (float3 _, float3x3 eigenVector) = JacobiEvd(covarianceMatrix);

            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < indices.Length; i += 3)
            {
                float3 p = new float3();
                Vector3 vertex = vertices[indices[i]];
                p[0] = vertex.x;
                p[1] = vertex.y;
                p[2] = vertex.z;

                float3 q = new float3();
                vertex = vertices[indices[i + 1]];
                q[0] = vertex.x;
                q[1] = vertex.y;
                q[2] = vertex.z;

                float3 r = new float3();
                vertex = vertices[indices[i + 2]];
                r[0] = vertex.x;
                r[1] = vertex.y;
                r[2] = vertex.z;

                var prime = math.mul(eigenVector, p);
                var prime2 = math.mul(eigenVector, q);
                var prime3 = math.mul(eigenVector, r);

                minX = math.min(minX, prime.x);
                minX = math.min(minX, prime2.x);
                minX = math.min(minX, prime3.x);

                maxX = math.max(maxX, prime.x);
                maxX = math.max(maxX, prime2.x);
                maxX = math.max(maxX, prime3.x);

                minY = math.min(minY, prime.y);
                minY = math.min(minY, prime2.y);
                minY = math.min(minY, prime3.y);

                maxY = math.max(maxY, prime.y);
                maxY = math.max(maxY, prime2.y);
                maxY = math.max(maxY, prime3.y);

                minZ = math.min(minZ, prime.z);
                minZ = math.min(minZ, prime2.z);
                minZ = math.min(minZ, prime3.z);

                maxZ = math.max(maxZ, prime.z);
                maxZ = math.max(maxZ, prime2.z);
                maxZ = math.max(maxZ, prime3.z);
            }

            float3 min = new float3(minX, minY, minZ);
            float3 max = new float3(maxX, maxY, maxZ);

            bounds.Min = min;
            bounds.Max = max;
            bounds.Rotation = math.transpose(eigenVector);
        }


        private static float L2Norm(float3x3 matrix)
        {
            float sum = 0;
            sum += matrix.c0.x * matrix.c0.x;
            sum += matrix.c0.y * matrix.c0.y;
            sum += matrix.c0.z * matrix.c0.z;
            sum += matrix.c1.x * matrix.c1.x;
            sum += matrix.c1.y * matrix.c1.y;
            sum += matrix.c1.z * matrix.c1.z;
            sum += matrix.c2.x * matrix.c2.x;
            sum += matrix.c2.y * matrix.c2.y;
            sum += matrix.c2.z * matrix.c2.z;
            return math.sqrt(sum);
        }

        private static float3x3 NormalizeRows(float3x3 eigenVector, float p)
        {
            float3 r1 = new float3(eigenVector.c0.x, eigenVector.c1.x, eigenVector.c2.x);
            float3 r2 = new float3(eigenVector.c0.y, eigenVector.c1.y, eigenVector.c2.y);
            float3 r3 = new float3(eigenVector.c0.z, eigenVector.c1.z, eigenVector.c2.z);

            r1 = math.normalize(r1) * p;
            r2 = math.normalize(r2) * p;
            r3 = math.normalize(r3) * p;

            return new float3x3(new float3(r1.x, r2.x, r3.x), new float3(r1.y, r2.y, r3.y),
                new float3(r1.z, r2.z, r3.z));
        }

        private static (float3 eigenvalues, float3x3 eigenvectors) JacobiEvd(float3x3 A)
        {
            float3x3 V = float3x3.identity;
            int maxIterations = 100;
            float eps = 1e-10f;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Find largest off-diagonal element A[p][q]
                int p = 0, q = 1;
                if (math.abs(A[2][0]) > math.abs(A[p][q])) q = 2;
                if (math.abs(A[2][1]) > math.abs(A[p][q]))
                {
                    p = 1;
                    q = 2;
                }

                float diff = A[q][q] - A[p][p];
                float div = math.abs(diff) < eps ? math.abs(A[p][q]) : math.sqrt(diff * diff + 4 * A[p][q] * A[p][q]);
                float t = diff < 0 ? -2 * A[p][q] / (diff - div) : 2 * A[p][q] / (diff + div);
                float c = 1 / math.sqrt(t * t + 1);
                float s = c * t;

                // Apply rotation
                for (int i = 0; i < 3; i++)
                {
                    float vip = V[i][p], viq = V[i][q];
                    V[i][p] = vip * c - viq * s;
                    V[i][q] = vip * s + viq * c;
                }

                float app = A[p][p], apq = A[p][q], aqq = A[q][q];
                A[p][p] = app * c * c - 2 * apq * c * s + aqq * s * s;
                A[q][q] = app * s * s + 2 * apq * c * s + aqq * c * c;
                A[p][q] = A[q][p] = 0;

                // Check for convergence
                if (math.abs(A[1][0]) < eps && math.abs(A[2][0]) < eps && math.abs(A[2][1]) < eps)
                    break;
            }

            return (new float3(A[0][0], A[1][1], A[2][2]), V);
        }
    }
}