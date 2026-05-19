using UnityEngine;

public class PaddleControl : MonoBehaviour
{
    public enum ControlledHand { Left, Right }
    public enum InputMethodType { HandTracking, Keyboard }

    [Header("Hand")]
    [SerializeField] private ControlledHand _controlledHand;
    [SerializeField] private HandPaddleInputBridge _inputBridge;

    [Header("Movement")]
    [SerializeField] private float _followSpeed = 18f;
    [SerializeField] private float _topBoundaryY = 250f;
    [SerializeField] private float _bottomBoundaryY = -250f;
    [SerializeField] private float _verticalOffset = 0f;
    [SerializeField] private bool _invertHandY = true;
    [SerializeField] private float _keyboardSpeed = 600f; // units per second

    [Header("State")]
    [SerializeField] private bool _isHandActive;
    [SerializeField] private InputMethodType _inputMethod = InputMethodType.HandTracking; // Default to hand tracking
    [SerializeField] private bool _movementEnabled = false;

    // --- BOT ---
    [Header("Bot")]
    [SerializeField] private bool _botEnabled = false; // runtime-controlled by HandPaddleInputBridge

    private RectTransform _rectTransform;

    public InputMethodType InputMethod
    {
        get => _inputMethod;
        set => _inputMethod = value;
    }

    public bool MovementEnabled
    {
        get => _movementEnabled;
        set => _movementEnabled = value;
    }

    public bool IsHandActive => _isHandActive;

    public bool BotEnabled
    {
        get => _botEnabled;
        set => _botEnabled = value;
    }

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_inputBridge == null)
        {
            _inputBridge = FindFirstObjectByType<HandPaddleInputBridge>();
        }
    }

    private void Update()
    {
        if (!_movementEnabled || _rectTransform == null)
            return;

        // If bot is enabled, DO NOT allow keyboard or hand tracking to affect this paddle.
        if (_botEnabled)
            return;

        HandleKeyboard(); 

        if (_inputMethod == InputMethodType.HandTracking)
        {
            HandleHandTracking();
        }
    }

    public void BotStepToBallY(float ballY)
    {
        if (!_movementEnabled || _rectTransform == null)
            return;

        float currentY = _rectTransform.anchoredPosition.y;
        float step = _keyboardSpeed * Time.deltaTime;
        float newY = Mathf.MoveTowards(currentY, ballY, step);

        newY = Mathf.Clamp(newY, _bottomBoundaryY, _topBoundaryY);

        var pos = _rectTransform.anchoredPosition;
        pos.y = newY;
        _rectTransform.anchoredPosition = pos;
    }

    private void HandleHandTracking()
    {
        if (_inputBridge == null)
            return;

        HandPaddleInputBridge.HandState handState;

        if (_controlledHand == ControlledHand.Left)
        {
            if (_inputBridge.BotModeActive && _inputBridge.RightHand.isActive)
                handState = _inputBridge.RightHand;

            else
                handState = _inputBridge.LeftHand;
        }
        else
        {
            handState = _inputBridge.RightHand;
        }

        _isHandActive = handState.isTracked;

        if (!handState.isTracked)
            return;

        float normalizedY = handState.normalizedY;
        if (_invertHandY)
            normalizedY = 1f - normalizedY;

        float targetY = Mathf.Lerp(_bottomBoundaryY, _topBoundaryY, normalizedY);
        targetY += _verticalOffset;
        targetY = Mathf.Clamp(targetY, _bottomBoundaryY, _topBoundaryY);

        var pos = _rectTransform.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * _followSpeed);
        _rectTransform.anchoredPosition = pos;
    }

    private void HandleKeyboard()
    {
        float input = 0f;
        if (_controlledHand == ControlledHand.Right)
        {
            if (Input.GetKey(KeyCode.W))
                input += 1f;
            if (Input.GetKey(KeyCode.S))
                input -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow))
                input += 1f;
            if (Input.GetKey(KeyCode.DownArrow))
                input -= 1f;
            if (_inputBridge.BotModeActive)
            {
                if (Input.GetKey(KeyCode.W))
                    input += 1f;
                if (Input.GetKey(KeyCode.S))
                    input -= 1f;
            }
        }

        if (input != 0f)
        {
            var pos = _rectTransform.anchoredPosition;
            pos.y += input * _keyboardSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, _bottomBoundaryY, _topBoundaryY);
            _rectTransform.anchoredPosition = pos;
        }
    }
}