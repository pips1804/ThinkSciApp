using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;  // For Slider

public class SplashScreenLoader : MonoBehaviour
{
    public Slider loadingSlider;   // Assign your Slider in the Inspector
    public float loadingSpeed = 0.5f; // Speed of loading  
    public DatabaseManager dbManager;

    void Start()
    {
        // Wait until user data is actually in the database before redirecting
        if (dbManager.HasUser())
        {
            // Redirect only if there is valid user data
            SceneManager.LoadScene("MainScene");
        }
        else
        {
            // Show account creation panel
            SceneManager.LoadScene("CreateAccount");
        }
    }
}
