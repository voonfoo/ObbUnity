using UnityEngine;
using g3;
using Voon.Obb;

public class MeshSelector : MonoBehaviour
{
    public GameObject target;

    private Mesh _unityMesh;
    private DMesh3 _mesh;
    private Transform _transform;

    private DMeshAABBTree3 _aabbTree3;
    private ObbTree _obbTree;
    private AxisAlignedBox3d _bounds;
    private Quaternion _currRotation;

    private NativeObbTree _voonTree;

    private void Start()
    {
        _unityMesh = target.GetComponent<MeshFilter>().mesh;
        _voonTree = new NativeObbTree();
        _voonTree.unityMesh = _unityMesh;
        _mesh = g3UnityUtils.UnityMeshToDMesh(_unityMesh);

        _aabbTree3 = new DMeshAABBTree3(_mesh, true);
        _currRotation = target.transform.rotation;

        _obbTree = new ObbTree(_mesh);
    }
    
    public void RotateMeshVertices(Mesh mesh, Quaternion rotation)
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = rotation * vertices[i];
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private async void Update()
    {
        if (!_currRotation.Equals(target.transform.rotation))
        {
            var rotation = target.transform.rotation;
            Debug.Log("rotated: " + (rotation * Quaternion.Inverse(_currRotation)).eulerAngles);
            // RotateMeshVertices(_unityMesh, rotation * Quaternion.Inverse(_currRotation));
            // _voonTree.unityMesh = _unityMesh;
            MeshTransforms.Rotate(_mesh, Vector3d.Zero, rotation * Quaternion.Inverse(_currRotation));
            _currRotation = rotation;
        }

        _aabbTree3.Build();
        _bounds = _aabbTree3.Bounds;
        float boundsX = (float) (-_bounds.Min.x + _bounds.Max.x);
        float boundsY = (float) (-_bounds.Min.y + _bounds.Max.y);
        float boundsZ = (float) (-_bounds.Min.z + _bounds.Max.z);
        
        DebugPlus.DrawWireCube(
            new Vector3(boundsX / 2 + (float) _bounds.Min.x,boundsY / 2 + (float) _bounds.Min.y, boundsZ / 2 + (float) _bounds.Min.z),
            new Vector3(boundsX, boundsY, boundsZ)).Color(Color.blue);
        
        _obbTree.Build();
        _bounds = new AxisAlignedBox3d(_obbTree.bounds.Min, _obbTree.bounds.Max);
        // _voonTree.Build();
        // var min = _voonTree.Bounds.Min;
        // var max = _voonTree.Bounds.Max;
        //
        // _bounds = new AxisAlignedBox3d(new Vector3d(min.x, min.y, min.z),new Vector3d(max.x, max.y, max.z));
        // boundsX = (float) (-_bounds.Min.x + _bounds.Max.x);
        // boundsY = (float) (-_bounds.Min.y + _bounds.Max.y);
        // boundsZ = (float) (-_bounds.Min.z + _bounds.Max.z);

        DebugPlus.DrawWireCube(
            new Vector3(boundsX / 2 + (float) _bounds.Min.x,boundsY / 2 + (float) _bounds.Min.y, boundsZ / 2 + (float) _bounds.Min.z),
            new Vector3(boundsX, boundsY, boundsZ)).Matrix(new Matrix4x4()
        {
            m00 = (float) _obbTree.bounds.Rotation[0, 0], 
            m01 = (float) _obbTree.bounds.Rotation[0, 1], 
            m02 = (float) _obbTree.bounds.Rotation[0, 2], 
            m03 = 0, 
            m10 = (float) _obbTree.bounds.Rotation[1, 0], 
            m11 = (float) _obbTree.bounds.Rotation[1, 1], 
            m12 = (float) _obbTree.bounds.Rotation[1, 2], 
            m13 = 0, 
            m20 = (float) _obbTree.bounds.Rotation[2, 0], 
            m21 = (float) _obbTree.bounds.Rotation[2, 1], 
            m22 = (float) _obbTree.bounds.Rotation[2, 2], 
            m23 = 0, 
            m30 = 0, 
            m31 = 0, 
            m32 = 0, 
            m33 = 1, 
        });
        
        // DebugPlus.DrawWireCube(
        //     new Vector3(boundsX / 2 + (float) _bounds.Min.x,boundsY / 2 + (float) _bounds.Min.y, boundsZ / 2 + (float) _bounds.Min.z),
        //     new Vector3(boundsX, boundsY, boundsZ)).Matrix(new Matrix4x4()
        // {
        //     m00 = (float) _voonTree.Bounds.Rotation.c0.x, 
        //     m01 = (float) _voonTree.Bounds.Rotation.c1.x,
        //     m02 = (float) _voonTree.Bounds.Rotation.c2.x, 
        //     m03 = 0, 
        //     m10 = (float) _voonTree.Bounds.Rotation.c0.y, 
        //     m11 = (float) _voonTree.Bounds.Rotation.c1.y, 
        //     m12 = (float) _voonTree.Bounds.Rotation.c2.y, 
        //     m13 = 0, 
        //     m20 = (float) _voonTree.Bounds.Rotation.c0.z, 
        //     m21 = (float) _voonTree.Bounds.Rotation.c1.z, 
        //     m22 = (float) _voonTree.Bounds.Rotation.c2.z, 
        //     m23 = 0, 
        //     m30 = 0, 
        //     m31 = 0, 
        //     m32 = 0, 
        //     m33 = 1, 
        // });
       
    }



}