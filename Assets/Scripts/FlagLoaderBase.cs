using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class FlagLoaderBase : MonoBehaviour
{
    protected const string PrefabsPath = "Prefabs/";
    protected const string ApiUrl = "http://localhost:3000";

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678;

    protected List<Prefab> flags;

    [System.Serializable]
    public class Prefab
    {
        public string modelName;
        public float positionX;
        public float positionZ;
        public int id;
    }

    [System.Serializable]
    public class TimeData
    {
        public float timeTaken;
    }

    [System.Serializable]
    public class DniResponse
    {
        public long dni;
    }

    protected IEnumerator GetDniFromApi()
    {
        string url = $"{ApiUrl}/config/dni";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<DniResponse>(webRequest.downloadHandler.text);
                    dni = response.dni;
                    Debug.Log("DNI obtenido de la API: " + dni);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Error al deserializar la respuesta del DNI: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("Error al obtener el DNI de la API: " + webRequest.error);
            }
        }
    }

    protected IEnumerator LoadFlagsFromApi()
    {
        yield return StartCoroutine(GetDniFromApi());

        string url = $"{ApiUrl}/flags/{dni}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                flags = JsonConvert.DeserializeObject<List<Prefab>>(webRequest.downloadHandler.text);

                if (flags.Count > 0)
                {
                    SpawnNextFlag();
                }
                else
                {
                    Debug.LogWarning("No hay banderas para instanciar.");
                }
            }
            else
            {
                Debug.LogError("Error al cargar las banderas: " + webRequest.error);
            }
        }
    }

    protected virtual void SpawnNextFlag()
    {
    }
}