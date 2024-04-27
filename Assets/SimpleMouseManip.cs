using System;
using UnityEngine;

public class SimpleMouseManip : MonoBehaviour
{
    public GameObject TargetObject;

    public float ScrollSpeed = 1.0f;
    public float RotateSpeed = 1.0f;
    public float PanSpeed = 0.1f;

    public event Action OnRelease;

    Vector3 last_mouse_pos;
    Quaternion initial_rotation;

    Camera mainCamera;

    public void Start()
    {
        mainCamera = Camera.main;
        last_mouse_pos = Input.mousePosition;
        initial_rotation = TargetObject.transform.rotation;
    }

    public void Update()
    {
        Vector3 delta = Input.mousePosition - last_mouse_pos;
        Transform x = mainCamera.transform;

        if (Input.mouseScrollDelta.y != 0)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize - ScrollSpeed * Input.mouseScrollDelta.y, 1, 1000);
            }
        }
        else if (Input.GetMouseButton(2))
        {
            mainCamera.transform.position +=
                (-PanSpeed * delta.x * x.right) +
                (-PanSpeed * delta.y * x.up);
        }
        else if (Input.GetMouseButton(1))
        {
            Quaternion rotatelr = Quaternion.AngleAxis(-RotateSpeed * delta.x, x.up);
            Quaternion rotateud = Quaternion.AngleAxis(RotateSpeed * delta.y, x.right);
            Quaternion cur_rotation = initial_rotation;
            Quaternion new_rotation = rotatelr * rotateud * cur_rotation;
            FindObjectOfType<MeshSelector>().RotateMeshVertices(new_rotation * Quaternion.Inverse(initial_rotation));
            initial_rotation = new_rotation;
            // TargetObject.transform.rotation = new_rotation;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            OnRelease?.Invoke();
        }

        last_mouse_pos = Input.mousePosition;
    }
}