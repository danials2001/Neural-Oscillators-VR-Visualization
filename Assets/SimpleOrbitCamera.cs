using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simple camera orbit controller
public class SimpleOrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float rotationSpeed = 5f;
    
    private float rotationY = 0f;
    private float rotationX = 30f;
    
    void Start()
    {
        if (target == null)
        {
            // Find the phi visualizer
            target = FindObjectOfType<PhiBinFileVisualizer>()?.transform;
        }
    }
    
    void Update()
    {
        if (target == null) return;
        
        // Mouse input
        if (Input.GetMouseButton(0))
        {
            rotationY += Input.GetAxis("Mouse X") * rotationSpeed;
            rotationX -= Input.GetAxis("Mouse Y") * rotationSpeed;
            rotationX = Mathf.Clamp(rotationX, -80f, 80f);
        }
        
        // Zoom
        distance -= Input.GetAxis("Mouse ScrollWheel") * 2f;
        distance = Mathf.Clamp(distance, 1f, 10f);
        
        // Apply rotation
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        transform.position = target.position + rotation * new Vector3(0, 0, -distance);
        transform.LookAt(target.position);
    }
}
