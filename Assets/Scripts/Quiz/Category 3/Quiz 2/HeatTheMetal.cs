using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class Question
{
    public string questionText;
    public string[] choices;
    public int correctAnswerIndex;
}

public class HeatTheMetal : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject gamePanel;
    public GameObject quizPanel;
    public GameObject resultPanel;

    [Header("Scenario 2 Panels")]
    public GameObject warmRoomPanel;
    public GameObject messagePanel;
    public Text messageText;
    public float messageDisplayTime = 2f;

    [Header("Scenario 1 Elements")]
    public GameObject heatTheMetalPanel;
    public Image heatEffect;
    public float heatingTime = 2f;

    [Header("Scenario 2 Elements")]
    public GameObject warmAirArrows;
    public GameObject coolAirArrows;
    public float moveDistance = 30f;
    public float moveSpeed = 1f;

    [Header("Scenario 3 Elements")]
    public GameObject solarPanelPanel;
    public Slider rotationSlider;  
    public RectTransform solarPanelImage;
    public Image sunlightBeam;         
    public float targetRotation = 45f; 
    public float rotationTolerance = 5f;
    public float holdTimeRequired = 1f;
    public float beamFadeSpeed = 1f;

    [Header("Quiz Elements")]
    public Text questionTextUI;
    public Button[] answerButtons;
    public Text[] answerButtonTexts;
    public Question[] questionsScenario1;
    public Question[] questionsScenario2;
    public Question[] questionsScenario3;

    [Header("Dialogue Reference")]
    public Dialogues dialogueManager; // <-- Reference to Dialogue script

    private int currentQuestionIndex = 0;
    private bool isHeating = false;
    private float heatProgress = 0f;
    private bool inScenario2 = false;
    private Coroutine arrowAnim;

    private float correctHoldTime = 0f;
    private bool scenario3Active = false;
    private bool scenario3Completed = false;

    void Start()
    {
        ShowIntro();
        warmRoomPanel.SetActive(false);
        warmAirArrows.SetActive(false);
        coolAirArrows.SetActive(false);
    }

    void ShowIntro()
    {
        introPanel.SetActive(true);
        gamePanel.SetActive(false);
        quizPanel.SetActive(false);
        resultPanel.SetActive(false);
        messagePanel.SetActive(false);
        warmRoomPanel.SetActive(false);
        heatEffect.color = new Color(heatEffect.color.r, heatEffect.color.g, heatEffect.color.b, 0);
    }

    public void StartGame()
    {
        introPanel.SetActive(false);

        dialogueManager.StartDialogue(1);

        // When dialogue finishes, continue to scenario 1 gameplay
        StartCoroutine(WaitForDialogueThen(() =>
        {
            gamePanel.SetActive(true);
            isHeating = false;
            heatProgress = 0f;
            heatTheMetalPanel.SetActive(true);
        }));
    }

    IEnumerator WaitForDialogueThen(System.Action onFinish)
    {
        // Wait until dialogue panel is closed
        while (dialogueManager.dialoguePanel.activeSelf)
            yield return null;

        onFinish?.Invoke();
    }

    void Update()
    {
        if (!inScenario2 && isHeating)
        {
            heatProgress += Time.deltaTime;
            Color c = heatEffect.color;
            c.a = Mathf.Clamp01(heatProgress / heatingTime);
            heatEffect.color = c;

            if (heatProgress >= heatingTime)
            {
                isHeating = false;
                StartQuizScenario1();
            }
        }

        if (scenario3Active && !scenario3Completed)
        {
            HandleScenario3();
        }
    }

    void StartQuizScenario1()
    {
        gamePanel.SetActive(true);
        quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario1();
    }

    void ShowQuestionScenario1()
    {
        Question q = questionsScenario1[currentQuestionIndex];
        questionTextUI.text = q.questionText;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtonTexts[i].text = q.choices[i];
            answerButtons[i].onClick.RemoveAllListeners();
            int index = i;
            answerButtons[i].onClick.AddListener(() => SelectAnswerScenario1(index));
        }
    }

    public void SelectAnswerScenario1(int index)
    {
        Question q = questionsScenario1[currentQuestionIndex];
        Debug.Log(index == q.correctAnswerIndex ? "Correct!" : "Wrong!");
        currentQuestionIndex++;
        if (currentQuestionIndex < questionsScenario1.Length)
            ShowQuestionScenario1();
        else
            EndScenario1();
    }

    void EndScenario1()
    {
        quizPanel.SetActive(false);
        resultPanel.SetActive(true);
        gamePanel.SetActive(false);

        // 🔹 Show Dialogue before Scenario 2
        dialogueManager.StartDialogue(2);
        StartCoroutine(WaitForDialogueThen(() =>
        {
            resultPanel.SetActive(false);
            heatTheMetalPanel.SetActive(false);
            StartScenario2();
        }));
    }

    void StartScenario2()
    {
        inScenario2 = true;
        gamePanel.SetActive(true);
        warmRoomPanel.SetActive(true);
        warmAirArrows.SetActive(false);
        coolAirArrows.SetActive(false);
    }

    public void FirePlacedSuccess()
    {
        isHeating = true;
    }

    public void HeaterPlacedSuccess()
    {
        warmAirArrows.SetActive(true);
        coolAirArrows.SetActive(true);
        if (arrowAnim != null) StopCoroutine(arrowAnim);
        arrowAnim = StartCoroutine(AnimateArrows());
        StartQuizScenario2();
    }

    private IEnumerator AnimateArrows()
    {
        Vector3 warmStart = warmAirArrows.transform.localPosition;
        Vector3 coolStart = coolAirArrows.transform.localPosition;

        while (true)
        {
            float t = Mathf.PingPong(Time.time * moveSpeed, 1f);
            warmAirArrows.transform.localPosition = warmStart + Vector3.up * Mathf.Lerp(0, moveDistance, t);
            coolAirArrows.transform.localPosition = coolStart + Vector3.down * Mathf.Lerp(0, moveDistance, t);
            yield return null;
        }
    }

    void StartQuizScenario2()
    {
        quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario2();
    }

    void ShowQuestionScenario2()
    {
        Question q = questionsScenario2[currentQuestionIndex];
        questionTextUI.text = q.questionText;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtonTexts[i].text = q.choices[i];
            answerButtons[i].onClick.RemoveAllListeners();
            int index = i;
            answerButtons[i].onClick.AddListener(() => SelectAnswerScenario2(index));
        }
    }

    public void SelectAnswerScenario2(int index)
    {
        currentQuestionIndex++;
        if (currentQuestionIndex < questionsScenario2.Length)
            ShowQuestionScenario2();
        else
            EndScenario2();
    }

    void EndScenario2()
    {
        quizPanel.SetActive(false);
        gamePanel.SetActive(false);

        // 🔹 Show Dialogue before Scenario 3
        dialogueManager.StartDialogue(3);
        StartCoroutine(WaitForDialogueThen(() =>
        {
            warmRoomPanel.SetActive(false);
            StartScenario3();
        }));
    }

    void StartScenario3()
    {
        gamePanel.SetActive(true);
        solarPanelPanel.SetActive(true);
        sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b, 0);
        correctHoldTime = 0f;
        scenario3Active = true;
        scenario3Completed = false;

        rotationSlider.onValueChanged.RemoveAllListeners();
        rotationSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        float rotation = Mathf.Lerp(0f, 90f, value);
        solarPanelImage.localEulerAngles = new Vector3(0, 0, rotation);
    }

    void HandleScenario3()
    {
        float currentRotation = solarPanelImage.localEulerAngles.z;
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentRotation, targetRotation));

        if (diff <= rotationTolerance)
        {
            correctHoldTime += Time.deltaTime;
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                Mathf.MoveTowards(sunlightBeam.color.a, 1f, beamFadeSpeed * Time.deltaTime));

            if (correctHoldTime >= holdTimeRequired)
            {
                scenario3Completed = true;
                scenario3Active = false;
                rotationSlider.interactable = false;
                StartQuizScenario3();
            }
        }
        else
        {
            correctHoldTime = 0f;
            sunlightBeam.color = new Color(sunlightBeam.color.r, sunlightBeam.color.g, sunlightBeam.color.b,
                Mathf.MoveTowards(sunlightBeam.color.a, 0f, beamFadeSpeed * Time.deltaTime));
        }
    }

    void StartQuizScenario3()
    {
        quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        ShowQuestionScenario3();
    }

    void ShowQuestionScenario3()
    {
        Question q = questionsScenario3[currentQuestionIndex];
        questionTextUI.text = q.questionText;
        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtonTexts[i].text = q.choices[i];
            answerButtons[i].onClick.RemoveAllListeners();
            int index = i;
            answerButtons[i].onClick.AddListener(() => SelectAnswerScenario3(index));
        }
    }

    public void SelectAnswerScenario3(int index)
    {
        currentQuestionIndex++;
        if (currentQuestionIndex < questionsScenario3.Length)
            ShowQuestionScenario3();
        else
            EndScenario3();
    }

    void EndScenario3()
    {
        quizPanel.SetActive(false);
        messagePanel.SetActive(true);
        messageText.text = "Excellent! You used solar radiation!";
        StartCoroutine(ResetGame());
    }

    IEnumerator ResetGame()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        messagePanel.SetActive(false);
    }
}
