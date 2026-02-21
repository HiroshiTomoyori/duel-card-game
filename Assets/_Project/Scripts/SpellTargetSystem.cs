using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellTargetSystem : MonoBehaviour
{
    public static SpellTargetSystem I { get; private set; }

    CardController currentSpell;
    Action<CardController> onTargetSelected;

    void Awake()
    {
        I = this;
    }

    // ===== プレイヤー用：クリック選択 =====
    public void StartTargetSelection(CardController spell, Action<CardController> callback)
    {
        currentSpell = spell;
        onTargetSelected = callback;
        Debug.Log("[Spell] Target selection started");
    }

    public bool IsSelecting => currentSpell != null;

    public void TrySelectTarget(CardController target)
    {
        if (!IsSelecting) return;

        if (target.owner != OwnerType.Enemy ||
            target.currentZone != ZoneType.Battle)
        {
            Debug.Log("[Spell] Invalid target");
            return;
        }

        onTargetSelected?.Invoke(target);

        currentSpell = null;
        onTargetSelected = null;

        Debug.Log("[Spell] Target selected");
    }

    public void CancelSelection()
    {
        if (!IsSelecting) return;
        currentSpell = null;
        onTargetSelected = null;
        Debug.Log("[Spell] Target selection cancelled");
    }

    // ===== AI用：候補取得 =====
    public List<CardController> GetValidTargetsForDestroyEnemyBattleOne(OwnerType casterOwner)
    {
        var list = new List<CardController>();

        OwnerType targetOwner = (casterOwner == OwnerType.Player)
            ? OwnerType.Enemy
            : OwnerType.Player;

        var battleCards = ZoneManager.I.GetCards(targetOwner, ZoneType.Battle);
        foreach (var c in battleCards)
        {
            if (c == null) continue;
            list.Add(c);
        }

        return list;
    }
}
