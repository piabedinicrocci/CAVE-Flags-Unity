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
    private bool playerNailFlag = false;
    private float flagInstantiatedTime = 0f;
    private bool flagNailTimeCalculated = false;
    private int currentIdAprendizaje = 0;

    private FlagLoader flagLoader;
    private List<FlagLoader.Prefab> flags;

    private List<GameObject> instantiatedFlags = new List<GameObject>(); // Lista para almacenar las banderas instanciadas
    private List<FlagLoader.Prefab> flagsDB = new List<FlagLoader.Prefab>();

    private Dictionary<GameObject, FlagLoader.Prefab> instantiatedFlagsMap = new Dictionary<GameObject, FlagLoader.Prefab>();

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
            Vector3 midpoint = (flag1Position + flag2Position) / 2;
            float radius = 1.5f;

            Debug.DrawLine(flag1Position, flag2Position, Color.green); // Línea entre banderas
            DrawCircle(midpoint, radius, Color.red); // Dibuja el círculo cada frame
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
        // Centro del círculo entre ambas banderas
        Vector3 midpoint = (flag1Position + flag2Position) / 2;
        float radius = 3f;
        // Distancia entre la bandera del usuario y el centro del círculo
        float distanceToCenter = Vector3.Distance(userFlagPosition, midpoint);
        return distanceToCenter <= radius;
    }

    void DrawCircle(Vector3 center, float radius, Color color)
    {
        int segments = 50; // Cuantos más segmentos, más suave será el círculo
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