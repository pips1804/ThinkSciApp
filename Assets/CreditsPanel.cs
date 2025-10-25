using UnityEngine;
using UnityEngine.UI;

public class CreditsPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button developersButton;
    public Button assetsButton;

    [Header("Content Panels")]
    public GameObject developersContent;
    public GameObject assetsContent;

    void Start()
    {
        // Add button listeners
        developersButton.onClick.AddListener(ShowDevelopers);
        assetsButton.onClick.AddListener(ShowAssets);

        // Show developers by default
        ShowDevelopers();
    }

    public void ShowDevelopers()
    {
        developersContent.SetActive(true);
        assetsContent.SetActive(false);
        HighlightButton(developersButton, true);
        HighlightButton(assetsButton, false);
    }

    public void ShowAssets()
    {
        developersContent.SetActive(false);
        assetsContent.SetActive(true);
        HighlightButton(developersButton, false);
        HighlightButton(assetsButton, true);
    }

    // Optional: visually highlight the active button
    void HighlightButton(Button button, bool isActive)
    {
        var colors = button.colors;
        colors.normalColor = isActive ? new Color(0.9f, 0.8f, 0.2f) : Color.white;
        button.colors = colors;
    }
}
