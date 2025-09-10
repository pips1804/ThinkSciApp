using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwipeManager : MonoBehaviour
{
    [System.Serializable]
    public class SwipeQuestion
{
    public int questionId;
    public string questionText;
    public string correctAnswer; // "Left" or "Right"
    public string explanationText;
}

    [Header("Question Display")]
    public List<SwipeQuestion> questions;
    public Image mainBackgroundImage; // The main background image (blank)
    public Text questionText; // Text component to display the question

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
    private float timer = 20f;
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

    // public Text passingHeader;
    // public Text passingScore;
    // public Text passingNote;

    // public Text failingHeader;
    // public Text failingScore;
    // public Text failingNote;

    // public Button retryButton;
    public BattleAnimationManager battleAnim;
    private bool isPlayer;

    public int enemyDamage = 10;

    // Skill 1: Double Damage
    public Button doubleDamageButton;
    public GameObject doubleSwordIcon;
    private bool isDoubleDamageActive = false;
    public float doubleDamageCooldown = 60f;
    private float doubleDamageTimer = 0f;
    public Image doubleDamageCooldownFill;

    // Skill 2: Shield
    public Button shieldButton;
    public GameObject shieldIcon;
    private bool isShieldActive = false;
    public float shieldCooldown = 45f;
    private float shieldTimer = 0f;
    public Image shieldCooldownFill;

    // Skill 3: Time Freeze
    public Button timeFreezeButton;
    public GameObject timeFreezeIcon;
    private bool isTimeFreezeActive = false;
    public float timeFreezeCooldown = 90f;
    private float timeFreezeTimer = 0f;
    private float timeFreezeDuration = 15f;
    private float timeFreezeRemaining = 0f;
    public Image timeFreezeCooldownFill;

    // Skill Tooltip System
    public GameObject skillTooltipModal;
    public Text skillTooltipTitle;
    public Text skillTooltipDescription;
    private Button currentLongPressButton = null;
    private float longPressTimer = 0f;
    private float longPressThreshold = 1.5f;
    private bool isLongPressing = false;

    public int lessonToUnlock;
    public int categoryToUnlock;
    public int earnedGold;
    public int healthToAdd;
    public int damageToAdd;

    private bool enemyDefeated = false;

    public Image enemyImage; // Drag the Image component in Inspector
    public Sprite enemySoulSprite; // Drag the soul sprite in Inspector
    private Sprite originalEnemySprite; // Backup of original image

    public Image playerImage; // Drag the Image component in Inspector
    public Sprite playerSoulSprite; // Drag the soul sprite in Inspector
    private Sprite originalPlayerSprite; // Backup of original image

    public Slider bgmSlider;
    public Slider sfxSlider;

    public AudioClip attack;
    public AudioClip hurt;
    public AudioClip passed;
    public AudioClip failed;
    public AudioClip correct;
    public AudioClip wrong;

    void Start()
    {
        enemyDefeated = false;
        playerStartPos = playerIcon.anchoredPosition;
        enemyStartPos = enemyIcon.anchoredPosition;
        originalScale = timerText.transform.localScale;

        questions = dbManager.GetRandomSwipeQuestions(quizId, 15);

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
        if (isTimerRunning && !isTimeFreezeActive)
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
                    if (battleAnim != null)
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

        // Skill cooldown timers
        UpdateSkillCooldowns();

        // Time Freeze effect
        if (isTimeFreezeActive && timeFreezeRemaining > 0)
        {
            timeFreezeRemaining -= Time.deltaTime;
            if (timeFreezeRemaining <= 0)
            {
                DeactivateTimeFreeze();
            }
        }

        // Long press detection for skill tooltips
        HandleSkillLongPress();
    }

    void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterBgmSlider(bgmSlider);
            AudioManager.Instance.RegisterSfxSlider(sfxSlider);
        }

        // Only store sprites if we haven't stored them yet
        if (originalEnemySprite == null && enemyImage != null)
            originalEnemySprite = enemyImage.sprite;
        if (originalPlayerSprite == null && playerImage != null)
            originalPlayerSprite = playerImage.sprite;

        RestartQuiz();
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
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(correct);
            bool isHit = Random.value <= (hitChancePercent * 0.01); // 80% hit chance

            if (isHit)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", questions[currentQuestionIndex].explanationText);
                int damage = isDoubleDamageActive ? baseDamage * 2 : baseDamage;
                isMiss = false;
                isPlayer = true;
                if (!enemyDefeated)
                {
                    bool enemyJustDefeated = battleManager.EnemyTakeDamage(damage);

                    if (enemyJustDefeated)
                    {
                        enemyDefeated = true;
                        Debug.Log("Enemy defeated!");

                        if (enemyImage != null && enemySoulSprite != null)
                        {
                            battleManager.StartCoroutine(battleAnim.FadeToSoul());
                        }
                    }

                    if (isDoubleDamageActive)
                    {
                        battleAnim.StartCoroutine(battleAnim.IntenseAttackAnimation(playerIcon, playerStartPos, new Vector3(300, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                        StartCoroutine(DeactivateDoubleDamageAfterDelay());
                    }
                    else
                    {
                        battleAnim.StartCoroutine(battleAnim.AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0), enemyIcon.position, true, isMiss, isPlayer));
                    }
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(attack);
                    battleAnim.StartCoroutine(battleAnim.HitShake(enemyIcon));
                    Color damageColor = new Color(1f, 0f, 0f); // Red
                    StartCoroutine(ShowFloatingText(damageText, "-" + damage, enemyIcon.position, damageColor));
                }
                else
                {
                    earnedGold += 5;
                    Debug.Log("Enemy already defeated. Skipping battle animation.");
                    StartCoroutine(ShowFloatingText(damageText, "+5 coins", enemyIcon.position, Color.yellow));
                }
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", questions[currentQuestionIndex].explanationText);
                isMiss = true;
                isPlayer = true;
                if (isDoubleDamageActive)
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
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(wrong);
            bool isHit = Random.value <= (hitChancePercent * 0.01);

            // Check if shield is active - blocks enemy attack
            if (isShieldActive)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Wrong!", questions[currentQuestionIndex].explanationText);
                isHit = false; // Shield blocks the attack
                DeactivateShield();
                Color shieldColor = new Color(0f, 0.5f, 1f); // Blue
                StartCoroutine(ShowFloatingText(missText, "Blocked!", playerIcon.position, shieldColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF);
            }
            else if (isHit)
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                isMiss = false;
                ShowFeedback("Wrong!", questions[currentQuestionIndex].explanationText);
                if (enemyDefeated)
                {
                    Debug.Log("Enemy already defeated. Skipping battle animation.");
                }
                else
                {
                    battleManager.PlayerTakeDamage(enemyDamage);
                    battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(hurt);
                    battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
                    Color damageColor = new Color(1f, 0f, 0f); // Red
                    StartCoroutine(ShowFloatingText(damageText, "-" + enemyDamage, playerIcon.position, damageColor));
                    isDoubleDamageActive = false;
                }
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
        isMiss = false;
        isPlayer = false;

        // Check if shield is active - blocks timeout damage
        if (isShieldActive)
        {
            DeactivateShield();
            Color shieldColor = new Color(0f, 0.5f, 1f); // Blue
            StartCoroutine(ShowFloatingText(missText, "Blocked!", playerIcon.position, shieldColor));
        }
        else if (enemyDefeated)
        {
            Debug.Log("Enemy already defeated. Skipping battle animation.");
        }
        else
        {
            battleManager.PlayerTakeDamage(enemyDamage);
            battleAnim.StartCoroutine(battleAnim.AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false, isMiss, isPlayer));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hurt);
            battleAnim.StartCoroutine(battleAnim.HitShake(playerIcon));
            Color damageColor = new Color(1f, 0f, 0f); // Red
            StartCoroutine(ShowFloatingText(damageText, "-" + enemyDamage, playerIcon.position, damageColor));
            isDoubleDamageActive = false;
        }
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
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(hurt);
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

        if (battleManager.playerHealth <= 0)
        {
            battleAnim.StartCoroutine(battleAnim.PlayerFadeToSoul());
            EndQuiz();
        }
    }

    void ShowFeedback(string resultText, string explanation)
    {
        feedbackPanel.SetActive(true);
        StartCoroutine(TypeFeedback(resultText, explanation));
        StartCoroutine(FadeOutText()); // Changed from FadeOutImage to FadeOutText
        StartCoroutine(WaitThenNextQuestion());
    }

    void DisplayQuestion()
    {
        StartCoroutine(FadeInText()); // Changed from FadeInImage to FadeInText
        var question = questions[currentQuestionIndex];
        questionText.text = question.questionText; // Set the question text
        timer = 20f;
        isTimerRunning = true;
        isBlinking = false; // Reset blinking state
        if (battleAnim != null)
            battleAnim.StartCoroutine(battleAnim.FadeTextLoop());
    }

    void EndQuiz()
    {
        isTimerRunning = false;
        timerText.text = "";

        if (questionText != null)
            questionText.enabled = false; // Changed from questionImage to questionText

        if (score >= 7)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(passed);
            passingModal.SetActive(true);

            // if (passingHeader != null && passingScore != null)
            // {
            // int earnedGold;
            // string scoreMsg, goldMsg;
            // GetResultMessage(score, out earnedGold, out scoreMsg, out goldMsg);

            // passingHeader.text = scoreMsg;
            // passingScore.text = goldMsg;

            bool alreadyGiven = dbManager.HasReceivedStatBonus(userId, quizId);

            if (!alreadyGiven)
            {
                dbManager.UnlockLessonForUser(userId, lessonToUnlock);
                dbManager.AddToPetStats(userId, healthToAdd, damageToAdd);
                dbManager.MarkStatBonusAsGiven(userId, quizId); // set Stats_Given = 1
            }

            if (categoryToUnlock != 0)
            {
                dbManager.UnlockCategoryForUser(userId, categoryToUnlock);
                // passingNote.text = "NOTE: Lesson completed, next lesson and new category unlocked!";
            }
            // else
            // {
            //     // passingNote.text = "NOTE: Lesson completed, next lesson unlocked!";
            // }
            // }
        }
        else if (battleManager.playerHealth <= 0 || score <= 6)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(failed);
            failingModal.SetActive(true);

            // if ((failingHeader != null && failingScore != null))
            // {
            //     int earnedGold;
            //     string scoreMsg, goldMsg;
            //     GetResultMessage(score, out earnedGold, out scoreMsg, out goldMsg);

            //     failingHeader.text = scoreMsg;
            //     failingScore.text = goldMsg;

            //     if (battleManager.playerHealth <= 0)
            //     {
            //         failingNote.text = "NOTE: You died, can not unlock the next lesson, retake the quiz!";
            //     }
            //     else
            //     {
            //         failingNote.text = "NOTE: You've got a low score, can not unlock the next lesson, retake the quiz!";
            //     }
            // }
        }

        OnQuizCompleted();
    }

    void GetResultMessage(int score, out int goldEarned)
    {
        if (score >= 9)
        {
            goldEarned = 100;
            // scoreMsg = $"Amazing! You aced the quiz with {score} points!";
            // goldMsg = $"You've earned {goldEarned} gold!";
        }
        else if (score >= 7)
        {
            goldEarned = 80;
            // scoreMsg = $"Great job! You scored {score} points.";
            // goldMsg = $"You've earned {goldEarned} gold!";
        }
        else if (score >= 5)
        {
            goldEarned = 60;
            // scoreMsg = $"Not bad! You got {score} points.";
            // goldMsg = $"You�ve earned {goldEarned} gold!";
        }
        else
        {
            goldEarned = 40;
            // scoreMsg = $"Keep trying! You scored {score} points.";
            // goldMsg = $"You earned {goldEarned} gold!";
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

    // Changed from FadeOutImage to FadeOutText
    IEnumerator FadeOutText(float duration = 0.3f)
    {
        Text txt = questionText;
        Color originalColor = txt.color;
        Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f); // Dimmed

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            txt.color = Color.Lerp(originalColor, targetColor, t);
            yield return null;
        }
        txt.color = targetColor;
    }

    // Changed from FadeInImage to FadeInText
    IEnumerator FadeInText(float duration = 0.3f)
    {
        Text txt = questionText;
        Color startColor = txt.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 1f); // Fully visible

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            txt.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        txt.color = targetColor;

        // Add pop animation to the text
        RectTransform rect = txt.rectTransform;
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

    // ========== SKILL SYSTEM ==========

    public void ActivateDoubleDamage()
    {
        if (doubleDamageTimer > 0) return;

        isDoubleDamageActive = true;
        doubleSwordIcon.SetActive(true);
        Image iconImage = doubleSwordIcon.GetComponent<Image>();
        if (iconImage != null)
        {
            Color color = iconImage.color;
            iconImage.color = new Color(color.r, color.g, color.b, 1f); // full opacity
        }

        doubleDamageTimer = doubleDamageCooldown;
        doubleDamageButton.interactable = false;
    }

    private IEnumerator DeactivateDoubleDamageAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        isDoubleDamageActive = false;

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

    public void ActivateShield()
    {
        if (shieldTimer > 0) return;

        isShieldActive = true;
        shieldIcon.SetActive(true);
        Image iconImage = shieldIcon.GetComponent<Image>();
        if (iconImage != null)
        {
            Color color = iconImage.color;
            iconImage.color = new Color(color.r, color.g, color.b, 1f); // full opacity
        }

        shieldTimer = shieldCooldown;
        shieldButton.interactable = false;
    }

    private void DeactivateShield()
    {
        isShieldActive = false;
        StartCoroutine(FadeOutShieldIcon());
    }

    private IEnumerator FadeOutShieldIcon()
    {
        // Fade out icon
        Image iconImage = shieldIcon.GetComponent<Image>();
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
        shieldIcon.SetActive(false);
    }

    public void ActivateTimeFreeze()
    {
        if (timeFreezeTimer > 0) return;

        isTimeFreezeActive = true;
        timeFreezeRemaining = timeFreezeDuration;
        timeFreezeIcon.SetActive(true);

        // Change timer text color to indicate freeze
        timerText.color = new Color(0.5f, 0.8f, 1f); // Light blue

        Image iconImage = timeFreezeIcon.GetComponent<Image>();
        if (iconImage != null)
        {
            Color color = iconImage.color;
            iconImage.color = new Color(color.r, color.g, color.b, 1f); // full opacity
        }

        timeFreezeTimer = timeFreezeCooldown;
        timeFreezeButton.interactable = false;
    }

    private void DeactivateTimeFreeze()
    {
        isTimeFreezeActive = false;
        timeFreezeRemaining = 0f;

        // Restore timer text color
        timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF);

        StartCoroutine(FadeOutTimeFreezeIcon());
    }

    private IEnumerator FadeOutTimeFreezeIcon()
    {
        // Fade out icon
        Image iconImage = timeFreezeIcon.GetComponent<Image>();
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
        timeFreezeIcon.SetActive(false);
    }

    private void UpdateSkillCooldowns()
    {
        // Double Damage cooldown
        if (doubleDamageTimer > 0)
        {
            doubleDamageTimer -= Time.deltaTime;
            if (doubleDamageTimer <= 0)
            {
                doubleDamageButton.interactable = true;
            }
        }

        // Shield cooldown
        if (shieldTimer > 0)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0)
            {
                shieldButton.interactable = true;
            }
        }

        // Time Freeze cooldown
        if (timeFreezeTimer > 0)
        {
            timeFreezeTimer -= Time.deltaTime;
            if (timeFreezeTimer <= 0)
            {
                timeFreezeButton.interactable = true;
            }
        }

        // Update cooldown visuals
        if (doubleDamageCooldownFill != null)
        {
            doubleDamageCooldownFill.fillAmount = doubleDamageTimer > 0 ? doubleDamageTimer / doubleDamageCooldown : 0;
        }

        if (shieldCooldownFill != null)
        {
            shieldCooldownFill.fillAmount = shieldTimer > 0 ? shieldTimer / shieldCooldown : 0;
        }

        if (timeFreezeCooldownFill != null)
        {
            timeFreezeCooldownFill.fillAmount = timeFreezeTimer > 0 ? timeFreezeTimer / timeFreezeCooldown : 0;
        }
    }

    // ========== SKILL TOOLTIP MODAL SYSTEM ==========

    private void HandleSkillLongPress()
    {
        bool anyButtonPressed = false;
        Button pressedButton = null;

        // Check if any skill button is being pressed
        if (Input.GetMouseButton(0)) // Left mouse button held
        {
            Vector2 mousePos = Input.mousePosition;

            // Check each skill button
            if (IsButtonPressed(doubleDamageButton, mousePos))
            {
                anyButtonPressed = true;
                pressedButton = doubleDamageButton;
            }
            else if (IsButtonPressed(shieldButton, mousePos))
            {
                anyButtonPressed = true;
                pressedButton = shieldButton;
            }
            else if (IsButtonPressed(timeFreezeButton, mousePos))
            {
                anyButtonPressed = true;
                pressedButton = timeFreezeButton;
            }
        }

        if (anyButtonPressed && pressedButton == currentLongPressButton)
        {
            if (!isLongPressing)
            {
                longPressTimer += Time.deltaTime;
                if (longPressTimer >= longPressThreshold)
                {
                    isLongPressing = true;
                    ShowSkillTooltip(pressedButton);
                }
            }
        }
        else
        {
            if (isLongPressing)
            {
                HideSkillTooltip();
            }
            longPressTimer = 0f;
            currentLongPressButton = pressedButton;
            isLongPressing = false;
        }
    }

    private bool IsButtonPressed(Button button, Vector2 mousePos)
    {
        if (button == null || !button.gameObject.activeInHierarchy) return false;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos, null);
    }

    private void ShowSkillTooltip(Button skillButton)
    {
        if (skillTooltipModal == null) return;

        string title = "";
        string description = "";

        if (skillButton == doubleDamageButton)
        {
            title = "Double Damage";
            description = "Your next attack deals 2x damage with guaranteed hit!\n\nCooldown: 60 seconds\nDuration: Until next attack";
        }
        else if (skillButton == shieldButton)
        {
            title = "Shield";
            description = "Blocks the next enemy attack completely!\n\nCooldown: 45 seconds\nDuration: Until next enemy attack";
        }
        else if (skillButton == timeFreezeButton)
        {
            title = "Time Freeze";
            description = "Pauses the timer for 15 seconds, giving you more time to think!\n\nCooldown: 90 seconds\nDuration: 15 seconds";
        }

        skillTooltipTitle.text = title;
        skillTooltipDescription.text = description;
        skillTooltipModal.SetActive(true);
    }

    private void HideSkillTooltip()
    {
        if (skillTooltipModal != null)
        {
            skillTooltipModal.SetActive(false);
        }
    }

    public void CloseSkillTooltip()
    {
        HideSkillTooltip();
        isLongPressing = false;
        longPressTimer = 0f;
        currentLongPressButton = null;
    }

    // ========== RESTART QUIZ ==========

    public void RestartQuiz()
    {
        questions = dbManager.GetRandomSwipeQuestions(quizId, 15);
        currentQuestionIndex = 0;
        score = 0;
        isTimerRunning = false;
        isDoubleDamageActive = false;
        isShieldActive = false;
        isTimeFreezeActive = false;
        doubleDamageTimer = 0f;
        shieldTimer = 0f;
        timeFreezeTimer = 0f;
        timeFreezeRemaining = 0f;

        if (doubleDamageButton != null) doubleDamageButton.interactable = true;
        if (shieldButton != null) shieldButton.interactable = true;
        if (timeFreezeButton != null) timeFreezeButton.interactable = true;

        // Reset progress bar and score UI
        if (progressBar != null) progressBar.value = 0;
        UpdateScoreText();

        // Hide modals and feedback
        passingModal.SetActive(false);
        failingModal.SetActive(false);
        feedbackPanel.SetActive(false);

        // Hide skill tooltip if open
        if (skillTooltipModal != null)
            skillTooltipModal.SetActive(false);

        // Reset shadows and any UI effects
        playerShadow.SetActive(true);
        enemyShadow.SetActive(true);

        // Reset player/enemy positions
        playerStartPos = playerIcon.anchoredPosition;
        enemyStartPos = enemyIcon.anchoredPosition;

        enemyImage.sprite = originalEnemySprite;
        enemyDefeated = false; // Reset flag!

        playerImage.sprite = originalPlayerSprite;

        // Reset skill icons
        if (doubleSwordIcon != null) doubleSwordIcon.SetActive(false);
        if (shieldIcon != null) shieldIcon.SetActive(false);
        if (timeFreezeIcon != null) timeFreezeIcon.SetActive(false);

        // Reset HP and state via battle manager
        battleManager.ResetBattle();
        battleAnim.StartCoroutine(battleAnim.GraduallyRestoreColor(3));

        questions = dbManager.GetRandomSwipeQuestions(quizId, 15);

        if (questionText != null)
            questionText.enabled = true;
        
        DisplayQuestion();
    }
}
