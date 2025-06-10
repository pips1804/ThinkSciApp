using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleAnimationManager : MonoBehaviour
{
    public GameObject impactImage;

    public IEnumerator AttackAnimation(RectTransform attacker, Vector3 originalPos, Vector3 attackOffset, Vector3 worldPos, bool isMiss, bool isEnemy)
    {
        Vector3 targetPos = originalPos + attackOffset;
        float duration = 0.35f;
        float elapsed = 0f;

        float tiltAngle = 25f;
        Quaternion startRotation = attacker.rotation;
        Quaternion tiltRotation = Quaternion.Euler(0, 0, attacker.name.Contains("Player") ? -tiltAngle : tiltAngle);

        Vector3 originalScale = attacker.localScale;
        Vector3 enlargedScale = originalScale * 1.2f;

        if (!isMiss && impactImage != null)
        {
            yield return ShowImpactImage(worldPos, isEnemy);
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

        attacker.rotation = startRotation;
        attacker.localScale = originalScale;
    }

    public IEnumerator DodgeAnimation(RectTransform defender)
    {
        Vector3 originalPos = defender.anchoredPosition;
        Vector3 dodgeOffset = new Vector3(80f, 0f, 0f);
        float dodgeTime = 0.35f;
        float elapsed = 0f;

        while (elapsed < dodgeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin((elapsed / dodgeTime) * Mathf.PI);
            defender.anchoredPosition = originalPos + dodgeOffset * t;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);
        defender.anchoredPosition = originalPos;
    }

    public IEnumerator HitShake(RectTransform target, float duration = 0.2f, float magnitude = 10f)
    {
        Vector3 originalPos = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            target.anchoredPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        target.anchoredPosition = originalPos;
    }

    public IEnumerator ShowFloatingText(Text textElement, string content, Vector3 startPos, Color color)
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
            float alpha = Mathf.SmoothStep(0f, 1f, t < 0.5f ? t * 2f : (1f - t) * 2f);
            textElement.color = new Color(color.r, color.g, color.b, alpha);
            textElement.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        textElement.gameObject.SetActive(false);
    }

    public IEnumerator ShowImpactImage(Vector3 worldPos, bool isEnemy)
    {
        if (impactImage == null) yield break;

        Vector3 offset = isEnemy ? new Vector3(60f, 0f, 0f) : new Vector3(-60f, 0f, 0f);

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
}
