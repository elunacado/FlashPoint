using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;  // Velocidad de movimiento de la cámara
    public float zoomSpeed = 20f;  // Velocidad de zoom
    public float minZoom = 5f;  // Distancia mínima de zoom
    public float maxZoom = 50f;  // Distancia máxima de zoom
    public float rotationSpeed = 100f;  // Velocidad de rotación de la cámara

    private float currentZoom = 20f;  // Zoom actual

    void Update()
    {
        // Movimiento
        float moveHorizontal = Input.GetAxis("Horizontal");  // Teclas A/D o flechas izquierda/derecha
        float moveVertical = Input.GetAxis("Vertical");  // Teclas W/S o flechas arriba/abajo
        Vector3 moveDirection = new Vector3(moveHorizontal, 0f, moveVertical).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");  // Rueda del ratón
        currentZoom -= scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        Camera.main.fieldOfView = currentZoom;

        // Rotación
        if (Input.GetMouseButton(1))  // Botón derecho del ratón
        {
            float rotateHorizontal = Input.GetAxis("Mouse X");
            float rotateVertical = Input.GetAxis("Mouse Y");

            transform.Rotate(Vector3.up, rotateHorizontal * rotationSpeed * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right, -rotateVertical * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}