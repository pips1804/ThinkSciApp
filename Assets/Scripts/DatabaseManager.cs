using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

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

public class ItemData
{
    public int ItemId;
    public string Name;
    public string Type;
    public int Price;
    public string SpritePath;
    public string Description;
    public int Quantity;  // ✅ add this
    public int EnergyValue;
}

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    public delegate void UserDataChangedHandler();
    public static event UserDataChangedHandler OnUserDataChanged;
    void Awake()
    {
        dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "UserDatabase.db");
        Debug.Log("Awake: Initial DB path set to " + dbPath);
        CreateDBIfNotExists();
    }

    void CreateDBIfNotExists()
    {
        string fileName = "UserDatabase.db";
        string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
        string targetPath = Path.Combine(Application.persistentDataPath, fileName);

        Debug.Log("StreamingAssets DB path: " + sourcePath);
        Debug.Log("PersistentDataPath DB path: " + targetPath);

        if (!File.Exists(targetPath))
        {
            Debug.Log("Database not found in persistentDataPath, copying...");

#if UNITY_ANDROID && !UNITY_EDITOR
        // On Android, StreamingAssets is inside a jar (apk), use UnityWebRequest
        StartCoroutine(CopyDatabaseAndroid(sourcePath, targetPath));
#else
            // On PC/iOS, File.Copy works
            File.Copy(sourcePath, targetPath);
            Debug.Log("Database copy complete!");
#endif
        }

        // Set connection string
        dbPath = "URI=file:" + targetPath;
        Debug.Log("Final database connection string: " + dbPath);
    }

    private IEnumerator CopyDatabaseAndroid(string sourcePath, string targetPath)
    {
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(sourcePath);
        yield return www.SendWebRequest();

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load DB from StreamingAssets: " + www.error);
        }
        else
        {
            File.WriteAllBytes(targetPath, www.downloadHandler.data);
            Debug.Log("Database copy complete (Android)!");
        }

        dbPath = "URI=file:" + targetPath;
    }

    public void UpdateUser(string firstName, string middleName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(middleName) || string.IsNullOrWhiteSpace(lastName))
        {
            Debug.LogWarning("Attempted to update user with incomplete or invalid data. Operation cancelled.");
            return;
        }

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                UPDATE users
                SET first_name = @first,
                    middle_name = @middle,
                    last_name = @last
                WHERE id = 1";  // Hardcoded ID

                command.Parameters.AddWithValue("@first", firstName);
                command.Parameters.AddWithValue("@middle", middleName);
                command.Parameters.AddWithValue("@last", lastName);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                    Debug.Log("User data successfully updated.");
                else
                    Debug.LogWarning("No user was updated. User ID 1 may not exist.");
            }
        }
    }

    public bool IsDefaultUser()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                SELECT first_name, middle_name, last_name
                FROM users
                WHERE id = 1
                LIMIT 1";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string firstName = reader.GetString(0);
                        string middleName = reader.GetString(1);
                        string lastName = reader.GetString(2);

                        // Adjust to match your default inserted values
                        return firstName == "Juan" && middleName == "Dela" && lastName == "Cruz";
                    }
                }
            }
        }

        return true; // Assume default if user not found
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

    public (string, string, string, int, int) GetUser()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT first_name, middle_name, last_name, coins, energy FROM users LIMIT 1";

                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4));
                    }
                }
            }
        }

        return ("", "", "", 0, 0);
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

    public int LoadPlayerEnergy()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT energy FROM users WHERE id = 1;";
                using (IDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetInt32(0); // return energy value
                    }
                }
            }
        }

        return 0; // default if not found
    }

    public void SaveQuizAndScore(int userId, int quizId, int score)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                INSERT INTO User_Quiz_Scores (User_ID, Quiz_ID, Score, Completed_At)
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

        Debug.Log("Lesson unlocked!");
    }

    public int? GetRequiredCollectibleForLesson(int lessonId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Item_ID FROM Items WHERE Lesson_ID = @lessonId LIMIT 1";
                cmd.Parameters.Add(new SqliteParameter("@lessonId", lessonId));

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);

                return null; // No collectible required
            }
        }
    }

    public string GetItemName(int itemId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Item_Name FROM Items WHERE Item_ID = @itemId";
                cmd.Parameters.Add(new SqliteParameter("@itemId", itemId));

                object result = cmd.ExecuteScalar();
                return result?.ToString() ?? "Unknown Item";
            }
        }
    }

    public bool HasCollectible(int userId, int itemId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM User_Items WHERE User_ID = @userId AND Item_ID = @itemId";
                cmd.Parameters.Add(new SqliteParameter("@userId", userId));
                cmd.Parameters.Add(new SqliteParameter("@itemId", itemId));

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
    }

    public List<Badge> GetUserBadges(int userId)
    {
        CheckAndUnlockBadges(userId);
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
                                    WHERE User_ID = @userId AND Badge_ID = @badgeId";

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
                    cmd.CommandText = @"UPDATE users
                                    SET coins = coins + @gold
                                    WHERE id = @userId";

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
    public (string name, int baseHealth, int baseDamage) GetPetStats(int userId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            string query = @"
            SELECT p.Pet_Name, p.Base_Health, p.Base_Damage
            FROM users u
            JOIN Pet_Table p ON u.Pet_ID = p.Pet_ID
            WHERE u.id = @userId";

            using (var cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string name = reader.GetString(0);
                        int baseHealth = reader.GetInt32(1);
                        int baseDamage = reader.GetInt32(2);
                        return (name, baseHealth, baseDamage);
                    }
                }
            }
        }

        Debug.LogWarning("Pet not found for user: " + userId);
        return ("None", 0, 0);
    }

    public void AddToPetStats(int userId, int healthToAdd, int damageToAdd)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            // First get Pet_ID from user
            string getPetIdQuery = "SELECT Pet_ID FROM users WHERE id = @userId";
            int petId = -1;

            using (var cmd = new SqliteCommand(getPetIdQuery, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    petId = int.Parse(result.ToString());
            }

            if (petId == -1)
            {
                Debug.LogWarning("User has no pet assigned.");
                return;
            }

            // Now update pet stats
            string updateQuery = @"
                UPDATE Pet_Table
                SET Base_Health = Base_Health + @health,
                    Base_Damage = Base_Damage + @damage
                WHERE Pet_ID = @petId";

            using (var cmd = new SqliteCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@health", healthToAdd);
                cmd.Parameters.AddWithValue("@damage", damageToAdd);
                cmd.Parameters.AddWithValue("@petId", petId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void AddCoin(int userId, int goldToAdd)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE users
                                SET coins = coins + @goldToAdd
                                WHERE id = @userId";

                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@goldToAdd";
                param1.Value = goldToAdd;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@userId";
                param2.Value = userId;
                cmd.Parameters.Add(param2);

                cmd.ExecuteNonQuery();
            }
        }
        OnUserDataChanged?.Invoke();
    }

    public void AddEnergy(int userId, int energyToAdd)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE users
                                SET energy = energy + @energyToAdd
                                WHERE id = @userId";

                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@energyToAdd";
                param1.Value = energyToAdd;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@userId";
                param2.Value = userId;
                cmd.Parameters.Add(param2);

                cmd.ExecuteNonQuery();
            }
        }
        OnUserDataChanged?.Invoke();
    }

    public void SpendEnergy(int userId, int energyToSpend)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"UPDATE users
                    SET energy = MAX(energy - @energyToSpend, 0)
                    WHERE id = @userId";

                var param1 = cmd.CreateParameter();
                param1.ParameterName = "@energyToSpend";
                param1.Value = energyToSpend;
                cmd.Parameters.Add(param1);

                var param2 = cmd.CreateParameter();
                param2.ParameterName = "@userId";
                param2.Value = userId;
                cmd.Parameters.Add(param2);

                cmd.ExecuteNonQuery();
            }
        }
        OnUserDataChanged?.Invoke();
    }

    public bool HasReceivedStatBonus(int userId, int quizId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Stats_Given FROM User_Quiz_Scores WHERE User_ID = @uid AND Quiz_ID = @qid";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@qid", quizId);
                var result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) == 1;
            }
        }
    }

    public void MarkStatBonusAsGiven(int userId, int quizId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE User_Quiz_Scores SET Stats_Given = 1 WHERE User_ID = @uid AND Quiz_ID = @qid";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@qid", quizId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public bool IsPetNameDefault()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Pet_Name FROM Pet_Table WHERE Pet_ID = 1";
                var result = command.ExecuteScalar()?.ToString();
                return result == "Iglot";
            }
        }
    }

    public void SavePetName(string newName)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE Pet_Table SET Pet_Name = @name WHERE Pet_ID = 1";
                command.Parameters.AddWithValue("@name", newName);
                command.ExecuteNonQuery();
            }
        }
    }

    public List<Question> LoadRandomQuestions(int quizId, string questionType, int count)
    {
        List<Question> allQuestions = new List<Question>();

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT q.Question_ID, q.Question_Text, qq.Correct_Answer_Index, qq.Quiz_Question_ID
                    FROM Questions q
                    JOIN Quiz_Questions qq ON q.Question_ID = qq.Question_ID
                    WHERE q.Quiz_ID = @quizId AND q.Question_Type = @type";
                cmd.Parameters.AddWithValue("@quizId", quizId);
                cmd.Parameters.AddWithValue("@type", questionType);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int questionId = reader.GetInt32(0);
                        string text = reader.GetString(1);
                        int correctIndex = reader.GetInt32(2);
                        int quizQuestionId = reader.GetInt32(3);

                        string[] answers = LoadAnswers(connection, quizQuestionId);

                        allQuestions.Add(new Question
                        {
                            id = questionId,
                            questionText = text,
                            correctAnswerIndex = correctIndex,
                            choices = answers
                        });
                    }
                }
            }
        }

        Shuffle(allQuestions);
        return allQuestions.Count > count ? allQuestions.GetRange(0, count) : allQuestions;
    }

    private string[] LoadAnswers(SqliteConnection connection, int quizQuestionId)
    {
        List<string> answers = new List<string>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Answer_Text FROM Quiz_Answers WHERE Quiz_Question_ID = @qqid ORDER BY Answer_Index ASC";
            cmd.Parameters.AddWithValue("@qqid", quizQuestionId);

            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    answers.Add(reader.GetString(0));
                }
            }
        }
        return answers.ToArray();
    }

    private void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    public List<JumbledQuestion1> Get10JumbledQuestions(int quizId)
    {
        List<JumbledQuestion1> questions = new List<JumbledQuestion1>();

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            while (questions.Count < 10)
            {
                List<JumbledQuestion1> batch = new List<JumbledQuestion1>();

                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.CommandText = @"
                    SELECT q.Question_ID, q.Question_Text, a.Correct_Answer, a.Explanation
                    FROM Questions q
                    JOIN Jumbled_Answers a ON q.Question_ID = a.Question_ID
                    WHERE q.Quiz_ID = @quizId
                      AND q.Question_Type = 'Jumbled Letters'
                      AND q.Is_Used = 0
                    ORDER BY RANDOM()
                    LIMIT @needed;";

                    selectCommand.Parameters.AddWithValue("@quizId", quizId);
                    selectCommand.Parameters.AddWithValue("@needed", 10 - questions.Count);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            JumbledQuestion1 q = new JumbledQuestion1
                            {
                                QuestionID = reader.GetInt32(0),
                                QuestionText = reader.GetString(1),
                                CorrectAnswer = reader.GetString(2),
                                Explanation = reader.GetString(3)
                            };
                            batch.Add(q);
                        }
                    }
                }

                if (batch.Count == 0)
                {
                    // No unused left → reset and try again
                    using (var resetCommand = connection.CreateCommand())
                    {
                        resetCommand.CommandText = @"
                        UPDATE Questions
                        SET Is_Used = 0
                        WHERE Quiz_ID = @quizId
                          AND Question_Type = 'Jumbled Letters';";
                        resetCommand.Parameters.AddWithValue("@quizId", quizId);
                        resetCommand.ExecuteNonQuery();
                    }
                    continue; // loop back and try again
                }

                // Mark as used
                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.CommandText = @"
                    UPDATE Questions
                    SET Is_Used = 1
                    WHERE Question_ID IN (" + string.Join(",", batch.Select((q, i) => "@id" + i)) + ");";

                    for (int i = 0; i < batch.Count; i++)
                    {
                        updateCommand.Parameters.AddWithValue("@id" + i, batch[i].QuestionID);
                    }

                    updateCommand.ExecuteNonQuery();
                }

                questions.AddRange(batch);
            }
        }

        return questions;
    }

    public List<SwipeManager.SwipeQuestion> GetRandomSwipeQuestions(int quizId, int questionCount)
    {
        List<SwipeManager.SwipeQuestion> swipeQuestions = new List<SwipeManager.SwipeQuestion>();

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            // Step 1: Count unused questions
            string checkQuery = @"
            SELECT COUNT(*)
            FROM Questions
            WHERE Quiz_ID = @quizId
              AND Question_Type = 'Swipe to Answer'
              AND Is_Used = 0;";

            int unusedCount = 0;
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = checkQuery;
                checkCmd.Parameters.AddWithValue("@quizId", quizId);
                unusedCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            }

            // Step 2: If not enough unused left, reset all
            if (unusedCount < questionCount)
            {
                string resetQuery = @"
                UPDATE Questions
                SET Is_Used = 0
                WHERE Quiz_ID = @quizId
                  AND Question_Type = 'Swipe to Answer';";

                using (var resetCmd = connection.CreateCommand())
                {
                    resetCmd.CommandText = resetQuery;
                    resetCmd.Parameters.AddWithValue("@quizId", quizId);
                    resetCmd.ExecuteNonQuery();
                }
            }

            // Step 3: Get random questions (guaranteed enough now)
            string query = @"
            SELECT q.Question_ID, q.Question_Text, s.Correct_Direction, s.Explanation
            FROM Questions q
            INNER JOIN Swipe_Answers s ON q.Question_ID = s.Question_ID
            WHERE q.Quiz_ID = @quizId
              AND q.Question_Type = 'Swipe to Answer'
              AND q.Is_Used = 0
            ORDER BY RANDOM()
            LIMIT @limit;";

            List<int> selectedIds = new List<int>();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@quizId", quizId);
                cmd.Parameters.AddWithValue("@limit", questionCount);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int questionId = reader.GetInt32(0);
                        selectedIds.Add(questionId);

                        swipeQuestions.Add(new SwipeManager.SwipeQuestion
                        {
                            questionId = questionId,
                            questionText = reader.GetString(1),
                            correctAnswer = reader.GetString(2),
                            explanationText = reader.GetString(3)
                        });
                    }
                }
            }

            // Step 4: Mark selected questions as used
            if (selectedIds.Count > 0)
            {
                string markQuery = $"UPDATE Questions SET Is_Used = 1 WHERE Question_ID IN ({string.Join(",", selectedIds)})";

                using (var markCmd = connection.CreateCommand())
                {
                    markCmd.CommandText = markQuery;
                    markCmd.ExecuteNonQuery();
                }
            }
        }

        return swipeQuestions;
    }

    public List<MultipleChoice.MultipleChoiceQuestions> GetRandomUnusedQuestions(int quizId, string questionType = null, int limit = 15)
    {
        List<MultipleChoice.MultipleChoiceQuestions> questionList = new List<MultipleChoice.MultipleChoiceQuestions>();

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            // Step 1: Count unused with optional type filter
            int unusedCount = 0;
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = @"SELECT COUNT(*)
                                     FROM Questions
                                     WHERE Quiz_ID = @quizId AND Is_Used = 0"
                                         + (questionType != null ? " AND Question_Type = @qType" : "");
                checkCmd.Parameters.AddWithValue("@quizId", quizId);
                if (questionType != null)
                    checkCmd.Parameters.AddWithValue("@qType", questionType);

                unusedCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            }

            // Step 2: Reset if not enough unused
            if (unusedCount < limit)
            {
                using (var resetCmd = connection.CreateCommand())
                {
                    resetCmd.CommandText = @"UPDATE Questions
                                         SET Is_Used = 0
                                         WHERE Quiz_ID = @quizId"
                                             + (questionType != null ? " AND Question_Type = @qType" : "");
                    resetCmd.Parameters.AddWithValue("@quizId", quizId);
                    if (questionType != null)
                        resetCmd.Parameters.AddWithValue("@qType", questionType);

                    resetCmd.ExecuteNonQuery();
                }
            }

            // Step 3: Select random unused with optional type filter
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT Question_ID, Question_Text
                                FROM Questions
                                WHERE Quiz_ID = @quizId AND Is_Used = 0"
                                    + (questionType != null ? " AND Question_Type = @qType" : "") +
                                    " ORDER BY RANDOM() LIMIT @limit";
                cmd.Parameters.AddWithValue("@quizId", quizId);
                cmd.Parameters.AddWithValue("@limit", limit);
                if (questionType != null)
                    cmd.Parameters.AddWithValue("@qType", questionType);

                List<int> selectedIds = new List<int>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int questionId = reader.GetInt32(0);
                        string questionText = reader.GetString(1);
                        selectedIds.Add(questionId);

                        var q = new MultipleChoice.MultipleChoiceQuestions();
                        q.question = questionText;

                        // Fetch options
                        using (var optCmd = connection.CreateCommand())
                        {
                            optCmd.CommandText = "SELECT Option_Text, Is_Correct, Explanation FROM MCQ_Options WHERE Question_ID = @qid";
                            optCmd.Parameters.AddWithValue("@qid", questionId);

                            using (var optReader = optCmd.ExecuteReader())
                            {
                                List<string> options = new List<string>();
                                while (optReader.Read())
                                {
                                    string optionText = optReader.GetString(0);
                                    int isCorrect = optReader.GetInt32(1);
                                    string explanation = optReader.GetString(2);

                                    options.Add(optionText);

                                    if (isCorrect == 1)
                                    {
                                        q.correctIndex = options.Count - 1;
                                        q.explanationText = explanation;
                                    }
                                }
                                q.options = options.ToArray();
                            }
                        }

                        questionList.Add(q);
                    }
                }

                // Step 4: Mark selected as used
                foreach (int qid in selectedIds)
                {
                    using (var updateCmd = connection.CreateCommand())
                    {
                        updateCmd.CommandText = "UPDATE Questions SET Is_Used = 1 WHERE Question_ID = @qid";
                        updateCmd.Parameters.AddWithValue("@qid", qid);
                        updateCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        return questionList;
    }

    public MultipleChoice.MultipleChoiceQuestions GetRandomUnusedQuestion(int quizId)
    {
        MultipleChoice.MultipleChoiceQuestions q = null;

        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            // Step 1: Check unused count
            int unusedCount = 0;
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM Questions WHERE Quiz_ID = @quizId AND Is_Used = 0";
                checkCmd.Parameters.AddWithValue("@quizId", quizId);
                unusedCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            }

            // Step 2: Reset all if none left
            if (unusedCount == 0)
            {
                using (var resetCmd = connection.CreateCommand())
                {
                    resetCmd.CommandText = "UPDATE Questions SET Is_Used = 0 WHERE Quiz_ID = @quizId";
                    resetCmd.Parameters.AddWithValue("@quizId", quizId);
                    resetCmd.ExecuteNonQuery();
                }
            }

            // Step 3: Select one unused question
            int questionId = -1;
            string questionText = "";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT Question_ID, Question_Text
                FROM Questions
                WHERE Quiz_ID = @quizId AND Is_Used = 0
                ORDER BY RANDOM()
                LIMIT 1";
                cmd.Parameters.AddWithValue("@quizId", quizId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        questionId = reader.GetInt32(0);
                        questionText = reader.GetString(1);
                    }
                }
            }

            // Step 4: Mark this question as used
            if (questionId != -1)
            {
                using (var updateCmd = connection.CreateCommand())
                {
                    updateCmd.CommandText = "UPDATE Questions SET Is_Used = 1 WHERE Question_ID = @qid";
                    updateCmd.Parameters.AddWithValue("@qid", questionId);
                    updateCmd.ExecuteNonQuery();
                }

                // Step 5: Load options
                q = new MultipleChoice.MultipleChoiceQuestions();
                q.question = questionText;

                using (var optCmd = connection.CreateCommand())
                {
                    optCmd.CommandText = @"
                    SELECT Option_Text, Is_Correct, Explanation
                    FROM MCQ_Options
                    WHERE Question_ID = @qid
                    ORDER BY MCQ_Option_ID ASC";
                    optCmd.Parameters.AddWithValue("@qid", questionId);

                    List<string> options = new List<string>();
                    using (var optReader = optCmd.ExecuteReader())
                    {
                        while (optReader.Read())
                        {
                            string optionText = optReader.GetString(0);
                            int isCorrect = optReader.GetInt32(1);
                            string explanation = optReader.GetString(2);

                            options.Add(optionText);

                            if (isCorrect == 1)
                            {
                                q.correctIndex = options.Count - 1;
                                q.explanationText = explanation;
                            }
                        }
                    }
                    q.options = options.ToArray();
                }
            }
        }

        return q;
    }
    public List<ItemData> GetAllItems()
    {
        List<ItemData> items = new List<ItemData>();

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Item_ID, Item_Name, Item_Type, Price, Sprite_Path, Description FROM Items";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemData
                        {
                            ItemId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Type = reader.GetString(2),
                            Price = reader.GetInt32(3),
                            SpritePath = reader.GetString(4),
                            Description = reader.GetString(5)
                        });
                    }
                }
            }
        }

        return items;
    }

    // Fetch user-owned items
    public List<ItemData> GetUserItems(int userId)
    {
        List<ItemData> items = new List<ItemData>();

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                SELECT i.Item_ID, i.Item_Name, i.Item_Type, i.Price, i.Sprite_Path, ui.Quantity, i.Description, i.EnergyValue
                FROM Items i
                JOIN User_Items ui ON i.Item_ID = ui.Item_ID
                WHERE ui.User_ID = @uid";
                cmd.Parameters.AddWithValue("@uid", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ItemData
                        {
                            ItemId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Type = reader.GetString(2),
                            Price = reader.GetInt32(3),
                            SpritePath = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Quantity = reader.GetInt32(5),
                            Description = reader.GetString(6),
                            EnergyValue = reader.IsDBNull(7) ? 0 : reader.GetInt32(7)
                        });
                    }
                }
            }
        }

        return items;
    }


    // Purchase item (deduct coins + insert into User_Items)
    public bool PurchaseItem(int userId, int itemId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                // 1. Get item type & price
                string itemType = "";
                int price = 0;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    cmd.CommandText = "SELECT Item_Type, Price FROM Items WHERE Item_ID = @iid";
                    cmd.Parameters.AddWithValue("@iid", itemId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            itemType = reader.GetString(0);
                            price = reader.GetInt32(1);
                        }
                        else
                        {
                            Debug.LogError("Item not found in database!");
                            return false;
                        }
                    }
                }

                // 2. Check user coins
                int coins = 0;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    cmd.CommandText = "SELECT coins FROM users WHERE id = @uid";
                    cmd.Parameters.AddWithValue("@uid", userId);
                    coins = Convert.ToInt32(cmd.ExecuteScalar());
                }

                if (coins < price)
                {
                    Debug.LogWarning("Not enough coins to purchase item.");
                    return false;
                }

                // 3. Deduct coins
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = trans;
                    cmd.CommandText = "UPDATE users SET coins = coins - @price WHERE id = @uid";
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }

                // 4. Insert or update User_Items
                if (itemType == "Collectible")
                {
                    // Check if already owned
                    using (var checkCmd = conn.CreateCommand())
                    {
                        checkCmd.Transaction = trans;
                        checkCmd.CommandText = "SELECT COUNT(*) FROM User_Items WHERE User_ID = @uid AND Item_ID = @iid";
                        checkCmd.Parameters.AddWithValue("@uid", userId);
                        checkCmd.Parameters.AddWithValue("@iid", itemId);

                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            Debug.LogWarning("User already owns this collectible.");
                            return false;
                        }
                    }

                    // Insert collectible
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = "INSERT INTO User_Items (User_ID, Item_ID, Quantity) VALUES (@uid, @iid, 1)";
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@iid", itemId);
                        cmd.ExecuteNonQuery();
                    }
                }
                else if (itemType == "Food")
                {
                    // Check if food already exists
                    long count = 0;
                    using (var checkCmd = conn.CreateCommand())
                    {
                        checkCmd.Transaction = trans;
                        checkCmd.CommandText = "SELECT COUNT(*) FROM User_Items WHERE User_ID = @uid AND Item_ID = @iid";
                        checkCmd.Parameters.AddWithValue("@uid", userId);
                        checkCmd.Parameters.AddWithValue("@iid", itemId);
                        count = (long)checkCmd.ExecuteScalar();
                    }

                    if (count > 0)
                    {
                        // ✅ Update quantity
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = "UPDATE User_Items SET Quantity = Quantity + 1 WHERE User_ID = @uid AND Item_ID = @iid";
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.Parameters.AddWithValue("@iid", itemId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // ✅ Insert new row with Quantity = 1
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = trans;
                            cmd.CommandText = "INSERT INTO User_Items (User_ID, Item_ID, Quantity) VALUES (@uid, @iid, 1)";
                            cmd.Parameters.AddWithValue("@uid", userId);
                            cmd.Parameters.AddWithValue("@iid", itemId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                trans.Commit();
            }
        }

        Debug.Log("Item purchased successfully!");
        OnUserDataChanged?.Invoke();
        return true;
    }

    public void ReduceItemQuantity(int userId, int itemId, int amount)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                UPDATE User_Items
                SET Quantity = Quantity - @amount
                WHERE User_ID = @uid AND Item_ID = @iid;

                DELETE FROM User_Items
                WHERE User_ID = @uid AND Item_ID = @iid AND Quantity <= 0;";
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@iid", itemId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public int CheckIfOwned(int userId, int itemId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();

            string query = "SELECT Quantity FROM User_Items WHERE User_ID = @userId AND Item_ID = @itemId";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@itemId", itemId);

                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
            }
        }

        return 0; // not owned
    }

    public void AddUserItem(int userId, int itemId, int amount = 1)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();

            // Check if user already has this item
            using (IDbCommand checkCmd = dbConn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT Quantity FROM User_Items WHERE User_ID = @userId AND Item_ID = @itemId";
                checkCmd.Parameters.Add(new SqliteParameter("@userId", userId));
                checkCmd.Parameters.Add(new SqliteParameter("@itemId", itemId));

                object result = checkCmd.ExecuteScalar();

                if (result != null)
                {
                    // Update existing
                    using (IDbCommand updateCmd = dbConn.CreateCommand())
                    {
                        updateCmd.CommandText = "UPDATE User_Items SET Quantity = Quantity + @amount WHERE User_ID = @userId AND Item_ID = @itemId";
                        updateCmd.Parameters.Add(new SqliteParameter("@amount", amount));
                        updateCmd.Parameters.Add(new SqliteParameter("@userId", userId));
                        updateCmd.Parameters.Add(new SqliteParameter("@itemId", itemId));
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // Insert new
                    using (IDbCommand insertCmd = dbConn.CreateCommand())
                    {
                        insertCmd.CommandText = "INSERT INTO User_Items (User_ID, Item_ID, Quantity) VALUES (@userId, @itemId, @amount)";
                        insertCmd.Parameters.Add(new SqliteParameter("@userId", userId));
                        insertCmd.Parameters.Add(new SqliteParameter("@itemId", itemId));
                        insertCmd.Parameters.Add(new SqliteParameter("@amount", amount));
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }

    public bool UserHasItem(int userId, int itemId)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM User_Items WHERE User_ID = @userId AND Item_ID = @itemId AND Quantity > 0";
                command.Parameters.Add(new SqliteParameter("@userId", userId));
                command.Parameters.Add(new SqliteParameter("@itemId", itemId));

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
        }
    }

    public bool CanUnlockLesson(int userId, int lessonId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                // Check if the user owns the required item for this lesson
                cmd.CommandText = @"
                SELECT COUNT(*)
                FROM Lesson_Requirements lr
                LEFT JOIN User_Items ui
                    ON lr.Required_Item_ID = ui.Item_ID AND ui.User_ID = @userId
                WHERE lr.Lesson_ID = @lessonId
                AND (ui.Quantity IS NULL OR ui.Quantity <= 0)";

                cmd.Parameters.Add(new SqliteParameter("@userId", userId));
                cmd.Parameters.Add(new SqliteParameter("@lessonId", lessonId));

                int missingCount = Convert.ToInt32(cmd.ExecuteScalar());

                // If missingCount == 0 → user has all required items
                return missingCount == 0;
            }
        }
    }

    public void CheckAndUnlockLesson(int userId, int lessonId)
    {
        if (CanUnlockLesson(userId, lessonId))
        {
            using (IDbConnection dbConn = new SqliteConnection(dbPath))
            {
                dbConn.Open();
                using (IDbCommand cmd = dbConn.CreateCommand())
                {
                    cmd.CommandText = @"
                    UPDATE User_Lesson_Unlocks
                    SET Is_Unlocked = 1
                    WHERE User_ID = @userId AND Lesson_ID = @lessonId";
                    cmd.Parameters.Add(new SqliteParameter("@userId", userId));
                    cmd.Parameters.Add(new SqliteParameter("@lessonId", lessonId));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public void CheckAndUnlockAllLessons(int userId)
    {
        using (IDbConnection dbConn = new SqliteConnection(dbPath))
        {
            dbConn.Open();

            // Get all lessons for this user
            using (IDbCommand cmd = dbConn.CreateCommand())
            {
                cmd.CommandText = "SELECT Lesson_ID FROM Lessons_Table";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int lessonId = reader.GetInt32(0);

                        // Check if user meets requirements
                        if (CanUnlockLesson(userId, lessonId))
                        {
                            using (IDbCommand updateCmd = dbConn.CreateCommand())
                            {
                                updateCmd.CommandText = @"
                                UPDATE User_Lesson_Unlocks
                                SET Is_Unlocked = 1
                                WHERE User_ID = @userId AND Lesson_ID = @lessonId";
                                updateCmd.Parameters.Add(new SqliteParameter("@userId", userId));
                                updateCmd.Parameters.Add(new SqliteParameter("@lessonId", lessonId));
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
    }

    public bool IsLessonUnlocked(int userId, int lessonId)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Is_Unlocked FROM User_Lesson_Unlocks WHERE User_ID = @uid AND Lesson_ID = @lid";
                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@lid", lessonId);

                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) == 1; // unlocked
                }
            }
        }

        return false; // not found or locked
    }

    public void MarkLessonAsCompleted(int userId, int lessonId)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                UPDATE User_Lesson_Unlocks
                SET Is_Completed = 1
                WHERE User_ID = @userId AND Lesson_ID = @lessonId";

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@lessonId", lessonId);

                int rowsAffected = command.ExecuteNonQuery();

                // If no row exists yet, insert instead
                if (rowsAffected == 0)
                {
                    command.CommandText = @"
                    INSERT INTO User_Lesson_Unlocks (User_ID, Lesson_ID, Is_Unlocked, Is_Completed)
                    VALUES (@userId, @lessonId, 1, 1)";
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public void CheckAndUnlockBadges(int userId)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                // ---------------- LESSON-BASED BADGES ----------------

                // 1. First Step: Finish your first lesson
                cmd.CommandText = "SELECT COUNT(*) FROM User_Lesson_Unlocks WHERE User_ID = @userId AND Is_Completed = 1";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int completedLessons = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 1, completedLessons >= 1);

                // 2. Lesson Explorer: Complete 5 lessons
                AwardBadgeIfEligible(connection, userId, 2, completedLessons >= 5);

                // 4. All-Rounder: At least 1 lesson in all 4 categories
                cmd.CommandText = @"SELECT COUNT(DISTINCT L.Category_ID) 
                                FROM User_Lesson_Unlocks U
                                JOIN Lessons_Table L ON U.Lesson_ID = L.Lesson_ID
                                WHERE U.User_ID = @userId AND U.Is_Completed = 1";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int categoriesDone = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 4, categoriesDone >= 4);

                // 5. Full Completionist: Finish all lessons
                cmd.CommandText = "SELECT COUNT(*) FROM Lessons_Table";
                cmd.Parameters.Clear();
                int totalLessons = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 5, completedLessons == totalLessons);

                // 9–12: Category Finishers
                for (int cat = 1; cat <= 4; cat++)
                {
                    // total lessons in category
                    cmd.CommandText = "SELECT COUNT(*) FROM Lessons_Table WHERE Category_ID = @cat";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@cat", cat);
                    int lessonsInCat = SafeExecuteInt(cmd);

                    // completed lessons in category
                    cmd.CommandText = @"SELECT COUNT(*) 
                                    FROM User_Lesson_Unlocks U
                                    JOIN Lessons_Table L ON U.Lesson_ID = L.Lesson_ID
                                    WHERE U.User_ID = @userId AND L.Category_ID = @cat AND U.Is_Completed = 1";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@cat", cat);
                    int completedInCat = SafeExecuteInt(cmd);

                    AwardBadgeIfEligible(connection, userId, 8 + cat, completedInCat == lessonsInCat);
                }

                // ---------------- QUIZ-BASED BADGES ----------------

                // 3. Correct Machine: Answer 100 total questions correctly
                cmd.CommandText = "SELECT SUM(Score) FROM User_Quiz_Scores WHERE User_ID = @userId";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int totalCorrect = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 3, totalCorrect >= 100);

                // 6. Quiz Champion: Score >= 90% (14/15) on any quiz
                cmd.CommandText = "SELECT COUNT(*) FROM User_Quiz_Scores WHERE User_ID = @userId AND Score >= 14";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int highScores = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 6, highScores > 0);

                // 7. Flawless Victory: Perfect score (15/15)
                cmd.CommandText = "SELECT COUNT(*) FROM User_Quiz_Scores WHERE User_ID = @userId AND Score = 15";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int perfectScores = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 7, perfectScores > 0);

                // 8. Quiz Veteran: Finished 10 quizzes
                cmd.CommandText = "SELECT COUNT(*) FROM User_Quiz_Scores WHERE User_ID = @userId";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@userId", userId);
                int quizCount = SafeExecuteInt(cmd);
                AwardBadgeIfEligible(connection, userId, 8, quizCount >= 10);
            }
        }
    }

    private int SafeExecuteInt(IDbCommand cmd)
    {
        object result = cmd.ExecuteScalar();
        return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
    }

    private void AwardBadgeIfEligible(SqliteConnection connection, int userId, int badgeId, bool condition)
    {
        if (!condition) return;

        using (var cmd = connection.CreateCommand())
        {
            // check if already unlocked
            cmd.CommandText = @"SELECT Is_Unlocked FROM User_Badges 
                            WHERE User_ID = @userId AND Badge_ID = @badgeId";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@badgeId", badgeId);

            object result = cmd.ExecuteScalar();

            bool alreadyUnlocked = (result != null && Convert.ToInt32(result) == 1);

            if (!alreadyUnlocked)
            {
                // unlock badge
                cmd.CommandText = @"INSERT INTO User_Badges (User_ID, Badge_ID, Is_Unlocked, Is_Claimed)
                                VALUES (@userId, @badgeId, 1, 0)
                                ON CONFLICT(User_ID, Badge_ID) DO UPDATE SET Is_Unlocked = 1";
                cmd.ExecuteNonQuery();

                // fetch badge name for logging
                using (var cmd2 = connection.CreateCommand())
                {
                    cmd2.CommandText = "SELECT Badges_Name FROM Badge_Table WHERE Badges_ID = @badgeId";
                    cmd2.Parameters.AddWithValue("@badgeId", badgeId);

                    string badgeName = (string)cmd2.ExecuteScalar();
                    Debug.Log($"🎉 Badge Unlocked → ID: {badgeId} | Name: {badgeName}");
                }
            }
        }
    }

}
