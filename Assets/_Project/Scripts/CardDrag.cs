using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CardController))]
public class CardDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rt;
    Canvas rootCanvas;
    CanvasGroup cg;
    Transform originalParent;
    Vector2 originalPos;

    CardController card;

    DropZone[] allZones;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        card = GetComponent<CardController>();

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = FindFirstObjectByType<Canvas>();

        cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        allZones = FindObjectsOfType<DropZone>(true);
    }

    bool CanDrag()
    {
        if (card.owner != OwnerType.Player) return false;
        if (card.currentZone != ZoneType.Hand) return false;
        if (TurnManager.I != null && !TurnManager.I.isPlayerTurn) return false;

        return true;
    }

    // =========================
    // Drag Start
    // =========================

    public void OnBeginDrag(PointerEventData eventData)
    {
        allZones = FindObjectsOfType<DropZone>(true);
        if (!CanDrag()) return;

        originalParent = transform.parent;
        originalPos = rt.anchoredPosition;

        cg.blocksRaycasts = false;

        // ⭐ 最前面へ（ドラッグ中）
        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        HighlightValidZones(true);
        Debug.Log($"[DragBegin] zones={allZones?.Length}");
    }

    // =========================
    // Dragging
    // =========================

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag()) return;

        rt.position = eventData.position;
    }

    // =========================
    // Drag End
    // =========================

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanDrag())
        {
            Restore();
            return;
        }

        cg.blocksRaycasts = true;

        // ⭐ Raycastを自前で取得（DropHit対策）
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DropZone drop = null;
        GameObject hitGo = null;

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            // ⭐ 自分の子は無視
            if (r.gameObject.transform.IsChildOf(transform))
                continue;

            hitGo = r.gameObject;
            drop = r.gameObject.GetComponentInParent<DropZone>();
            if (drop != null) break;
        }

        Debug.Log($"[DropRaycastAll] hit={(hitGo ? hitGo.name : "NULL")} drop={(drop ? drop.name : "NULL")} results={results.Count}");

        bool moved = false;

        if (drop != null && drop.owner == OwnerType.Player)
            moved = TryMove(drop.zoneType);

        if (!moved)
            Restore();

        HighlightValidZones(false);
    }

    // =========================
    // Move Logic
    // =========================

    bool TryMove(ZoneType toZone)
    {
        Debug.Log($"[TryMove] toZone={toZone} phase={TurnManager.I?.CurrentPhase}");

        if (card.owner != OwnerType.Player) return false;
        if (card.currentZone != ZoneType.Hand) return false;

        // ===== Hand → Mana =====
        if (toZone == ZoneType.Mana)
        {
            if (TurnManager.I != null && !TurnManager.I.CanPlayMana())
                return false;

            bool ok = ZoneManager.I.Move(card, ZoneType.Mana);

            if (ok)
                TurnManager.I?.OnPlayedMana();

            return ok;
        }

        // ===== Hand → Battle =====
        if (toZone == ZoneType.Battle)
        {
            if (TurnManager.I != null && !TurnManager.I.CanSummon())
                return false;

            int cost = card.Cost;

            if (TurnManager.I != null &&
                !TurnManager.I.TrySpendMana(OwnerType.Player, cost))
                return false;

            bool ok = ZoneManager.I.Move(card, ZoneType.Battle);

            if (ok)
            {
                card.SetSummoningSick(true);

                // ⭐ ここが重要：出たカードを最前面に固定
                if (BattleLineLayout.I != null)
                    BattleLineLayout.I.forceFrontCard = card;
            }

            return ok;
        }

        return false;
    }

    // =========================
    // Highlight Zones
    // =========================

    void HighlightValidZones(bool on)
    {
        foreach (var zone in allZones)
        {
            bool valid = false;

            if (card.owner == OwnerType.Player &&
                card.currentZone == ZoneType.Hand &&
                zone.owner == OwnerType.Player)
            {
                if (zone.zoneType == ZoneType.Mana &&
                    TurnManager.I != null &&
                    TurnManager.I.CanPlayMana())
                    valid = true;

                if (zone.zoneType == ZoneType.Battle &&
                    TurnManager.I != null &&
                    TurnManager.I.CanSummon())
                    valid = true;
            }

            zone.Highlight(on && valid);
        }
    }

    // =========================
    // Restore
    // =========================

    void Restore()
    {
        cg.blocksRaycasts = true;

        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            rt.anchoredPosition = originalPos;
        }

        HandFanLayout.I?.Layout();
    }
}