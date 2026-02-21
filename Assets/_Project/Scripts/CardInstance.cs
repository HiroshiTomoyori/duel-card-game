using UnityEngine;

public enum CardType
{
    Creature,
    Spell
}

public enum OnSummonEffectType
{
    None,
    Card5,
    Card6,
    Card10,
    Card13
}

public enum SpellEffectType
{
    None,
    DestroyEnemyBattleOne,
    TapAllEnemyBattle
}

public class CardInstance
{
    public int id;
    public Sprite sprite;

    // ✅ 表示上の数字（1〜13）
    public int rank;

    // ✅ 実コスト（Aは4など）
    public int cost;

    // ===== 能力 =====
    public CardType type = CardType.Creature;

    public bool canAttack = true;
    public bool canBlock = false;
    public bool ignoreSummonSickness = false;

    public bool isJoker = false;

    public SpellEffectType spellEffect = SpellEffectType.None;

    public OnSummonEffectType onSummonEffect = OnSummonEffectType.None;

    public CardInstance(int id, Sprite sprite)
    {
        this.id = id;
        this.sprite = sprite;

        // ✅ まず rank を決める
        rank = ParseRankFromName(sprite ? sprite.name : "");

        // 初期コストは rank と同じ
        cost = rank;

        ApplyRules();
    }

    static int ParseRankFromName(string n)
    {
        if (string.IsNullOrEmpty(n)) return 0;

        string digits = "";
        for (int i = 0; i < n.Length; i++)
            if (char.IsDigit(n[i])) digits += n[i];

        if (digits.Length == 0) return 0;

        if (!int.TryParse(digits, out int v)) return 0;

        if (v < 0) v = 0;
        if (v > 13) v = 13;

        return v;
    }

    void ApplyRules()
    {
        // ===== 初期化 =====
        type = CardType.Creature;
        canAttack = true;
        canBlock = false;
        ignoreSummonSickness = false;
        spellEffect = SpellEffectType.None;
        onSummonEffect = OnSummonEffectType.None;
        isJoker = false;

        string n = sprite ? sprite.name.ToLower() : "";

        // =========================
        // 🃏 ジョーカー（最優先）
        // =========================
        if (n.StartsWith("joker"))
        {
            isJoker = true;

            rank = 13;   // 表示的にも13扱い
            cost = 13;   // 実コストも13

            ignoreSummonSickness = true;
            canBlock = true;
            canAttack = true;

            type = CardType.Creature;
            spellEffect = SpellEffectType.None;
            onSummonEffect = OnSummonEffectType.None;

            return;
        }

        // =========================
        // 通常クリーチャールール（rank基準）
        // =========================

        // ブロック可能：2,3,4,8,11
        if (rank == 2 || rank == 3 || rank == 4 || rank == 8 || rank == 11)
            canBlock = true;

        // 攻撃できない：2,3,8
        if (rank == 2 || rank == 3 || rank == 8)
            canAttack = false;

        // 召喚酔い無効：7
        if (rank == 7)
            ignoreSummonSickness = true;

        // 召喚成功時効果：5,6,10,13
        if (rank == 5)  onSummonEffect = OnSummonEffectType.Card5;
        if (rank == 6)  onSummonEffect = OnSummonEffectType.Card6;
        if (rank == 10) onSummonEffect = OnSummonEffectType.Card10;
        if (rank == 13) onSummonEffect = OnSummonEffectType.Card13;

        // =========================
        // 🧙 スペル：A（rank=1）
        // =========================
        if (rank == 1)
        {
            type = CardType.Spell;

            cost = 4; // 実コスト

            canAttack = false;
            canBlock = false;
            ignoreSummonSickness = true;

            spellEffect = SpellEffectType.DestroyEnemyBattleOne;
            onSummonEffect = OnSummonEffectType.None;

            return;
        }

        // =========================
        // 🧙 スペル：9
        // =========================
        if (rank == 9)
        {
            type = CardType.Spell;

            cost = 9;

            canAttack = false;
            canBlock = false;
            ignoreSummonSickness = true;

            spellEffect = SpellEffectType.TapAllEnemyBattle;
            onSummonEffect = OnSummonEffectType.None;

            return;
        }

        // それ以外は通常クリーチャー
        cost = rank;
    }
}
