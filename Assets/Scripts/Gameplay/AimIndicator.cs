using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimIndicator : MonoBehaviour
{
    [SerializeField] private float maxLineLength = 3f;
    [SerializeField] private float heightOffset = .05f;
    private LineRenderer line;
    private GameObject contact;
    private readonly RaycastHit[] hits = new RaycastHit[32];
    private void Awake()
    {
        line = GetComponent<LineRenderer>(); line.positionCount = 2; line.useWorldSpace = true;
        contact = GameObject.CreatePrimitive(PrimitiveType.Sphere); contact.name = "İlk temas";
        contact.transform.SetParent(transform); Destroy(contact.GetComponent<Collider>());
        contact.transform.localScale = Vector3.one * .2f;
        contact.GetComponent<Renderer>().sharedMaterial = line.sharedMaterial;
        Hide();
    }
    public void Show(Vector3 origin, Vector3 direction, float power, bool guide = false, Collider shooter = null)
    {
        line.enabled = power > .01f;
        float length = guide ? 12f : maxLineLength * power;
        Vector3 end = origin + direction * length;
        bool found = false;
        if (guide && power > .01f)
        {
            float closest = length;
            float radius = shooter != null ? shooter.bounds.extents.x : .25f;
            int count = Physics.SphereCastNonAlloc(origin, radius * .96f, direction, hits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == shooter || hit.collider.gameObject.name == "Ground" || hit.distance <= .01f) continue;
                if (hit.distance < closest) { closest = hit.distance; end = origin + direction * closest; found = true; }
            }
        }
        line.SetPosition(0, origin + Vector3.up * heightOffset); line.SetPosition(1, end + Vector3.up * heightOffset);
        Color color = Color.Lerp(new Color(.22f,.84f,.72f), new Color(1f,.66f,.2f), power);
        line.startColor = color; line.endColor = new Color(color.r,color.g,color.b,.55f);
        contact.SetActive(found); contact.transform.position = end + Vector3.up * .08f;
    }
    public void Hide() { if (line != null) line.enabled = false; if (contact != null) contact.SetActive(false); }
}
