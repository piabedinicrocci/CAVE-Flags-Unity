using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ExperimentLoader : FlagLoaderBase
{
    private TextMeshProUGUI miTexto;
    private int currentFlagIndex = 0;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;
    private float flagInstantiatedTime = 0f;
    private int currentIdAprendizaje = 0;


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

    void Update()
    {
        if (!isPlayerOnBase && currentFlag != null)
        {
            Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);
            Vector3 flagPosition2D = new Vector3(currentFlag.transform.position.x, 0, currentFlag.transform.position.z);

            if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
            {
                float timeTaken = Time.time - flagInstantiatedTime;
                Debug.Log($"TIEMPO QUE TARDÓ EN ENCONTRAR LA BANDERA: {timeTaken} segundos.");

                StartCoroutine(UpdateFlagTimeLearning(timeTaken, currentIdAprendizaje));

                currentFlag.SetActive(false);
                currentFlag = null;

                SpawnNextFlag();
            }
        }
    }

    IEnumerator UpdateFlagTimeLearning(float timeTaken, int flagId)
    {
        TimeData data = new TimeData();
        data.timeTaken = timeTaken;
        string jsonData = JsonUtility.ToJson(data);
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

    IEnumerator MostrarEsperarYOcultarTexto()
    {
        miTexto.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        miTexto.gameObject.SetActive(false);

        // REDIRIJO A PROXIMA ESCENA (RANDOM) después de ocultar el texto
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    protected override void SpawnNextFlag()
    {
        if (currentFlagIndex >= flags.Count)
        {
            Debug.Log("Todas las banderas han sido instanciadas y encontradas.");
            StartCoroutine(MostrarEsperarYOcultarTexto());
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

            Debug.Log($"Instanciado {flagData.modelName} con idAprendizaje {flagData.id} en posición {flagPosition}.");

            currentFlagIndex++;
        }
        else
        {
            Debug.LogError($"No se encontró el prefab {flagData.modelName} en {PrefabsPath}.");
        }
    }
}