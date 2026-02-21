using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SummonButton : MonoBehaviour
{
    public Button summonButton; // Btn_Summon
    public Button magicButton;  // Btn_Magic

    public TMP_Text summonLabel; // Btn_Summon の子 Text(TMP)
    public TMP_Text magicLabel;  // Btn_Magic の子 Text(TMP)

    CardController current;

    void Awake()
    {
        // Inspectorの OnClick を使わず、コードで統一
        if (summonButton) summonButton.onClick.AddListener(OnClickSummon);
        if (magicButton)  magicButton.onClick.AddListener(OnClickMagic);
    }

    void Start()
    {
        RefreshButtons(null);
    }

    void Update()
    {
        var sel = SelectionManager.I ? SelectionManager.I.selected : null;
        if (sel == current) return;

        current = sel;
        RefreshButtons(current);
    }

    void RefreshButtons(CardController card)
    {
        if (!summonButton || !magicButton) return;

        if (card == null || card.currentZone != ZoneType.Hand)
        {
            summonButton.gameObject.SetActive(false);
            magicButton.gameObject.SetActive(false);
            return;
        }

        bool isJoker = (card.instance != null && card.instance.isJoker);
        bool isSpell = (card.instance != null && card.instance.type == CardType.Spell);

        if (isJoker)
        {
            // ジョーカー：Summon + Magic 両方
            summonButton.gameObject.SetActive(true);
            magicButton.gameObject.SetActive(true);
        }
        else if (isSpell)
        {
            // スペル：Magicのみ
            summonButton.gameObject.SetActive(false);
            magicButton.gameObject.SetActive(true);
        }
        else
        {
            // クリーチャー：Summonのみ
            summonButton.gameObject.SetActive(true);
            magicButton.gameObject.SetActive(false);
        }

        if (summonLabel) summonLabel.text = "Summon";
        if (magicLabel)  magicLabel.text  = "Magic";
    }

    // =========================
    // Buttons
    // =========================

    void OnClickSummon()
    {
        //Debug.LogError("### SUMMON CLICKED ###");

        if (current == null) return;
        if (current.currentZone != ZoneType.Hand) return;

        // Mainのみ
        if (TurnManager.I != null && !TurnManager.I.CanSummon())
        {
            Debug.Log("[Summon] cannot summon now");
            return;
        }

        bool isJoker = (current.instance != null && current.instance.isJoker);
        bool isSpell = (current.instance != null && current.instance.type == CardType.Spell);
        if (isSpell && !isJoker)
        {
            Debug.Log("[Summon] spell cannot be summoned");
            return;
        }

        SummonSelectedToBattle();
    }

    void OnClickMagic()
    {
        //Debug.LogError("### MAGIC CLICKED ###");

        if (current == null) return;
        if (current.currentZone != ZoneType.Hand) return;

        // Mainのみ
        if (TurnManager.I != null && !TurnManager.I.CanPlaySpell())
        {
            Debug.Log("[Magic] cannot play spell now");
            return;
        }

        var inst = current.instance;
        if (inst == null) return;

        // ✅ Joker Spell：全バトル一掃（使い捨て）
        if (inst.isJoker)
        {
            Cast_Joker_WipeAll(cost: 13);
            return;
        }

        if (inst.type != CardType.Spell)
        {
            Debug.Log("[Magic] not a spell");
            return;
        }

        // ✅ ここが重要：Aは cost=4 に上書きされるので、spellEffect で判定する
        switch (inst.spellEffect)
        {
            case SpellEffectType.DestroyEnemyBattleOne:
                Cast_A_DestroyOne(realCost: inst.cost); // Aは実コスト4
                break;

            case SpellEffectType.TapAllEnemyBattle:
                Cast_9_TapAll(cost: inst.cost); // 9は9
                break;

            default:
                Debug.Log($"[Magic] unsupported spell effect={inst.spellEffect}");
                break;
        }
    }

    // =========================
    // Summon
    // =========================

public void SummonSelectedToBattle()
{
    // まずnull安全
    if (current == null) return;

    if (current.currentZone != ZoneType.Hand) return;

    var inst = current.instance;
    if (inst == null) return;

    // ★絶対ガード：スペルは召喚禁止（ジョーカーSpellも召喚禁止）
    if (inst.type == CardType.Spell)
    {
        Debug.Log("[Summon] blocked: spell cannot be summoned");
        return;
    }

    var tm = TurnManager.I;
    var zm = ZoneManager.I;

    if (tm == null || zm == null)
    {
        Debug.LogError($"[Summon] missing refs tm={tm} zm={zm}");
        return;
    }

    int cost = current.Cost;

    // ===== まず通常召喚を試す =====
    if (!tm.TrySpendMana(OwnerType.Player, cost))
    {
        // ===== K(13)だけ特殊召喚を試す =====
        bool isKing13 = (!inst.isJoker && inst.rank == 13);
        if (!isKing13)
        {
            Debug.Log($"[Summon] not enough mana cost={cost}");
            return;
        }

        var ss = SpecialSummonTargetSystem.I;
        if (ss == null)
        {
            Debug.LogError("[SpecialSummon-K] SpecialSummonTargetSystem is missing in scene");
            return;
        }

        // すでに選択中ならキャンセル（事故防止）
        if (ss.IsSelecting) ss.Cancel();

        // クリックで素材(7/8/10)を選ばせる
        ss.StartSelection(current, (baseCard) =>
        {
            if (baseCard == null) return;

            int diff = ss.GetDiffCost(baseCard);

            // 差分支払い
            if (!tm.TrySpendMana(OwnerType.Player, diff))
            {
                Debug.Log($"[SpecialSummon-K] not enough mana diff={diff}");
                return;
            }

            // 素材は消滅（管理上は墓地送りにしておく）
            zm.SendToGrave(baseCard);

            // Kをバトルへ
            bool moved2 = zm.Move(current, ZoneType.Battle);
            if (!moved2)
            {
                Debug.LogError("[SpecialSummon-K] ZoneManager.Move failed");
                return;
            }

            // ✅ 特殊召喚：召喚酔い無視
            current.SetSummoningSick(false);
            current.SetTapped(false);

            // 召喚成功時効果（出た扱いなら発動）
            if (SummonEffectSystem.I != null)
                SummonEffectSystem.I.OnSummoned(current);

            Debug.Log($"[SpecialSummon-K] success base={baseCard.name} diff={diff}");
        });

        return; // 通常召喚失敗時はここで終了（選択待ち）
    }

    // ===== 通常召喚（成功） =====
    bool moved = zm.Move(current, ZoneType.Battle);
    if (!moved)
    {
        Debug.LogError("[Summon] ZoneManager.Move failed");
        return;
    }

    // 召喚酔い：ignoreSummonSickness が true なら付けない
    bool ignoreSick = current.IgnoreSummonSickness;
    current.SetSummoningSick(!ignoreSick);

    current.SetTapped(false);

    // 召喚成功時効果
    if (SummonEffectSystem.I != null)
        SummonEffectSystem.I.OnSummoned(current);

    Debug.Log($"[Summon] summoned {current.name} cost={cost} ignoreSick={ignoreSick}");
}


    // =========================
    // Spells
    // =========================

    // A：実コスト4 / 敵バトル1体破壊（対象選択式） / 使用後墓地
    void Cast_A_DestroyOne(int realCost)
    {
        var tm = TurnManager.I;
        var zm = ZoneManager.I;
        var sts = SpellTargetSystem.I;

        if (tm == null || zm == null || sts == null)
        {
            Debug.LogError($"[Magic-A] missing refs tm={tm} zm={zm} sts={sts}");
            return;
        }

        // 対象がいないなら不発（支払いもしない）
        var candidates = sts.GetValidTargetsForDestroyEnemyBattleOne(OwnerType.Player);
        if (candidates == null || candidates.Count == 0)
        {
            Debug.Log("[Magic-A] no valid targets");
            return;
        }

        if (!tm.TrySpendMana(OwnerType.Player, realCost))
        {
            Debug.Log($"[Magic-A] not enough mana cost={realCost}");
            return;
        }

        sts.StartTargetSelection(current, (target) =>
        {
            if (target == null) return;

            zm.SendToGrave(target);
            zm.SendToGrave(current);

            Debug.Log($"[Magic-A] resolved destroy={target.name}");
        });

        Debug.Log("[Magic-A] selecting target... click enemy battle card");
    }

    // 9：コスト9 / 相手バトル全タップ / 使用後墓地
    void Cast_9_TapAll(int cost)
    {
        var tm = TurnManager.I;
        var zm = ZoneManager.I;

        if (tm == null || zm == null)
        {
            Debug.LogError($"[Magic-9] missing refs tm={tm} zm={zm}");
            return;
        }

        if (!tm.TrySpendMana(OwnerType.Player, cost))
        {
            Debug.Log($"[Magic-9] not enough mana cost={cost}");
            return;
        }

        var enemies = zm.GetCards(OwnerType.Enemy, ZoneType.Battle);
        foreach (var c in enemies)
        {
            if (c == null) continue;
            c.SetTapped(true);
        }

        zm.SendToGrave(current);
        Debug.Log("[Magic-9] resolved tap all");
    }

    // Joker：コスト13 / 敵味方のバトル全破壊（一掃） / 使用後墓地（使い捨て）
    void Cast_Joker_WipeAll(int cost)
    {
        Debug.Log($"[JokerMagic] START zone={current.currentZone} name={current.name}");
        var tm = TurnManager.I;
        var zm = ZoneManager.I;

        if (tm == null || zm == null)
        {
            Debug.LogError($"[JokerMagic] missing refs tm={tm} zm={zm}");
            return;
        }

        // 対象選択中ならキャンセル（事故防止）
        if (SpellTargetSystem.I != null && SpellTargetSystem.I.IsSelecting)
            SpellTargetSystem.I.CancelSelection();

        if (!tm.TrySpendMana(OwnerType.Player, cost))
        {
            Debug.Log($"[JokerMagic] not enough mana cost={cost}");
            return;
        }

        var playerBattle = new List<CardController>(zm.GetCards(OwnerType.Player, ZoneType.Battle));
        var enemyBattle  = new List<CardController>(zm.GetCards(OwnerType.Enemy,  ZoneType.Battle));

        foreach (var c in playerBattle)
        {
            if (c == null) continue;
            zm.SendToGrave(c);
        }

        foreach (var c in enemyBattle)
        {
            if (c == null) continue;
            zm.SendToGrave(c);
        }

        zm.SendToGrave(current);
        Debug.Log($"[JokerMagic] END zone={current.currentZone} name={current.name}");
        Debug.Log("[JokerMagic] wiped ALL battle cards (both sides)");
    }

    public void CastSpellFromAnywhere_Free(CardController spellCard)
    {
        if (spellCard == null || spellCard.instance == null) return;

        var inst = spellCard.instance;
        var zm = ZoneManager.I;
        if (zm == null) return;

        // Joker Spell：全体破壊（無料）
        if (inst.isJoker)
        {
            // 元の Cast_Joker_WipeAll を少し改造して「支払い無し」版を呼ぶ
            Cast_Joker_WipeAll_Free(spellCard);
            return;
        }

        // Spellじゃないなら無視
        if (inst.type != CardType.Spell) return;

        switch (inst.spellEffect)
        {
            case SpellEffectType.DestroyEnemyBattleOne:
                Cast_A_DestroyOne_Free(spellCard);
                break;

            case SpellEffectType.TapAllEnemyBattle:
                Cast_9_TapAll_Free(spellCard);
                break;

            default:
                Debug.Log($"[ShieldTrigger] unsupported spell effect={inst.spellEffect}");
                // 使えないなら手札へ逃がしてもOK
                zm.AddToZone(spellCard, OwnerType.Player, ZoneType.Hand);
                break;
        }
    }

    void Cast_A_DestroyOne_Free(CardController source)
    {
        var zm = ZoneManager.I;
        var sts = SpellTargetSystem.I;
        if (zm == null || sts == null) return;

        var candidates = sts.GetValidTargetsForDestroyEnemyBattleOne(OwnerType.Player);
        if (candidates == null || candidates.Count == 0)
        {
            // 対象いない→手札へ（DMだと手札行きが自然）
            zm.AddToZone(source, OwnerType.Player, ZoneType.Hand);
            return;
        }

        sts.StartTargetSelection(source, (target) =>
        {
            if (target == null) return;
            zm.SendToGrave(target);
            zm.SendToGrave(source); // トリガーで使ったら墓地
            Debug.Log("[ShieldTrigger-A] resolved free");
        });
    }

    void Cast_9_TapAll_Free(CardController source)
    {
        var zm = ZoneManager.I;
        if (zm == null) return;

        var enemies = zm.GetCards(OwnerType.Enemy, ZoneType.Battle);
        foreach (var c in enemies)
            if (c != null) c.SetTapped(true);

        zm.SendToGrave(source);
        Debug.Log("[ShieldTrigger-9] resolved free");
    }

    void Cast_Joker_WipeAll_Free(CardController source)
    {
        var zm = ZoneManager.I;
        if (zm == null) return;

        if (SpellTargetSystem.I != null && SpellTargetSystem.I.IsSelecting)
            SpellTargetSystem.I.CancelSelection();

        var playerBattle = new System.Collections.Generic.List<CardController>(zm.GetCards(OwnerType.Player, ZoneType.Battle));
        var enemyBattle  = new System.Collections.Generic.List<CardController>(zm.GetCards(OwnerType.Enemy,  ZoneType.Battle));

        foreach (var c in playerBattle) if (c != null) zm.SendToGrave(c);
        foreach (var c in enemyBattle)  if (c != null) zm.SendToGrave(c);

        zm.SendToGrave(source);
        Debug.Log("[ShieldTrigger-Joker] resolved free wipe");
    }


}
