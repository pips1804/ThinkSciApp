using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HeatHeroRescue : MonoBehaviour
{
    [Header("UI References")]
    public Image petImage;
    public Image powerBarFill;
    public Text timerText;
    public GameObject resultPanel;
    public Text resultText;
    public RectTransform petPanel;

    [Header("Settings")]
    public float maxPower = 10f;
    public float powerGain = 1f;
    public float powerLoss = 0.5f;
    public float gameTime = 30f;

    [Header("Panel Animation")]
    public float slideDistance = 300f;
    public float slideDuration = 0.5f;

    private  float currentPower;
    private float timeLeft;
    private bool isGameOver = false;

    void Start()
    {
        currentPower = 0;
        timeLeft = gameTime;
        resultPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return;

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


        resultPanel.SetActive(true);
        resultText.text = win ? " Blast Off! You Won!" : " Time’s Up! Try Again.";
    }

}
