using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;

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
                command.CommandText = @"
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    first_name TEXT,
                    middle_name TEXT,
                    last_name TEXT,
                    coins INTEGER DEFAULT 200
                );

                CREATE TABLE IF NOT EXISTS Category_Table (
                    Category_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Category_Name TEXT
                );

                CREATE TABLE IF NOT EXISTS User_Category_Unlocks (
                    User_ID INTEGER,
                    Category_ID INTEGER,
                    Is_Unlocked INTEGER DEFAULT 0,
                    PRIMARY KEY(User_ID, Category_ID),
                    FOREIGN KEY(User_ID) REFERENCES users(id),
                    FOREIGN KEY(Category_ID) REFERENCES Category_Table(Category_ID)
                );

                CREATE TABLE IF NOT EXISTS Lessons_Table (
                    Lesson_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Category_ID INTEGER,
                    Lesson_Name TEXT,
                    FOREIGN KEY(Category_ID) REFERENCES Category_Table(Category_ID)
                );

                CREATE TABLE IF NOT EXISTS User_Lesson_Unlocks (
                    User_ID INTEGER,
                    Lesson_ID INTEGER,
                    Is_Unlocked INTEGER DEFAULT 0,
                    PRIMARY KEY(User_ID, Lesson_ID),
                    FOREIGN KEY(User_ID) REFERENCES users(id),
                    FOREIGN KEY(Lesson_ID) REFERENCES Lessons_Table(Lesson_ID)
                );

                CREATE TABLE IF NOT EXISTS Quiz_Table (
                    Quiz_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Lesson_ID INTEGER,
                    Quiz_Name TEXT,
                    FOREIGN KEY(Lesson_ID) REFERENCES Lessons_Table(Lesson_ID)
                );

                CREATE TABLE IF NOT EXISTS User_Quiz_Scores (
                    User_ID INTEGER,
                    Quiz_ID INTEGER,
                    Score INTEGER,
                    Completed_At TEXT,
                    PRIMARY KEY(User_ID, Quiz_ID),
                    FOREIGN KEY(User_ID) REFERENCES users(id),
                    FOREIGN KEY(Quiz_ID) REFERENCES Quiz_Table(Quiz_ID)
                );

                CREATE TABLE IF NOT EXISTS Badge_Table (
                    Badges_ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    Badges_Name TEXT,
                    Badges_Description TEXT
                );

                CREATE TABLE IF NOT EXISTS User_Badges (
                    User_ID INTEGER,
                    Badge_ID INTEGER,
                    Is_Unlocked INTEGER DEFAULT 0,
                    PRIMARY KEY(User_ID, Badge_ID),
                    FOREIGN KEY(User_ID) REFERENCES users(id),
                    FOREIGN KEY(Badge_ID) REFERENCES Badge_Table(Badges_ID)
                );";

                command.ExecuteNonQuery();
            }
        }
    }

    public void SaveUser(string firstName, string middleName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(middleName) || string.IsNullOrWhiteSpace(lastName))
        {
            Debug.LogWarning("Attempted to save user with incomplete or invalid data. Operation cancelled.");
            return;
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
                        return (reader.GetString(0), reader.GetString(1), reader.GetString(2));
                    }
                }
            }
        }

        return ("", "", "");
    }

    public void SavePlayerStats(int coins)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE users SET coins = @coins WHERE id = 1;";
                command.Parameters.AddWithValue("@coins", coins);
                command.ExecuteNonQuery();
            }
        }
    }

    public int LoadPlayerStats()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT coins FROM users WHERE id = 1;";
                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                }
            }
        }

        return 200;
    }

    public void SaveQuizAndScore(int userId, int quizId, int score)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                INSERT OR REPLACE INTO User_Quiz_Scores (User_ID, Quiz_ID, Score, Completed_At)
                VALUES (@userId, @quizId, @score, datetime('now'))";
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@quizId", quizId);
                cmd.Parameters.AddWithValue("@score", score);
                cmd.ExecuteNonQuery();
            }

            Debug.Log($"Saved quiz (ID: {quizId}) for user {userId} with score {score}");
        }
    }
}
