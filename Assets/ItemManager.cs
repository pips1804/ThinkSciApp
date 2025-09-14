using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;

public class ItemManager : MonoBehaviour
{
    // public DatabaseManager database;  // drag your DatabaseManager into Inspector

    // /// <summary>
    // /// Buy an item for a user
    // /// </summary>
    // public void BuyItem(int userId, int itemId)
    // {
    //     using (var conn = database.GetConnection())
    //     {
    //         conn.Open();

    //         // 1. Get user coins
    //         int userCoins = GetUserCoins(conn, userId);
    //         if (userCoins < 0) return; // no user found

    //         // 2. Get item price and type
    //         int price = 0;
    //         string itemType = "";
    //         int lessonId = 0;

    //         using (var cmd = conn.CreateCommand())
    //         {
    //             cmd.CommandText = "SELECT Price, Item_Type, IFNULL(Lesson_ID, 0) FROM Items WHERE Item_ID = @itemId";
    //             cmd.Parameters.Add(new SqliteParameter("@itemId", itemId));
    //             using (IDataReader reader = cmd.ExecuteReader())
    //             {
    //                 if (reader.Read())
    //                 {
    //                     price = reader.GetInt32(0);
    //                     itemType = reader.GetString(1);
    //                     lessonId = reader.GetInt32(2);
    //                 }
    //             }
    //         }

    //         // 3. Check if user can afford
    //         if (userCoins < price)
    //         {
    //             Debug.Log("Not enough coins!");
    //             return;
    //         }

    //         // 4. Deduct coins
    //         UpdateUserCoins(conn, userId, userCoins - price);

    //         // 5. Add item to User_Items
    //         AddUserItem(conn, userId, itemId);

    //         Debug.Log($"User {userId} bought item {itemId} for {price} coins.");

    //         // 6. Unlock lesson/category if collectible
    //         if (itemType == "Collectible" && lessonId > 0)
    //         {
    //             FindObjectOfType<DatabaseManager>().UnlockLessonForUser(userId, lessonId);
    //             Debug.Log($"Lesson {lessonId} unlocked by purchasing item {itemId}!");
    //         }
    //     }
    // }

    // private int GetUserCoins(IDbConnection conn, int userId)
    // {
    //     using (var cmd = conn.CreateCommand())
    //     {
    //         cmd.CommandText = "SELECT Coins FROM Users WHERE User_ID = @userId";
    //         cmd.Parameters.Add(new SqliteParameter("@userId", userId));
    //         object result = cmd.ExecuteScalar();
    //         return (result != null) ? int.Parse(result.ToString()) : -1;
    //     }
    // }

    // private void UpdateUserCoins(IDbConnection conn, int userId, int newAmount)
    // {
    //     using (var cmd = conn.CreateCommand())
    //     {
    //         cmd.CommandText = "UPDATE Users SET Coins = @coins WHERE User_ID = @userId";
    //         cmd.Parameters.Add(new SqliteParameter("@coins", newAmount));
    //         cmd.Parameters.Add(new SqliteParameter("@userId", userId));
    //         cmd.ExecuteNonQuery();
    //     }
    // }

    // private void AddUserItem(IDbConnection conn, int userId, int itemId)
    // {
    //     using (var cmd = conn.CreateCommand())
    //     {
    //         cmd.CommandText = @"
    //             INSERT INTO User_Items (User_ID, Item_ID, Quantity)
    //             VALUES (@userId, @itemId, 1)";
    //         cmd.Parameters.Add(new SqliteParameter("@userId", userId));
    //         cmd.Parameters.Add(new SqliteParameter("@itemId", itemId));
    //         cmd.ExecuteNonQuery();
    //     }
    // }
}
