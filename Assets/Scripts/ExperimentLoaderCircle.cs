using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExperimentLoaderCircle : FlagLoaderBase
{
    public TextMeshProUGUI miTexto;
    private const string BlueFlagPrefabName = "FlagBlue";

    private List<GameObject> instantiatedFlags = new List<GameObject>();
    private Dictionary<GameObject, Prefab> instantiatedFlagsMap = new Dictionary<GameObject, Prefab>();
    private List<Prefab> flagsDB = new List<Prefab>();

    private Vector3 flag1Position;
    private Vector3 flag2Position;

    private float startTime;
    private Dictionary<int, float> flagFoundTimes = new Dictionary<int, float>();
    private List<int> foundFlagIds = new List<int>();

    void Start()
    {
        // Encuentra todos los objetos con la etiqueta "Text"
        GameObject[] textosGameObjects = GameObject.FindGameObjectsWithTag("Text");
        if (textosGameObjects.Length > 0)
        {
            // Obtiene el primer texto (texto de experimento finalizado)
            GameObject primerTextoGameObject = textosGameObjects[0];
            miTexto = primerTextoGameObject.GetComponent<TextMeshProUGUI>();
            miTexto.gameObject.SetActive(false);
        }

        if (basePlane != null)
        {
            basePlane.gameObject.SetActive(false);
        }

        StartCoroutine(base.LoadFlagsFromApi());
    }

    void Update()
    {
        CheckForFlags();
    }

    IEnumerator MostrarEsperarYOcultarTexto()
    {
        miTexto.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
    }

    void CheckForFlags()
    {
        if (player == null || instantiatedFlagsMap.Count == 0) return;

        Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);

        List<GameObject> flagsToRemove = new List<GameObject>();

        foreach (var kvp in instantiatedFlagsMap)
        {
            GameObject flag = kvp.Key;
            Prefab flagData = kvp.Value;

            if (flag != null)
            {
                Vector3 flagPosition2D = new Vector3(flag.transform.position.x, 0, flag.transform.position.z);
                if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
                {
                    float timeTaken = Time.time - startTime;
                    if (foundFlagIds.Count > 0)
                    {
                        timeTaken = Time.time - flagFoundTimes[foundFlagIds.Last()];
                    }

                    Debug.Log($"TIEMPO QUE TARDÓ EN ENCONTRAR LA BANDERA CON ID {flagData.id}: {timeTaken} segundos.");

                    flagFoundTimes[flagData.id] = Time.time;
                    foundFlagIds.Add(flagData.id);

                    StartCoroutine(UpdateFlagTimeCircle(timeTaken, flagData.id));

                    flagsToRemove.Add(flag);
                    break;
                }
            }
        }

        foreach (GameObject flagToRemove in flagsToRemove)
        {
            instantiatedFlagsMap.Remove(flagToRemove);
            Destroy(flagToRemove);
        }

        if (instantiatedFlagsMap.Count == 0)
        {
            StartCoroutine(MostrarEsperarYOcultarTexto());
            Debug.Log("Jugador encontró todas las banderas exitosamente.");
        }
    }

    // Le pega a la API para actualizar el tiempo_encontrada_circun y su flag
    IEnumerator UpdateFlagTimeCircle(float timeTaken, int flagId)
    {
        // Crea el objeto JSON usando JsonUtility
        TimeData data = new TimeData();
        data.timeTaken = timeTaken;
        string jsonData = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        // Actualiza tiempo en circle
        string timeUrl = $"{ApiUrl}/flags/circle/{dni}/{flagId}";
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

        // Actualiza f_circulo
        string circleUrl = $"{ApiUrl}/flags/circleFlag/{flagId}";
        using (UnityWebRequest circleRequest = UnityWebRequest.Put(circleUrl, ""))
        {
            circleRequest.method = "PUT";
            yield return circleRequest.SendWebRequest();

            if (circleRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al actualizar f_circulo: " + circleRequest.error);
            }
        }
    }

    void HideFlag(Vector3 position)
    {
        foreach (GameObject flag in instantiatedFlags)
        {
            if (Vector3.Distance(flag.transform.position, position) < 0.5f)
            {
                flag.SetActive(false);
                break;
            }
        }
    }

    //void MovePlayerToCircleCenter()
    //{
    //    if (player != null && flag1Position != Vector3.zero && flag2Position != Vector3.zero)
    //    {
    //        Vector3 center = (flag1Position + flag2Position) / 2;
    //        center.y = player.position.y;
    //        player.position = center;
    //        Debug.Log("Jugador movido al centro del círculo.");
    //    }
    //    else
    //    {
    //        Debug.LogWarning("No se pudo mover el jugador al centro del círculo.");
    //    }
    //}

    void SpawnFlagsOnCircle()
    {
        if (flag1Position == Vector3.zero || flag2Position == Vector3.zero)
            return;

        Vector3 center = (flag1Position + flag2Position) / 2;
        float radius = Vector3.Distance(flag1Position, flag2Position) / 2;

        float circumference = 2 * Mathf.PI * radius;
        float flagSpacing = Vector3.Distance(flag1Position, flag2Position) / 5;
        int flagCount = Mathf.FloorToInt(circumference / flagSpacing);
        float angleStep = 360f / flagCount;

        GameObject blueFlagPrefab = Resources.Load<GameObject>(PrefabsPath + BlueFlagPrefabName);
        if (blueFlagPrefab == null)
        {
            Debug.LogError("No se encontró el prefab de la bandera azul en los recursos.");
            return;
        }

        for (int i = 0; i < flagCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 position = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            position.y = 3;

            bool isOverlapping = false;
            foreach (GameObject flag in instantiatedFlags)
            {
                if (Vector3.Distance(flag.transform.position, position) < flagSpacing * 0.8f)
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                GameObject newFlag = Instantiate(blueFlagPrefab, position, Quaternion.identity);
                instantiatedFlags.Add(newFlag);

                // Find the Prefab from flags list that is at the same position.
                Prefab flagData = flags.Find(f => Mathf.Approximately(f.positionX, position.x) && Mathf.Approximately(f.positionZ, position.z));
                if (flagData != null)
                {
                    instantiatedFlagsMap[newFlag] = flagData;
                }
            }
        }
    }

    // Instancia las 2 primeras banderas de LoadFlagsFromApi e invoca a SpawnFlagsOnCircle que genera la circunferencia con esas 2 e instancia las demás
    protected override void SpawnNextFlag()
    {
        for (int i = 0; i < 2; i++)
        {
            Prefab nextFlag = flags[i];
            string prefabPath = PrefabsPath + BlueFlagPrefabName;
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);

            if (prefabObject != null)
            {
                Vector3 position = new Vector3(nextFlag.positionX, 3, nextFlag.positionZ);
                GameObject instantiatedFlag = Instantiate(prefabObject, position, Quaternion.identity);

                instantiatedFlags.Add(instantiatedFlag);
                instantiatedFlagsMap[instantiatedFlag] = nextFlag;
                flagsDB.Add(nextFlag);

                if (i == 0) flag1Position = position;
                else flag2Position = position;

                Debug.Log($"Instanciado {nextFlag.modelName} en posición {position}.");
            }
            else
            {
                Debug.LogError($"No se encontró el prefab {nextFlag.modelName} en {PrefabsPath}.");
            }
        }
        SpawnFlagsOnCircle();

        // Mueve al jugador al centro del circulo
        Vector3 center = (flag1Position + flag2Position) / 2;
        center.y = player.position.y;
        player.position = center;
        Debug.Log("Jugador movido al centro del círculo.");

        startTime = Time.time;
    }

}