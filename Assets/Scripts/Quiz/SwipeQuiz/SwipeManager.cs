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
    public Text finalScoreText;
    public Text scoreMessageText;
    public float hitChancePercent = 50f; // you can adjust in Inspector
    public Text missText;
    public Text damageText;
    public GameObject feedbackPanel;
    public Text feedbackText;
    private bool canAnswer = true;
    public GameObject impactImage;
    private bool isMiss = false;
    public Text timerText;
    public GameObject resultPanel;
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

    public IdleAnimation idleAnim;

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
                    StartCoroutine(HeartbeatEffect());
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
                int damage = Random.Range(10, 16); // 10 to 15
                battleManager.EnemyTakeDamage(damage);
                isMiss = false;
                StartCoroutine(AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0), enemyIcon.position, false));
                StartCoroutine(HitShake(enemyIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, enemyIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Correct!", questions[currentQuestionIndex].explanationText);
                isMiss = true;
                StartCoroutine(AttackAnimation(playerIcon, playerStartPos, new Vector3(250, 0, 0), enemyIcon.position, true));
                StartCoroutine(DodgeAnimation(enemyIcon));
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
                int damage = Random.Range(10, 16);
                battleManager.PlayerTakeDamage(damage);
                isMiss = false;
                StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false));
                StartCoroutine(HitShake(playerIcon));
                Color damageColor = new Color(1f, 0f, 0f); // Red
                StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
                timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha

            }
            else
            {
                timerContainer.SetActive(false);
                scoreContainer.SetActive(false);
                ShowFeedback("Wrong!", questions[currentQuestionIndex].explanationText);
                isMiss = true;
                StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, true));
                StartCoroutine(DodgeAnimation(playerIcon));
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

    IEnumerator DodgeAnimation(RectTransform defender)
    {
        Vector3 originalPos = defender.anchoredPosition;
        Vector3 dodgeOffset = new Vector3(80f, 0f, 0f); // More distance
        float dodgeTime = 0.35f; // Slower movement

        float elapsed = 0f;

        idleAnim.StopIdle();

        // Move sideways with a bounce effect (ease in/out)
        while (elapsed < dodgeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / dodgeTime) * Mathf.PI); // Ease in/out
            defender.anchoredPosition = originalPos + dodgeOffset * t;
            yield return null;
        }

        // Optional: Small delay at the end for clarity
        yield return new WaitForSeconds(0.05f);

        defender.anchoredPosition = originalPos;

        idleAnim.StartIdle();
    }

    IEnumerator AttackAnimation(RectTransform attacker, Vector3 originalPos, Vector3 attackOffset, Vector3 worldPos, bool isEnemy)
    {
        Vector3 targetPos = originalPos + attackOffset * 1.5f;
        float duration = 0.35f; // slightly slower now
        float elapsed = 0f;

        // Small tilt angle
        float tiltAngle = 25f;
        Quaternion startRotation = attacker.rotation;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, attacker == playerIcon ? -tiltAngle : tiltAngle);

        Vector3 originalScale = attacker.localScale;
        Vector3 enlargedScale = originalScale * 1.2f; // slightly bigger during impact

        idleAnim.StopIdle();

        if (attacker == playerIcon)
        {
            playerShadow.SetActive(false);
        }
        else
        {
            enemyShadow.SetActive(false);
        }

        if (!isMiss)
        {
            StartCoroutine(ShowImpactImage(worldPos, isEnemy));
        }

        // --- Move forward with tilt and grow ---
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            attacker.anchoredPosition = Vector3.Lerp(originalPos, targetPos, t);
            attacker.rotation = Quaternion.Slerp(startRotation, tiltRotation, t);
            attacker.localScale = Vector3.Lerp(originalScale, enlargedScale, t);

            yield return null;
        }

        elapsed = 0f;

        // --- Move back with reset ---
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            attacker.anchoredPosition = Vector3.Lerp(targetPos, originalPos, t);
            attacker.rotation = Quaternion.Slerp(tiltRotation, startRotation, t);
            attacker.localScale = Vector3.Lerp(enlargedScale, originalScale, t);

            yield return null;
        }

        attacker.rotation = startRotation;
        attacker.localScale = originalScale;

        if (attacker == playerIcon)
        {
            playerShadow.SetActive(true);
        }
        else
        {
            enemyShadow.SetActive(true);
        }

        idleAnim.StartIdle();
    }

    IEnumerator HitShake(RectTransform target, float duration = 0.2f, float magnitude = 10f)
    {
        Vector3 originalPos = target.anchoredPosition;
        float elapsed = 0f;
        idleAnim.StopIdle();


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            target.anchoredPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        target.anchoredPosition = originalPos;

        idleAnim.StartIdle();
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
        StartCoroutine(AttackAnimation(enemyIcon, enemyStartPos, new Vector3(-250, 0, 0), playerIcon.position, false));
        StartCoroutine(HitShake(playerIcon));
        Color damageColor = new Color(1f, 0f, 0f); // Red
        StartCoroutine(ShowFloatingText(damageText, "-" + damage, playerIcon.position, damageColor));
        timerText.color = new Color32(0xE8, 0xE8, 0xCC, 0xFF); // RGB + full alpha
    }

    void MoveToNextQuestion()
    {
        if (currentQuestionIndex >= questions.Count - 3)
        {
            StartCoroutine(GraduallyTurnRed(3f)); // 3 seconds transition
            battleManager.PlayerTakeDamage(suddenDeathDamage);
            battleManager.EnemyTakeDamage(suddenDeathDamage);

            // Optional: Show visual effects for damage taken
            Color suddenColor = new Color(1f, 0.5f, 0f); // Orange
            StartCoroutine(ShowFloatingText(playerSuddenText, "-" + suddenDeathDamage, playerIcon.position, suddenColor));
            StartCoroutine(ShowFloatingText(enemySuddenText, "-" + suddenDeathDamage, enemyIcon.position, suddenColor));

            StartCoroutine(HitShake(playerIcon));
            StartCoroutine(HitShake(enemyIcon));
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
        StartCoroutine(FadeTextLoop());
    }

    string GetScoreMessage(int score)
    {
        if (score >= 8)
            return "Excellent! You're a quiz master!";
        else if (score >= 5)
            return "Good job! Keep practicing!";
        else
            return "Don't worry, try again and improve!";
    }

    void EndQuiz()
    {
        isTimerRunning = false;
        timerText.text = "0";

        if (questionImage != null)
            questionImage.enabled = false;

        resultPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = "Your Score: " + score.ToString() + "/10";

        if (scoreMessageText != null)
            scoreMessageText.text = GetScoreMessage(score);
    }


    IEnumerator ShowImpactImage(Vector3 worldPos, bool isEnemy)
    {

        Vector3 offset = isEnemy ? new Vector3(60f, 0f, 0f) : new Vector3(-60f, 0f, 0f); // adjust 30f as needed

        // Animate pop
        impactImage.SetActive(true);
        impactImage.transform.position = worldPos + offset;
        impactImage.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            impactImage.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 7f;
            impactImage.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        impactImage.SetActive(false);
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

    IEnumerator GraduallyTurnRed(float duration)
    {
        Color startColor = battleBackground.color;
        Color targetColor = new Color(3f, .5f, .5f); // Dark red (adjust as needed)

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            battleBackground.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            yield return null;
        }

        battleBackground.color = targetColor; // Ensure exact final color
    }

    IEnumerator HeartbeatEffect()
    {
        while (isTimerRunning && Mathf.CeilToInt(timer) <= 10)
        {
            yield return ScaleTo(originalScale * 1.2f, 0.2f);
            yield return ScaleTo(originalScale, 0.2f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = timerText.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            timerText.transform.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        timerText.transform.localScale = targetScale;
    }

    IEnumerator FadeTextLoop()
    {
        float duration = 0.5f; // Half a second for fade out, half for fade in

        while (true)
        {
            // Fade out
            yield return StartCoroutine(FadeToAlpha(0f, duration));

            // Fade in
            yield return StartCoroutine(FadeToAlpha(1f, duration));
        }
    }

    IEnumerator FadeToAlpha(float targetAlpha, float duration)
    {
        float startAlpha = timerText.color.a;
        float time = 0f;

        while (time < duration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            Color c = timerText.color;
            c.a = newAlpha;
            timerText.color = c;

            time += Time.deltaTime;
            yield return null;
        }

        // Ensure final alpha is set
        Color finalColor = timerText.color;
        finalColor.a = targetAlpha;
        timerText.color = finalColor;
    }
}
