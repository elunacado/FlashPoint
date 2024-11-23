using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class WebClient : MonoBehaviour
{
    public GameObject cube;  // El cubo que se moverá
    private float moveStep = 1.0f;  // Distancia a mover en X con cada espacio

    void Update()
    {
        // Detecta si se presionó la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveCube();
        }
    }

    void MoveCube()
    {
        // Mueve el cubo en el eje X
        cube.transform.position += new Vector3(moveStep, 0, 0);

        // Convierte la nueva posición a JSON y envía al servidor
        Vector3 newPosition = cube.transform.position;
        string json = JsonUtility.ToJson(newPosition);

        Debug.Log($"New Position: {newPosition}");  // Verifica la posición en la consola de Unity
        StartCoroutine(SendData(json));
    }

    IEnumerator SendData(string json)
    {
        using (UnityWebRequest www = new UnityWebRequest("http://localhost:8585", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Position sent successfully!");
                // Maneja la respuesta
                string responseText = www.downloadHandler.text;
                Debug.Log($"Response: {responseText}");
            }
        }
    }
}
