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
        Debug.Log("### OnClickSummon CALLED ###");
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

        // ✅ 通常スペルは召喚不可、ただしジョーカーは例外で召喚できる（召喚モード）
        if (isSpell && !isJoker)
        {
            Debug.Log("[Summon] spell cannot be summoned");
            return;
        }

        SummonSelectedToBattle();
    }

    void OnClickMagic()
    {
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

        // ✅ Joker Magic：追加ターン +1（使用後墓地）
        if (inst.isJoker)
        {
            Cast_Joker_ExtraTurn(cost: 13);
            return;
        }

        if (inst.type != CardType.Spell)
        {
            Debug.Log("[Magic] not a spell");
            return;
        }

        // ✅ Aは cost=4 に上書きされるので、spellEffect で判定する
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
    Debug.Log("=== SummonSelectedToBattle START ===");

    if (current == null)
    {
        Debug.LogWarning("[Summon] current is NULL");
        return;
    }

    Debug.Log($"[Summon] card={current.name} zone={current.currentZone}");

    if (current.currentZone != ZoneType.Hand)
    {
        Debug.LogWarning("[Summon] not in hand");
        return;
    }

    var inst = current.instance;
    if (inst == null)
    {
        Debug.LogError("[Summon] instance is NULL");
        return;
    }

    Debug.Log($"[Summon] isJoker={inst.isJoker} type={inst.type} rank={inst.rank}");

    // ★通常スペルは召喚禁止（ジョーカーは例外）
    if (inst.type == CardType.Spell && !inst.isJoker)
    {
        Debug.LogWarning("[Summon] blocked: normal spell cannot be summoned");
        return;
    }

    var tm = TurnManager.I;
    var zm = ZoneManager.I;

    Debug.Log($"[Summon] TurnManager={(tm ? "OK" : "NULL")}  ZoneManager={(zm ? "OK" : "NULL")}  SummonEffectSystem={(SummonEffectSystem.I ? "OK" : "NULL")}");

    if (tm == null || zm == null)
    {
        Debug.LogError("[Summon] missing core managers");
        return;
    }

    int cost = current.Cost;
    Debug.Log($"[Summon] cost={cost}");

    // =========================
    // 通常召喚を試す
    // =========================
    bool manaOK = tm.TrySpendMana(OwnerType.Player, cost);
    Debug.Log($"[Summon] TrySpendMana result={manaOK}");

    if (!manaOK)
    {
        Debug.Log("[Summon] normal summon failed -> check special summon");

        bool isKing13 = (!inst.isJoker && inst.rank == 13);
        Debug.Log($"[Summon] isKing13={isKing13}");

        if (!isKing13)
        {
            Debug.LogWarning("[Summon] not enough mana and not special summon target");
            return;
        }

        var ss = SpecialSummonTargetSystem.I;
        Debug.Log($"[Summon] SpecialSummonTargetSystem={(ss ? "OK" : "NULL")}");

        if (ss == null)
        {
            Debug.LogError("[SpecialSummon-K] system missing");
            return;
        }

        if (ss.IsSelecting) ss.Cancel();

        ss.StartSelection(current, (baseCard) =>
        {
            Debug.Log("[SpecialSummon-K] selection callback");

            if (baseCard == null)
            {
                Debug.LogWarning("[SpecialSummon-K] baseCard null");
                return;
            }

            int diff = ss.GetDiffCost(baseCard);
            Debug.Log($"[SpecialSummon-K] diffCost={diff}");

            if (!tm.TrySpendMana(OwnerType.Player, diff))
            {
                Debug.LogWarning("[SpecialSummon-K] not enough mana for diff");
                return;
            }

            zm.SendToGrave(baseCard);

            bool moved2 = zm.Move(current, ZoneType.Battle);
            Debug.Log($"[SpecialSummon-K] Move result={moved2} newZone={current.currentZone}");

            if (!moved2)
            {
                Debug.LogError("[SpecialSummon-K] Move failed");
                return;
            }

            current.SetSummoningSick(false);
            current.SetTapped(false);

            Debug.Log(">>> CALL OnSummoned (Special)");
            if (SummonEffectSystem.I == null)
                Debug.LogError("[Summon] SummonEffectSystem is NULL!");
            /*else
                SummonEffectSystem.I.OnSummoned(current);*/
        });

        return;
    }

    // =========================
    // 通常召喚成功
    // =========================

    bool moved = zm.Move(current, ZoneType.Battle);
    Debug.Log($"[Summon] Move result={moved} newZone={current.currentZone}");

    if (!moved)
    {
        Debug.LogError("[Summon] ZoneManager.Move failed");
        return;
    }

    bool ignoreSick = current.IgnoreSummonSickness;
    current.SetSummoningSick(!ignoreSick);
    current.SetTapped(false);

    Debug.Log(">>> CALL OnSummoned (Normal)");

    if (SummonEffectSystem.I == null)
    {
        Debug.LogError("[Summon] SummonEffectSystem is NULL!");
    }
    else
    {
        //SummonEffectSystem.I.OnSummoned(current);
    }

    Debug.Log("=== SummonSelectedToBattle END ===");
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

    // ✅ Joker：コスト13 / 追加ターン +1 / 使用後墓地
    void Cast_Joker_ExtraTurn(int cost)
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

        // ✅ 追加ターン付与（このターンの後、もう一回プレイヤーターン）
        tm.GrantExtraTurn(OwnerType.Player);

        // 使用後墓地
        zm.SendToGrave(current);

        Debug.Log($"[JokerMagic] END zone={current.currentZone} name={current.name}");
        Debug.Log("[JokerMagic] resolved extra turn +1");
    }

    // =========================
    // Shield Trigger / Free Cast
    // =========================

    public void CastSpellFromAnywhere_Free(CardController spellCard)
    {
        if (spellCard == null || spellCard.instance == null) return;

        var inst = spellCard.instance;
        var zm = ZoneManager.I;
        if (zm == null) return;

        // ✅ Joker Spell：追加ターン（無料）
        if (inst.isJoker)
        {
            Cast_Joker_ExtraTurn_Free(spellCard);
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
            // 対象いない→手札へ
            zm.AddToZone(source, OwnerType.Player, ZoneType.Hand);
            return;
        }

        sts.StartTargetSelection(source, (target) =>
        {
            if (target == null) return;
            zm.SendToGrave(target);
            zm.SendToGrave(source);
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

    void Cast_Joker_ExtraTurn_Free(CardController source)
    {
        var tm = TurnManager.I;
        var zm = ZoneManager.I;
        if (tm == null || zm == null) return;

        if (SpellTargetSystem.I != null && SpellTargetSystem.I.IsSelecting)
            SpellTargetSystem.I.CancelSelection();

        // ✅ 追加ターン付与（無料トリガーでも同じ）
        tm.GrantExtraTurn(OwnerType.Player);

        zm.SendToGrave(source);
        Debug.Log("[ShieldTrigger-Joker] resolved free extra turn +1");
    }
}