using System;
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

// For container type alias (NormalizedLandmarks)
using mptcc = Mediapipe.Tasks.Components.Containers;

public class HandLandmarkerUiInputBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandUiPointerClicker _uiPointer;

    [Header("Pointer Landmark")]
    [SerializeField] private int _handIndex = 0;            // 0 = first hand
    [SerializeField] private int _pointerLandmarkIndex = 8; // IndexFingerTip
    [SerializeField] private int _thumbTipIndex = 4;        // ThumbTip

    [Header("Pinch")]
    [Tooltip("Pinch when distance(thumbTip, indexTip) is below this threshold (normalized landmark units).")]
    [SerializeField] private float _pinchDistanceThreshold = 0.05f;

    [Tooltip("Optional smoothing to reduce jitter. 0 = no smoothing.")]
    [Range(0f, 1f)]
    [SerializeField] private float _smoothing = 0.2f;

    // --- shared state (written from MediaPipe thread, read from Unity main thread) ---
    private readonly object _lock = new object();
    private bool _hasSample;
    private Vector3 _latestIndexTip; // normalized x,y,z
    private Vector3 _latestThumbTip; // normalized x,y,z

    // main-thread state
    private Vector2 _smoothedScreenPos;
    private bool _wasPinchingMainThread;

    /// <summary>
    /// Called from HandLandmarkerRunner callback (NOT main thread).
    /// Do NOT call Unity APIs here.
    /// </summary>
    public void OnHandResult(HandLandmarkerResult result)
    {
        // Only copy minimal data; avoid Screen/Time/UI here.
        var hands = result.handLandmarks;
        if (hands == null || hands.Count <= _handIndex) return;

        mptcc.NormalizedLandmarks hand = hands[_handIndex];
        var lms = hand.landmarks;
        if (lms == null) return;

        var maxIndex = Mathf.Max(_pointerLandmarkIndex, _thumbTipIndex);
        if (lms.Count <= maxIndex) return;

        var indexTip = lms[_pointerLandmarkIndex];
        var thumbTip = lms[_thumbTipIndex];

        lock (_lock)
        {
            _latestIndexTip = new Vector3(indexTip.x, indexTip.y, indexTip.z);
            _latestThumbTip = new Vector3(thumbTip.x, thumbTip.y, thumbTip.z);
            _hasSample = true;
        }
    }

    private void Update()
    {
        if (_uiPointer == null) return;

        Vector3 indexTip, thumbTip;
        bool hasSample;

        lock (_lock)
        {
            hasSample = _hasSample;
            indexTip = _latestIndexTip;
            thumbTip = _latestThumbTip;
        }

        if (!hasSample) return;

        // Now we are on main thread: Unity APIs are OK.
        var screenPos = new Vector2(
          indexTip.x * Screen.width,
          (1f - indexTip.y) * Screen.height
        );

        if (_smoothing > 0f)
        {
            // simple framerate-independent-ish smoothing
            var t = 1f - Mathf.Pow(1f - _smoothing, Time.deltaTime * 60f);
            _smoothedScreenPos = Vector2.Lerp(_smoothedScreenPos, screenPos, t);
        }
        else
        {
            _smoothedScreenPos = screenPos;
        }

        var dist = Vector3.Distance(indexTip, thumbTip);
        var isPinching = dist < _pinchDistanceThreshold;

        // Optional: basic debounce so it doesn't spam click if pinch jitters
        // Click only on pinch "rising edge"
        if (isPinching && !_wasPinchingMainThread)
        {
            _uiPointer.UpdatePointer(_smoothedScreenPos, true);
        }
        else
        {
            _uiPointer.UpdatePointer(_smoothedScreenPos, false);
        }

        _wasPinchingMainThread = isPinching;
    }
}