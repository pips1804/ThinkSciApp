using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SortingGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Image earthImage;             // The UI Image for Earth health
    public Sprite[] earthStates;         // 5 sprites from healthy to polluted
    public Text scoreText;               // UI text for score display
    public Text iconNameText;            // UI text to show current icon name
    public Slider progressSlider; // UI Slider to track progress

    [Header("Game Settings")]
    public RectTransform spawnArea;      // Parent canvas/area for spawning
    public GameObject[] energyIcons;     // Prefabs for energy sources
    public string[] iconNames;           // Names for each icon (match index with energyIcons)
    public bool[] isRenewableIcon;       // True if icon at index is renewable, false if fossil

    [Header("Bin Settings")]
    public Image binImage;                 // UI Image for the bin
    public Sprite renewableBinSprite;      // Sprite when in Renewable mode
    public Sprite fossilBinSprite;         // Sprite when in Fossil mode
    public float catchRange = 100f;        // Distance allowed for catching
    public float binMoveSpeed = 600f;           // Single bin (player controlled)
    public float fallDuration = 3f;       // Time for icon to fall from top to bottom
    public int maxIcons = 10;            // Limit number of icons

    [Header("Double Tap Settings")]
    public float doubleTapTimeWindow = 0.5f;  // Time window for double tap detection

    [Header("Game State")]
    private int currentEarthState = 0;
    private int score = 0;
    private GameObject currentIcon;
    private int iconsSpawned = 0;
    private int consecutiveCorrect = 0;
    private bool binIsRenewable = true;  // Current bin mode
    private Coroutine fallingCoroutine;  // Track the falling animation

    [Header("Dialogue System")]
    public Dialogues dialogues;
    private bool gameStarted = false;

    [Header("Panel")]
    public GameObject Earth;
    public GameObject SpawnArea;
    public GameObject MainBin;
    public GameObject Score;
    public GameObject IconName;
    public GameObject Header;
    public GameObject Settings;
    public GameObject QuizProgress;

    // Double tap detection variables
    private float lastTapTime = 0f;
    private int tapCount = 0;
    private bool isDragging = false;
    private Vector2 lastTouchPosition;

    void Start()
    {
        Earth.SetActive(false);
        SpawnArea.SetActive(false);
        MainBin.SetActive(false);
        Score.SetActive(false); 
        IconName.SetActive(false);
        Header.SetActive(false);
        Settings.SetActive(false);
        QuizProgress.SetActive(false);

        // Start the dialogue before the game
        if (dialogues != null)
        {
            dialogues.StartDialogue(0);
            StartCoroutine(WaitForDialogueThenStartGame());
        }
        else
        {
            // If no dialogue assigned, start game immediately
            BeginGame();
        }
    }

    IEnumerator WaitForDialogueThenStartGame()
    {
        // Wait until the player finishes the intro dialogue
        yield return new WaitUntil(() => dialogues.dialogueFinished);
        BeginGame();
    }

    void BeginGame()
    {
        Earth.SetActive(true);
        SpawnArea.SetActive(true);
        MainBin.SetActive(true);
        Score.SetActive(true); 
        IconName.SetActive(true);
        Header.SetActive(true);
        Settings.SetActive(true);
        QuizProgress.SetActive(true);
        gameStarted = true;
        UpdateScore();
        UpdateBinModeUI();

        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = maxIcons;
            progressSlider.value = 0;
        }

        SpawnNewIcon();
    }


    void Update()
    {
        HandleBinMovement();
        HandleDoubleTap();
    }

    void HandleBinMovement()
    {
        float move = 0;

        // Keyboard input (for testing on PC)
        move = Input.GetAxis("Horizontal"); // -1 = left, +1 = right

        // Touch input (for mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPos = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                isDragging = false;
                lastTouchPosition = touchPos;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                // Check if touch moved enough to be considered a drag
                float dragDistance = Vector2.Distance(touchPos, lastTouchPosition);
                if (dragDistance > 10f) // 10 pixel threshold for drag detection
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    // Convert screen touch to local UI position
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        spawnArea, touchPos, null, out localPos);

                    // Move bin directly towards touch X
                    Vector2 currentBinPos = binImage.rectTransform.anchoredPosition;
                    binImage.rectTransform.anchoredPosition = new Vector2(localPos.x, currentBinPos.y);
                }
            }
            return;
        }

        // Apply movement (keyboard / joystick)
        if (move != 0)
        {
            Vector2 pos = binImage.rectTransform.anchoredPosition;
            pos.x += move * binMoveSpeed * Time.deltaTime;

            // Clamp inside spawn area
            float halfWidth = spawnArea.rect.width / 2f;
            float binHalf = binImage.rectTransform.rect.width / 2f;
            pos.x = Mathf.Clamp(pos.x, -halfWidth + binHalf, halfWidth - binHalf);

            binImage.rectTransform.anchoredPosition = pos;
        }
    }

    void HandleDoubleTap()
    {
        // Handle mouse clicks (for testing on PC)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (IsTouchingBin(mousePos))
            {
                RegisterTap();
            }
        }

        // Handle touch input (for mobile) - only register taps, not drags
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended && !isDragging)
            {
                if (IsTouchingBin(touch.position))
                {
                    RegisterTap();
                }
            }
        }

        // Reset tap count after time window
        if (Time.time - lastTapTime > doubleTapTimeWindow)
        {
            tapCount = 0;
        }
    }

    bool IsTouchingBin(Vector2 screenPosition)
    {
        // Convert screen position to local position relative to the bin
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            binImage.rectTransform, screenPosition, null, out localPos);

        // Check if the touch is within the bin's bounds
        Rect binRect = binImage.rectTransform.rect;
        return binRect.Contains(localPos);
    }

    void RegisterTap()
    {
        float currentTime = Time.time;

        if (currentTime - lastTapTime <= doubleTapTimeWindow)
        {
            tapCount++;
            if (tapCount >= 2)
            {
                // Double tap detected!
                SwitchBinMode();
                tapCount = 0; // Reset
            }
        }
        else
        {
            tapCount = 1; // First tap
        }

        lastTapTime = currentTime;
    }

    void SwitchBinMode()
    {
        binIsRenewable = !binIsRenewable;
        UpdateBinModeUI();
    }

    void UpdateBinModeUI()
    {
        // Store current position and scale before changing sprite
        Vector2 currentPos = binImage.rectTransform.anchoredPosition;
        Vector3 currentScale = binImage.rectTransform.localScale;
        Quaternion currentRotation = binImage.rectTransform.rotation;

        // Change the sprite
        binImage.sprite = binIsRenewable ? renewableBinSprite : fossilBinSprite;

        // Explicitly restore all transform properties to prevent any shifts
        binImage.rectTransform.anchoredPosition = currentPos;
        binImage.rectTransform.localScale = currentScale;
        binImage.rectTransform.rotation = currentRotation;

        // Force layout rebuild to ensure position stays exactly where it was
        LayoutRebuilder.ForceRebuildLayoutImmediate(binImage.rectTransform);

        // Additional safety measure - set position again after layout rebuild
        binImage.rectTransform.anchoredPosition = currentPos;
    }

    void SpawnNewIcon()
    {
        if (iconsSpawned >= maxIcons)
        {
            GameOver();
            return;
        }

        int randIndex = Random.Range(0, energyIcons.Length);
        currentIcon = Instantiate(energyIcons[randIndex], spawnArea);

        // Random X spawn at top of screen
        float randomX = Random.Range(-spawnArea.rect.width / 2, spawnArea.rect.width / 2);
        RectTransform iconRect = currentIcon.GetComponent<RectTransform>();
        iconRect.anchoredPosition = new Vector2(randomX, spawnArea.rect.height / 2);

        // Show icon name
        iconNameText.text = iconNames[randIndex];

        iconsSpawned++;

        if (progressSlider != null)
            progressSlider.value = iconsSpawned;


        // Start the falling animation
        fallingCoroutine = StartCoroutine(AnimateIconFalling(iconRect, randIndex));
    }

    IEnumerator AnimateIconFalling(RectTransform iconRect, int iconIndex)
    {
        float startY = spawnArea.rect.height / 2;
        float endY = -spawnArea.rect.height / 2; // Bottom of screen
        float binY = binImage.rectTransform.anchoredPosition.y;

        float elapsedTime = 0f;
        Vector2 startPos = iconRect.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, endY);

        bool hasCheckedBin = false;

        while (elapsedTime < fallDuration)
        {
            // Calculate current position using smooth animation
            float t = elapsedTime / fallDuration;
            // Use ease-in curve for more realistic falling
            float easedT = t * t;

            Vector2 currentPos = Vector2.Lerp(startPos, endPos, easedT);
            iconRect.anchoredPosition = currentPos;

            // Check if icon has reached bin level and hasn't been checked yet
            if (!hasCheckedBin && currentPos.y <= binY + 50f)
            {
                hasCheckedBin = true;
                CheckIconPlacement(iconIndex);
                // Don't break here - let the icon continue falling for visual effect
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // If somehow we missed the bin check, do it now
        if (!hasCheckedBin)
        {
            CheckIconPlacement(iconIndex);
        }

        // Clean up and spawn next icon
        if (currentIcon != null)
        {
            Destroy(currentIcon);
            currentIcon = null;
        }

        SpawnNewIcon();
    }

    void CheckIconPlacement(int iconIndex)
    {
        if (currentIcon == null) return;

        RectTransform iconRect = currentIcon.GetComponent<RectTransform>();
        Vector2 iconPos = iconRect.anchoredPosition;
        Vector2 binPos = binImage.rectTransform.anchoredPosition;
        float distanceToBin = Vector2.Distance(iconPos, binPos);

        // Check if the icon is renewable based on the array
        bool isRenewable = isRenewableIcon[iconIndex];

        Debug.Log($"Icon {iconNames[iconIndex]} position: {iconPos}");
        Debug.Log($"Bin position: {binPos}");
        Debug.Log($"Distance: {distanceToBin}, CatchRange: {catchRange}");
        Debug.Log($"Icon renewable: {isRenewable}, Bin renewable: {binIsRenewable}");

        // Use a more generous catch range - check both distance and X overlap
        float horizontalDistance = Mathf.Abs(iconPos.x - binPos.x);
        float binWidth = binImage.rectTransform.rect.width;
        bool isInHorizontalRange = horizontalDistance < (binWidth / 2 + 50f); // Bin width + 50 pixels extra

        Debug.Log($"Horizontal distance: {horizontalDistance}, Bin width: {binWidth}, In range: {isInHorizontalRange}");

        if (isInHorizontalRange)
        {
            // Correct if: renewable icon in renewable bin OR fossil icon in fossil bin
            if ((binIsRenewable && isRenewable) || (!binIsRenewable && !isRenewable))
            {
                Debug.Log("CORRECT ANSWER!");
                CorrectAnswer();
                StartCoroutine(ShowCorrectFeedback());
            }
            else
            {
                Debug.Log("WRONG TYPE!");
                WrongAnswer();
                StartCoroutine(ShowWrongFeedback());
            }
        }
        else
        {
            Debug.Log("MISSED BIN!");
            WrongAnswer();
            StartCoroutine(ShowMissedFeedback());
        }
    }

    IEnumerator ShowCorrectFeedback()
    {
        // Flash bin green
        Color originalColor = binImage.color;
        binImage.color = Color.green;
        yield return new WaitForSeconds(0.2f);
        binImage.color = originalColor;
    }

    IEnumerator ShowWrongFeedback()
    {
        // Flash bin red
        Color originalColor = binImage.color;
        binImage.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        binImage.color = originalColor;
    }

    IEnumerator ShowMissedFeedback()
    {
        // Flash bin yellow (missed)
        Color originalColor = binImage.color;
        binImage.color = Color.yellow;
        yield return new WaitForSeconds(0.2f);
        binImage.color = originalColor;
    }

    void CorrectAnswer()
    {
        score += 10;
        consecutiveCorrect++;

        if (consecutiveCorrect >= 2 && currentEarthState > 0)
        {
            currentEarthState--;
            StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], false));
            consecutiveCorrect = 0;
        }

        UpdateScore();
    }

    void WrongAnswer()
    {
        consecutiveCorrect = 0;
        currentEarthState++;

        if (currentEarthState < earthStates.Length)
            StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], true));
        else
            GameOver();
    }

    IEnumerator AnimateEarthChange(Sprite newSprite, bool isWrong)
    {
        if (isWrong)
        {
            float shakeDuration = 0.3f;
            float shakeMagnitude = 10f;
            Vector3 originalPos = earthImage.rectTransform.anchoredPosition;
            Image img = earthImage;
            Sprite oldSprite = img.sprite;

            GameObject overlayObj = new GameObject("EarthOverlay");
            overlayObj.transform.SetParent(img.transform.parent, false);
            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.sprite = oldSprite;
            overlayImg.rectTransform.sizeDelta = img.rectTransform.sizeDelta;
            overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
            overlayImg.preserveAspect = true;
            overlayImg.raycastTarget = false;

            img.sprite = newSprite;
            img.color = new Color(1, 1, 1, 0);

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                img.rectTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);
                overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;

                float fadeT = elapsed / shakeDuration;
                overlayImg.color = new Color(1, 1, 1, 1f - fadeT);
                img.color = new Color(1, 1, 1, fadeT);

                elapsed += Time.deltaTime;
                yield return null;
            }

            img.rectTransform.anchoredPosition = originalPos;
            Destroy(overlayObj);
            img.color = Color.white;
        }
        else
        {
            float duration = 0.2f;
            Vector3 originalScale = earthImage.rectTransform.localScale;
            Vector3 enlargedScale = originalScale * 1.2f;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);
                earthImage.rectTransform.localScale = Vector3.Lerp(originalScale, enlargedScale, easedT);
                yield return null;
            }

            earthImage.sprite = newSprite;

            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float easedT = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                earthImage.rectTransform.localScale = Vector3.Lerp(enlargedScale, originalScale, easedT);
                yield return null;
            }

            earthImage.rectTransform.localScale = originalScale;
        }
    }

    void UpdateScore()
    {
        scoreText.text = score.ToString();
    }

    void GameOver()
    {
        Earth.SetActive(false);
        SpawnArea.SetActive(false);
        MainBin.SetActive(false);
        Score.SetActive(false); 
        IconName.SetActive(false);
        Header.SetActive(false);
        Settings.SetActive(false);
        QuizProgress.SetActive(false);
        Debug.Log("Game Over! The Earth is fully polluted or all icons used.");

        // Stop any current falling animation
        if (fallingCoroutine != null)
        {
            StopCoroutine(fallingCoroutine);
        }

        // Clean up current icon
        if (currentIcon != null)
        {
            Destroy(currentIcon);
            currentIcon = null;
        }

        // Trigger Game Over dialogue
        if (dialogues != null)
        {
            dialogues.StartDialogue(1);
        }
    }
}
