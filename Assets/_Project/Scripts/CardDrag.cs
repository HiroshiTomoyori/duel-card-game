using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rect;
    Canvas canvas;
    Transform originalParent;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (!rect)
            Debug.LogError("[CardDrag] RectTransform missing. Attach CardDrag to a UI object.");

        canvas = GetComponentInParent<Canvas>();
        if (!canvas)
        {
            // 親から取れない場合はシーン全体から拾う（最前面のCanvasを優先）
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (!canvas)
            Debug.LogError("[CardDrag] Canvas not found in parents or scene.");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!rect || !canvas)
        {
            Debug.LogError($"[CardDrag] BeginDrag blocked. rect={(rect ? "OK" : "NULL")} canvas={(canvas ? canvas.name : "NULL")}");
            return;
        }

        originalParent = transform.parent;
        transform.SetParent(canvas.transform, true); // worldPositionStays=true
        rect.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!rect) return;
        rect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalParent == null)
        {
            Debug.LogWarning("[CardDrag] originalParent missing.");
            return;
        }

        var go = eventData.pointerCurrentRaycast.gameObject;
        Zone dropZone = go ? go.GetComponentInParent<Zone>() : null;

        bool accepted = false;

        if (dropZone != null && dropZone.isPlayer)
        {
            Zone fromZone = originalParent.GetComponentInParent<Zone>();

            if (fromZone != null &&
                fromZone.zoneType == ZoneType.Hand &&
                dropZone.zoneType == ZoneType.Mana)
            {
                if (TurnManager.I != null && TurnManager.I.CanPlayMana())
                {
                    transform.SetParent(dropZone.transform, true);
                    TurnManager.I.OnPlayedMana();
                    accepted = true;
                }
            }
        }

        if (!accepted)
        {
            transform.SetParent(originalParent, true);
        }
    }
}