using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ShieldRowClickPicker : MonoBehaviour, IPointerClickHandler
{
    public OwnerType rowOwner; // Inspectorで Enemy / Player

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[ShieldRowClickPicker] clicked row={rowOwner}");

        if (ShieldBreakInput.I == null) return;
        if (!ShieldBreakInput.I.IsSelecting) return;
        if (ShieldBreakInput.I.TargetOwner != rowOwner) return;

        // ✅ IReadOnlyList で受ける
        IReadOnlyList<CardController> shields =
            ZoneManager.I.GetCards(rowOwner, ZoneType.Shield);

        if (shields == null || shields.Count == 0) return;

        // とりあえず左端（0番目）を選択
        CardController target = shields[0];
        if (target == null) return;

        ShieldBreakInput.I.OnShieldClicked(target);
    }
}