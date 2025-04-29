using UnityEngine;
using UnityEngine.UI;

public class PetHintAbility : MonoBehaviour
{
    public float cooldownDuration = 3f;
    private float cooldownTimer = 0f;

    public Button hintButton;
    public Text cooldownText;

    private bool isReady = true;

    void Start()
    {
        if (hintButton != null)
            hintButton.onClick.AddListener(UseAbility);
    }

    void Update()
    {
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            cooldownText.text = $"CD: {Mathf.Ceil(cooldownTimer)}s";

            if (cooldownTimer <= 0)
            {
                isReady = true;
                cooldownText.text = "Hint Ready!";
                hintButton.interactable = true;
            }
        }
    }

    public void UseAbility()
    {
        if (!isReady) return;

        FindObjectOfType<QuizHandler>().UseHint();

        // Start cooldown
        isReady = false;
        cooldownTimer = cooldownDuration;
        hintButton.interactable = false;
    }
}
