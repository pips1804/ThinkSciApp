using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MultipleChoice : MonoBehaviour
{
    [System.Serializable]
    public class MultipleChoiceQuestions
    {
        public string question;
        public string[] options = new string[4]; // Choices A, B, C, D
        public int correctIndex; // 0 to 3 (A-D)
        public string explanationText;
    }

    public List<MultipleChoiceQuestions> questions;

    public Button[] answerButtons; // A, B, C, D
    public GameObject modalPanel;
    public Text modalText;

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
    private float timer = 20f; 
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

    public float hitChancePercent = 50f; 

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

    public DatabaseManager dbManager;
    public int quizId;
    public int userId = 1;
    public int currentScore;

    private bool longPressTriggered = false;

    public GameObject passingModal;
    public GameObject failingModal;

    public Text passingHeader;
    public Text passingScore;
    public Text passingNote;

    public Text failingHeader;
    public Text failingScore;
    public Text failingNote;

    public Button retryButton;

    public int enemyDamage = 10;

    public int lessonToUnlock;
    public int categoryToUnlock;
    public int firstLesson;
    public int earnedGold;
    public int healthToAdd;
    public int damageToAdd;

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
        var q = questions[currentQuestionIndex];
        questionText.text = q.question;

        for (int i = 0; i < 4; i++)
        {
            int choiceIndex = i;
            int capturedIndex = choiceIndex;

            answerButtons[i].onClick.RemoveAllListeners();
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(capturedIndex));

            var longPress = answerButtons[i].GetComponent<LongPressHandler>();
            if (longPress != null)
            {
                longPress.onLongPress = () =>
                {
                    longPressTriggered = true;
                    ShowModal(questions[currentQuestionIndex].options[capturedIndex]);
                };

                longPress.onRelease = () =>
                {
                    HideModal();
                    StartCoroutine(ResetLongPressFlagDelayed());
                };
            }
        }

        longPressTriggered = false;
        timer = 30f;
        isTimerRunning = true;
        canAnswer = true;
        battleAnim.StartCoroutine(battleAnim.FadeTextLoop());
    }
    private IEnumerator ResetLongPressFlagDelayed()
    {
        yield return new WaitForEndOfFrame(); // Let one frame pass so click won't trigger
        longPressTriggered = false;
    }


    void HideModal()
    {
        modalPanel.SetActive(false);
    }


    void OnAnswerSelected(int selectedIndex)
    {
        if (!canAnswer || longPressTriggered) return; // BLOCK if long press happened

        isTimerRunning = false;
        canAnswer = false;

        var currentQ = questions[currentQuestionIndex];
        bool isCorrect = selectedIndex == currentQ.correctIndex;

        var (name, baseHealth, baseDamage) = dbManager.GetPetStats(userId);

        if (isCorrect)
        {
            isPlayer = true;
            bool isHit = Random.value <= (hitChancePercent * 0.01f);

            if (isHit)
            {
                ShowFeedback("Correct!", currentQ.explanationText);
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
        battleManager.PlayerTakeDamage(enemyDamage);
        isMiss = false;
        isPlayer = false;
        battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
        battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
        Color damageColor = new Color(1f, 0f, 0f); // Red
        StartCoroutine(ShowFloatingText(damageText, "-" + enemyDamage, playerIcon.position, damageColor));
        ShowFeedback("Time's Up!", "You didn't answer in time.");
        StartCoroutine(WaitThenNextQuestion());
        timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
    }


    void ShowFeedback(string resultText, string explanation)
    {
        feedbackPanel.SetActive(true);
        StartCoroutine(TypeFeedback(resultText, explanation));
        StartCoroutine(WaitThenNextQuestion());
    }
    void ShowModal(string fullText)
    {
        modalPanel.SetActive(true);
        modalText.text = fullText;
    }
    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
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
        questionText.text = "";
        timerText.text = "";

        if (score >= 7)
        {
            passingModal.SetActive(true);

            if (passingHeader != null && passingScore != null)
            {
                int earnedGold;
                string scoreMsg, goldMsg;
                GetResultMessage(score, out earnedGold, out scoreMsg, out goldMsg);

                passingHeader.text = scoreMsg;
                passingScore.text = goldMsg;

                dbManager.UnlockLessonForUser(userId, lessonToUnlock);

                if (categoryToUnlock != 0 && firstLesson != 0)
                {
                    dbManager.UnlockCategoryForUser(userId, categoryToUnlock);
                    dbManager.UnlockLessonForUser(userId, firstLesson);
                    dbManager.AddToPetStats(userId, healthToAdd, damageToAdd);
                    passingNote.text = "NOTE: Lesson completed, next lesson and new category unlocked!";
                }
                else
                {
                    passingNote.text = "NOTE: Lesson completed, next lesson unlocked!";
                }
            }
        }
        else
        {
            failingModal.SetActive(true);

            if (failingHeader != null && failingScore != null)
            {
                int earnedGold;
                string scoreMsg, goldMsg;
                GetResultMessage(score, out earnedGold, out scoreMsg, out goldMsg);

                failingHeader.text = scoreMsg;
                failingScore.text = goldMsg;
                failingNote.text = "NOTE: Can not unlock the next lesson, retake the quiz!";
            }
        }

        OnQuizCompleted();
    }

    void GetResultMessage(int score, out int goldEarned, out string scoreMsg, out string goldMsg)
    {
        if (score >= 9)
        {
            goldEarned = 100;
            scoreMsg = $"Amazing! You aced the quiz with {score} points!";
            goldMsg = $"You've earned {goldEarned} gold!";
        }
        else if (score >= 7)
        {
            goldEarned = 80;
            scoreMsg = $"Great job! You scored {score} points.";
            goldMsg = $"You've earned {goldEarned} gold!";
        }
        else if (score >= 5)
        {
            goldEarned = 60;
            scoreMsg = $"Not bad! You got {score} points.";
            goldMsg = $"You’ve earned {goldEarned} gold!";
        }
        else
        {
            goldEarned = 40;
            scoreMsg = $"Keep trying! You scored {score} points.";
            goldMsg = $"You earned {goldEarned} gold!";
        }

        earnedGold = goldEarned;
    }

    public void OnQuizCompleted()
    {
        dbManager.SaveQuizAndScore(userId, quizId, score);
        dbManager.AddCoin(userId, earnedGold);
        Debug.Log("Quiz and score saved to database.");
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

    public void RestartQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        timer = 30f;
        isTimerRunning = false;
        isSkillActive = false;
        skillTimer = 0f;

        if (skillButton != null)
        {
            skillButton.interactable = true;
        }

        // Reset progress bar and score UI
        if (progressBar != null) progressBar.value = 0;
        UpdateScoreText();

        // Hide modals and feedback
        passingModal.SetActive(false);
        failingModal.SetActive(false);
        feedbackPanel.SetActive(false);

        // Reset shadows and any UI effects
        playerShadow.SetActive(true);
        enemyShadow.SetActive(true);

        // Reset player/enemy positions
        playerIcon.anchoredPosition = playerStartPos;
        enemyIcon.anchoredPosition = enemyStartPos;

        // Reset HP and state via battle manager
        battleManager.ResetBattle();
        battleAnim.StartCoroutine(battleAnim.GraduallyRestoreColor(3));

        DisplayQuestion();
    }
}
