using UnityEngine;

public class PaddleControl : MonoBehaviour
{
    public enum ControlledHand
    {
        Left,
        Right
    }

    [Header("Hand")]
    [SerializeField] private ControlledHand _controlledHand;
    [SerializeField] private HandPaddleInputBridge _inputBridge;

    [Header("Movement")]
    [SerializeField] private float _followSpeed = 18f;
    [SerializeField] private float _topBoundaryY = 250f;
    [SerializeField] private float _bottomBoundaryY = -250f;
    [SerializeField] private float _verticalOffset = 0f;
    [SerializeField] private bool _invertHandY = true;

    [Header("State")]
    [SerializeField] private bool _isHandActive;

    private RectTransform _rectTransform;

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
        if (_inputBridge == null || _rectTransform == null)
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

    public bool IsHandActive => _isHandActive;
}