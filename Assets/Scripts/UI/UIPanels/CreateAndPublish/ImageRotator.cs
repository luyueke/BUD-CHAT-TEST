using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageRotator : MonoBehaviour
{
    public float rotationSpeed = -360f; // Rotation speed in degrees per second

    private void Update()
    {
        // Calculate the rotation angle based on time and speed
        float angleDelta = rotationSpeed * Time.deltaTime;

        // Rotate the Image around its Z-axis
        transform.Rotate(Vector3.forward, angleDelta);
    }
}
