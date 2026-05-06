using UnityEngine;

public class TitlePulse : MonoBehaviour
{
    public float speed = 2f;
    public float amount = 0.05f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * amount;
        transform.localScale = originalScale * scale;
    }
}