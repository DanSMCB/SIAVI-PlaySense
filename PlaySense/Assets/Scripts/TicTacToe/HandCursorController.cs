using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCursorController : MonoBehaviour
{
    public RectTransform cursor;

    public Vector2 indexTipPosition;
    public Vector2 thumbTipPosition;

    public float pinchThreshold = 0.05f;
    private bool isPinching = false;

    void Update()
    {
        MoveCursor();
        DetectPinch();
    }

    void MoveCursor()
    {
        float x = indexTipPosition.x * Screen.width;
        float y = indexTipPosition.y * Screen.height;

        cursor.position = Vector2.Lerp(cursor.position, new Vector2(x, y), Time.deltaTime * 10);
    }

    void DetectPinch()
    {
        float distance = Vector2.Distance(indexTipPosition, thumbTipPosition);

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

        foreach (var result in results)
        {
            var button = result.gameObject.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.Invoke();
                break;
            }
        }
    }
}