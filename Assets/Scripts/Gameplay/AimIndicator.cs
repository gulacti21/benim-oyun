using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimIndicator : MonoBehaviour
{
    [SerializeField] private float maxLineLength = 3f;
    [SerializeField] private float heightOffset = .05f;
    // USTA GOZU: carpilan misketin gidecegi yonu gosteren ikinci cizginin boyu.
    [SerializeField] private float afterLength = 2.2f;
    private LineRenderer line;
    private LineRenderer after;      // carpma sonrasi yon
    private GameObject contact;
    private readonly RaycastHit[] hits = new RaycastHit[32];

    private void Awake()
    {
        line = GetComponent<LineRenderer>(); line.positionCount = 2; line.useWorldSpace = true;

        contact = GameObject.CreatePrimitive(PrimitiveType.Sphere); contact.name = "İlk temas";
        contact.transform.SetParent(transform); Destroy(contact.GetComponent<Collider>());
        contact.transform.localScale = Vector3.one * .2f;
        contact.GetComponent<Renderer>().sharedMaterial = line.sharedMaterial;

        // Carpma sonrasi cizgisi ayri bir LineRenderer; ana cizgiyle ayni malzeme,
        // biraz daha ince, boylece kilavuz cizgisinden ayirt edilir.
        var go = new GameObject("Çarpma sonrası yön");
        go.transform.SetParent(transform, false);
        after = go.AddComponent<LineRenderer>();
        after.positionCount = 2; after.useWorldSpace = true;
        after.sharedMaterial = line.sharedMaterial;
        after.widthMultiplier = line.widthMultiplier * .72f;
        after.numCapVertices = line.numCapVertices;
        after.sortingOrder = line.sortingOrder;
        after.enabled = false;

        Hide();
    }

    public void Show(Vector3 origin, Vector3 direction, float power, bool guide = false, Collider shooter = null)
    {
        line.enabled = power > .01f;
        float length = guide ? 12f : maxLineLength * power;
        Vector3 end = origin + direction * length;
        bool found = false;
        RaycastHit best = default;

        if (guide && power > .01f)
        {
            float closest = length;
            float radius = shooter != null ? shooter.bounds.extents.x : .25f;
            int count = Physics.SphereCastNonAlloc(origin, radius * .96f, direction, hits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == shooter || hit.collider.gameObject.name == "Ground" || hit.distance <= .01f) continue;
                if (hit.distance < closest) { closest = hit.distance; end = origin + direction * closest; found = true; best = hit; }
            }
        }

        line.SetPosition(0, origin + Vector3.up * heightOffset); line.SetPosition(1, end + Vector3.up * heightOffset);
        Color color = Color.Lerp(new Color(.22f,.84f,.72f), new Color(1f,.66f,.2f), power);
        line.startColor = color; line.endColor = new Color(color.r,color.g,color.b,.55f);
        contact.SetActive(found); contact.transform.position = end + Vector3.up * .08f;

        ShowAfter(found, end, direction, best);
    }

    // Carpilan misketin (ya da duvarin) ardindan cikacak yonu cizer.
    private void ShowAfter(bool found, Vector3 end, Vector3 direction, RaycastHit hit)
    {
        if (!found || hit.collider == null) { if (after != null) after.enabled = false; return; }

        Vector3 from, dir;
        if (hit.collider.attachedRigidbody != null)
        {
            // Misket: iki kurenin merkezleri arasindaki dogrultuda firlar.
            Vector3 target = hit.collider.attachedRigidbody.position;
            dir = target - end; dir.y = 0f;
            if (dir.sqrMagnitude < .0001f) { after.enabled = false; return; }
            dir.Normalize();
            from = target;
        }
        else
        {
            // Duvar ya da engel: misket yuzeyden seker.
            Vector3 n = hit.normal; n.y = 0f;
            if (n.sqrMagnitude < .0001f) { after.enabled = false; return; }
            dir = Vector3.Reflect(direction, n.normalized); dir.y = 0f;
            if (dir.sqrMagnitude < .0001f) { after.enabled = false; return; }
            dir.Normalize();
            from = hit.point;
        }

        from.y = end.y;
        after.enabled = true;
        after.SetPosition(0, from + Vector3.up * heightOffset);
        after.SetPosition(1, from + dir * afterLength + Vector3.up * heightOffset);
        var head = new Color(1f, .95f, .82f, .95f);
        after.startColor = head;
        after.endColor = new Color(head.r, head.g, head.b, .18f);
    }

    public void Hide()
    {
        if (line != null) line.enabled = false;
        if (after != null) after.enabled = false;
        if (contact != null) contact.SetActive(false);
    }
}
