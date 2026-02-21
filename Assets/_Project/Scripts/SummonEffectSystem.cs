using UnityEngine;

public class SummonEffectSystem : MonoBehaviour
{
    public static SummonEffectSystem I { get; private set; }

    void Awake()
    {
        I = this;
    }

    public void OnSummoned(CardController card)
    {
        if (card == null) return;
        if (card.instance == null) return;
        if (card.currentZone != ZoneType.Battle) return;

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

            // 10：相手手札ランダム1枚ハンデス（スペルも含む、墓地へ）
            case OnSummonEffectType.Card10:
                Effect_RandomDiscard(OpponentOf(card.owner));
                return;

            // 13：召喚成功時、山札トップをシールドに追加（回復）
            case OnSummonEffectType.Card13:
                Effect_AddTopToShield(card.owner);
                return;
        }
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

        ManaCountUI.I?.Refresh();
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
    // 10) ランダムハンデス（手札→墓地）
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
    // 13) 山札トップをシールドへ（回復）
    // =========================
    void Effect_AddTopToShield(OwnerType owner)
    {
        Debug.Log("[SummonEffect] Card13: Add top to shield");

        var deck = FindFirstObjectByType<DeckFromSprites>();
        if (deck == null) return;

        var added = deck.DrawTopToZone(owner, ZoneType.Shield);
        if (added == null) return;

        // シールドは裏＆非表示運用（DeckFromSprites側でもやってるが、保険で）
        added.ShowBack(null);          // backSpriteを持ってないなら無理に触らない
        added.gameObject.SetActive(false);

        ShieldCountUI.I?.Refresh();
    }

    static OwnerType OpponentOf(OwnerType o)
        => (o == OwnerType.Player) ? OwnerType.Enemy : OwnerType.Player;
}
