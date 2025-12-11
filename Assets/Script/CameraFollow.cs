using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;   // the player
    public Vector3 offset = new Vector3(0, 2, -4);
    public float smoothSpeed = 5f;
    public float mouseSensitivity = 100f;
    public float minYAngle = -35f;
    public float maxYAngle = 60f;

    public float yaw;
    public float pitch;

    private float xRotation = 0f;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        // Initialize yaw to match the current camera rotation
        if (target != null)
        {
            yaw = transform.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Update yaw and pitch
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        // Calculate camera rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // Calculate desired camera position
        Vector3 desiredPosition = target.position + rotation * offset;

        // Smoothly move camera to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Make camera look at target
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
