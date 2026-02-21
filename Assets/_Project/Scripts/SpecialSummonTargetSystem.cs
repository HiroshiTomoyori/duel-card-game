using System;
using System.Collections.Generic;
using UnityEngine;

public class SpecialSummonTargetSystem : MonoBehaviour
{
    public static SpecialSummonTargetSystem I { get; private set; }

    public bool IsSelecting => _selecting;
    bool _selecting;

    CardController _king; // 手札のK
    Action<CardController> _onSelected;

    void Awake()
    {
        I = this;
    }

    public void StartSelection(CardController king, Action<CardController> onSelected)
    {
        _king = king;
        _onSelected = onSelected;
        _selecting = true;

        Debug.Log("[SpecialSummon] selecting base... click your 7/8/10 on battle");
    }

    public void Cancel()
    {
        _selecting = false;
        _king = null;
        _onSelected = null;
        Debug.Log("[SpecialSummon] canceled");
    }

    public bool IsValidBase(CardController baseCard)
    {
        if (!_selecting) return false;
        if (_king == null || _king.instance == null) return false;
        if (baseCard == null || baseCard.instance == null) return false;

        // 自分のバトル場のみ
        if (baseCard.currentZone != ZoneType.Battle) return false;
        if (baseCard.owner != OwnerType.Player) return false;

        // 7/8/10のみ（Joker除外）
        if (baseCard.instance.isJoker) return false;
        int r = baseCard.instance.rank;
        return (r == 7 || r == 8 || r == 10);
    }

    public int GetDiffCost(CardController baseCard)
    {
        // K(13)との差分
        int r = baseCard.instance.rank;
        return 13 - r; // 7->6, 8->5, 10->3
    }

    public void TrySelectBase(CardController clicked)
    {
        if (!_selecting) return;
        if (!IsValidBase(clicked))
        {
            Debug.Log("[SpecialSummon] invalid base");
            return;
        }

        _selecting = false;
        _onSelected?.Invoke(clicked);

        _king = null;
        _onSelected = null;
    }
}
