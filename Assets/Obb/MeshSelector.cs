using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using g3;

public class MeshSelector : MonoBehaviour
{
    public GameObject target;

    private Mesh _unityMesh;
    private DMesh3 _mesh;
    private MeshFaceSelection _faceSelection;
    private Transform _transform;

    private DMeshAABBTree3 _aabbTree3;
    private ObbTree _obbTree;
    private AxisAlignedBox3d _bounds;
    private Quaternion _currRotation;

    private void Start()
    {
        _unityMesh = target.GetComponent<MeshFilter>().mesh;
        _mesh = g3UnityUtils.UnityMeshToDMesh(_unityMesh);

        _faceSelection = new MeshFaceSelection(_mesh);

        _aabbTree3 = new DMeshAABBTree3(_mesh, true);
        _currRotation = target.transform.rotation;

        _obbTree = new ObbTree(_mesh);
    }

    private async void Update()
    {
        if (!_currRotation.Equals(target.transform.rotation))
        {
            var rotation = target.transform.rotation;
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
        boundsX = (float) (-_bounds.Min.x + _bounds.Max.x);
        boundsY = (float) (-_bounds.Min.y + _bounds.Max.y);
        boundsZ = (float) (-_bounds.Min.z + _bounds.Max.z);

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

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _transform = hit.collider.transform;
                ResetMeshColour(_unityMesh);

                Color[] colourArray = new Color[_unityMesh.vertexCount];
                for (int i = 0; i < colourArray.Length; i++)
                {
                    colourArray[i] = Color.gray;
                }

                await _faceSelection.FloodFillAsync(hit.triangleIndex, (id) => TriangleFilter(id, _transform), null,
                    x => { ColourTriangle(x, colourArray); });
            }
            else
            {
                ResetMeshColour(_unityMesh);
            }

            _faceSelection.DeselectAll();
        }
    }

    private void ColourTriangle(int tId, Color[] colourArray)
    {
        var vertIndex1 = _unityMesh.triangles[tId * 3 + 0];
        var vertIndex2 = _unityMesh.triangles[tId * 3 + 1];
        var vertIndex3 = _unityMesh.triangles[tId * 3 + 2];

        colourArray[vertIndex1] = Color.cyan;
        colourArray[vertIndex2] = Color.cyan;
        colourArray[vertIndex3] = Color.cyan;

        _unityMesh.colors = colourArray;
    }

    private bool TriangleFilter(int tID, Transform tf)
    {
        bool res = true;
        Index3i indexes = _mesh.GetTriNeighbourTris(tID);
        double angle = 20;
        if (indexes[0] != DMesh3.InvalidID && indexes[1] != DMesh3.InvalidID)
        {
            res &= IsAngleWithinLimits(indexes[0], indexes[1], angle, tf);
        }

        if (indexes[1] != DMesh3.InvalidID && indexes[2] != DMesh3.InvalidID)
        {
            res &= IsAngleWithinLimits(indexes[1], indexes[2], angle, tf);
        }

        if (indexes[2] != DMesh3.InvalidID && indexes[0] != DMesh3.InvalidID)
        {
            res &= IsAngleWithinLimits(indexes[2], indexes[0], angle, tf);
        }

        return res;
    }

    private bool IsAngleWithinLimits(int id1, int id2, double angle, Transform tf)
    {
        double rad = angle * Mathf.Deg2Rad;
        Vector3 norm1 = ComputeNormal(id1, tf);
        Vector3 norm2 = ComputeNormal(id2, tf);
        double ang = norm1.x * norm2.x + norm1.y * norm2.y + norm1.z * norm2.z;
        return System.Math.Acos(ang) < rad;
    }

    private Vector3 ComputeNormal(int idx, Transform tf)
    {
        var vertIndex1 = _unityMesh.triangles[idx * 3 + 0];
        var vertIndex2 = _unityMesh.triangles[idx * 3 + 1];
        var vertIndex3 = _unityMesh.triangles[idx * 3 + 2];

        var vert1 = _unityMesh.vertices[vertIndex1];
        var vert2 = _unityMesh.vertices[vertIndex2];
        var vert3 = _unityMesh.vertices[vertIndex3];

        vert1 = tf.TransformPoint(vert1);
        vert2 = tf.TransformPoint(vert2);
        vert3 = tf.TransformPoint(vert3);

        // var center = (vert1 + vert2 + vert3) / 3;
        Vector3 norm = Vector3.Cross(vert2 - vert1, vert3 - vert1).normalized;
        // Debug.DrawLine(center, center + (norm * 10), Color.blue);

        return norm;
    }

    private void ResetMeshColour(Mesh mesh)
    {
        Color[] colourArray = new Color[mesh.vertexCount];
        for (int i = 0; i < colourArray.Length; i++)
        {
            colourArray[i] = Color.gray;
        }

        mesh.colors = colourArray;
    }
}