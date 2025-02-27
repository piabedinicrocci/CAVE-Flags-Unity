using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPlayer : MonoBehaviour
{
    public float Velocidad = 100f;
    float RotacionX = 0;
    public Transform Jugador;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float MouseX = Input.GetAxis("Mouse X") * Velocidad * Time.deltaTime;
        float MouseY = Input.GetAxis("Mouse Y") * Velocidad * Time.deltaTime;

        RotacionX -= MouseY;
        RotacionX = Mathf.Clamp(RotacionX,-90f,90f);

        transform.localRotation = Quaternion.Euler(RotacionX,0f,0f);
        Jugador.Rotate(Vector3.up * MouseX);
    }
}
