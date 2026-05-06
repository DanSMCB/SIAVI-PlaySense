using UnityEngine;
using UnityEngine.UI;

public class BackgroundChanger : MonoBehaviour
{
    public Image background;
    public Color[] colors;

    private int currentIndex = 0;

    void Start()
    {
        ApplyColor();
    }

    public void NextColor()
    {
        currentIndex++;

        if (currentIndex >= colors.Length)
            currentIndex = 0;

        ApplyColor();
    }

    public void PreviousColor()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = colors.Length - 1;

        ApplyColor();
    }

    void ApplyColor()
    {
        background.color = colors[currentIndex];
    }
}
