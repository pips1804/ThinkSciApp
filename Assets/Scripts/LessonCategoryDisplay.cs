using UnityEngine;
using UnityEngine.UI;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

public class LessonCategoryTextDisplay : MonoBehaviour
{
    public enum DisplayType { Lesson, Category }

    public DisplayType displayType = DisplayType.Lesson;
    public int id = 1;

    private Text textComponent;
    private string dbPath;

    void Start()
    {
        dbPath = "URI=file:" + Path.Combine(Application.persistentDataPath, "UserDatabase.db");
        textComponent = GetComponent<Text>();

        if (displayType == DisplayType.Lesson)
            LoadLessonName();
        else
            LoadCategoryName();
    }

    void LoadLessonName()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Lesson_Name FROM Lessons_Table WHERE Lesson_ID = @id;";
                command.Parameters.Add(new SqliteParameter("@id", id));

                using (IDataReader reader = command.ExecuteReader())
                {
                    textComponent.text = reader.Read() ? reader.GetString(0) : "No Lesson";
                }
            }
        }
    }

    void LoadCategoryName()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Category_Name FROM Category_Table WHERE Category_ID = @id;";
                command.Parameters.Add(new SqliteParameter("@id", id));

                using (IDataReader reader = command.ExecuteReader())
                {
                    textComponent.text = reader.Read() ? reader.GetString(0) : "No Category";
                }
            }
        }
    }
}
