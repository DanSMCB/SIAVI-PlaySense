using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 targetScale;
    public float speed = 10f;

    void Start()
    {
        targetScale = Vector3.one;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = new Vector3(1.1f, 1.1f, 1);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }
}