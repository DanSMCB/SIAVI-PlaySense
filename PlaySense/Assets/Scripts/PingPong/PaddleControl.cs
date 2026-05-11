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

        HandleKeyboard(); // Always handle

        if (_inputMethod == InputMethodType.HandTracking)
        {
            HandleHandTracking(); // Optional, on top of keyboard
        }
    }

    private void HandleHandTracking()
    {
        if (_inputBridge == null)
            return;

        var handState = _controlledHand == ControlledHand.Left
            ? _inputBridge.LeftHand
            : _inputBridge.RightHand;

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