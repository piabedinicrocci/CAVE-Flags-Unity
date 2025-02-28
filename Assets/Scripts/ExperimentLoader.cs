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
    private float flagInstantiatedTime = 0f;
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
                flagLoader.UpdateFlagTimeInDatabaseLearning(timeTaken, currentIdAprendizaje, dni);

                // Desactivar la bandera actual
                currentFlag.SetActive(false);
                currentFlag = null;

                // Instanciar la siguiente bandera
                SpawnNextFlag();
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

        FlagLoader.Prefab flagData = flags[currentFlagIndex];
        string prefabPath = PrefabsPath + flagData.modelName;
        GameObject prefabObject = Resources.Load<GameObject>(prefabPath);

        if (prefabObject != null)
        {
            Vector3 flagPosition = new Vector3(flagData.positionX, 3, flagData.positionZ);
            currentFlag = Instantiate(prefabObject, flagPosition, Quaternion.identity);

            currentIdAprendizaje = flagData.id;
            flagInstantiatedTime = Time.time;

            Debug.Log($"Instanciado {flagData.modelName} con idAprendizaje {flagData.id} en posición {flagPosition}.");

            currentFlagIndex++; // Pasar a la siguiente bandera en la lista
        }
        else
        {
            Debug.LogError($"No se encontró el prefab {flagData.modelName} en {PrefabsPath}.");
        }
    }
}
