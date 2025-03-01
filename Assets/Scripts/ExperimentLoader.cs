using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

public class ExperimentLoader : MonoBehaviour
{
    private const string PrefabsPath = "Prefabs/";
    private const string ApiUrl = "http://localhost:3000";

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678;

    private int currentFlagIndex = 0;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;
    private float flagInstantiatedTime = 0f;
    private int currentIdAprendizaje = 0;

    private List<Prefab> flags;

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
            isPlayerOnBase = true;
            Debug.Log("Jugador volvi� a la base.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            isPlayerOnBase = false;
            Debug.Log("Jugador sali� del plano.");
        }
    }

    void Update()
    {
        if (!isPlayerOnBase && currentFlag != null)
        {
            Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);
            Vector3 flagPosition2D = new Vector3(currentFlag.transform.position.x, 0, currentFlag.transform.position.z);

            if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
            {
                float timeTaken = Time.time - flagInstantiatedTime;
                Debug.Log($"TIEMPO QUE TARD� EN ENCONTRAR LA BANDERA: {timeTaken} segundos.");

                StartCoroutine(UpdateFlagTimeLearning(timeTaken, currentIdAprendizaje));

                currentFlag.SetActive(false);
                currentFlag = null;

                SpawnNextFlag();
            }
        }
    }

    IEnumerator UpdateFlagTimeLearning(float timeTaken, int flagId)
    {
        string jsonData = "{\"timeTaken\":" + timeTaken.ToString() + "}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        string timeUrl = $"{ApiUrl}/flags/learning/{dni}/{flagId}";
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
    }

    void SpawnNextFlag()
    {
        if (currentFlagIndex >= flags.Count)
        {
            Debug.Log("Todas las banderas han sido instanciadas y encontradas.");
            return;
        }

        Prefab flagData = flags[currentFlagIndex];
        string prefabPath = PrefabsPath + flagData.modelName;
        GameObject prefabObject = Resources.Load<GameObject>(prefabPath);

        if (prefabObject != null)
        {
            Vector3 flagPosition = new Vector3(flagData.positionX, 3, flagData.positionZ);
            currentFlag = Instantiate(prefabObject, flagPosition, Quaternion.identity);

            currentIdAprendizaje = flagData.id;
            flagInstantiatedTime = Time.time;

            Debug.Log($"Instanciado {flagData.modelName} con idAprendizaje {flagData.id} en posici�n {flagPosition}.");

            currentFlagIndex++;
        }
        else
        {
            Debug.LogError($"No se encontr� el prefab {flagData.modelName} en {PrefabsPath}.");
        }
    }
}