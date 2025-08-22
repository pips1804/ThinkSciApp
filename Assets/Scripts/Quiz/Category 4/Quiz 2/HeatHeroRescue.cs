using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeatHeroRescue : MonoBehaviour
{
    [Header("UI References")]
    public Image petImage;
    public Image powerBarFill;
    public Text timerText;
    public RectTransform petPanel;
    public GameObject gamePanel;
    public GameObject petpanel;

    [Header("Settings")]
    public float maxPower = 10f;
    public float powerGain = 1f;
    public float powerLoss = 0.5f;
    public float gameTime = 30f;

    [Header("Panel Animation")]
    public float slideDistance = 300f;
    public float slideDuration = 0.5f;

    public Dialogues dialogues;

    private float currentPower;
    private float timeLeft;
    private bool isGameOver = false;
    private bool gameStarted = false; // ✅ flag to check if game has begun

    void Start()
    {
        gamePanel.SetActive(false);
        petpanel.SetActive(false);

        currentPower = 0;
        timeLeft = gameTime;

        // Start intro dialogue (index 0)
        dialogues.StartDialogue(0);
    }

    void Update()
    {
        // ✅ Wait until intro dialogue finishes before starting
        if (!gameStarted && dialogues.dialogueFinished)
        {
            gameStarted = true;
            gamePanel.SetActive(true);
            petpanel.SetActive(true);
        }

        if (isGameOver || !gameStarted) return;

        timeLeft -= Time.deltaTime;
        timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        if (currentPower >= maxPower)
        {
            EndGame(true);
        }
        else if (timeLeft <= 0)
        {
            EndGame(false);
        }
    }

    public void Answer(bool isHeatDevice, bool correctAnswer)
    {
        if (!gameStarted || isGameOver) return;

        if (isHeatDevice == correctAnswer)
        {
            currentPower += powerGain;
        }
        else
        {
            currentPower = Mathf.Max(0, currentPower - powerLoss);
        }

        powerBarFill.fillAmount = currentPower / maxPower;
    }

    public void EndGame(bool win)
    {
        isGameOver = true;
        StartCoroutine(SlidePetPanelUpperRight(win));
    }

    private IEnumerator SlidePetPanelUpperRight(bool win)
    {
        Vector2 startPos = petPanel.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(slideDistance, slideDistance);

        float elapsed = 0;
        while (elapsed < slideDuration)
        {
            petPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / slideDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        petPanel.anchoredPosition = endPos;

        yield return new WaitForSeconds(1);

        petpanel.SetActive(false);
        gamePanel.SetActive(false);

        // ✅ Show end dialogue
        if (win)
            dialogues.StartDialogue(1);
        else
            dialogues.StartDialogue(2);
    }
}
