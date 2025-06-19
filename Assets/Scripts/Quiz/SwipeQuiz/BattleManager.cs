using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public int playerHealth = 100;
    public int enemyHealth = 100;

    public int enemyHealthFixed = 100;

    public HealthBar playerHealthBar;
    public HealthBar enemyHealthBar;
    public DatabaseManager dbManager;


    public int userID = 1;

    //public Animator playerAnimator;
    //public Animator enemyAnimator;
    void Start()
    {
        var (name, baseHealth, baseDamage) = dbManager.GetPetStats(userID);
        playerHealth = baseHealth;
        playerHealthBar.SetMaxHealth(playerHealth);
        enemyHealthBar.SetMaxHealth(enemyHealth);
    }

    public void EnemyTakeDamage(int damage)
    {
        enemyHealth -= damage;
        enemyHealthBar.SetHealth(enemyHealth);
        //enemyAnimator.SetTrigger("Hit");
    }

    public void SuddenDeathDamage(int damage)
    {
        enemyHealth -= damage;
        playerHealth -= damage;
        enemyHealthBar.SetHealth(enemyHealth);
        playerHealthBar.SetHealth(playerHealth);
    }

    public void PlayerTakeDamage(int amount)
    {
        playerHealth -= amount;
        playerHealthBar.SetHealth(playerHealth);
        //playerAnimator.SetTrigger("Hit");
    }

    public void ResetBattle()
    {
        var (name, baseHealth, baseDamage) = dbManager.GetPetStats(userID);

        playerHealth = baseHealth;
        enemyHealth = enemyHealthFixed; // Or use a variable if enemy HP scales

        playerHealthBar.SetMaxHealth(playerHealth);
        playerHealthBar.SetHealth(playerHealth);

        enemyHealthBar.SetMaxHealth(enemyHealth);
        enemyHealthBar.SetHealth(enemyHealth);
    }

}
