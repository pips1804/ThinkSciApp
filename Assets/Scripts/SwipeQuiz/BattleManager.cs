using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    public int playerHealth = 100;
    public int enemyHealth = 100;

    public HealthBar playerHealthBar;
    public HealthBar enemyHealthBar;

    //public Animator playerAnimator;
    //public Animator enemyAnimator;
    void Start()
    {
        playerHealthBar.SetMaxHealth(playerHealth);
        enemyHealthBar.SetMaxHealth(enemyHealth);
    }

    public void EnemyTakeDamage(int amount)
    {
        enemyHealth -= amount;
        enemyHealthBar.SetHealth(enemyHealth);
        //enemyAnimator.SetTrigger("Hit");
    }

    public void PlayerTakeDamage(int amount)
    {
        playerHealth -= amount;
        playerHealthBar.SetHealth(playerHealth);
        //playerAnimator.SetTrigger("Hit");
    }
}
