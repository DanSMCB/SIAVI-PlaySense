using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float speed = 2f;

    void Start()
    {
        canvasGroup.alpha = 0;
    }

    void Update()
    {
        if (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha += Time.deltaTime * speed;
        }
    }
}