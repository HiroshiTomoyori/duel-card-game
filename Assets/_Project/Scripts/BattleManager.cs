using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager I { get; private set; }

    void Awake()
    {
        I = this;
    }

    // =========================
    // ターン開始時に呼ぶ（重要）
    // - 自分のバトルゾーンをアンタップ
    // - 召喚酔い解除
    // =========================
    public void OnTurnStart(OwnerType turnOwner)
    {
        var battle = ZoneManager.I.GetCards(turnOwner, ZoneType.Battle);
        foreach (var c in battle)
        {
            if (c == null) continue;
            c.SetTapped(false);
            c.SetSummoningSick(false);
        }
    }

    // =========================
    // 攻撃可能判定（各カード1回＝タップ管理）
    // =========================
    bool CanAttack(CardController attacker)
    {
        if (attacker == null) return false;

        int cost = attacker.Cost;

        // ✅ コスト2,3,8は攻撃不可
        if (cost == 2 || cost == 3 || cost == 8)
            return false;

        // 召喚酔い中は攻撃不可
        if (attacker.SummoningSick) return false;

        // タップ中は攻撃不可（＝そのターン既に攻撃した）
        if (attacker.IsTapped) return false;

        if (!attacker.CanAttackBase) return false;

        return true;
    }

    // =========================
    // 公開API：攻撃宣言
    // attackerIsEnemy:
    //   true  = 敵が攻撃
    //   false = プレイヤーが攻撃
    // =========================
    public void DeclareAttack(CardController attacker, bool attackerIsEnemy)
    {
        if (attacker == null) return;

        // ✅ 攻撃可能チェック
        if (!CanAttack(attacker))
        {
            Debug.Log($"[Battle] Attack denied tapped={attacker.IsTapped} sick={attacker.SummoningSick}");
            return;
        }

        Debug.Log($"[Battle] StartCoroutine DeclareAttackRoutine attackerIsEnemy={attackerIsEnemy}");
        StartCoroutine(DeclareAttackRoutine(attacker, attackerIsEnemy));
    }

    // =========================
    // 攻撃処理本体（待てる形）
    // =========================
    IEnumerator DeclareAttackRoutine(CardController attacker, bool attackerIsEnemy)
    {
        // ✅ 攻撃確定：このカードをタップ（＝このターン再攻撃不可）
        attacker.SetTapped(true);

        // 防御側のOwner
        var defenderOwner = attackerIsEnemy ? OwnerType.Player : OwnerType.Enemy;

        // 防御側バトルゾーンのブロッカー候補
        var blockers = FindBlockers(defenderOwner);

        CardController chosenBlocker = null;

        Debug.Log($"[Battle] defender={defenderOwner} blockers={blockers.Count}");

        // 敵が攻撃してきたときだけ、プレイヤーが手動でブロックを選ぶ
        if (attackerIsEnemy && blockers.Count > 0)
        {
            Debug.Log("[Battle] Need player block choice");

            if (BlockSelectUI.I == null)
            {
                Debug.LogError("[Battle] BlockSelectUI.I is NULL (sceneに置けてない/ Awake未実行)");
            }
            else
            {
                Debug.Log("[Battle] Calling BlockSelectUI.ChooseBlocker");
                yield return StartCoroutine(BlockSelectUI.I.ChooseBlocker(blockers, b => chosenBlocker = b));
                Debug.Log("[Battle] Returned from ChooseBlocker, chosen=" + (chosenBlocker ? chosenBlocker.name : "null"));
            }
        }
        else
        {
            Debug.Log($"[Battle] Skip choice attackerIsEnemy={attackerIsEnemy} blockers={blockers.Count}");
            chosenBlocker = AutoChooseBlocker(blockers);
        }

        // 解決
        if (chosenBlocker != null)
        {
            ResolveBlock(attacker, chosenBlocker);
        }
        else
        {
            bool isJoker = (attacker.instance != null && attacker.instance.isJoker);

            // 12/13は2枚ブレイク（ジョーカーは除外）
            int breakCount = (!isJoker && (attacker.Cost == 12 || attacker.Cost == 13)) ? 2 : 1;

            // クリックで割るフェイズへ移行（自動ブレイクはしない）
            if (ShieldBreakInput.I != null)
            {
                Debug.Log($"[Battle] Enter shield select: target={defenderOwner} count={breakCount}");
                ShieldBreakInput.I.BeginSelect(defenderOwner, breakCount);
                // ✅ 発光ON/OFFは ShieldBreakInput 側に一元化（ここでは触らない）
            }
            else
            {
                Debug.LogWarning("[Battle] ShieldBreakInput.I is NULL -> fallback auto break");
                BreakOneShield(defenderOwner, breakCount);
            }
        }

        yield break;
    }

    // =========================
    // ブロッカー候補抽出
    // =========================
    List<CardController> FindBlockers(OwnerType defenderOwner)
    {
        var cards = ZoneManager.I.GetCards(defenderOwner, ZoneType.Battle);

        var result = new List<CardController>();
        foreach (var c in cards)
        {
            if (c == null) continue;
            if (!c.IsBlocker) continue;

            // タップ中のカードはブロック不可（ルールは後で調整可）
            if (c.IsTapped) continue;

            result.Add(c);
        }
        return result;
    }

    CardController AutoChooseBlocker(List<CardController> blockers)
    {
        if (blockers == null || blockers.Count == 0) return null;
        return blockers[0];
    }

    // =========================
    // 特例勝敗（11 / Joker）
    // =========================
    enum MatchupResult { None, Win, Lose }

    MatchupResult GetSpecialMatchup(CardController self, CardController other)
    {
        if (self == null || other == null) return MatchupResult.None;

        int selfCost = self.Cost;
        int otherCost = other.Cost;

        bool selfIsJoker = IsJoker(self);
        bool otherIsJoker = IsJoker(other);

        // ジョーカー：3,4,11に無条件敗北、それ以外に無条件勝利
        if (selfIsJoker)
        {
            if (otherCost == 3 || otherCost == 4 || otherCost == 11) return MatchupResult.Lose;
            return MatchupResult.Win;
        }

        // 11：ジョーカー,12,13に無条件勝利。ただし7,8,10に無条件敗北
        if (selfCost == 11)
        {
            if (otherIsJoker || otherCost == 12 || otherCost == 13) return MatchupResult.Win;
            if (otherCost == 7 || otherCost == 8 || otherCost == 10) return MatchupResult.Lose;
        }

        return MatchupResult.None;
    }

    bool IsJoker(CardController c)
    {
        if (c == null) return false;

        // CardInstance側に isJoker を持っているなら優先
        if (c.instance != null && c.instance.isJoker) return true;

        var sp = c.instance != null ? c.instance.sprite : null;
        string n = sp ? sp.name.ToLower() : "";
        return n.StartsWith("joker") || n.StartsWith("j");
    }

    // =========================
    // ブロック戦闘解決
    // =========================
    void ResolveBlock(CardController attacker, CardController blocker)
    {
        if (attacker == null || blocker == null) return;

        // ✅ ブロックした時点でブロッカーはタップ
        blocker.SetTapped(true);

        // ✅ 特例勝敗（11 / Joker）を優先
        var aSp = GetSpecialMatchup(attacker, blocker);
        var bSp = GetSpecialMatchup(blocker, attacker);

        if (aSp == MatchupResult.Win || bSp == MatchupResult.Lose)
        {
            RemoveFromBattleAndDestroy(blocker);
            return;
        }
        if (aSp == MatchupResult.Lose || bSp == MatchupResult.Win)
        {
            RemoveFromBattleAndDestroy(attacker);
            return;
        }

        // ✅ 通常パワー比較（パワー＝コスト）
        int atk = attacker.Power;
        int blk = blocker.Power;

        if (atk > blk)
        {
            RemoveFromBattleAndDestroy(blocker);
        }
        else if (atk < blk)
        {
            RemoveFromBattleAndDestroy(attacker);
        }
        else
        {
            RemoveFromBattleAndDestroy(attacker);
            RemoveFromBattleAndDestroy(blocker);
        }
    }

    // =========================
    // シールド破壊（複数対応）※フォールバック用
    // =========================
    void BreakOneShield(OwnerType defenderOwner, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var shields = ZoneManager.I.GetCards(defenderOwner, ZoneType.Shield);
            if (shields == null || shields.Count <= 0)
            {
                Debug.Log($"[Battle] {defenderOwner} has no shields");
                break;
            }

            CardController target = null;
            foreach (var s in shields)
            {
                if (s != null) { target = s; break; }
            }
            if (target == null) break;

            if (ShieldTriggerSystem.I != null)
            {
                ShieldTriggerSystem.I.OnShieldBroken(target, defenderOwner);
            }
            else
            {
                ZoneManager.I.Move(target, ZoneType.Hand);
            }
        }

        ShieldCountUI.I?.Refresh();
    }

    // =========================
    // 破壊ユーティリティ
    // =========================
    void RemoveFromBattleAndDestroy(CardController card)
    {
        if (card == null) return;
        ZoneManager.I.SendToGrave(card);
    }

    void RemoveFromShieldAndDestroy(CardController card)
    {
        if (card == null) return;
        ZoneManager.I.SendToGrave(card);
    }

    // 既存の呼び出し口（ownerから敵味方判定する版）
    public void DeclareAttack(CardController attacker)
    {
        if (attacker == null) return;
        bool attackerIsEnemy = (attacker.owner == OwnerType.Enemy);
        Debug.Log($"[Battle] DeclareAttack called attacker={attacker.name} attackerIsEnemy={attackerIsEnemy}");
        DeclareAttack(attacker, attackerIsEnemy);
    }

    IEnumerator RefreshShieldNextFrame()
    {
        yield return null; // 1フレーム待つ（Destroy反映待ち）
        ShieldCountUI.I?.Refresh();
    }

    // =========================
    // UI用：攻撃ボタン表示判定
    // =========================
    public bool CanAttackFromUI(CardController attacker)
    {
        if (attacker == null) return false;
        if (attacker.owner != OwnerType.Player) return false;

        int cost = attacker.Cost;

        // ✅ 攻撃不可コスト
        if (cost == 2 || cost == 3 || cost == 8)
            return false;

        if (attacker.SummoningSick) return false;
        if (attacker.IsTapped) return false;

        return true;
    }

    public void BreakSpecificShield(CardController target)
    {
        if (target == null) return;

        var defenderOwner = target.owner;

        // トリガー処理を既存の流れで呼ぶ
        if (ShieldTriggerSystem.I != null)
        {
            ShieldTriggerSystem.I.OnShieldBroken(target, defenderOwner);
        }
        else
        {
            // ✅ ここを墓地→手札へ
            ZoneManager.I.Move(target, ZoneType.Hand);
        }

        ShieldCountUI.I?.Refresh();
    }
}