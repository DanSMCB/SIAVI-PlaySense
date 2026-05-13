using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCursor : MonoBehaviour
{
    public RectTransform cursor;

    public Vector2 cursorPosition;
    public Vector2 thumbPosition;

    public float pinchThreshold = 0.05f;

    private bool isPinching = false;

    void Update()
    {
        MoveCursor();
        DetectPinch();
    }

    void MoveCursor()
    {
        float x = cursorPosition.x * Screen.width;
        float y = cursorPosition.y * Screen.height;

        cursor.position = Vector2.Lerp(
            cursor.position,
            new Vector2(x, y),
            Time.deltaTime * 10f
        );
    }

    void DetectPinch()
    {
        float distance = Vector2.Distance(cursorPosition, thumbPosition);

        if (distance < pinchThreshold && !isPinching)
        {
            isPinching = true;
            OnPinch();
        }

        if (distance > pinchThreshold)
        {
            isPinching = false;
        }
    }

    void OnPinch()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = cursor.position;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count == 0)
            return;

        var topResult = results[0];

        var button = topResult.gameObject.GetComponent<Button>();

        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }
}