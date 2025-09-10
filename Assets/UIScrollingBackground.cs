using UnityEngine;
using UnityEngine.UI;

public class UIScrollingBackground : MonoBehaviour
{
    public float scrollSpeed = 0.5f;
    private RawImage rawImage;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        rawImage.uvRect = new Rect(Time.time * scrollSpeed, 0, 1, 1);
    }
}
