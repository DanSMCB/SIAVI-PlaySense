using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonController : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Zero-based position inside ButtonsContainer")]
    public int Index;

    [Header("Colors")]
     public Color neutralColor  = new Color(0.85f, 0.85f, 0.85f);
    public Color coloredColor  = new Color(0.957f, 0.878f, 0.302f);
    public Color hitColor      = new Color(0.306f, 0.349f, 0.549f);
    public Color missedColor   = new Color(0.643f, 0.141f, 0.231f);
    public Color wrongColor    = new Color(0.741f, 0.388f, 0.184f);

    private Button   _button;
    private Image    _image;
    private bool     _isColored;
    private bool     _alreadyTapped;

    void Awake()
    {
        _button = GetComponent<Button>();
        _image  = GetComponent<Image>();
        _button.onClick.AddListener(OnTapped);
    }

    public void SetNeutral()
    {
        _isColored     = false;
        _alreadyTapped = false;
        _button.interactable = true;
        SetColor(neutralColor);
    }

    public void SetColored()
    {
        _isColored = true;
        SetColor(coloredColor);
    }

    public void SetHit()
    {
        _alreadyTapped = true;
        _button.interactable = false;
        SetColor(hitColor);
    }

    public void SetMissed()
    {
        _button.interactable = false;
        SetColor(missedColor);
    }

    public void SetWrong()
    {
        SetColor(wrongColor);
        Invoke(nameof(SetNeutral), 0.4f);
    }

    void OnTapped()
    {
        if (_alreadyTapped) return;
        if (!ReactionGameManager.Instance.RoundActive) return;

        ReactionUIManager.Instance.OnButtonTapped(Index, _isColored);
    }

    void SetColor(Color c)
    {
        if (_image != null) _image.color = c;
 
        ColorBlock cb    = _button.colors;
        cb.normalColor      = c;
        cb.highlightedColor = c;
        cb.pressedColor     = new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f);
        cb.selectedColor    = c;
        cb.disabledColor    = new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, 0.8f);
        cb.colorMultiplier  = 1f;
        _button.colors      = cb;
    }
}