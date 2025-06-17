using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleAnimationManager : MonoBehaviour
{
    public IdleAnimation playerIdleAnim;
    public IdleAnimation enemyIdleAnim;

    public GameObject enemyShadow;
    public GameObject playerShadow;
    public GameObject impactImage;

    public Text timerText;

    public Image battleBackground;

    public IEnumerator DodgeAnimation(RectTransform defender)
    {
        Vector3 originalPos = defender.anchoredPosition;
        Vector3 dodgeOffset = new Vector3(80f, 0f, 0f);
        float dodgeTime = 0.35f;
        float elapsed = 0f;

        playerIdleAnim.StopIdle();
        enemyIdleAnim.StopIdle();

        while (elapsed < dodgeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / dodgeTime) * Mathf.PI);
            defender.anchoredPosition = originalPos + dodgeOffset * t;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);
        defender.anchoredPosition = originalPos;

        playerIdleAnim.StartIdle();
        enemyIdleAnim.StartIdle();
    }

    public IEnumerator AttackAnimation(RectTransform attacker, Vector3 originalPos, Vector3 attackOffset, Vector3 worldPos, bool isEnemy, bool isMiss, bool isPlayer)
    {
        Vector3 targetPos = originalPos + attackOffset * 1.5f;
        float duration = 0.35f;
        float elapsed = 0f;

        float tiltAngle = 25f;
        Quaternion startRotation = attacker.rotation;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, isPlayer ? -tiltAngle : tiltAngle);


        Vector3 originalScale = attacker.localScale;
        Vector3 enlargedScale = originalScale * 1.2f;

        playerIdleAnim.StopIdle();
        enemyIdleAnim.StopIdle();

        if (isEnemy)
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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            attacker.anchoredPosition = Vector3.Lerp(targetPos, originalPos, t);
            attacker.rotation = Quaternion.Slerp(tiltRotation, startRotation, t);
            attacker.localScale = Vector3.Lerp(enlargedScale, originalScale, t);

            yield return null;
        }

        attacker.anchoredPosition = originalPos;
        attacker.rotation = startRotation;
        attacker.localScale = originalScale;

        if (isEnemy)
        {
            playerShadow.SetActive(true);
        }
        else
        {
            enemyShadow.SetActive(true);

        }

        playerIdleAnim.StartIdle();
        enemyIdleAnim.StartIdle();

        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator IntenseAttackAnimation(RectTransform attacker, Vector3 originalPos, Vector3 attackOffset, Vector3 worldPos, bool isEnemy, bool isMiss, bool isPlayer)
    {
        Vector3 targetPos = originalPos + attackOffset * 1.5f;
        float duration = 0.25f;
        float elapsed = 0f;

        float tiltAngle = 35f;
        Quaternion startRotation = attacker.rotation;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, isPlayer ? -tiltAngle : tiltAngle);


        Vector3 originalScale = attacker.localScale;
        Vector3 enlargedScale = originalScale * 1.4f;

        playerIdleAnim.StopIdle();
        enemyIdleAnim.StopIdle();

        if (isEnemy)
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

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            attacker.anchoredPosition = Vector3.Lerp(targetPos, originalPos, t);
            attacker.rotation = Quaternion.Slerp(tiltRotation, startRotation, t);
            attacker.localScale = Vector3.Lerp(enlargedScale, originalScale, t);
            yield return null;
        }

        attacker.anchoredPosition = originalPos;
        attacker.rotation = startRotation;
        attacker.localScale = originalScale;

        if (isEnemy)
        {
            playerShadow.SetActive(true);
        }
        else
        {
            enemyShadow.SetActive(true);
        }

        playerIdleAnim.StartIdle();
        enemyIdleAnim.StartIdle();

        yield return new WaitForSeconds(0.1f);
    }

    public IEnumerator ShowImpactImage(Vector3 worldPos, bool isEnemy)
    {

        Vector3 offset = isEnemy ? new Vector3(-60f, 0f, 0f) : new Vector3(60f, 0f, 0f); // adjust 30f as needed

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

    public IEnumerator HitShake(RectTransform rectTransform)
    {
        Vector3 originalPos = rectTransform.anchoredPosition;
        float elapsed = 0f;
        float duration = 0.20f;
        float magnitude = 20f;

        playerIdleAnim.StopIdle();
        enemyIdleAnim.StopIdle();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude;
            rectTransform.anchoredPosition = originalPos + new Vector3(x, 0, 0);
            yield return null;
        }

        rectTransform.anchoredPosition = originalPos;

        playerIdleAnim.StartIdle();
        enemyIdleAnim.StartIdle();
    }
    public IEnumerator GraduallyTurnRed(float duration)
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


    public IEnumerator FadeTextLoop()
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

    public IEnumerator FadeToAlpha(float targetAlpha, float duration)
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

    public IEnumerator HeartbeatEffect(bool isTimerRunning, Vector3 originalScale, float timer)
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

}
