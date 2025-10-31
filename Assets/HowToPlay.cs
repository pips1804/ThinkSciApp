using UnityEngine;
using UnityEngine.UI;

public class HowToPlay : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 4)] public string[] texts; // multiple text steps per image
        public Sprite image;
    }

    [Header("UI References")]
    public Image tutorialImage;
    public Text tutorialText;
    public Button nextButton;
    public Button backButton;
    public Button startButton; // Take Quiz or Start button
    public Text nextButtonText; // optional text for "Next"
    public Image nextButtonImage; // optional icon for "Next"
    public Sprite normalNextSprite;

    [Header("Tutorial Data")]
    public TutorialStep[] steps;

    private int currentStepIndex = 0;
    private int currentTextIndex = 0;

    void Start()
    {
        // Set up listeners
        nextButton.onClick.AddListener(Next);
        backButton.onClick.AddListener(Back);

        // Start button behavior (you can replace this logic)
        startButton.onClick.AddListener(() =>
        {
            Debug.Log("Starting quiz...");
            gameObject.SetActive(false);
            // Example: yourQuizPanel.SetActive(true);
        });

        // Hide start button initially
        startButton.gameObject.SetActive(false);

        // Show initial step
        ShowStep();
    }

    void OnEnable()
    {
        // Reset when the panel is opened again
        currentStepIndex = 0;
        currentTextIndex = 0;

        nextButton.gameObject.SetActive(true);
        startButton.gameObject.SetActive(false);
        ShowStep();
    }

    void ShowStep()
    {
        TutorialStep step = steps[currentStepIndex];
        tutorialImage.sprite = step.image;
        tutorialText.text = step.texts[currentTextIndex];

        // Disable back button at very start
        backButton.interactable = !(currentStepIndex == 0 && currentTextIndex == 0);

        // Check if this is the final step
        bool isLastStep = (currentStepIndex == steps.Length - 1) && (currentTextIndex == step.texts.Length - 1);

        // Toggle buttons
        if (isLastStep)
        {
            nextButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
        }
        else
        {
            nextButton.gameObject.SetActive(true);
            startButton.gameObject.SetActive(false);

            // if (nextButtonText != null)
            //     nextButtonText.text = "Next";
            // if (nextButtonImage != null && normalNextSprite != null)
            //     nextButtonImage.sprite = normalNextSprite;
        }
    }

    void Next()
    {
        TutorialStep step = steps[currentStepIndex];

        if (currentTextIndex < step.texts.Length - 1)
        {
            currentTextIndex++;
        }
        else if (currentStepIndex < steps.Length - 1)
        {
            currentStepIndex++;
            currentTextIndex = 0;
        }

        ShowStep();
    }

    void Back()
    {
        if (currentTextIndex > 0)
        {
            currentTextIndex--;
        }
        else if (currentStepIndex > 0)
        {
            currentStepIndex--;
            currentTextIndex = steps[currentStepIndex].texts.Length - 1;
        }

        ShowStep();
    }
}
