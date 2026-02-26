using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI I { get; private set; }

    [Header("Refs")]
    public DeckFromSprites deck;

    [Header("Timings")]
    public float thinkDelay = 0.4f;

    void Awake()
    {
        I = this;
        if (!deck) deck = FindFirstObjectByType<DeckFromSprites>();
    }

    public void TakeTurn()
    {
        StartCoroutine(TakeTurnRoutine());
    }

    IEnumerator TakeTurnRoutine()
    {
        // ✅ ターン開始：アンタップ / 召喚酔い解除 / 1ターン1回攻撃リセット
        //BattleManager.I?.OnTurnStart(OwnerType.Enemy);

        // 1) ドロー
        yield return new WaitForSeconds(thinkDelay);
        //deck?.DrawTo(OwnerType.Enemy, 1);
        EnemyHandCountUI.I?.Refresh();

        // 2) マナ置き（手札の先頭を1枚）
        yield return new WaitForSeconds(thinkDelay);
        TryPlayMana();

        // 3) スペル（唱えられるなら1回）
        yield return new WaitForSeconds(thinkDelay);
        TryCastBestSpell();

        // 4) 召喚（出せる中でコスト最大を1枚）※スペルは除外
        yield return new WaitForSeconds(thinkDelay);
        TrySummonBest();

        // 5) 攻撃（バトルゾーンの全員で攻撃）
        yield return new WaitForSeconds(thinkDelay);
        Debug.Log("[EnemyAI] Before TryAttack");
        TryAttack();
        Debug.Log("[EnemyAI] After TryAttack");

        // 6) ターン終了（プレイヤーへ）
        yield return new WaitForSeconds(thinkDelay);
        Debug.Log("[EnemyAI] Calling EndTurnFromEnemy now");
        TurnManager.I?.EndTurnFromEnemy();
    }

    void TryPlayMana()
    {
        if (TurnManager.I == null || TurnManager.I.isPlayerTurn) return;
        if (TurnManager.I.hasPlayedManaThisTurn) return;

        var handAnchor = ZoneManager.I.GetAnchor(OwnerType.Enemy, ZoneType.Hand);
        if (!handAnchor) return;

        CardController first = null;
        foreach (Transform ch in handAnchor)
        {
            var cc = ch.GetComponent<CardController>();
            if (cc) { first = cc; break; }
        }
        if (!first) return;

        bool ok = ZoneManager.I.Move(first, ZoneType.Mana);
        if (ok)
        {
            first.gameObject.SetActive(false);

            //TurnManager.I.OnPlayedMana(); // ※このままだと「プレイヤー用フラグ」を立てる可能性あり（後述）
            ManaCountUI.RefreshOwner(OwnerType.Enemy);
            EnemyHandCountUI.I?.Refresh();
            Debug.Log("[EnemyAI] Played mana");
        }
    }

    // ✅ AIが唱えられるスペルを1回だけ唱える（今は DestroyEnemyBattleOne のみ対応）
    bool TryCastBestSpell()
    {
        if (TurnManager.I == null || TurnManager.I.isPlayerTurn) return false;
        if (SpellTargetSystem.I == null) { Debug.LogWarning("[EnemyAI] SpellTargetSystem missing"); return false; }

        int mana = CountCards(OwnerType.Enemy, ZoneType.Mana);

        var handAnchor = ZoneManager.I.GetAnchor(OwnerType.Enemy, ZoneType.Hand);
        if (!handAnchor) return false;

        CardController bestSpell = null;
        int bestCost = -1;

        foreach (Transform ch in handAnchor)
        {
            var cc = ch.GetComponent<CardController>();
            if (!cc) continue;
            if (!cc.IsSpell) continue;

            int cost = cc.Cost;
            if (cost <= mana && cost > bestCost)
            {
                bestSpell = cc;
                bestCost = cost;
            }
        }

        if (!bestSpell) return false;
        if (bestSpell.instance == null) return false;

        // ✅ スペルのマナ支払い（敵もタップ消費する）
        if (!TurnManager.I.TrySpendMana(OwnerType.Enemy, bestSpell.Cost))
        {
            Debug.Log("[EnemyAI] Not enough available mana for spell");
            return false;
        }

        switch (bestSpell.instance.spellEffect)
        {
            case SpellEffectType.DestroyEnemyBattleOne:
            {
                var candidates = SpellTargetSystem.I.GetValidTargetsForDestroyEnemyBattleOne(bestSpell.owner);
                if (candidates == null || candidates.Count == 0)
                {
                    Debug.Log("[EnemyAI] Spell has no valid targets");
                    return false;
                }

                // 選び方：コスト最大（=Power最大）を破壊
                CardController target = ChooseHighestCost(candidates);
                if (target == null) return false;

                ZoneManager.I.SendToGrave(target);     // 対象破壊
                ZoneManager.I.SendToGrave(bestSpell);  // スペル消費

                EnemyHandCountUI.I?.Refresh();
                Debug.Log($"[EnemyAI] Cast spell cost={bestSpell.Cost} targetCost={target.Cost}");
                return true;
            }

            case SpellEffectType.TapAllEnemyBattle:
            {
                OwnerType enemy =
                    bestSpell.owner == OwnerType.Player
                        ? OwnerType.Enemy
                        : OwnerType.Player;

                var targets = ZoneManager.I.GetCards(enemy, ZoneType.Battle);

                foreach (var t in targets)
                {
                    if (t == null) continue;
                    t.SetTapped(true);
                }

                ZoneManager.I.SendToGrave(bestSpell);
                EnemyHandCountUI.I?.Refresh();

                Debug.Log("[EnemyAI] Cast TapAllEnemyBattle");
                return true;
            }


            default:
                return false;
        }
    }

    CardController ChooseHighestCost(List<CardController> list)
    {
        CardController best = null;
        int bestCost = -1;

        foreach (var c in list)
        {
            if (c == null) continue;
            if (c.Cost > bestCost)
            {
                best = c;
                bestCost = c.Cost;
            }
        }
        return best;
    }

    void TrySummonBest()
    {
        if (TurnManager.I == null || TurnManager.I.isPlayerTurn) return;

        int mana = CountCards(OwnerType.Enemy, ZoneType.Mana);

        var handAnchor = ZoneManager.I.GetAnchor(OwnerType.Enemy, ZoneType.Hand);
        if (!handAnchor) return;

        CardController best = null;
        int bestCost = -1;

        foreach (Transform ch in handAnchor)
        {
            var cc = ch.GetComponent<CardController>();
            if (!cc) continue;

            if (cc.IsSpell) continue; // ✅ スペルは召喚しない

            int cost = cc.Cost;
            if (cost <= mana && cost > bestCost)
            {
                best = cc;
                bestCost = cost;
            }
        }

        if (!best) { Debug.Log("[EnemyAI] No summonable card"); return; }

        // ✅ 召喚のマナ支払い（敵もタップ消費する）
        if (!TurnManager.I.TrySpendMana(OwnerType.Enemy, best.Cost))
        {
            Debug.Log("[EnemyAI] Not enough available mana for summon");
            return;
        }

        bool ok = ZoneManager.I.Move(best, ZoneType.Battle);
        if (ok)
        {
            best.transform.localRotation = Quaternion.identity;
            best.gameObject.SetActive(true);
            best.ShowFront();
            EnemyHandCountUI.I?.Refresh();

            // ✅ 召喚酔い：能力ベースで統一
            bool hasSummonSickness = !best.IgnoreSummonSickness;
            best.SetSummoningSick(hasSummonSickness);

            best.SetTapped(false);

            Debug.Log($"[EnemyAI] Summoned cost={bestCost} mana={mana}");
        }
    }

    int CountCards(OwnerType owner, ZoneType zone)
    {
        var anchor = ZoneManager.I.GetAnchor(owner, zone);
        if (!anchor) return 0;

        int c = 0;
        foreach (Transform ch in anchor)
            if (ch.GetComponent<CardController>() != null)
                c++;

        return c;
    }

    void TryAttack()
    {
        var battle = ZoneManager.I.GetCards(OwnerType.Enemy, ZoneType.Battle);

        foreach (var c in battle)
        {
            if (c == null) continue;
            if (c.SummoningSick) continue;
            if (c.IsTapped) continue;

            BattleManager.I.DeclareAttack(c);
        }
    }
}
