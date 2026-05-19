using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System;
using TMPro; // For TextMeshProUGUI
using UnityEngine;

public class HandPaddleInputBridge : MonoBehaviour
{
    #region Handtracking State Management
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

                if (handedness != null && i < handedness.Count)
                {
                    var cls = handedness[i];

                    if (cls.categories != null && cls.categories.Count > 0)
                    {
                        var label = cls.categories[0].categoryName;
                        isLeft = string.Equals(label, "Left", StringComparison.OrdinalIgnoreCase); // This is inverted. All the "Left" are actually the right hand, and vice versa. This is a quirk of the mediapipe handedness labeling.
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
    #endregion

    #region === PONG GAME LOGIC  ===

    [Header("Pong Elements")]
    [SerializeField] private RectTransform _ball;
    [SerializeField] private RectTransform _leftPaddle;
    [SerializeField] private PaddleControl _leftPaddleControl;
    [SerializeField] private RectTransform _rightPaddle;
    [SerializeField] private PaddleControl _rightPaddleControl;
    [SerializeField] private TextMeshProUGUI _leftScoreText;    // Blue (left)
    [SerializeField] private TextMeshProUGUI _rightScoreText;   // Red (right)
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private TextMeshProUGUI _winScreenText;
    [SerializeField] private GameObject _handTrackingCursor;
    [SerializeField] private GameObject _handTrackingCursor2;

    [Header("Pong Settings")]
    public int winGameValue = 3;
    [SerializeField] private float ballStartSpeed = 350f;
    [SerializeField] private float ballSpeedIncrement = 60f;
    [SerializeField] private float maxBounceAngle = 45f; // (degrees)
    [SerializeField] private float edgeBounceRandomness = 8f; // (degrees)
    [SerializeField] private float topBoundaryY = 670f;
    [SerializeField] private float bottomBoundaryY = -670f;
    [SerializeField] private float leftBoundaryX = -1200f;
    [SerializeField] private float rightBoundaryX = 1200f;
    [SerializeField] private float paddleWidth = 40f;
    [SerializeField] private float paddleHeight = 310f;
    [SerializeField] private float ballRadius = 40f;

    private Vector2 _ballDirection;  // Always normalized
    private float _ballSpeed;
    private int _leftScore, _rightScore;
    private bool _gameOver = false;
    private bool _launchToLeftAfterScore = false;
    private bool _handTrackingActivated = false;

    private bool _botMode = false;
    public bool BotModeActive => _botMode;

    void Update()
    {
        if (_ball == null || _leftPaddle == null || _rightPaddle == null || _gameOver) return;

        UpdateBotModeFromRuntimeSettings();

        // Drive bot paddle if enabled (right paddle only)
        if (_leftPaddleControl != null && _leftPaddleControl.BotEnabled)
        {
            _leftPaddleControl.BotStepToBallY(_ball.anchoredPosition.y);
        }

        PongGameUpdate();
    }

    #region === GAME LOGIC ===

    private void UpdateBotModeFromRuntimeSettings()
    {
        bool botModeNow = HandLandmarkRuntimeSettings.NumHands == 1;

        // Only apply if changed (optional, but avoids redundant sets)
        if (botModeNow == _botMode)
            return;

        _botMode = botModeNow;

        if (_leftPaddleControl != null)
            _leftPaddleControl.BotEnabled = _botMode;

        if (_rightPaddleControl != null)
            _rightPaddleControl.BotEnabled = false;
    }

    public void SetHandTrackingActive(bool active)
    {
        _handTrackingActivated = active;
        if (active)
        {
            _leftPaddleControl.InputMethod = PaddleControl.InputMethodType.HandTracking;
            _rightPaddleControl.InputMethod = PaddleControl.InputMethodType.HandTracking;
        }
        else
        {
            _leftPaddleControl.InputMethod = PaddleControl.InputMethodType.Keyboard;
            _rightPaddleControl.InputMethod = PaddleControl.InputMethodType.Keyboard;
        }
    }

    public void ResetGame()
    {
        // Start Paddles
        _leftPaddleControl.MovementEnabled = true;
        _rightPaddleControl.MovementEnabled = true;

        // Reset scores and UI
        _leftScore = 0;
        _rightScore = 0;
        UpdateScoreUI();

        // Hide win panel
        if (_winScreen != null) _winScreen.SetActive(false);
        _gameOver = false;

        // Center ball and launch randomly
        _ball.anchoredPosition = Vector2.zero;
        _ballSpeed = ballStartSpeed;

        // Pick random initial direction (always horizontal, left or right)
        _launchToLeftAfterScore = UnityEngine.Random.value > 0.5f;
        LaunchBall(_launchToLeftAfterScore ? Vector2.left : Vector2.right);
    }

    private void LaunchBall(Vector2 lateralDir)
    {
        // lateralDir: Vector2.left or Vector2.right
        float angle = UnityEngine.Random.Range(-20f, 20f) * Mathf.Deg2Rad;
        _ballDirection = (Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg) * lateralDir).normalized;
        // Clamp minimal X movement
        _ballDirection.x = Mathf.Sign(_ballDirection.x) * Mathf.Max(Mathf.Abs(_ballDirection.x), 0.6f);
        _ballDirection = _ballDirection.normalized;
    }

    private void PongGameUpdate()
    {
        // Move ball
        Vector2 pos = _ball.anchoredPosition;
        Vector2 newPos = pos + _ballDirection * _ballSpeed * Time.deltaTime;

        // --- Top/bottom edge bounce
        if ((newPos.y + ballRadius) >= topBoundaryY)
        {
            newPos.y = topBoundaryY - ballRadius;
            _ballDirection.y *= -1f;
            ApplyEdgeRandomness();
            IncreaseBallSpeed();
        }
        else if ((newPos.y - ballRadius) <= bottomBoundaryY)
        {
            newPos.y = bottomBoundaryY + ballRadius;
            _ballDirection.y *= -1f;
            ApplyEdgeRandomness();
            IncreaseBallSpeed();
        }

        // --- Left/right scoring
        if ((newPos.x - ballRadius) <= leftBoundaryX)
        {
            ReceivePoint("right");
            return;
        }
        else if ((newPos.x + ballRadius) >= rightBoundaryX)
        {
            ReceivePoint("left");
            return;
        }

        // --- Paddle collision
        // Left paddle
        if (BallIntersectsPaddle(newPos, _leftPaddle.anchoredPosition))
        {
            Vector2 paddleCenter = _leftPaddle.anchoredPosition;
            Vector2 relative = (newPos - paddleCenter) / (paddleHeight * 0.5f);
            float bounceAngle = relative.y * maxBounceAngle + UnityEngine.Random.Range(-edgeBounceRandomness, edgeBounceRandomness);
            float bounceAngleRad = bounceAngle * Mathf.Deg2Rad;
            // Ball should go to right after collision
            _ballDirection = new Vector2(Mathf.Cos(bounceAngleRad), Mathf.Sin(bounceAngleRad));
            _ballDirection.x = Mathf.Abs(_ballDirection.x); // ensure always right
            _ballDirection = _ballDirection.normalized;
            newPos.x = paddleCenter.x + paddleWidth * 0.5f + ballRadius;
            IncreaseBallSpeed();
        }
        // Right paddle
        else if (BallIntersectsPaddle(newPos, _rightPaddle.anchoredPosition))
        {
            Vector2 paddleCenter = _rightPaddle.anchoredPosition;
            Vector2 relative = (newPos - paddleCenter) / (paddleHeight * 0.5f);
            float bounceAngle = -relative.y * maxBounceAngle + UnityEngine.Random.Range(-edgeBounceRandomness, edgeBounceRandomness);
            float bounceAngleRad = bounceAngle * Mathf.Deg2Rad;
            // Ball should go to left after collision
            _ballDirection = new Vector2(-Mathf.Cos(bounceAngleRad), Mathf.Sin(bounceAngleRad));
            _ballDirection.x = -Mathf.Abs(_ballDirection.x); // ensure always left
            _ballDirection = _ballDirection.normalized;
            newPos.x = paddleCenter.x - paddleWidth * 0.5f - ballRadius;
            IncreaseBallSpeed();
        }

        _ball.anchoredPosition = newPos;
    }

    private bool BallIntersectsPaddle(Vector2 ballPos, Vector2 paddleCenter)
    {
        // Simple rectangle vs circle
        float dx = Mathf.Max(Mathf.Abs(ballPos.x - paddleCenter.x) - paddleWidth * 0.5f, 0f);
        float dy = Mathf.Max(Mathf.Abs(ballPos.y - paddleCenter.y) - paddleHeight * 0.5f, 0f);
        return (dx * dx + dy * dy) < (ballRadius * ballRadius);
    }

    private void ApplyEdgeRandomness()
    {
        // Add random angle variation when bouncing at top/bottom
        float angle = UnityEngine.Random.Range(-edgeBounceRandomness, edgeBounceRandomness);
        Vector2 newDirection = Quaternion.Euler(0, 0, angle) * _ballDirection;
        newDirection.x = Mathf.Sign(_ballDirection.x) * Mathf.Abs(newDirection.x); // preserve X sign
        _ballDirection = newDirection.normalized;
    }

    private void IncreaseBallSpeed()
    {
        _ballSpeed += ballSpeedIncrement;
    }
    #endregion

    #region === SCORING ===

    public void ReceivePoint(string winningSide)
    {
        // "left" or "right"
        if (winningSide == "left")
            _leftScore++;
        else
            _rightScore++;

        UpdateScoreUI();
        WinGameCheck();

        if (!_gameOver)
            ResetBall(winningSide == "right" ? Vector2.left : Vector2.right);
    }

    private void UpdateScoreUI()
    {
        if (_leftScoreText != null)
            _leftScoreText.text = _leftScore.ToString();
        if (_rightScoreText != null)
            _rightScoreText.text = _rightScore.ToString();
    }

    private void ResetBall(Vector2 direction)
    {
        _ball.anchoredPosition = Vector2.zero;
        _ballSpeed = ballStartSpeed;
        _ballDirection = direction.normalized;
        _launchToLeftAfterScore = (direction.x < 0);
        LaunchBall(direction);
    }

    private void WinGameCheck()
    {
        if (_leftScore >= winGameValue)
        {
            WinGame("Left");
        }
        else if (_rightScore >= winGameValue)
        {
            WinGame("Right");
        }
    }

    private void WinGame(string winner)
    {
        _gameOver = true;
        if (_winScreen != null) _winScreen.SetActive(true);
        if (_winScreenText != null)
            _winScreenText.text = $"{winner} Player Wins!";

        _leftPaddleControl.MovementEnabled = false;
        _rightPaddleControl.MovementEnabled = false;
        _handTrackingCursor.SetActive(_handTrackingActivated);
        _handTrackingCursor2.SetActive(_handTrackingActivated);
    }

    public void StopGame()
    {
        _gameOver = true;

        _leftPaddleControl.MovementEnabled = false;
        _rightPaddleControl.MovementEnabled = false;
    }
    #endregion

    #endregion
}