using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public float longPressDuration = 0.6f;

    public Action onLongPress;
    public Action onRelease;
    public bool longPressed = false;

    private bool isPointerDown = false;
    private float timer = 0f;

    void Update()
    {
        if (isPointerDown)
        {
            timer += Time.deltaTime;
            if (timer >= longPressDuration && !longPressed)
            {
                longPressed = true;
                onLongPress?.Invoke();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        timer = 0f;
        longPressed = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;

        if (longPressed)
        {
            onRelease?.Invoke(); // hide the modal
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (longPressed)
        {
            // Suppress click if it was a long press
            return;
        }

        // Let the button do its usual onClick
        ExecuteEvents.Execute(gameObject, eventData, ExecuteEvents.submitHandler);
    }
}
