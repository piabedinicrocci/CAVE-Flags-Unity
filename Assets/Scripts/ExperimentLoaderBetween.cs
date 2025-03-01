using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

public class ExperimentLoaderBetween : MonoBehaviour
{
    private const string PrefabsPath = "Prefabs/";
    private const string ApiUrl = "http://localhost:3000"; // Reemplaza con la URL de tu API

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678;

    private GameObject currentFlag;

    private Vector3 currentFlagPosition;
    private bool playerNailFlag = false;
    private float flagInstantiatedTime = 0f;

    private List<Prefab> flags;

    private List<GameObject> instantiatedFlags = new List<GameObject>();
    private List<Prefab> flagsDB = new List<Prefab>();

    private Dictionary<GameObject, Prefab> instantiatedFlagsMap = new Dictionary<GameObject, Prefab>();

    private Vector3 flag1Position;
    private Vector3 flag2Position;

    [System.Serializable]
    public class Prefab
    {
        public string modelName;
        public float positionX;
        public float positionZ;
        public int id;
    }

    void Start()
    {
        StartCoroutine(LoadFlagsFromApi());
    }

    IEnumerator LoadFlagsFromApi()
    {
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
                        StartCoroutine(UpdateFlagTimeBetween(timeTaken, flagData.id));
                    }
                }
            }
        }
    }

    IEnumerator UpdateFlagTimeBetween(float timeTaken, int flagId)
    {
        Debug.Log("TIEMPOOOOOOOO: " + timeTaken);

        string jsonData = "{\"timeTaken\":" + timeTaken.ToString() + "}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

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

    void SpawnNextFlag()
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