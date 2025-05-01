using UnityEngine;
using UnityEngine.UI;

public class PetHintAbility : MonoBehaviour
{
    public float cooldownDuration = 3f;
    private float cooldownTimer = 0f;

    public Button hintButton;
    public Text cooldownText; // This will now appear *over* the pet
    public Image petImage; // Assign your pet's Image component here
    public Material grayscaleMaterial; // Assign a grayscale material
    public Material normalMaterial; // Original material

    private bool isReady = true;

    void Start()
    {
        if (hintButton != null)
            hintButton.onClick.AddListener(UseAbility);

        // Start normal
        petImage.material = normalMaterial;
        cooldownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isReady)
        {
            cooldownTimer -= Time.deltaTime;
            cooldownText.text = $"{Mathf.Ceil(cooldownTimer)}";

            if (cooldownTimer <= 0)
            {
                isReady = true;
                cooldownText.text = "";
                cooldownText.gameObject.SetActive(false);
                petImage.material = normalMaterial;
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

        // Apply grayscale & show CD text
        petImage.material = grayscaleMaterial;
        cooldownText.gameObject.SetActive(true);
    }
}
