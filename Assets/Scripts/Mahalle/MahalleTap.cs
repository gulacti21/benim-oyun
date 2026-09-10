using UnityEngine;
using UnityEngine.EventSystems;

// Dokunulan her şey hafifçe içeri basılır. Butonun tepki verdiği buradan anlaşılır.
public class MahalleTap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressed = .94f;
    private Vector3 normal = Vector3.one;
    private bool captured;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!captured) { normal = transform.localScale; captured = true; }
        transform.localScale = normal * pressed;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (captured) transform.localScale = normal;
    }

    private void OnDisable()
    {
        if (captured) transform.localScale = normal;
    }
}
