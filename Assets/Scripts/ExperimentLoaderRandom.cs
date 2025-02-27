using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;

public class ExperimentLoaderRandom : MonoBehaviour
{
    private const string PrefabsPath = "Prefabs/";

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678;

    private int currentFlagIndex = 0;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;
    private Vector3 currentFlagPosition;
    private bool playerFoundFlag = false;
    private float flagInstantiatedTime = 0f;
    private bool flagFoundTimeCalculated = false;
    private int currentIdAprendizaje = 0;
    private int flagsFounded = 0;

    private FlagLoader flagLoader;
    private List<FlagLoader.Prefab> flags;

    private List<GameObject> instantiatedFlags = new List<GameObject>(); // Lista para almacenar las banderas instanciadas
    private List<FlagLoader.Prefab> flagsDB = new List<FlagLoader.Prefab>();

    private Dictionary<GameObject, FlagLoader.Prefab> instantiatedFlagsMap = new Dictionary<GameObject, FlagLoader.Prefab>();



    void Start()
    {
        flagLoader = new FlagLoader(dni);
        flags = flagLoader.GetFlags();

        if (flags.Count > 0)
        {
            SpawnNextFlag();
            //PrintFlagsDB();
        }
        else
        {
            Debug.LogWarning("No hay banderas para instanciar.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            isPlayerOnBase = true;
            Debug.Log("Jugador volvió a la base.");

            if (isPlayerOnBase && flagsFounded == flags.Count)
            {
                Debug.Log("¡Felicidades! Has encontrado todas las banderas.");
            }
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
    if (!isPlayerOnBase && instantiatedFlags.Count > 0)
    {
        Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);

        foreach (var kvp in instantiatedFlagsMap)
        {
            GameObject flag = kvp.Key;
            FlagLoader.Prefab flagData = kvp.Value;

            if (flag != null)
            {
                Vector3 flagPosition2D = new Vector3(flag.transform.position.x, 0, flag.transform.position.z);
                if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
                {
                    float timeTaken = Time.time - flagInstantiatedTime;
                    Debug.Log($"TIEMPO QUE TARDÓ EN ENCONTRAR LA BANDERA: {timeTaken} segundos.");
                    playerFoundFlag = true;
                    flagFoundTimeCalculated = true;
                    flagsFounded++;

                    flagLoader.UpdateFlagTimeInDatabaseRandom(timeTaken, flagData.id, dni);
                    flagLoader.UpdateFlagRandomInDatabase(flagData.id);

                    // Eliminar la bandera del diccionario
                    instantiatedFlagsMap.Remove(flag);
                    //flag.SetActive(false);
                    Destroy(flag);
                    break;
                }
            }
        }

        }
    }


    void SpawnNextFlag()
    {
        while (currentFlagIndex < flags.Count )
        {
            FlagLoader.Prefab nextFlag = flags[currentFlagIndex];
            string prefabPath = PrefabsPath + nextFlag.modelName;
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);
            currentFlagPosition = new Vector3(nextFlag.positionX, 3, nextFlag.positionZ);
            currentFlag = Instantiate(prefabObject, currentFlagPosition, Quaternion.identity);

            instantiatedFlagsMap[currentFlag] = nextFlag; // Asociar GameObject con su Prefab

            instantiatedFlags.Add(currentFlag); // Agregar la bandera a la lista
            flagsDB.Add(nextFlag);
            flagInstantiatedTime = Time.time;
            currentIdAprendizaje = nextFlag.id;
            flagFoundTimeCalculated = false;
            Debug.Log($"Instanciado {nextFlag.modelName} con idAprendizaje {nextFlag.id} en posición {currentFlagPosition}.");
            currentFlagIndex++;
        }

        for (int i = 0; i < 5; i++)
        {
            // Seleccionar aleatoriamente si será una bandera azul o roja
            string modelNameRandom = Random.value > 0.5f ? "flagBlue" : "flagRed";
            string prefabPathRandom = PrefabsPath + modelNameRandom;
            GameObject prefabObjectRandom = Resources.Load<GameObject>(prefabPathRandom);

            if (prefabObjectRandom != null)
            {
                // Generar una posición aleatoria en el rango especificado
                float randomX = Random.Range(-65f, 65f);
                float randomZ = Random.Range(-65f, 65f);
                Vector3 randomPosition = new Vector3(randomX, 3, randomZ);

                // Instanciar la bandera
                GameObject flag = Instantiate(prefabObjectRandom, randomPosition, Quaternion.identity);
                Debug.Log($"Instanciado bandera random {modelNameRandom} en posición {randomPosition}.");
            }
        }
    }

    //void PrintFlagsDB()
    //{
    //    Debug.Log("Lista de banderas instanciadas:");
    //    foreach (var flag in flagsDB)
    //    {
    //        Debug.Log("id "+flag.id);
    //    }
    //}


}