using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwipeManager : MonoBehaviour
{
    [System.Serializable]
    public class SwipeQuestion
    {
        public Sprite questionImage;
        public string correctAnswer; // "Left" or "Right"
        public string explanationText;       // Short feedback text
    }


    [Header("Question Image and Answers")]
    public List<SwipeQuestion> questions;
    public Image questionImage;

    [Header("Player and Enemy")]
    public RectTransform playerIcon;
    public RectTransform enemyIcon;
    private Vector3 playerStartPos;
    private Vector3 enemyStartPos;
    public BattleManager battleManager;
    public int suddenDeathDamage = 10;

    [Header("UI")]
    public Text scoreText; // Drag your UI text here in the inspector
    private int score = 0;
    public float hitChancePercent = 50f; // you can adjust in Inspector
    public Text missText;
    public Text damageText;
    public GameObject feedbackPanel;
    public Text feedbackText;
    private bool canAnswer = true;
    public GameObject impactImage;
    private bool isMiss = false;
    public Text timerText;
    private int currentQuestionIndex = 0;
    private float timer = 10f;
    private bool isTimerRunning = false;
    public Slider progressBar;
    private float targetProgress = 0f;
    public RectTransform progressHandle;
    public GameObject playerShadow;
    public GameObject enemyShadow;
    public Image battleBackground;

    public GameObject timerContainer;
    public GameObject scoreContainer;
    public Text enemySuddenText;
    public Text playerSuddenText;

    private bool isBlinking = false;
    private Vector3 originalScale;

    public DatabaseManager dbManager;
    public int quizId;
    public int userId = 1;
    public int currentScore;

    public GameObject passingModal;
    public GameObject failingModal;

    public Text passingHeader;
    public Text passingScore;
    public Text passingNote;

    public Text failingHeader;
    public Text failingScore;
    public Text failingNote;

    public Button retryButton;
    public BattleAnimationManager battleAnim;
    private bool isPlayer;

    public int enemyDamage = 10;

    public Button skillButton;
    public GameObject doubleSwordIcon;
    private bool isSkillActive = false;
    private float skillCooldown = 30f;
    private float skillTimer = 0f;

    public Image skillCooldownFill;

    public int lessonToUnlock;
    public int categoryToUnlock;
    public int firstLesson;
    public int earnedGold;
    public int healthToAdd;
    public int damageToAdd;


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
            if (displayTime <= 5)
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
    }

    public void HandleAnswer(string swipeDirection)
    {
        var (name, baseHealth, baseDamage) = dbManager.GetPetStats(userId);
        isTimerRunning = false;
        var question = questions[currentQuestionIndex];
        bool isCorrect = swipeDirection == question.correctAnswer;

        if (!canAnswer) return; // Don't allow answering if disabled
        canAnswer = false;

        if (isCorrect)
        {
            bool isHit = Random.value <= (hitChancePercent * 0.01); // 80% hit chance

            if (isHit)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", questions[currentQuestionIndex].explanationText);
                battleManager.EnemyTakeDamage(baseDamage);
                isMiss = false;
                isPlayer = true;
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
                StartCoroutine(ShowFloatingText(damageText, "-" + baseDamage, enemyIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", questions[currentQuestionIndex].explanationText);
                isMiss = true;
                isPlayer = true;
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
        }
        else
        {
            bool isHit = Random.value <= (hitChancePercent * 0.01);

            if (isHit)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Wrong!", questions[currentQuestionIndex].explanationText);
                battleManager.PlayerTakeDamage(enemyDamage);
                isMiss = false;
                isPlayer = false;
                battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
                battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + enemyDamage, playerIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha

            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Wrong!", questions[currentQuestionIndex].explanationText);
                isMiss = true;
                isPlayer = false;
                battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
                battleAnim.StartCoroutine(battleAnim.DodgeAnimation(playerIcon));
                Color missColor;
                ColorUtility.TryParseHtmlString("#5f9103", out missColor);
                StartCoroutine(ShowFloatingText(missText, "Miss!", playerIcon.position, missColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha

            }
        }

        if (isCorrect)
            score++;
        UpdateScoreText();
    }

    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    IEnumerator ShowFloatingText(Text textElement, string content, Vector3 startPos, Color color)
    {
        textElement.text = content;
        textElement.color = new Color(color.r, color.g, color.b, 0f);
        textElement.transform.position = startPos;
        textElement.gameObject.SetActive(true);

        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 endPos = startPos + new Vector3(0, 50f, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.SmoothStep(0f, 1f, t < 0.5f ? t * 2f : (1f - t) * 2f); // Fade in/out
            textElement.color = new Color(color.r, color.g, color.b, alpha);
            textElement.transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        textElement.gameObject.SetActive(false);
    }

    void PlayerMissedAnswer()
    {
        timerContainer.SetActive(false);
        scoreContainer.SetActive(false);
        ShowFeedback("Time's Up!", "You didn't answer in time.");
        int damage = Random.Range(10, 16);
        battleManager.PlayerTakeDamage(damage);
        isMiss = false;
        isPlayer = false;
        battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
        battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
        Color damageColor = new Color(1f, 0f, 0f); // Red
        StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
        timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
    }

    void MoveToNextQuestion()
    {
        if (currentQuestionIndex >= questions.Count - 3)
        {
            battleAnim.StartCoroutine(battleAnim.GraduallyTurnRed(3f)); // 3 seconds transition
            battleManager.SuddenDeathDamage(suddenDeathDamage);
            battleManager.SuddenDeathDamage(suddenDeathDamage);

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
            EndQuiz();
        }
    }

    void ShowFeedback(string resultText, string explanation)
    {
        feedbackPanel.SetActive(true);
        StartCoroutine(TypeFeedback(resultText, explanation));
        StartCoroutine(FadeOutImage());
        StartCoroutine(WaitThenNextQuestion());
    }


    void DisplayQuestion()
    {
        StartCoroutine(FadeInImage());
        var question = questions[currentQuestionIndex];
        questionImage.sprite = question.questionImage;
        timer = 10f;
        isTimerRunning = true;
        battleAnim.StartCoroutine(battleAnim.FadeTextLoop());
    }
    void EndQuiz()
    {
        isTimerRunning = false;
        timerText.text = "0";

        if (questionImage != null)
            questionImage.enabled = false;

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

    IEnumerator WaitThenNextQuestion()
    {
        yield return new WaitForSeconds(5f);
        feedbackPanel.SetActive(false);
        timerContainer.SetActive(true);
        scoreContainer.SetActive(true);
        canAnswer = true;
        MoveToNextQuestion();
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

    IEnumerator FadeOutImage(float duration = 0.3f)
    {
        Image img = questionImage;
        Color originalColor = img.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // Dimmed

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            img.color = Color.Lerp(originalColor, targetColor, t);
            yield return null;
        }
        img.color = targetColor;
    }

    IEnumerator FadeInImage(float duration = 0.3f)
    {
        Image img = questionImage;
        Color startColor = img.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f); // Fully visible

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            img.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        img.color = targetColor;

        // Add pop animation
        RectTransform rect = img.rectTransform;
        Vector3 originalScale = Vector3.one;
        Vector3 popScale = originalScale * 1.2f;

        float popDuration = 0.15f;
        elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            rect.localScale = Vector3.Lerp(originalScale, popScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;
            rect.localScale = Vector3.Lerp(popScale, originalScale, t);
            yield return null;
        }

        rect.localScale = originalScale;
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
