using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Voon.Obb
{
    public class ObbTestScene : MonoBehaviour
    {
        public GameObject target;

        private Mesh _unityMesh;
        private Transform _transform;
        private NativeArray<float3> _meshVertices;
        private NativeArray<int> _meshIndices;

        private NativeOrientedBox3D _obbBounds;
        private NativeAxisAlignedBox3D _bounds;
        private bool _treeCreated;

        // mouse drag rotation
        public float scrollSpeed = 0.5f;
        public float rotateSpeed = 0.5f;
        public float panSpeed = 0.1f;

        private Vector3 _lastMousePos;
        private Quaternion _initialRotation;
        private Camera _mainCamera;
        private Stopwatch _stopWatch;

        private void Start()
        {
            _mainCamera = Camera.main;
            _lastMousePos = Input.mousePosition;
            _initialRotation = target.transform.rotation;

            _unityMesh = target.GetComponent<MeshFilter>().mesh;
            _meshVertices = MeshNativeOps.GetNativeArray(_unityMesh.vertices);
            var triangleArray = _unityMesh.triangles;
            _meshIndices = MeshNativeOps.GetNativeArray(triangleArray);
            _treeCreated = true;

            _stopWatch = new Stopwatch();
        }

        private void Update()
        {
            UpdateMeshVerticesOnDrag();

            _stopWatch.Reset();
            _stopWatch.Start();
            NativeObbTree.Build(ref _meshVertices, ref _meshIndices, ref _obbBounds);
            _stopWatch.Stop();
            Debug.Log("Obb build time (burst): " + _stopWatch.ElapsedMilliseconds + "ms");

            var min = _obbBounds.Min;
            var max = _obbBounds.Max;
            _bounds = new NativeAxisAlignedBox3D(min, max);
        }

        private void UpdateMeshVerticesOnDrag()
        {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            Transform x = _mainCamera.transform;

            if (Input.mouseScrollDelta.y != 0)
            {
                if (_mainCamera.orthographic)
                {
                    _mainCamera.orthographicSize = Mathf.Clamp(
                        _mainCamera.orthographicSize - scrollSpeed * Input.mouseScrollDelta.y, 1, 1000);
                }
            }
            else if (Input.GetMouseButton(2))
            {
                _mainCamera.transform.position +=
                    -panSpeed * delta.x * x.right +
                    -panSpeed * delta.y * x.up;
            }
            else if (Input.GetMouseButton(1))
            {
                Quaternion rotatelr = Quaternion.AngleAxis(-rotateSpeed * delta.x, x.up);
                Quaternion rotateud = Quaternion.AngleAxis(rotateSpeed * delta.y, x.right);
                Quaternion newRotation = rotatelr * rotateud * _initialRotation;
                RotateMeshVertices(newRotation * Quaternion.Inverse(_initialRotation));
                _initialRotation = newRotation;
            }

            _lastMousePos = Input.mousePosition;
        }

        private void RotateMeshVertices(Quaternion rotation)
        {
            for (int i = 0; i < _meshVertices.Length; i++)
            {
                _meshVertices[i] = rotation * _meshVertices[i];
            }

            _unityMesh.SetVertices(_meshVertices);
            var triangleArray = _unityMesh.triangles;
            _meshIndices.Dispose();
            _meshIndices = MeshNativeOps.GetNativeArray(triangleArray);
        }

        private void OnDrawGizmos()
        {
            if (!_treeCreated) return;

            Gizmos.matrix = new Matrix4x4()
            {
                m00 = _obbBounds.Rotation.c0.x,
                m01 = _obbBounds.Rotation.c1.x,
                m02 = _obbBounds.Rotation.c2.x,
                m03 = 0,
                m10 = _obbBounds.Rotation.c0.y,
                m11 = _obbBounds.Rotation.c1.y,
                m12 = _obbBounds.Rotation.c2.y,
                m13 = 0,
                m20 = _obbBounds.Rotation.c0.z,
                m21 = _obbBounds.Rotation.c1.z,
                m22 = _obbBounds.Rotation.c2.z,
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

        private void OnDestroy()
        {
            _meshVertices.Dispose();
            _meshIndices.Dispose();
        }
    }
}