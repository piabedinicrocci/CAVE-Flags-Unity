using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExperimentLoaderRandom : FlagLoaderBase
{
    public TextMeshProUGUI miTexto;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;
    private Vector3 currentFlagPosition;
    private float flagInstantiatedTime = 0f;
    private int currentIdAprendizaje = 0;
    private int flagsFounded = 0;

    private List<GameObject> instantiatedFlags = new List<GameObject>();
    private List<Prefab> flagsDB = new List<Prefab>();

    private Dictionary<GameObject, Prefab> instantiatedFlagsMap = new Dictionary<GameObject, Prefab>();

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
            isPlayerOnBase = true;
            Debug.Log("Jugador volvió a la base.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            isPlayerOnBase = false;
            Debug.Log("Jugador salió del plano.");
        }
    }

    IEnumerator MostrarEsperarYOcultarTexto()
    {
        miTexto.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        miTexto.gameObject.SetActive(false);

        // REDIRIJO A PROXIMA ESCENA (RANDOM) después de ocultar el texto
        SceneManager.LoadScene(2, LoadSceneMode.Single);
    }

    void Update()
    {
        if (!isPlayerOnBase && instantiatedFlags.Count > 0)
        {
            Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);

            foreach (var kvp in instantiatedFlagsMap)
            {
                GameObject flag = kvp.Key;
                Prefab flagData = kvp.Value;

                if (flag != null)
                {
                    Vector3 flagPosition2D = new Vector3(flag.transform.position.x, 0, flag.transform.position.z);
                    if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
                    {
                        float timeTaken = Time.time - flagInstantiatedTime;
                        Debug.Log($"TIEMPO QUE TARDÓ EN ENCONTRAR LA BANDERA: {timeTaken} segundos.");
                        flagsFounded++;

                        StartCoroutine(UpdateFlagTimeAndRandom(timeTaken, flagData.id));

                        instantiatedFlagsMap.Remove(flag);
                        Destroy(flag);

                        if (flagsFounded == 2)
                        {
                            Debug.Log("¡Felicidades! Has encontrado todas las banderas.");
                            StartCoroutine(MostrarEsperarYOcultarTexto());
                        }
                        break;
                    }
                }
            }
        }
    }

    //IEnumerator UpdateFlagTimeAndRandom(float timeTaken, int flagId)
    //{
    //    // Construir el cuerpo de la solicitud JSON manualmente
    //    string jsonData = "{\"timeTaken\":" + timeTaken.ToString() + "}";
    //    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

    //    // Actualizar tiempo en random
    //    string timeUrl = $"{ApiUrl}/flags/random/{dni}/{flagId}";
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

    //    // Actualizar f_random
    //    string randomUrl = $"{ApiUrl}/flags/randomFlag/{flagId}";
    //    using (UnityWebRequest randomRequest = UnityWebRequest.Put(randomUrl, ""))
    //    {
    //        randomRequest.method = "PUT";
    //        yield return randomRequest.SendWebRequest();

    //        if (randomRequest.result != UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogError("Error al actualizar f_random: " + randomRequest.error);
    //        }
    //    }
    //}

    IEnumerator UpdateFlagTimeAndRandom(float timeTaken, int flagId)
    {
        // Crear el objeto JSON usando JsonUtility
        TimeData data = new TimeData();
        data.timeTaken = timeTaken;
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Actualizar tiempo en random
        string timeUrl = $"{ApiUrl}/flags/random/{dni}/{flagId}";
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

        // Actualizar f_random
        string randomUrl = $"{ApiUrl}/flags/randomFlag/{flagId}";
        using (UnityWebRequest randomRequest = UnityWebRequest.Put(randomUrl, ""))
        {
            randomRequest.method = "PUT";
            yield return randomRequest.SendWebRequest();

            if (randomRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al actualizar f_random: " + randomRequest.error);
            }
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
                currentFlagPosition = new Vector3(nextFlag.positionX, 3, nextFlag.positionZ);
                GameObject newFlag = Instantiate(prefabObject, currentFlagPosition, Quaternion.identity);

                instantiatedFlagsMap[newFlag] = nextFlag;
                instantiatedFlags.Add(newFlag);
                flagsDB.Add(nextFlag);

                flagInstantiatedTime = Time.time;
                currentIdAprendizaje = nextFlag.id;
                Debug.Log($"Instanciado {nextFlag.modelName} con idAprendizaje {nextFlag.id} en posición {currentFlagPosition}.");
            }
            else
            {
                Debug.LogError($"No se encontró el prefab {nextFlag.modelName} en {PrefabsPath}.");
            }
        }

        for (int i = 0; i < 5; i++)
        {
            string modelNameRandom = Random.value > 0.5f ? "flagBlue" : "flagRed";
            string prefabPathRandom = PrefabsPath + modelNameRandom;
            GameObject prefabObjectRandom = Resources.Load<GameObject>(prefabPathRandom);

            if (prefabObjectRandom != null)
            {
                float randomX = Random.Range(-65f, 65f);
                float randomZ = Random.Range(-65f, 65f);
                Vector3 randomPosition = new Vector3(randomX, 3, randomZ);

                GameObject flag = Instantiate(prefabObjectRandom, randomPosition, Quaternion.identity);
                Debug.Log($"Instanciado bandera random {modelNameRandom} en posición {randomPosition}.");
            }
        }
    }
}