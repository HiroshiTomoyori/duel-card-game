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

    public int rank;
    public int cost;

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

        string name = sprite ? sprite.name : "NULL";

        Debug.Log($"[CardInstance-CONSTRUCTOR] id={id} name={name}");

        // rank決定
        rank = ParseRankFromName(name);
        cost = rank;

        Debug.Log($"[CardInstance] after ParseRank rank={rank} cost={cost}");

        ApplyRules();

        Debug.Log(
            $"[CardInstance-RESULT] name={name} " +
            $"isJoker={isJoker} rank={rank} cost={cost} type={type} " +
            $"spellEffect={spellEffect} onSummonEffect={onSummonEffect}"
        );
    }

    static int ParseRankFromName(string n)
    {
        if (string.IsNullOrEmpty(n))
        {
            Debug.LogWarning("[ParseRank] empty name");
            return 0;
        }

        string digits = "";
        for (int i = 0; i < n.Length; i++)
            if (char.IsDigit(n[i])) digits += n[i];

        Debug.Log($"[ParseRank] name={n} digits={digits}");

        if (digits.Length == 0) return 0;

        if (!int.TryParse(digits, out int v))
        {
            Debug.LogWarning($"[ParseRank] parse failed name={n}");
            return 0;
        }

        if (v < 0) v = 0;
        if (v > 13) v = 13;

        return v;
    }

    void ApplyRules()
    {
        // 初期化
        type = CardType.Creature;
        canAttack = true;
        canBlock = false;
        ignoreSummonSickness = false;
        spellEffect = SpellEffectType.None;
        onSummonEffect = OnSummonEffectType.None;
        isJoker = false;

        string n = sprite ? sprite.name.ToLower() : "";

        Debug.Log($"[ApplyRules] START name={n} rank={rank}");

        // =========================
        // 🃏 ジョーカー（最優先）
        // =========================
        if (n.StartsWith("joker") || n == "j01" || n == "j02")
        {
            isJoker = true;

            rank = 13;
            cost = 13;

            ignoreSummonSickness = true;
            canBlock = true;
            canAttack = true;

            type = CardType.Creature;
            spellEffect = SpellEffectType.None;
            onSummonEffect = OnSummonEffectType.None;
            return;
        }
        // =========================
        // 通常クリーチャー系
        // =========================

        if (rank == 2 || rank == 3 || rank == 4 || rank == 8 || rank == 11)
            canBlock = true;

        if (rank == 2 || rank == 3 || rank == 8)
            canAttack = false;

        if (rank == 7)
            ignoreSummonSickness = true;

        if (rank == 5)  onSummonEffect = OnSummonEffectType.Card5;
        if (rank == 6)  onSummonEffect = OnSummonEffectType.Card6;
        if (rank == 10) onSummonEffect = OnSummonEffectType.Card10;
        if (rank == 13) onSummonEffect = OnSummonEffectType.Card13;

        // =========================
        // 🧙 A (rank=1)
        // =========================
        if (rank == 1)
        {
            Debug.Log("[ApplyRules] Rank1 -> Spell A");

            type = CardType.Spell;
            cost = 4;

            canAttack = false;
            canBlock = false;
            ignoreSummonSickness = true;

            spellEffect = SpellEffectType.DestroyEnemyBattleOne;
            onSummonEffect = OnSummonEffectType.None;

            return;
        }

        // =========================
        // 🧙 9
        // =========================
        if (rank == 9)
        {
            Debug.Log("[ApplyRules] Rank9 -> Spell");

            type = CardType.Spell;
            cost = 9;

            canAttack = false;
            canBlock = false;
            ignoreSummonSickness = true;

            spellEffect = SpellEffectType.TapAllEnemyBattle;
            onSummonEffect = OnSummonEffectType.None;

            return;
        }

        // その他
        cost = rank;
    }
}