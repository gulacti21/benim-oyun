using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Harita duragi icin dokunma. Unity'nin Button'i, parmak kaydiktan sonra
// birakilsa bile tiklama sayar; haritada bu yanlislikla bolum secer.
// Burada basma ve birakma noktasi arasindaki mesafe olculur, esigi asarsa
// dokunma sayilmaz ve suruklemeyi ScrollRect devralir.
public class MahalleStopTap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Action onTap;
    public float threshold = 26f;

    private Vector2 down;
    private bool pressed;

    public void OnPointerDown(PointerEventData e)
    {
        down = e.position;
        pressed = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!pressed) return;
        pressed = false;
        if (Vector2.Distance(down, e.position) > threshold) return;
        if (onTap != null) onTap();
    }
}
