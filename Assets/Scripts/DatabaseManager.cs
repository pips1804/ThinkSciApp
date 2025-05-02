using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    void Awake()
    {
        dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "UserDatabase.db");

        CreateDBIfNotExists();
    }

    void CreateDBIfNotExists()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT,
                    middle_name TEXT,
                    last_name TEXT,
                    coins INTEGER DEFAULT 200,
                    energy INTEGER DEFAULT 20,
                    max_energy INTEGER DEFAULT 20,
                    experience INTEGER DEFAULT 0,
                    level INTEGER DEFAULT 1,
                    pet_health INTEGER DEFAULT 100
                );";

                command.ExecuteNonQuery();
            }
        }
    }

    public void SaveUser(string firstName, string middleName, string lastName)
    {
        // Defensive validation
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(middleName) || string.IsNullOrWhiteSpace(lastName))
        {
            Debug.LogWarning("Attempted to save user with incomplete or invalid data. Operation cancelled.");
            return; // Stop here if any value is null or empty or whitespace
        }

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO users (first_name, middle_name, last_name) VALUES (@first, @middle, @last)";
                command.Parameters.AddWithValue("@first", firstName);
                command.Parameters.AddWithValue("@middle", middleName);
                command.Parameters.AddWithValue("@last", lastName);
                command.ExecuteNonQuery();
            }
        }

        Debug.Log("User data successfully saved.");
    }

    public bool HasUser()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM users";
                int count = System.Convert.ToInt32(command.ExecuteScalar());
                // Return true if user data exists, false otherwise
                return count > 0;
            }
        }
    }

    public (string, string, string) GetUser()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT first_name, middle_name, last_name FROM users LIMIT 1";

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string first = reader.GetString(0);
                        string middle = reader.GetString(1);
                        string last = reader.GetString(2);
                        return (first, middle, last);
                    }
                }
            }
        }

        return ("", "", "");
    }

    public void SavePlayerStats(int coins, int energy, int maxEnergy, int experience, int level, int petHealth)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE users SET 
                coins = @coins,
                energy = @energy,
                max_energy = @max_energy,
                experience = @experience,
                level = @level,
                pet_health = @pet_health
                WHERE id = 1;";
                command.Parameters.AddWithValue("@coins", coins);
                command.Parameters.AddWithValue("@energy", energy);
                command.Parameters.AddWithValue("@max_energy", maxEnergy);
                command.Parameters.AddWithValue("@experience", experience);
                command.Parameters.AddWithValue("@level", level);
                command.Parameters.AddWithValue("@pet_health", petHealth);
                command.ExecuteNonQuery();
            }
        }
    }




    public (int coins, int energy, int maxEnergy, int experience, int level, int petHealth) LoadPlayerStats()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT coins, energy, max_energy, experience, level, pet_health FROM users WHERE id = 1;";
                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetInt32(2),
                            reader.GetInt32(3),
                            reader.GetInt32(4),
                            reader.GetInt32(5)
                        );
                    }
                }
            }
        }

        return (200, 20, 20, 0, 1, 100); // Default values
    }




}




