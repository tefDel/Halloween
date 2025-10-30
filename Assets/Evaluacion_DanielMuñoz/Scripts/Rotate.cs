using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    public float rotationSpeed = 20f; // Velocidad de rotaci�n de la plataforma

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
