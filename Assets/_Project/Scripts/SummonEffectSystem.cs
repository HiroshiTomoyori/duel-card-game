using UnityEngine;
using System.Collections.Generic;

public class SummonEffectSystem : MonoBehaviour
{
    public static SummonEffectSystem I { get; private set; }

    void Awake()
    {
        I = this;
    }

    public void OnSummoned(CardController card)
    {
        Debug.Log($"[OnSummoned] name={card.name} zone={card.currentZone} isJoker={card.instance?.isJoker} type={card.instance?.type}");
        if (card == null) return;
        if (card.instance == null) return;
        if (card.currentZone != ZoneType.Battle) return;

        // =========================
        // ★ Joker 最優先処理
        // =========================
        if (card.instance.isJoker)
        {
            Effect_Joker_WipeAllExceptSelf(card);
            return; // 他の効果は処理しない
        }

        // =========================
        // 通常カード効果
        // =========================
        switch (card.instance.onSummonEffect)
        {
            case OnSummonEffectType.None:
                return;

            // 5：山札トップをマナにチャージ
            case OnSummonEffectType.Card5:
                Effect_ChargeTopToMana(card.owner);
                return;

            // 6：1ドロー
            case OnSummonEffectType.Card6:
                Effect_Draw(card.owner, 1);
                return;

            // 10：相手手札ランダム1枚ハンデス
            case OnSummonEffectType.Card10:
                Effect_RandomDiscard(OpponentOf(card.owner));
                return;

            // 13：山札トップをシールドに追加（回復）
            case OnSummonEffectType.Card13:
                Effect_AddTopToShield(card.owner);
                return;
        }
    }

    // =====================================================
    // 🔥 Joker：バトル全破壊（自分以外）
    // =====================================================
    void Effect_Joker_WipeAllExceptSelf(CardController joker)
    {
        Debug.Log($"[JokerSummon] START joker={joker.name} owner={joker.owner}");
        Debug.Log("[SummonEffect] Joker: wipe all except self");

        var zm = ZoneManager.I;
        if (zm == null) return;

        foreach (OwnerType owner in System.Enum.GetValues(typeof(OwnerType)))
        {
            var battle = zm.GetCards(owner, ZoneType.Battle);
            if (battle == null) continue;

            // ★直接回すと壊れるのでコピー
            var copy = new List<CardController>(battle);

            foreach (var c in copy)
            {
                if (c == null) continue;
                if (c == joker) continue;

                zm.SendToGrave(c);
            }
        }

        Debug.Log("[SummonEffect] Joker resolved: only joker remains");
    }

    // =========================
    // 5) 山札トップをマナへ
    // =========================
    void Effect_ChargeTopToMana(OwnerType owner)
    {
        Debug.Log("[SummonEffect] Card5: Charge top to mana");

        var deck = FindFirstObjectByType<DeckFromSprites>();
        if (deck == null) return;

        deck.DrawTopToZone(owner, ZoneType.Mana);

        ManaCountUI.RefreshOwner(owner);
        if (owner == OwnerType.Player) HandFanLayout.I?.Layout();
        else EnemyHandCountUI.I?.Refresh();
    }

    // =========================
    // 6) ドロー
    // =========================
    void Effect_Draw(OwnerType owner, int n)
    {
        Debug.Log($"[SummonEffect] Card6: Draw {n}");

        var deck = FindFirstObjectByType<DeckFromSprites>();
        if (deck == null) return;

        deck.DrawTo(owner, n);

        if (owner == OwnerType.Player) HandFanLayout.I?.Layout();
        else EnemyHandCountUI.I?.Refresh();
    }

    // =========================
    // 10) ランダムハンデス
    // =========================
    void Effect_RandomDiscard(OwnerType targetOwner)
    {
        Debug.Log("[SummonEffect] Card10: Random discard 1");

        var handCards = ZoneManager.I.GetCards(targetOwner, ZoneType.Hand);
        if (handCards == null || handCards.Count == 0) return;

        int idx = Random.Range(0, handCards.Count);
        var target = handCards[idx];
        if (target == null) return;

        ZoneManager.I.SendToGrave(target);

        if (targetOwner == OwnerType.Player) HandFanLayout.I?.Layout();
        else EnemyHandCountUI.I?.Refresh();
    }

    // =========================
    // 13) 山札トップをシールドへ
    // =========================
    void Effect_AddTopToShield(OwnerType owner)
    {
        Debug.Log("[SummonEffect] Card13: Add top to shield");

        var deck = FindFirstObjectByType<DeckFromSprites>();
        if (deck == null) return;

        var added = deck.DrawTopToZone(owner, ZoneType.Shield);
        if (added == null) return;

        added.ShowBack(null);
        added.gameObject.SetActive(false);

        ShieldCountUI.I?.Refresh();
    }

    static OwnerType OpponentOf(OwnerType o)
        => (o == OwnerType.Player) ? OwnerType.Enemy : OwnerType.Player;
}