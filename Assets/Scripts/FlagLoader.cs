using System;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;

public class FlagLoader
{
    private const string Host = "mysql-vrmarket-pladema-ef62.d.aivencloud.com";
    private const int Port = 26116;
    private const string User = "avnadmin";
    private const string Password = "AVNS_BD6D-d03halBr0cMHzd";
    private const string Database = "cave";

    [System.Serializable]
    public class Prefab
    {
        public string modelName;
        public float positionX;
        public float positionZ;
        public int id;
    }

    private List<Prefab> flags = new List<Prefab>();

    public FlagLoader(long dni)
    {
        LoadFlagsFromDatabase(dni);
    }

    private void LoadFlagsFromDatabase(long dni)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"
            SELECT B.color, A.posicionX, A.posicionZ, A.id
            FROM APRENDIZAJE A
            JOIN BANDERA B ON A.id_bandera = B.id
            WHERE A.dni = @dni AND DATE(A.fecha) = CURDATE()";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dni", dni);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string color = reader.GetString("color");
                            float posX = reader.GetFloat("posicionX");
                            float posZ = reader.GetFloat("posicionZ");
                            int id = reader.GetInt32("id");

                            string prefabName = MapColorToPrefab(color);
                            if (prefabName != null)
                            {
                                flags.Add(new Prefab { modelName = prefabName, positionX = posX, positionZ = posZ, id = id });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al conectar a la base de datos: " + ex.Message);
        }
    }

    private string MapColorToPrefab(string color)
    {
        switch (color.ToLower())
        {
            case "azul": return "FlagBlue";
            case "rojo": return "FlagRed";
            case "verde": return "FlagGreen";
            default:
                Debug.LogWarning("Color de bandera desconocido: " + color);
                return null;
        }
    }

    public List<Prefab> GetFlags()
    {
        for (int i = 0; i < flags.Count; i++)
        {
            Debug.Log("IDDDDDDDDD: " + flags[i].id);
        }
        return flags;
    }


    // -------------------------------------------------------------------------------
    // --------------------------------- APRENDIZAJE ---------------------------------
    // -------------------------------------------------------------------------------
    public void UpdateFlagTimeInDatabaseLearning(float timeTaken, int currentIdAprendizaje, long dni)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"UPDATE APRENDIZAJE SET tiempo_encontrada_aprendizaje = @timeTaken WHERE dni = @dni AND id = @currentIdAprendizaje";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dni", dni);
                    cmd.Parameters.AddWithValue("@timeTaken", timeTaken);
                    cmd.Parameters.AddWithValue("@currentIdAprendizaje", currentIdAprendizaje);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                        Debug.Log($"Base de datos actualizada: tiempo_encontrada = {timeTaken} segundos para la bandera {currentIdAprendizaje} y DNI {dni}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al conectar a la base de datos: " + ex.Message);
        }
    }

    // -------------------------------------------------------------------------------
    // ----------------------------- EVALUACION - RANDOM -----------------------------
    // -------------------------------------------------------------------------------
    public void UpdateFlagTimeInDatabaseRandom(float timeTaken, int currentIdAprendizaje, long dni)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"UPDATE APRENDIZAJE SET tiempo_encontrada_random = @timeTaken WHERE dni = @dni AND id = @currentIdAprendizaje";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dni", dni);
                    cmd.Parameters.AddWithValue("@timeTaken", timeTaken);
                    cmd.Parameters.AddWithValue("@currentIdAprendizaje", currentIdAprendizaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al actualizar la base de datos: " + ex.Message);
        }
    }

    public void UpdateFlagRandomInDatabase(int idAprendizaje)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"UPDATE APRENDIZAJE SET f_random = TRUE WHERE id = @idAprendizaje";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idAprendizaje", idAprendizaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al actualizar f_random en la base de datos: " + ex.Message);
        }
    }

    // -------------------------------------------------------------------------------
    // ----------------------------- EVALUACION - ENTRE ------------------------------
    // -------------------------------------------------------------------------------
    public void UpdateFlagTimeInDatabaseBetween(float timeTaken, int currentIdAprendizaje, long dni)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"UPDATE APRENDIZAJE SET tiempo_plantada_entre = @timeTaken WHERE dni = @dni AND id = @currentIdAprendizaje";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@dni", dni);
                    cmd.Parameters.AddWithValue("@timeTaken", timeTaken);
                    cmd.Parameters.AddWithValue("@currentIdAprendizaje", currentIdAprendizaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al actualizar la base de datos: " + ex.Message);
        }
    }

    public void UpdateFlagBetweenInDatabase(int idAprendizaje)
    {
        string connString = $"Server={Host};Port={Port};Database={Database};Uid={User};Pwd={Password};";
        string query = @"UPDATE APRENDIZAJE SET f_entre = TRUE WHERE id = @idAprendizaje";

        try
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@idAprendizaje", idAprendizaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al actualizar f_random en la base de datos: " + ex.Message);
        }
    }


    // -------------------------------------------------------------------------------
    // ------------------------- EVALUACION - CIRCUNFERENCIA -------------------------
    // -------------------------------------------------------------------------------





}
