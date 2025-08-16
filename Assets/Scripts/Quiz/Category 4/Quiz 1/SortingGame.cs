using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SortingGame : MonoBehaviour
{
    [Header("UI Elements")]
    public Image earthImage;             // The UI Image for Earth health
    public Sprite[] earthStates;         // 5 sprites from healthy to polluted
    public Text scoreText;               // UI text for score display

    [Header("Game Settings")]
    public Transform spawnPoint;         // Where icons start
    public GameObject[] energyIcons;     // Prefabs for energy sources
    public Transform greenBin;           // Target bin for renewable
    public Transform fossilBin;          // Target bin for fossil fuels
    public float fallSpeed = 2f;         // Speed of falling icons
    public int maxIcons = 10;            // Limit number of icons

    private int currentEarthState = 0;
    private int score = 0;
    private GameObject currentIcon;
    private int iconsSpawned = 0;
    private int consecutiveCorrect = 0;

    void Start()
    {
        UpdateScore();
        SpawnNewIcon();
    }

    void Update()
    {
        if (currentIcon != null)
        {
            // Move the icon downward
            currentIcon.transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);

            // Touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    spawnPoint.parent as RectTransform,
                    touch.position,
                    null, // null means screen space overlay canvas
                    out localPos
                );

                // Move icon with finger
                if (touch.phase == TouchPhase.Moved)
                {
                    currentIcon.GetComponent<RectTransform>().anchoredPosition = localPos;
                }

                // Drop icon and check if it hits a bin
                if (touch.phase == TouchPhase.Ended)
                {
                    CheckIconPlacement();
                }
            }
        }
    }

    void SpawnNewIcon()
    {
        if (iconsSpawned >= maxIcons)
        {
            GameOver();
            return;
        }

        int randIndex = Random.Range(0, energyIcons.Length);
        currentIcon = Instantiate(energyIcons[randIndex]);
        currentIcon.transform.SetParent(spawnPoint.parent, false);

        // Set anchored position so it spawns exactly at the spawn point in UI space
        RectTransform iconRect = currentIcon.GetComponent<RectTransform>();
        RectTransform spawnRect = spawnPoint.GetComponent<RectTransform>();
        iconRect.anchoredPosition = spawnRect.anchoredPosition;

        iconsSpawned++;
    }

    void CheckIconPlacement()
    {
        RectTransform iconRect = currentIcon.GetComponent<RectTransform>();
        RectTransform greenRect = greenBin.GetComponent<RectTransform>();
        RectTransform fossilRect = fossilBin.GetComponent<RectTransform>();

        // Compare in anchoredPosition (UI local space)
        float distanceToGreen = Vector2.Distance(iconRect.anchoredPosition, greenRect.anchoredPosition);
        float distanceToFossil = Vector2.Distance(iconRect.anchoredPosition, fossilRect.anchoredPosition);

        // Identify if icon is renewable or fossil
        bool isRenewable = currentIcon.CompareTag("Renewable");

        if (distanceToGreen < 100f && isRenewable)
        {
            CorrectAnswer();
        }
        else if (distanceToFossil < 100f && !isRenewable)
        {
            CorrectAnswer();
        }
        else
        {
            WrongAnswer();
        }

        Destroy(currentIcon);
        SpawnNewIcon();
    }

    void CorrectAnswer()
    {
        score += 10;
        consecutiveCorrect++;

        // Recover earth state after 2 consecutive correct answers
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
        consecutiveCorrect = 0; // reset streak

        currentEarthState++;

        if (currentEarthState < earthStates.Length)
        {
            StartCoroutine(AnimateEarthChange(earthStates[currentEarthState], true));
        }
        else
        {
            GameOver();
        }
    }

    IEnumerator AnimateEarthChange(Sprite newSprite, bool isWrong)
    {
        if (isWrong)
        {
            // Shake + fade effect
            float shakeDuration = 0.3f;
            float shakeMagnitude = 10f;
            Vector3 originalPos = earthImage.rectTransform.anchoredPosition;
            Image img = earthImage;

            // Store the original sprite for fade blending
            Sprite oldSprite = img.sprite;

            // Create a temporary overlay image for the fade
            GameObject overlayObj = new GameObject("EarthOverlay");
            overlayObj.transform.SetParent(img.transform.parent, false);
            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.sprite = oldSprite;
            overlayImg.rectTransform.sizeDelta = img.rectTransform.sizeDelta;
            overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;
            overlayImg.preserveAspect = true;
            overlayImg.raycastTarget = false;

            // Set base image to the new sprite but invisible
            img.sprite = newSprite;
            img.color = new Color(1, 1, 1, 0);

            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                // Shake position
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                img.rectTransform.anchoredPosition = originalPos + new Vector3(x, y, 0);
                overlayImg.rectTransform.anchoredPosition = img.rectTransform.anchoredPosition;

                // Fade between old and new
                float fadeT = elapsed / shakeDuration;
                overlayImg.color = new Color(1, 1, 1, 1f - fadeT); // old fades out
                img.color = new Color(1, 1, 1, fadeT);             // new fades in

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Reset position & remove overlay
            img.rectTransform.anchoredPosition = originalPos;
            Destroy(overlayObj);
            img.color = Color.white;
        }
        else
        {
            // Smooth pop transition
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
        scoreText.text =  score.ToString();
    }

    void GameOver()
    {
        Debug.Log("Game Over! The Earth is fully polluted or all icons used.");
        // Show Game Over panel if desired
    }
}
