// TugOfWarSimulation.cs - Handles the tug of war simulation (UI spawning version)
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class TugOfWarSimulation : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform[] leftSpawnPoints = new Transform[3];
    public Transform[] rightSpawnPoints = new Transform[3];

    [Header("UI Parent")]
    public Transform uiParent; // Parent transform for spawned pets (usually the Canvas or a UI Panel)

    [Header("Pet Sprites")]
    public GameObject petPrefab; // UI Image prefab
    public Sprite leftPetSprite; // Sprite for left side pets
    public Sprite rightPetSprite; // Sprite for right side pets

    [Header("Rope")]
    public Transform rope;
    public float ropeMovementSpeed = 2f;
    public float maxRopeOffset = 150f; // UI units for portrait mode - adjust as needed

    [Header("UI")]
    public Button addLeftPetButton;
    public Button addRightPetButton;
    public Button removeLeftPetButton;
    public Button removeRightPetButton;
    public Button startSimulationButton;
    public Text leftForceText;
    public Text rightForceText;

    [Header("Simulation Timer")]
    public float simulationDuration = 60f; // total freeplay time
    public Text timerText;                 // UI text to show countdown

    private float timeRemaining;
    private bool timerRunning = false;
    [Header("Force Values")]
    public int leftPetForceValue = 2; // Force value for left pets
    public int rightPetForceValue = 2; // Force value for right pets

    private List<GameObject> leftPets = new List<GameObject>();
    private List<GameObject> rightPets = new List<GameObject>();
    private int leftTotalForce = 0;
    private int rightTotalForce = 0;
    private bool simulationRunning = false;
    private Vector3 initialRopePosition;

    [Header("Group Movement")]
    public RectTransform tugOfWarGroup;

    private void Start()
    {
        initialRopePosition = rope.position;

        addLeftPetButton.onClick.AddListener(() => AddPet(true));
        addRightPetButton.onClick.AddListener(() => AddPet(false));
        removeLeftPetButton.onClick.AddListener(() => RemovePet(true));
        removeRightPetButton.onClick.AddListener(() => RemovePet(false));
        startSimulationButton.onClick.AddListener(StartTugOfWar);

        UpdateUI();
    }

    public void StartSimulation()
    {
        ResetSimulation();
        BeginFreeplayPhase();
        addLeftPetButton.interactable = true;
        addRightPetButton.interactable = true;
        startSimulationButton.interactable = false;
    }

    private void AddPet(bool isLeft)
    {
        if (simulationRunning) return;

        if (isLeft && leftPets.Count >= 3) return;
        if (!isLeft && rightPets.Count >= 3) return;

        Transform[] spawnPoints = isLeft ? leftSpawnPoints : rightSpawnPoints;
        List<GameObject> petList = isLeft ? leftPets : rightPets;

        // Find next available spawn point
        Transform spawnPoint = spawnPoints[petList.Count];

        // Create pet
        GameObject newPet = Instantiate(petPrefab, spawnPoint.parent);

        // Reset scale so prefab keeps its original size
        RectTransform petRect = newPet.GetComponent<RectTransform>();
        petRect.localScale = Vector3.one;

        // Place at the spawn point
        RectTransform spawnRect = spawnPoint.GetComponent<RectTransform>();
        if (petRect != null && spawnRect != null)
        {
            petRect.localPosition = spawnRect.localPosition;
            petRect.anchorMin = spawnRect.anchorMin;
            petRect.anchorMax = spawnRect.anchorMax;
            petRect.pivot = spawnRect.pivot;
        }

        // Assign sprite and force based on side
        Image petImage = newPet.GetComponent<Image>();
        if (petImage != null)
        {
            if (isLeft)
            {
                petImage.sprite = leftPetSprite;
            }
            else
            {
                petImage.sprite = rightPetSprite;
            }
        }

        PetController petController = newPet.GetComponent<PetController>();
        if (petController == null)
            petController = newPet.AddComponent<PetController>();

        // Set force value based on side
        petController.forceValue = isLeft ? leftPetForceValue : rightPetForceValue;
        petController.isLeft = isLeft;

        petList.Add(newPet);

        if (isLeft)
            leftTotalForce += petController.forceValue;
        else
            rightTotalForce += petController.forceValue;

        UpdateUI();

        // Enable start button if both sides have at least one pet
        if (leftPets.Count > 0 && rightPets.Count > 0)
        {
            startSimulationButton.interactable = true;
        }
    }

    // private void StartTugOfWar()
    // {
    //     if (leftPets.Count == 0 || rightPets.Count == 0)
    //     {
    //         Debug.Log("Both sides need at least one pet!");
    //         return;
    //     }

    //     simulationRunning = true;
    //     addLeftPetButton.interactable = false;
    //     addRightPetButton.interactable = false;
    //     startSimulationButton.interactable = false;

    //     StartCoroutine(SimulateTugOfWar());
    // }

    private void StartTugOfWar()
    {
        if (!timerRunning) return; // only allow if inside freeplay

        if (leftPets.Count == 0 || rightPets.Count == 0)
        {
            Debug.Log("Both sides need at least one pet!");
            return;
        }

        if (simulationRunning) return; // prevent overlapping

        simulationRunning = true;
        addLeftPetButton.interactable = true;
        addRightPetButton.interactable = true;
        removeLeftPetButton.interactable = true;
        removeRightPetButton.interactable = true;
        startSimulationButton.interactable = true;
        StartCoroutine(SimulateTugOfWar());
    }


    private IEnumerator SimulateTugOfWar()
    {
        // Start pet pulling animations
        foreach (var pet in leftPets) pet.GetComponent<PetController>().StartPulling();
        foreach (var pet in rightPets) pet.GetComponent<PetController>().StartPulling();

        int forceDifference = leftTotalForce - rightTotalForce;

        float roundTime = 3f;
        float elapsed = 0f;

        Vector3 startPos = tugOfWarGroup.anchoredPosition;
        Vector3 targetPos = startPos;

        if (forceDifference > 0) // left wins
            targetPos += Vector3.left * Mathf.Clamp(forceDifference * 40f, 40f, maxRopeOffset);
        else if (forceDifference < 0) // right wins
            targetPos += Vector3.right * Mathf.Clamp(Mathf.Abs(forceDifference) * 40f, 40f, maxRopeOffset);

        // Pets + rope move together for 3 seconds
        while (elapsed < roundTime)
        {
            float progress = elapsed / roundTime;
            tugOfWarGroup.anchoredPosition = Vector3.Lerp(startPos, targetPos, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Small pause
        yield return new WaitForSeconds(0.5f);

        // Reset smoothly back to center
        elapsed = 0f;
        Vector3 resetStart = tugOfWarGroup.anchoredPosition;
        while (elapsed < 1f)
        {
            tugOfWarGroup.anchoredPosition = Vector3.Lerp(resetStart, Vector3.zero, elapsed / 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        tugOfWarGroup.anchoredPosition = Vector3.zero;

        // Stop animations
        foreach (var pet in leftPets) pet.GetComponent<PetController>().StopPulling();
        foreach (var pet in rightPets) pet.GetComponent<PetController>().StopPulling();

        simulationRunning = false;
        // FindObjectOfType<ForceGameManager>().OnSimulationComplete();
    }

    public void BeginFreeplayPhase()
    {
        ResetSimulation(); // clear everything
        timeRemaining = simulationDuration;
        timerRunning = true;
        StartCoroutine(TimerCountdown());
    }

    private IEnumerator TimerCountdown()
    {
        while (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = "" + Mathf.CeilToInt(timeRemaining).ToString();
            }
            yield return null;
        }

        timerRunning = false;
        EndFreeplayPhase();
    }

    private void EndFreeplayPhase()
    {
        // Stop any running simulations
        StopAllCoroutines();

        // Reset rope and pets
        ResetSimulation();

        // Move to quiz
        FindObjectOfType<ForceGameManager>().OnSimulationComplete();
    }


    public void RemovePet(bool isLeft)
    {
        List<GameObject> petList = isLeft ? leftPets : rightPets;
        if (petList.Count == 0) return;

        GameObject pet = petList[petList.Count - 1];
        petList.RemoveAt(petList.Count - 1);
        Destroy(pet);

        if (isLeft) leftTotalForce -= leftPetForceValue;
        else rightTotalForce -= rightPetForceValue;

        UpdateUI();
    }

    private void UpdateUI()
    {
        leftForceText.text = "Left Force: " + leftTotalForce;
        rightForceText.text = "Right Force: " + rightTotalForce;

        addLeftPetButton.interactable = !simulationRunning && leftPets.Count < 3;
        addRightPetButton.interactable = !simulationRunning && rightPets.Count < 3;
        removeLeftPetButton.interactable = !simulationRunning && leftPets.Count > 0;
        removeRightPetButton.interactable = !simulationRunning && rightPets.Count > 0;
    }

    public void ResetSimulation()
    {
        // Clear existing pets
        foreach (var pet in leftPets)
        {
            if (pet != null) DestroyImmediate(pet);
        }
        foreach (var pet in rightPets)
        {
            if (pet != null) DestroyImmediate(pet);
        }

        leftPets.Clear();
        rightPets.Clear();
        leftTotalForce = 0;
        rightTotalForce = 0;
        simulationRunning = false;

        // Reset rope position
        rope.position = initialRopePosition;

        UpdateUI();
    }

    // Getter methods for quiz questions
    public int GetLeftForce() { return leftTotalForce; }
    public int GetRightForce() { return rightTotalForce; }
    public bool DidRightWin() { return rightTotalForce > leftTotalForce; }
}
