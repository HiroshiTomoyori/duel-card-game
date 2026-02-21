using UnityEngine;

public class ShieldTriggerSystem : MonoBehaviour
{
    public static ShieldTriggerSystem I { get; private set; }

    [Header("Refs")]
    public SummonButton summonButton;          // シーンの SummonButton をドラッグ
    public JokerTriggerChoiceUI jokerChoiceUI; // 後で作るUI（Step3）

    void Awake() => I = this;

    public void OnShieldBroken(CardController shieldCard, OwnerType defenderOwner)
    {
        if (shieldCard == null || shieldCard.instance == null) return;

        // 今はプレイヤー側だけ（敵トリガーは後で）
        if (defenderOwner != OwnerType.Player)
        {
            // ✅ 敵も割れたら手札へ（トリガー処理は後で追加でOK）
            ZoneManager.I.Move(shieldCard, ZoneType.Hand);
            shieldCard.gameObject.SetActive(true);
            //shieldCard.ShowFront(); // 無ければ削除OK

            ShieldCountUI.I?.Refresh();
            return;
        }

        // ① まずは「割れたカードは手札へ」（DMの基本）
        //    これで "カード要素を持ったまま" 進行できる
        ZoneManager.I.Move(shieldCard, ZoneType.Hand);
        shieldCard.gameObject.SetActive(true);
        shieldCard.ShowFront(); // 無ければこの行は削除OK

        // ② トリガー対象か判定
        if (!IsShieldTriggerTarget(shieldCard))
        {
            // トリガー無し → 手札に入ったまま
            ShieldCountUI.I?.Refresh();
            return;
        }

        var inst = shieldCard.instance;

        // ③ Jokerは選択
        if (inst.isJoker)
        {
            if (jokerChoiceUI == null)
            {
                Debug.LogError("[ShieldTrigger] JokerChoiceUI missing");
                // 無いなら保険で手札のまま
                ShieldCountUI.I?.Refresh();
                return;
            }

            jokerChoiceUI.Show(
                onSummon: () => TriggerSummonCreatureToBattle(shieldCard),
                onMagic:  () => TriggerCastSpell(shieldCard),
                onKeep:   () => { /* 手札に残す */ ShieldCountUI.I?.Refresh(); }
            );
            return;
        }

        // ④ Spellは即発動
        if (inst.type == CardType.Spell)
        {
            TriggerCastSpell(shieldCard);
            return;
        }

        // ⑤ Creatureはバトル場へ
        TriggerSummonCreatureToBattle(shieldCard);
    }

    bool IsShieldTriggerTarget(CardController c)
    {
        if (c == null || c.instance == null) return false;

        if (c.instance.isJoker) return true;

        int r = c.instance.rank; // A=1, 2,8,9,12
        return (r == 1 || r == 2 || r == 8 || r == 9 || r == 12);
    }

    void TriggerCastSpell(CardController spellCard)
    {
        if (summonButton == null)
        {
            Debug.LogError("[ShieldTrigger] SummonButton ref is missing");
            // 保険：手札のまま
            ShieldCountUI.I?.Refresh();
            return;
        }

        Debug.Log("[ShieldTrigger] Cast spell for free: " + spellCard.name);

        // 重要：SummonButton側に Freeキャスト関数が必要
        summonButton.CastSpellFromAnywhere_Free(spellCard);

        ShieldCountUI.I?.Refresh();
    }

    void TriggerSummonCreatureToBattle(CardController creatureCard)
    {
        var zm = ZoneManager.I;
        if (zm == null) return;

        Debug.Log("[ShieldTrigger] Summon creature to battle for free: " + creatureCard.name);

        // 手札 → バトル場（マナ支払いなし）
        zm.Move(creatureCard, ZoneType.Battle);

        // 召喚酔い：通常扱い（攻撃だけ止まる）
        bool ignoreSick = creatureCard.IgnoreSummonSickness;
        creatureCard.SetSummoningSick(!ignoreSick);

        creatureCard.SetTapped(false);

        // 召喚成功時効果も走らせる（必要なら）
        if (SummonEffectSystem.I != null)
            SummonEffectSystem.I.OnSummoned(creatureCard);

        ShieldCountUI.I?.Refresh();
    }
}
