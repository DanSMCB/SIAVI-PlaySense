using System;
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class HandPaddleInputBridge : MonoBehaviour
{
    public enum HandSide
    {
        Left,
        Right
    }

    [Serializable]
    public class HandState
    {
        public bool isTracked;
        public bool isActive;
        public Vector3 pinkyTip;
        public Vector3 pinkyMcp;
        public float normalizedY;
    }

    [Header("Landmark Indices")]
    [SerializeField] private int _pinkyTipIndex = 20;
    [SerializeField] private int _pinkyMcpIndex = 17;

    public HandState LeftHand { get; private set; } = new HandState();
    public HandState RightHand { get; private set; } = new HandState();

    private readonly object _lock = new object();

    public void OnHandResult(HandLandmarkerResult result)
    {
        lock (_lock)
        {
            LeftHand.isTracked = false;
            RightHand.isTracked = false;

            var hands = result.handLandmarks;
            var handedness = result.handedness;

            if (hands == null)
                return;

            for (int i = 0; i < hands.Count; i++)
            {
                var hand = hands[i];
                var lms = hand.landmarks;
                if (lms == null)
                    continue;

                if (lms.Count <= Mathf.Max(_pinkyTipIndex, _pinkyMcpIndex))
                    continue;

                var pinkyTip = lms[_pinkyTipIndex];
                var pinkyMcp = lms[_pinkyMcpIndex];

                bool isLeft = false;

                // Try to read handedness from MediaPipe container
                if (handedness != null && i < handedness.Count)
                {
                    var cls = handedness[i];

                    // Common MediaPipe container pattern: Classifications.categories[0].categoryName
                    if (cls.categories != null && cls.categories.Count > 0)
                    {
                        var label = cls.categories[0].categoryName;
                        isLeft = string.Equals(label, "Left", StringComparison.OrdinalIgnoreCase);
                    }
                }

                var state = new HandState
                {
                    isTracked = true,
                    isActive = true,
                    pinkyTip = new Vector3(pinkyTip.x, pinkyTip.y, pinkyTip.z),
                    pinkyMcp = new Vector3(pinkyMcp.x, pinkyMcp.y, pinkyMcp.z),
                    normalizedY = pinkyTip.y
                };

                if (isLeft)
                {
                    LeftHand.isTracked = true;
                    LeftHand.isActive = true;
                    LeftHand.pinkyTip = state.pinkyTip;
                    LeftHand.pinkyMcp = state.pinkyMcp;
                    LeftHand.normalizedY = state.normalizedY;
                }
                else
                {
                    RightHand.isTracked = true;
                    RightHand.isActive = true;
                    RightHand.pinkyTip = state.pinkyTip;
                    RightHand.pinkyMcp = state.pinkyMcp;
                    RightHand.normalizedY = state.normalizedY;
                }
            }

            LeftHand.isActive = LeftHand.isTracked;
            RightHand.isActive = RightHand.isTracked;
        }
    }
}