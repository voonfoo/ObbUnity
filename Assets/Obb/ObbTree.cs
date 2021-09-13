using System.Collections.Generic;
using System.Linq;
using g3;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

public class ObbTree
{
    private DMesh3 _mesh;
    public OrientedBox3d bounds;

    public ObbTree(DMesh3 mesh)
    {
        _mesh = mesh;
    }

    public DMesh3 Mesh => _mesh;

    public void BuildFromCovarianceMatrix(Matrix3d covarianceMatrix)
    {
        Matrix<double> matrix = Matrix<double>.Build.Dense(3, 3, covarianceMatrix.ToBuffer());
        Evd<double> evd = matrix.Evd(Symmetricity.Hermitian);
        Matrix<double> vectors = evd.EigenVectors;
        vectors.NormalizeRows(vectors.L2Norm());

        List<double> list1 = new List<double>();
        List<double> list2 = new List<double>();
        List<double> list3 = new List<double>();
        for (int i = 0; i < _mesh.TriangleCount; i++)
        {
            Vector3d p = new Vector3d();
            Vector3d q = new Vector3d();
            Vector3d r = new Vector3d();
            _mesh.GetTriVertices(i, ref p, ref q, ref r);

            Vector<double> v = Vector<double>.Build.Dense(new double[3] {p[0], p[1], p[2]});
            Vector<double> v0 = Vector<double>.Build.Dense(new double[3] {q[0], q[1], q[2]});
            Vector<double> v1 = Vector<double>.Build.Dense(new double[3] {r[0], r[1], r[2]});

            Vector<double> primes = vectors.Multiply(v);
            Vector<double> primes1 = vectors.Multiply(v0);
            Vector<double> primes2 = vectors.Multiply(v1);

            list1.Add(primes[0]);
            list1.Add(primes1[0]);
            list1.Add(primes2[0]);

            list2.Add(primes[1]);
            list2.Add(primes1[1]);
            list2.Add(primes2[1]);

            list3.Add(primes[2]);
            list3.Add(primes1[2]);
            list3.Add(primes2[2]);
        }

        Vector3d min = new Vector3d(list1.Min(), list2.Min(), list3.Min());
        Vector3d max = new Vector3d(list1.Max(), list2.Max(), list3.Max());

        bounds = new OrientedBox3d
        {
            Min = min,
            Max = max,
            Rotation = new Matrix3d(
                vectors[0, 0], vectors[0, 1], vectors[0, 2],
                vectors[1, 0], vectors[1, 1], vectors[1, 2],
                vectors[2, 0], vectors[2, 1], vectors[2, 2]).Transpose()
        };
    }

    public void Build()
    {
        Vector3d weightedMean = new Vector3d(0, 0, 0);
        double areaSum = 0;
        double c00 = 0;
        double c01 = 0;
        double c02 = 0;
        double c11 = 0;
        double c12 = 0;
        double c22 = 0;

        for (int i = 0; i < _mesh.TriangleCount; i++)
        {
            Vector3d p = new Vector3d();
            Vector3d q = new Vector3d();
            Vector3d r = new Vector3d();
            _mesh.GetTriVertices(i, ref p, ref q, ref r);

            p = new Vector3d(p[0], p[1], p[2]);
            q = new Vector3d(q[0], q[1], q[2]);
            r = new Vector3d(r[0], r[1], r[2]);

            Vector3d mean = (p + q + r) / 3.0;
            double area = Vector3d.Cross((q - p), (r - p)).Normalize() / 2.0;
            weightedMean += mean * area;
            areaSum += area;
            c00 += (9.0 * mean[0] * mean[0] + p[0] * p[0] + q[0] * q[0] + r[0] * r[0]) * (area / 12.0);
            c01 += (9.0 * mean[0] * mean[1] + p[0] * p[1] + q[0] * q[1] + r[0] * r[1]) * (area / 12.0);
            c02 += (9.0 * mean[0] * mean[2] + p[0] * p[2] + q[0] * q[2] + r[0] * r[2]) * (area / 12.0);
            c11 += (9.0 * mean[1] * mean[1] + p[1] * p[1] + q[1] * q[1] + r[1] * r[1]) * (area / 12.0);
            c12 += (9.0 * mean[1] * mean[2] + p[1] * p[2] + q[1] * q[2] + r[1] * r[2]) * (area / 12.0);
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

        Matrix3d covarianceMatrix = new Matrix3d(c00, c01, c02, c01, c11, 0, c02, c12, c22);
        BuildFromCovarianceMatrix(covarianceMatrix);
    }
}