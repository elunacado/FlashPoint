using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Wall
{
    public int x;
    public int y;
    public Walls Walls;
}

[System.Serializable]
public class Walls
{
    public bool top;
    public bool left;
    public bool bottom;
    public bool right;
}

[System.Serializable]
public class ServerResponse
{
    public List<Wall> Walls;
   // public List<List<object>> Poi;
    //public List<List<int>> Goo;
    //public List<List<int>> Doors;
    //public List<List<int>> Entry_points;
}

public class WebClient : MonoBehaviour
{
    public GameObject horizontalWallPrefab;
    public GameObject verticalWallPrefab;
   
    
    void Start()
    {
        // Llama al servidor al iniciar el juego
        StartCoroutine(SendRequest());
    }

    IEnumerator SendRequest()
    {
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm("http://localhost:8585", ""))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Maneja la respuesta
                string responseText = www.downloadHandler.text;

                // Procesar el JSON recibido
                ProcessReceivedJson(responseText);
                
                
            
            }
        }
    }

    void ProcessReceivedJson(string json)
    {
        try
        {
            // Deserializa el JSON recibido
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);
            
            // Accede a la lista Walls y muestra su contenido
            foreach (Wall wall in response.Walls)
            {
                // Instanciar prefabs dependiendo de la información de la pared
                Vector3 position = new Vector3(wall.x*-1, 0, wall.y*-1);
                if( wall.Walls.bottom && wall.Walls.left && wall.Walls.right){
                    //WORKS
                    Debug.Log("Bottom, left and right");
                    Instantiate(horizontalWallPrefab, position + new Vector3(-.894f,0,0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if(wall.Walls.top && wall.Walls.left && wall.Walls.right){
                    Debug.Log("Top, left and right");
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);

                }
                else if (wall.Walls.bottom && wall.Walls.left){
                    Debug.Log("Bottom and left");
                    Instantiate(horizontalWallPrefab, position + new Vector3(-0.9f,0,0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                }
                else if (wall.Walls.bottom && wall.Walls.right){
                    Debug.Log("Bottom and right");
                    Instantiate(horizontalWallPrefab, position + new Vector3(-0.9f,0,0), Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.Walls.top && wall.Walls.left){
                    Debug.Log("Top and left");
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);

                }
                else if (wall.Walls.top && wall.Walls.right){
                    Debug.Log("Top and right");
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else if (wall.Walls.top){
                    Debug.Log("Top");
                    Instantiate(horizontalWallPrefab, position, Quaternion.identity);
                }
                else if (wall.Walls.bottom){
                    Debug.Log("Bottom");
                    Instantiate(horizontalWallPrefab, position + new Vector3(-0.9f,0,0), Quaternion.identity);
                }
                else if (wall.Walls.left)
                {
                    Debug.Log("Left");
                    // Desplaza -.5 en x y .5 en z
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, 0.5f), Quaternion.identity);
                }
                else if (wall.Walls.right)
                {   
                    Debug.Log("Right");
                    // Desplaza -.5 en x y .5 en z
                    Instantiate(verticalWallPrefab, position + new Vector3(-0.5f, 0, -0.5f), Quaternion.identity);
                }
                else{
                    Debug.Log("No walls");
                };

            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error parsing JSON: {ex.Message}");
        }
    }
}