using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandUiPointerClicker : MonoBehaviour
{
    [SerializeField] private EventSystem _eventSystem;
    [SerializeField] private RectTransform _cursorVisual; // optional

    private PointerEventData _ped;
    private bool _wasPinching;

    private void Awake()
    {
        if (_eventSystem == null) _eventSystem = EventSystem.current;
        _ped = new PointerEventData(_eventSystem);
    }

    public void UpdatePointer(Vector2 screenPos, bool isPinching)
    {
        if (_cursorVisual != null)
            _cursorVisual.position = screenPos;

        _ped.Reset();
        _ped.position = screenPos;

        var results = new List<RaycastResult>();
        _eventSystem.RaycastAll(_ped, results);

        GameObject target = null;
        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go == null) continue;

            // pick the first selectable (Button, Toggle, etc.)
            if (go.GetComponentInParent<Selectable>() != null)
            {
                target = go;
                break;
            }
        }

        // Pinch start = proper click sequence
        if (isPinching && !_wasPinching && target != null)
        {
            _ped.pointerPressRaycast = results.Count > 0 ? results[0] : default;
            _ped.pressPosition = screenPos;
            _ped.pointerPress = target;

            ExecuteEvents.ExecuteHierarchy(target, _ped, ExecuteEvents.pointerDownHandler);
        }

        // Pinch end = release + click (matches how Unity UI expects it)
        if (!isPinching && _wasPinching && target != null)
        {
            ExecuteEvents.ExecuteHierarchy(target, _ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(target, _ped, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.ExecuteHierarchy(target, _ped, ExecuteEvents.submitHandler);
        }

        _wasPinching = isPinching;
    }
}