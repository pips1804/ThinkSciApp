using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour
{
    public float floatSpeed = 50f;
    public float fadeTime = 1f;
    private Text text;
    private Color originalColor;
    private float timer;

    void Start()
    {
        text = GetComponent<Text>();
        originalColor = text.color;
        Destroy(gameObject, fadeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }
}
