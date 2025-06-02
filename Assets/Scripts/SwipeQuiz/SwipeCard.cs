using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SwipeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public float swipeThreshold = 100f;
    public QuizManager quizManager;
    public float swipeSpeed = 1000f;
    private Vector3 startPos;
    private bool isSwiping = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSwiping) return;
        startPos = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isSwiping) return;
        transform.position += new Vector3(eventData.delta.x, 0, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isSwiping) return;

        float deltaX = transform.position.x - startPos.x;

        if (Mathf.Abs(deltaX) > swipeThreshold)
        {
            string direction = deltaX > 0 ? "Right" : "Left";
            StartCoroutine(SwipeAndHandle(direction));
        }
        else
        {
            // Reset position if not enough swipe
            transform.position = startPos;
        }
    }

    IEnumerator SwipeAndHandle(string direction)
    {
        isSwiping = true;

        Vector3 targetPos = direction == "Right"
            ? startPos + Vector3.right * 1000
            : startPos + Vector3.left * 1000;

        float time = 0;
        while (time < 0.3f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * swipeSpeed);
            time += Time.deltaTime;
            yield return null;
        }

        // Handle the quiz logic
        quizManager.HandleAnswer(direction);

        // Reset position (QuizManager will update question text)
        transform.position = startPos;
        isSwiping = false;
    }
}
