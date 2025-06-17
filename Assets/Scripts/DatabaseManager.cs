using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using System.Collections.Generic;

public class LessonUnlockData
{
    public int LessonID;
    public int IsUnlocked;
}

public class CategoryUnlockData
{
    public int CategoryID;
    public int IsUnlocked;
}
public class Badge
{
    public int BadgeID;
    public string Name;
    public string Description;
    public bool IsUnlocked;
    public bool IsClaimed;

    public bool IsDone => IsUnlocked && IsClaimed;
}


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
                    Is_Claimed INTEGER DEFAULT 0,
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

    public List<CategoryUnlockData> GetCategoryUnlockData(int userId)
    {
        List<CategoryUnlockData> result = new List<CategoryUnlockData>();

        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Category_ID, Is_Unlocked FROM User_Category_Unlocks WHERE User_ID = @userId";
                SqliteParameter param = new SqliteParameter("@userId", userId);
                cmd.Parameters.Add(param);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CategoryUnlockData()
                        {
                            CategoryID = reader.GetInt32(0),
                            IsUnlocked = reader.GetInt32(1)
                        });
                    }
                }
            }
        }

        return result;
    }

    public void UnlockCategoryForUser(int userId, int categoryId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "UPDATE User_Category_Unlocks SET Is_Unlocked = 1 WHERE User_ID = @userId AND Category_ID = @categoryId";
                cmd.Parameters.Add(new SqliteParameter("@userId", userId));
                cmd.Parameters.Add(new SqliteParameter("@categoryId", categoryId));
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<LessonUnlockData> GetLessonUnlockData(int userId)
    {
        List<LessonUnlockData> unlockData = new List<LessonUnlockData>();

        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Lesson_ID, Is_Unlocked FROM User_Lesson_Unlocks WHERE User_ID = @userId";
                SqliteParameter param = new SqliteParameter("@userId", userId);
                cmd.Parameters.Add(param);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        unlockData.Add(new LessonUnlockData
                        {
                            LessonID = reader.GetInt32(0),
                            IsUnlocked = reader.GetInt32(1)
                        });
                    }
                }
            }
        }

        return unlockData;
    }

    public void UnlockLessonForUser(int userId, int lessonId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "UPDATE User_Lesson_Unlocks SET Is_Unlocked = 1 WHERE User_ID = @userId AND Lesson_ID = @lessonId";
                cmd.Parameters.Add(new SqliteParameter("@userId", userId));
                cmd.Parameters.Add(new SqliteParameter("@lessonId", lessonId));
                cmd.ExecuteNonQuery();
            }
        }
    }

    public List<Badge> GetUserBadges(int userId)
    {
        List<Badge> badges = new List<Badge>();

        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT B.Badges_ID, B.Badges_Name, B.Badges_Description, UB.Is_Unlocked, UB.Is_Claimed
                FROM Badge_Table B
                JOIN User_Badges UB ON B.Badges_ID = UB.Badge_ID
                WHERE UB.User_ID = @userId;
            ";

                var param = cmd.CreateParameter();
                param.ParameterName = "@userId";
                param.Value = userId;
                cmd.Parameters.Add(param);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Badge badge = new Badge
                        {
                            BadgeID = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            IsUnlocked = reader.GetInt32(3) == 1,
                            IsClaimed = reader.GetInt32(4) == 1
                        };

                        badges.Add(badge);
                    }
                }
            }
        }

        Debug.Log("BADGES FETCHED FROM DB: " + badges.Count);
        return badges;
    }


    public void ClaimBadge(int userId, int badgeId, int goldReward)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbTransaction transaction = dbConn.BeginTransaction())
            {
                using (IDbCommand cmd = dbConn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"UPDATE User_Badges 
                                    SET Is_Claimed = 1 
                                    WHERE User_ID = @userId AND Badges_ID = @badgeId";

                    var param1 = cmd.CreateParameter();
                    param1.ParameterName = "@userId";
                    param1.Value = userId;
                    cmd.Parameters.Add(param1);

                    var param2 = cmd.CreateParameter();
                    param2.ParameterName = "@badgeId";
                    param2.Value = badgeId;
                    cmd.Parameters.Add(param2);

                    cmd.ExecuteNonQuery();
                }

                using (IDbCommand cmd = dbConn.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"UPDATE User 
                                    SET Coins = Coins + @gold 
                                    WHERE User_ID = @userId";

                    var param1 = cmd.CreateParameter();
                    param1.ParameterName = "@gold";
                    param1.Value = goldReward;
                    cmd.Parameters.Add(param1);

                    var param2 = cmd.CreateParameter();
                    param2.ParameterName = "@userId";
                    param2.Value = userId;
                    cmd.Parameters.Add(param2);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }

}
