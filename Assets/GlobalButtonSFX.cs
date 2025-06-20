using UnityEngine;
using UnityEngine.UI;

public class GlobalButtonSFX : MonoBehaviour
{
    public AudioClip clickSound;

    void Start()
    {
        // Find all Buttons in the scene
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtons)
        {
            btn.onClick.AddListener(() =>
            {
                if (clickSound != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(clickSound);
                }
            });
        }
    }
}
