using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentLoader : MonoBehaviour
{
    private const string PrefabsPath = "Prefabs/";

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678; // Asegúrate de actualizar este valor con el DNI correcto

    private int currentFlagIndex = 0;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;
    private Vector3 currentFlagPosition;
    private bool playerFoundFlag = false;
    private float flagInstantiatedTime = 0f;
    private bool flagFoundTimeCalculated = false;
    private int currentIdAprendizaje = 0;

    private FlagLoader flagLoader;
    private List<FlagLoader.Prefab> flags;

    void Start()
    {
        flagLoader = new FlagLoader(dni);
        flags = flagLoader.GetFlags();

        if (flags.Count > 0)
        {
            SpawnNextFlag();
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

            if (playerFoundFlag && currentFlagIndex < flags.Count)
            {
                currentFlagIndex++;
                SpawnNextFlag();
                playerFoundFlag = false;

                if (currentFlagIndex == flags.Count)
                {
                    Debug.Log("¡Felicidades! Has encontrado todas las banderas.");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject == basePlane.gameObject)
        {
            isPlayerOnBase = false;
            Debug.Log("Jugador salió del plano.");

            Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);
            Vector3 planePosition2D = new Vector3(basePlane.position.x, 0, basePlane.position.z);

            if (currentFlag != null && Vector3.Distance(playerPosition2D, planePosition2D) > 1f)
            {
                currentFlag.SetActive(false);
                Debug.Log("Bandera oculta.");
            }

            Vector3 flagPosition2D = new Vector3(currentFlagPosition.x, 0, currentFlagPosition.z);
            if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f)
            {
                Debug.Log("Jugador llegó a la posición de la bandera.");
                playerFoundFlag = true;
            }
        }
    }

    void Update()
    {
        if (!isPlayerOnBase && currentFlag != null)
        {
            Vector3 playerPosition2D = new Vector3(player.position.x, 0, player.position.z);
            Vector3 flagPosition2D = new Vector3(currentFlagPosition.x, 0, currentFlagPosition.z);

            if (Vector3.Distance(playerPosition2D, flagPosition2D) < 1f && !flagFoundTimeCalculated)
            {
                float timeTaken = Time.time - flagInstantiatedTime;
                Debug.Log($"TIEMPO QUE TARDÓ EN ENCONTRAR LA BANDERA: {timeTaken} segundos.");
                playerFoundFlag = true;
                flagFoundTimeCalculated = true;
                flagLoader.UpdateFlagTimeInDatabaseLearning(timeTaken, currentIdAprendizaje, dni);
            }
        }
    }

    void SpawnNextFlag()
    {
        if (currentFlagIndex < flags.Count)
        {
            FlagLoader.Prefab nextFlag = flags[currentFlagIndex];
            string prefabPath = PrefabsPath + nextFlag.modelName;
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);

            if (prefabObject != null)
            {
                currentFlagPosition = new Vector3(nextFlag.positionX, 3, nextFlag.positionZ);
                currentFlag = Instantiate(prefabObject, currentFlagPosition, Quaternion.identity);
                flagInstantiatedTime = Time.time;
                currentIdAprendizaje = nextFlag.id;
                Debug.Log($"Instanciado {nextFlag.modelName} con idAprendizaje {nextFlag.id} en posición {currentFlagPosition}.");
                flagFoundTimeCalculated = false;
            }
            else
            {
                Debug.LogError($"No se encontró el prefab {nextFlag.modelName} en {PrefabsPath}.");
            }
        }
    }
}
