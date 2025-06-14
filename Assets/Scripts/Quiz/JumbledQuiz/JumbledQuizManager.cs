using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class JumbledQuizManager : MonoBehaviour
{
    [System.Serializable]
    public class JumbledQuestion
    {
        public string question;
        public string answer;
        public string explanationText;  // Add this for feedback, or you can modify accordingly
    }

    [Header("Questions and Answers")]
    public List<JumbledQuestion> questions;

    [Header("Player and Enemy")]
    public RectTransform playerIcon;
    public RectTransform enemyIcon;

    private Vector3 playerStartPos;
    private Vector3 enemyStartPos;
    public int suddenDeathDamage = 10;

    public BattleManager battleManager;

    [Header("UI and Elements")]
    public Text questionText;
    public GameObject letterButtonPrefab;
    public Transform letterContainer;
    public Text timerText;

    private int currentQuestionIndex = 0;
    private float timer = 20f;  // Jumbled quiz timer
    private bool isTimerRunning = false;

    private bool isBlinking = false;
    private Vector3 originalScale;

    public Slider progressBar;
    private float targetProgress = 0f;
    public RectTransform progressHandle;

    public Text scoreText;
    private int score = 0;

    public Text finalScoreText;
    public Text scoreMessageText;

    public float hitChancePercent = 50f; // adjustable

    public Text missText;
    public Text damageText;
    public Text enemySuddenText;
    public Text playerSuddenText;

    public GameObject feedbackPanel;
    public Text feedbackText;
    private bool canAnswer = true;

    private List<Button> letterButtons = new List<Button>();
    private int selectedIndex = -1;

    private Color defaultColor;
    private Color selectedColor;

    // Skill system
    public Button skillButton;
    public GameObject doubleSwordIcon;
    private bool isSkillActive = false;
    private float skillCooldown = 30f;
    private float skillTimer = 0f;

    public Image skillCooldownFill;

    public GameObject impactImage;
    private bool isMiss = false;
    private bool isPlayer = true;

    public GameObject resultPanel;

    public GameObject playerShadow;
    public GameObject enemyShadow;
    public Image battleBackground;

    public GameObject timerContainer;
    public GameObject scoreContainer;

    public IdleAnimation playerIdleAnim;
    public IdleAnimation enemyIdleAnim;

    public BattleAnimationManager battleAnim;


    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#116530", out defaultColor);
        ColorUtility.TryParseHtmlString("#E8E8CC", out selectedColor);  

    }

    void Start()
    {
        playerStartPos = playerIcon.anchoredPosition;
        enemyStartPos = enemyIcon.anchoredPosition;
        originalScale = timerText.transform.localScale;

        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = questions.Count;
            progressBar.value = 0;
        }

        DisplayQuestion();
        UpdateScoreText();
    }

    void Update()
    {
        if (isTimerRunning)
        {
            timer -= Time.deltaTime;
            int displayTime = Mathf.CeilToInt(timer);
            timerText.text = displayTime.ToString();


            // Heartbeat effect when timer is 10 or below
            if (displayTime <= 10)
            {
                timerText.color = Color.red;

                if (!isBlinking)
                {
                    isBlinking = true;
                    battleAnim.StartCoroutine(battleAnim.HeartbeatEffect(isTimerRunning, originalScale, timer));
                }
            }

            if (timer <= 0)
            {
                isTimerRunning = false;
                timerText.text = "0";
                timerText.transform.localScale = originalScale;
                StopAllCoroutines();
                PlayerMissedAnswer();
            }
        }

        // Skill cooldown timer
        if (skillTimer > 0)
        {
            skillTimer -= Time.deltaTime;
            if (skillTimer <= 0)
            {
                skillButton.interactable = true;
            }
        }

        // Cooldown visual
        if (skillCooldownFill != null)
        {
            skillCooldownFill.fillAmount = skillTimer > 0 ? skillTimer / skillCooldown : 0;
        }
    }

    void DisplayQuestion()
    {
        ClearLetters();
        var question = questions[currentQuestionIndex];
        questionText.text = question.question;

        string shuffled = new string(question.answer.ToCharArray().OrderBy(c => Random.value).ToArray());

        // Debug log the shuffled letters
        Debug.Log($"Shuffled letters: {shuffled}");

        for (int i = 0; i < shuffled.Length; i++)
        {
            GameObject letterObj = Instantiate(letterButtonPrefab, letterContainer);
            Button letterButton = letterObj.GetComponent<Button>();
            int index = i;
            letterButton.GetComponentInChildren<Text>().text = shuffled[i].ToString();
            letterButton.onClick.AddListener(() => OnLetterClick(index));
            letterButtons.Add(letterButton);
        }

        timer = 30f;
        isTimerRunning = true;
        canAnswer = true;
        selectedIndex = -1;
        battleAnim.StartCoroutine(battleAnim.FadeTextLoop());
    }

    void OnLetterClick(int index)
    {
        if (!canAnswer) return;
        if (index < 0 || index >= letterButtons.Count)
            return;

        if (selectedIndex == -1)
        {
            selectedIndex = index;
            letterButtons[index].GetComponentInChildren<Text>().color = selectedColor;
        }
        else if (selectedIndex != index)
        {
            // Start the animation coroutine instead of instantly swapping
            StartCoroutine(AnimateSwap(selectedIndex, index));
        }
        else
        {
            letterButtons[selectedIndex].GetComponentInChildren<Text>().color = defaultColor;
            selectedIndex = -1;
        }
    }

    public void OnSubmitButton()
    {
        if (!canAnswer) return;
        isTimerRunning = false;
        SubmitAnswer();
    }

    void SubmitAnswer()
    {
        if (!canAnswer) return;
        canAnswer = false;

        var currentQ = questions[currentQuestionIndex];
        string userAnswer = string.Concat(letterButtons.Select(b => b.GetComponentInChildren<Text>().text.Trim()).Where(t => !string.IsNullOrEmpty(t)));
        string correctAnswer = questions[currentQuestionIndex].answer.Trim();
        bool isCorrect = string.Equals(userAnswer, correctAnswer, System.StringComparison.OrdinalIgnoreCase);



        if (isCorrect)
        {
            isPlayer = true;
            bool isHit = Random.value <= (hitChancePercent * 0.01f);

            Debug.Log($"User Answer: {userAnswer}");
            Debug.Log($"Correct Answer: {correctAnswer}");

            if (isHit)
            {
                ShowFeedback("Correct!", currentQ.explanationText);
                int baseDamage = Random.Range(10, 16);
                int damage = isSkillActive ? baseDamage * 2 : baseDamage;
                battleManager.EnemyTakeDamage(damage);
                isMiss = false;
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                if (isSkillActive)
                {
                    battleAnim.StartCoroutine(battleAnim.IntenseAttackAnimation(playerIcon, playerStartPos, new Vector3(300, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                }
                else
                {
                    battleAnim.StartCoroutine(battleAnim.AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                }
                battleAnim.StartCoroutine(battleAnim.HitShake(enemyIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, enemyIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha

            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", currentQ.explanationText);
                isMiss = true;
                if (isSkillActive)
                {
                    battleAnim.StartCoroutine(battleAnim.IntenseAttackAnimation(playerIcon, playerStartPos, new Vector3(300, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                }
                else
                {
                    battleAnim.StartCoroutine(battleAnim.AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                }
                battleAnim.StartCoroutine(battleAnim.DodgeAnimation(enemyIcon));
                Color missColor;
                ColorUtility.TryParseHtmlString("#5f9103", out missColor);
                StartCoroutine(ShowFloatingText(missText, "Miss!", enemyIcon.position, missColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha


            }

            score++;
        }
        else
        {
            bool isHit = Random.value <= (hitChancePercent * 0.01f);

            Debug.Log($"User Answer: {userAnswer}");
            Debug.Log($"Correct Answer: {correctAnswer}");
            isPlayer = false;

            if (isHit)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                isMiss = false;
                ShowFeedback("Wrong!", currentQ.explanationText);
                int damage = Random.Range(10, 16);
                battleManager.PlayerTakeDamage(damage);
                battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
                battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                isMiss = true;
                ShowFeedback("Wrong!", currentQ.explanationText);
                battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
                battleAnim.StartCoroutine(battleAnim.DodgeAnimation(playerIcon));
                Color missColor;
                ColorUtility.TryParseHtmlString("#5f9103", out missColor);
                StartCoroutine(ShowFloatingText(missText, "Miss!", playerIcon.position, missColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha

            }
        }
        UpdateScoreText();
    }

    void PlayerMissedAnswer()
    {
        timerContainer.SetActive(false);
        scoreContainer.SetActive(false);
        canAnswer = false;
        int damage = Random.Range(10, 16);
        battleManager.PlayerTakeDamage(damage);
        isMiss = false;
        isPlayer = false;
        battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
        battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
        Color damageColor = new Color(1f, 0f, 0f); // Red
        StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
        ShowFeedback("Time's Up!", "You didn't answer in time.");
        StartCoroutine(WaitThenNextQuestion());
        timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    void ShowFeedback(string resultText, string explanation)
    {
        feedbackPanel.SetActive(true);
        StartCoroutine(TypeFeedback(resultText, explanation));
        StartCoroutine(WaitThenNextQuestion());
    }

    public void ActivateSkill()
    {
        if (skillTimer > 0) return;

        isSkillActive = true;
        doubleSwordIcon.SetActive(true);
        Image iconImage = doubleSwordIcon.GetComponent<Image>();
        if (iconImage != null)
        {
            Color color = iconImage.color;
            iconImage.color = new Color(color.r, color.g, color.b, 1f); // full opacity
        }

        skillTimer = skillCooldown;
        skillButton.interactable = false;

        StartCoroutine(DeactivateSkillAfterDelay());
    }

    void NextQuestionOrEnd()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        // Sudden death mechanic for last 3 questions
        if (currentQuestionIndex >= questions.Count - 3)
        {
            battleAnim.StartCoroutine(battleAnim.GraduallyTurnRed(3f)); // 3 seconds transition
            battleManager.PlayerTakeDamage(suddenDeathDamage);
            battleManager.EnemyTakeDamage(suddenDeathDamage);

            // Optional: Show visual effects for damage taken
            Color suddenColor = new Color(1f, 0.5f, 0f); // Orange
            StartCoroutine(ShowFloatingText(playerSuddenText, "-" + suddenDeathDamage, playerIcon.position, suddenColor));
            StartCoroutine(ShowFloatingText(enemySuddenText, "-" + suddenDeathDamage, enemyIcon.position, suddenColor));

            battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
            battleAnim.StartCoroutine(battleAnim.HitShake(enemyIcon));
        }

        currentQuestionIndex++;

        if (progressBar != null)
        {
            targetProgress = currentQuestionIndex;
            StartCoroutine(AnimateProgressBar());
        }

        if (currentQuestionIndex < questions.Count)
        {
            DisplayQuestion();
        }
        else
        {
            ShowResult();
        }
    }

    void ShowResult()
    {
        resultPanel.SetActive(true);
        questionText.text = "";
        timerText.text = "";
        ClearLetters();

        string gradeMsg = "Good try!";
        if (score >= questions.Count * 0.8f)
            gradeMsg = "Excellent! You're a quiz master!";
        else if (score >= questions.Count * 0.5f)
            gradeMsg = "Good job! Keep practicing!";
        else
            gradeMsg = "Don't worry, try again and improve!";

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Your Score: {score}/{questions.Count}"; 
        }

        if (scoreMessageText != null)
        {
            scoreMessageText.text = gradeMsg;
        }
    }

    void ClearLetters()
    {
        foreach (Transform child in letterContainer)
        {
            Destroy(child.gameObject);
        }
        letterButtons.Clear();
    }

    IEnumerator AnimateProgressBar()
    {
        float startValue = progressBar.value;
        float duration = 0.3f;
        float elapsed = 0f;

        // Scale effect variables
        Vector3 originalScale = progressHandle.localScale;
        Vector3 zoomedScale = originalScale * 1.3f; // adjust zoom level

        // Zoom in
        if (progressHandle != null)
            progressHandle.localScale = zoomedScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            progressBar.value = Mathf.Lerp(startValue, targetProgress, t);
            yield return null;
        }

        progressBar.value = targetProgress;

        // Optional delay before zoom out
        yield return new WaitForSeconds(0.05f);

        // Zoom out smoothly
        float shrinkTime = 0.2f;
        float shrinkElapsed = 0f;

        while (shrinkElapsed < shrinkTime)
        {
            shrinkElapsed += Time.deltaTime;
            float t = shrinkElapsed / shrinkTime;
            if (progressHandle != null)
                progressHandle.localScale = Vector3.Lerp(zoomedScale, originalScale, t);
            yield return null;
        }

        // Ensure final scale is exact
        if (progressHandle != null)
            progressHandle.localScale = originalScale;
    }


    IEnumerator AnimateSwap(int firstIndex, int secondIndex)
    {
        Button firstButton = letterButtons[firstIndex];
        Button secondButton = letterButtons[secondIndex];

        RectTransform firstRect = firstButton.GetComponent<RectTransform>();
        RectTransform secondRect = secondButton.GetComponent<RectTransform>();

        Vector3 firstStartPos = firstRect.localPosition;
        Vector3 secondStartPos = secondRect.localPosition;

        float duration = 0.3f;
        float elapsed = 0f;

        // Highlight
        firstButton.GetComponentInChildren<Text>().color = selectedColor;
        secondButton.GetComponentInChildren<Text>().color = selectedColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            firstRect.localPosition = Vector3.Lerp(firstStartPos, secondStartPos, t);
            secondRect.localPosition = Vector3.Lerp(secondStartPos, firstStartPos, t);

            yield return null;
        }

        // Swap the text content
        string temp = firstButton.GetComponentInChildren<Text>().text;
        firstButton.GetComponentInChildren<Text>().text = secondButton.GetComponentInChildren<Text>().text;
        secondButton.GetComponentInChildren<Text>().text = temp;

        // Return buttons to original position
        firstRect.localPosition = firstStartPos;
        secondRect.localPosition = secondStartPos;

        // Reset colors
        firstButton.GetComponentInChildren<Text>().color = defaultColor;
        secondButton.GetComponentInChildren<Text>().color = defaultColor;

        selectedIndex = -1;
    }

    IEnumerator ShowFloatingText(Text textObj, string message, Vector3 worldPos, Color color)
    {
        textObj.text = message;
        textObj.color = color;
        textObj.transform.position = worldPos;
        textObj.gameObject.SetActive(true);

        Vector3 originalPos = textObj.transform.position;
        Vector3 targetPos = originalPos + new Vector3(0, 70, 0);
        float duration = 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textObj.transform.position = Vector3.Lerp(originalPos, targetPos, elapsed / duration);
            yield return null;
        }

        textObj.gameObject.SetActive(false);
    }


    IEnumerator WaitThenNextQuestion()
    {
        yield return new WaitForSeconds(5f);
        feedbackPanel.SetActive(false);
        timerContainer.SetActive(true);
        scoreContainer.SetActive(true);
        canAnswer = true;
        NextQuestionOrEnd();
    }

    IEnumerator TypeFeedback(string result, string explanation, float typeSpeed = 0.02f)
    {
        feedbackText.text = "";
        string fullText = $"{result}\n{explanation}";

        foreach (char c in fullText)
        {
            feedbackText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    private IEnumerator DeactivateSkillAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        isSkillActive = false;

        // Fade out icon
        Image iconImage = doubleSwordIcon.GetComponent<Image>();
        if (iconImage != null)
        {
            float fadeDuration = 0.5f;
            float elapsed = 0f;
            Color originalColor = iconImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                iconImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }

            iconImage.color = originalColor; // Reset for next use
        }
        doubleSwordIcon.SetActive(false);
    }
}
