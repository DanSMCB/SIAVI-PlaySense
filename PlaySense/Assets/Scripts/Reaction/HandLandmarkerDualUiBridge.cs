using Mediapipe.Tasks.Vision.HandLandmarker;
using System;
using UnityEngine;

public class HandLandmarkerDualUiBridge : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private HandUiPointerClicker _leftHandPointer;
    [SerializeField] private HandUiPointerClicker _rightHandPointer;

    [Header("Settings")]
    [SerializeField] private float _pinchDistanceThreshold = 0.05f;

    [Range(0f, 1f)]
    [SerializeField] private float _smoothing = 0.2f;

    [SerializeField] private int _maxMissingFrames = 5;

    private class HandData
    {
        public Vector3 indexTip;
        public Vector3 thumbTip;
        public Vector3 indexBase;
        public Vector3 pinkyBase;
        public Vector3 wrist;
        public bool hasSample;
    }

    private HandData _handA = new HandData();
    private HandData _handB = new HandData();

    private Vector2 _smoothedA;
    private Vector2 _smoothedB;

    private int _aMissingFrames;
    private int _bMissingFrames;

    private readonly object _lock = new object();

    public void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null)
            return;

        lock (_lock)
        {
            _handA.hasSample = false;
            _handB.hasSample = false;

            int assigned = 0;

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                var lms = result.handLandmarks[i].landmarks;
                if (lms == null || lms.Count < 21) continue;

                var data = new HandData
                {
                    indexTip = new Vector3(lms[8].x, lms[8].y, lms[8].z),
                    thumbTip = new Vector3(lms[4].x, lms[4].y, lms[4].z),
                    wrist = new Vector3(lms[0].x, lms[0].y, lms[0].z),
                    indexBase = new Vector3(lms[5].x, lms[5].y, lms[5].z),
                    pinkyBase = new Vector3(lms[17].x, lms[17].y, lms[17].z),
                    hasSample = true
                };

                if (assigned == 0)
                {
                    _handA = data;
                    assigned++;
                }
                else
                {
                    _handB = data;
                    assigned++;
                }
            }
        }
    }

    private void Update()
    {
        UpdateVisibility(_handA, ref _aMissingFrames, _leftHandPointer);
        UpdateVisibility(_handB, ref _bMissingFrames, _rightHandPointer);

        ProcessHand(_handA, _leftHandPointer, ref _smoothedA);
        ProcessHand(_handB, _rightHandPointer, ref _smoothedB);
    }

    private void UpdateVisibility(HandData hand, ref int missingFrames, HandUiPointerClicker pointer)
    {
        if (hand.hasSample)
            missingFrames = 0;
        else
            missingFrames++;

        bool visible = missingFrames < _maxMissingFrames;

        if (pointer != null)
        {
            var img = pointer.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.enabled = visible;
        }
    }

    private void ProcessHand(HandData data, HandUiPointerClicker pointer, ref Vector2 smoothedPos)
    {
        if (pointer == null || !data.hasSample)
            return;

        float centerX = (data.wrist.x * 0.5f + data.indexBase.x * 0.25f + data.pinkyBase.x * 0.25f);
        float centerY = (data.wrist.y * 0.5f + data.indexBase.y * 0.25f + data.pinkyBase.y * 0.25f);

        Vector2 screenPos = new Vector2(
            centerX * Screen.width,
            (1f - centerY) * Screen.height
        );

        float t = 1f - Mathf.Pow(1f - _smoothing, Time.deltaTime * 60f);
        smoothedPos = Vector2.Lerp(smoothedPos, screenPos, t);

        bool isPinching = Vector3.Distance(data.indexTip, data.thumbTip) < _pinchDistanceThreshold;

        pointer.UpdatePointer(smoothedPos, isPinching);
    }
}