using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExperimentLoaderBetween : FlagLoaderBase
{
    public TextMeshProUGUI miTexto;
    private GameObject currentFlag;

    private Vector3 currentFlagPosition;
    private bool playerNailFlag = false;
    private float flagInstantiatedTime = 0f;

    private List<GameObject> instantiatedFlags = new List<GameObject>();
    private List<Prefab> flagsDB = new List<Prefab>();

    private Dictionary<GameObject, Prefab> instantiatedFlagsMap = new Dictionary<GameObject, Prefab>();

    private Vector3 flag1Position;
    private Vector3 flag2Position;

    void Start()
    {
        // Encontrar todos los objetos con la etiqueta "Text"
        GameObject[] textosGameObjects = GameObject.FindGameObjectsWithTag("Text");
        if (textosGameObjects.Length > 0)
        {
            // Obtener el primer objeto
            GameObject primerTextoGameObject = textosGameObjects[0];
            miTexto = primerTextoGameObject.GetComponent<TextMeshProUGUI>();
            miTexto.gameObject.SetActive(false);
        }

        StartCoroutine(base.LoadFlagsFromApi());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            Debug.Log("Jugador volvió a la base.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            Debug.Log("Jugador salió del plano.");
        }
    }

    IEnumerator MostrarEsperarYOcultarTexto()
    {
        miTexto.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        miTexto.gameObject.SetActive(false);

        // REDIRIJO A PROXIMA ESCENA (RANDOM) después de ocultar el texto
        SceneManager.LoadScene(3, LoadSceneMode.Single);
    }

    void Update()
    {
        if (flag1Position != Vector3.zero && flag2Position != Vector3.zero)
        {
            Vector3 midpoint = (flag1Position + flag2Position) / 2;
            float radius = 1.5f;

            Debug.DrawLine(flag1Position, flag2Position, Color.green);
            DrawCircle(midpoint, radius, Color.red);
        }

        if (Input.GetMouseButtonDown(0))
        {
            string prefabPath = PrefabsPath + "FlagRed";
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);
            Vector3 playerPosition = new Vector3(player.position.x, 3, player.position.z);
            GameObject userFlag = Instantiate(prefabObject, playerPosition, Quaternion.identity);
            playerNailFlag = true;

            foreach (var kvp in instantiatedFlagsMap)
            {
                GameObject flag = kvp.Key;
                Prefab flagData = kvp.Value;

                if (flag != null)
                {
                    if (IsFlagBetween(playerPosition) && playerNailFlag)
                    {
                        float timeTaken = Time.time - flagInstantiatedTime;
                        Debug.Log($"TIEMPO QUE TARDÓ EN CLAVAR LA BANDERA: {timeTaken} segundos.");
                        Debug.Log("¡Felicidades! Clavaste la bandera correctamente!.");
                        StartCoroutine(MostrarEsperarYOcultarTexto());
                        StartCoroutine(UpdateFlagTimeBetween(timeTaken, flagData.id));
                    }
                }
            }
        }
    }

    //IEnumerator UpdateFlagTimeBetween(float timeTaken, int flagId)
    //{
    //    string jsonData = "{\"timeTaken\":" + timeTaken.ToString() + "}";
    //    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

    //    string timeUrl = $"{ApiUrl}/flags/between/{dni}/{flagId}";
    //    using (UnityWebRequest timeRequest = new UnityWebRequest(timeUrl, "PUT"))
    //    {
    //        timeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
    //        timeRequest.downloadHandler = new DownloadHandlerBuffer();
    //        timeRequest.SetRequestHeader("Content-Type", "application/json");

    //        yield return timeRequest.SendWebRequest();

    //        if (timeRequest.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError("Error al actualizar el tiempo: " + timeRequest.error);
    //            Debug.LogError("Respuesta del servidor: " + timeRequest.downloadHandler.text);
    //        }
    //    }

    //    string betweenUrl = $"{ApiUrl}/flags/betweenFlag/{flagId}";
    //    using (UnityWebRequest betweenRequest = UnityWebRequest.Put(betweenUrl, ""))
    //    {
    //        betweenRequest.method = "PUT";
    //        yield return betweenRequest.SendWebRequest();

    //        if (betweenRequest.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError("Error al actualizar f_entre: " + betweenRequest.error);
    //        }
    //    }
    //}

    IEnumerator UpdateFlagTimeBetween(float timeTaken, int flagId)
    {
        // Crear el objeto JSON usando JsonUtility
        TimeData data = new TimeData();
        data.timeTaken = timeTaken;
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Actualizar tiempo en between
        string timeUrl = $"{ApiUrl}/flags/between/{dni}/{flagId}";
        using (UnityWebRequest timeRequest = new UnityWebRequest(timeUrl, "PUT"))
        {
            timeRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            timeRequest.downloadHandler = new DownloadHandlerBuffer();
            timeRequest.SetRequestHeader("Content-Type", "application/json");

            yield return timeRequest.SendWebRequest();

            if (timeRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al actualizar el tiempo: " + timeRequest.error);
                Debug.LogError("Respuesta del servidor: " + timeRequest.downloadHandler.text);
            }
        }

        // Actualizar f_entre
        string betweenUrl = $"{ApiUrl}/flags/betweenFlag/{flagId}";
        using (UnityWebRequest betweenRequest = UnityWebRequest.Put(betweenUrl, ""))
        {
            betweenRequest.method = "PUT";
            yield return betweenRequest.SendWebRequest();

            if (betweenRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al actualizar f_entre: " + betweenRequest.error);
            }
        }
    }

    bool IsFlagBetween(Vector3 userFlagPosition)
    {
        Vector3 midpoint = (flag1Position + flag2Position) / 2;
        float radius = 3f;
        float distanceToCenter = Vector3.Distance(userFlagPosition, midpoint);
        return distanceToCenter <= radius;
    }

    void DrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 50;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        prevPoint.y = 3;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            newPoint.y = 3;

            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }

    protected override void SpawnNextFlag()
    {
        if (flags.Count < 2)
        {
            Debug.LogWarning("No hay suficientes banderas para instanciar.");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            Prefab nextFlag = flags[i];
            string prefabPath = PrefabsPath + nextFlag.modelName;
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);

            if (prefabObject != null)
            {
                Vector3 position = new Vector3(nextFlag.positionX, 3, nextFlag.positionZ);
                GameObject instantiatedFlag = Instantiate(prefabObject, position, Quaternion.identity);

                instantiatedFlags.Add(instantiatedFlag);
                instantiatedFlagsMap[instantiatedFlag] = nextFlag;
                flagsDB.Add(nextFlag);

                Debug.Log($"Instanciado {nextFlag.modelName} con idAprendizaje {nextFlag.id} en posición {position}.");

                if (i == 0)
                {
                    flag1Position = position;
                    Debug.Log("FLAG1POS " + flag1Position);
                }
                else
                {
                    flag2Position = position;
                    Debug.Log("FLAG2POS " + flag2Position);
                }
            }
            else
            {
                Debug.LogError($"No se encontró el prefab {nextFlag.modelName} en {PrefabsPath}.");
            }
        }
    }
}