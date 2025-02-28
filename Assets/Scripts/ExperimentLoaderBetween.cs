using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;

public class ExperimentLoaderBetween : MonoBehaviour
{
    private const string PrefabsPath = "Prefabs/";

    public Transform player;
    public Transform basePlane;
    public long dni = 12345678;

    private int currentFlagIndex = 0;
    private GameObject currentFlag;
    private bool isPlayerOnBase = true;

    private Vector3 currentFlagPosition;
    private bool playerNailFlag = false; //playerFoundFlag
    private float flagInstantiatedTime = 0f;
    private bool flagNailTimeCalculated = false; //flagFoundTimeCalculated
    private int currentIdAprendizaje = 0;

    private FlagLoader flagLoader;
    private List<FlagLoader.Prefab> flags;

    private List<GameObject> instantiatedFlags = new List<GameObject>(); // Lista para almacenar las banderas instanciadas
    private List<FlagLoader.Prefab> flagsDB = new List<FlagLoader.Prefab>();

    private Dictionary<GameObject, FlagLoader.Prefab> instantiatedFlagsMap = new Dictionary<GameObject, FlagLoader.Prefab>();

    public float tolerance = 1.5f; // Factor de tolerancia ajustable

    private Vector3 flag1Position;
    private Vector3 flag2Position;


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
        if (flag1Position != Vector3.zero && flag2Position != Vector3.zero)
        {
            Vector3 direction = (flag2Position - flag1Position).normalized;
            Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x) * tolerance; // Vector perpendicular

            Debug.DrawLine(flag1Position, flag2Position, Color.green); // Línea principal
            Debug.DrawLine(flag1Position + perpendicular, flag2Position + perpendicular, Color.blue); // Margen superior
            Debug.DrawLine(flag1Position - perpendicular, flag2Position - perpendicular, Color.blue); // Margen inferior
        }


        if (Input.GetMouseButtonDown(0))
        {
            string prefabPath = PrefabsPath + "FlagRed";
            GameObject prefabObject = Resources.Load<GameObject>(prefabPath);
            Vector3 playerPosition = new Vector3(player.position.x, 3, player.position.z);
            GameObject userFlag = Instantiate(prefabObject, playerPosition, Quaternion.identity); // o transform.rotation
            playerNailFlag = true;

            foreach (var kvp in instantiatedFlagsMap)
            {
                GameObject flag = kvp.Key;
                FlagLoader.Prefab flagData = kvp.Value;

                if (flag != null)
                {
                    if (IsFlagBetween(playerPosition) && playerNailFlag) //&& !isPlayerOnBase?
                    {
                        float timeTaken = Time.time - flagInstantiatedTime;
                        Debug.Log($"TIEMPO QUE TARDÓ EN CLAVAR LA BANDERA: {timeTaken} segundos.");
                        Debug.Log("¡Felicidades! Clavaste la bandera correctamente!.");
                        flagNailTimeCalculated = true;

                        flagLoader.UpdateFlagTimeInDatabaseBetween(timeTaken, flagData.id, dni);
                        flagLoader.UpdateFlagBetweenInDatabase(flagData.id);
                    }
                }
            }
        }
    }

    bool IsFlagBetween(Vector3 userFlagPosition)
    {
        // Centro de la línea entre ambas banderas instanciadas
        Vector3 midpoint = (flag1Position + flag2Position) / 2;

        // Vector dirección entre las banderas
        Vector3 direction = (flag2Position - flag1Position).normalized;

        // Proyectar la bandera del usuario sobre la línea entre banderas
        Vector3 projected = flag1Position + Vector3.Project(userFlagPosition - flag1Position, direction);

        // Distancia del usuario a la línea entre banderas
        float distanceToLine = Vector3.Distance(userFlagPosition, projected);

        // Verifica si la bandera del usuario está dentro del área de tolerancia
        bool withinMargin = distanceToLine <= tolerance;

        // Verifica si la bandera del usuario está entre las dos banderas en la dirección de la línea
        float dotProduct1 = Vector3.Dot(userFlagPosition - flag1Position, direction);
        float dotProduct2 = Vector3.Dot(userFlagPosition - flag2Position, direction);

        bool isBetween = dotProduct1 > 0 && dotProduct2 < 0;

        return isBetween && withinMargin;
    }

    void SpawnNextFlag()
    {
        if (flags.Count < 2)
        {
            Debug.LogWarning("No hay suficientes banderas para instanciar.");
            return;
        }

        // Instanciar las primeras dos banderas de la lista
        for (int i = 0; i < 2; i++)
        {
            FlagLoader.Prefab nextFlag = flags[i];
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