using UnityEngine;
using Voon.Obb;

public class MeshSelector : MonoBehaviour
{
    public GameObject target;

    private Mesh _unityMesh;
    private Transform _transform;
    private NativeAxisAlignedBox3D _bounds;
    private NativeObbTree _nativeObbTree;

    private void Start()
    {
        _unityMesh = target.GetComponent<MeshFilter>().mesh;
        _nativeObbTree = new NativeObbTree();
        _nativeObbTree.unityMesh = _unityMesh;
    }

    public void RotateMeshVertices(Quaternion rotation)
    {
        Vector3[] vertices = _unityMesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = rotation * vertices[i];
        }

        _unityMesh.vertices = vertices;
    }

    private void Update()
    {
        var stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
        _nativeObbTree.Build();
        stopWatch.Stop();
        Debug.Log("Build time: " + stopWatch.ElapsedMilliseconds + "ms");
        var min = _nativeObbTree.Bounds.Min;
        var max = _nativeObbTree.Bounds.Max;
        _bounds = new NativeAxisAlignedBox3D(min, max);
    }

    private void OnDrawGizmos()
    {
        if (_nativeObbTree == null)
        {
            return;
        }

        Gizmos.matrix = new Matrix4x4()
        {
            m00 = _nativeObbTree.Bounds.Rotation.c0.x,
            m01 = _nativeObbTree.Bounds.Rotation.c1.x,
            m02 = _nativeObbTree.Bounds.Rotation.c2.x,
            m03 = 0,
            m10 = _nativeObbTree.Bounds.Rotation.c0.y,
            m11 = _nativeObbTree.Bounds.Rotation.c1.y,
            m12 = _nativeObbTree.Bounds.Rotation.c2.y,
            m13 = 0,
            m20 = _nativeObbTree.Bounds.Rotation.c0.z,
            m21 = _nativeObbTree.Bounds.Rotation.c1.z,
            m22 = _nativeObbTree.Bounds.Rotation.c2.z,
            m23 = 0,
            m30 = 0,
            m31 = 0,
            m32 = 0,
            m33 = 1,
        };
        Gizmos.color = Color.red;

        var boundsX = (-_bounds.Min.x + _bounds.Max.x);
        var boundsY = (-_bounds.Min.y + _bounds.Max.y);
        var boundsZ = (-_bounds.Min.z + _bounds.Max.z);
        Gizmos.DrawWireCube(
            new Vector3(boundsX / 2 + _bounds.Min.x, boundsY / 2 + _bounds.Min.y,
                boundsZ / 2 + _bounds.Min.z),
            new Vector3(boundsX, boundsY, boundsZ));
    }
}