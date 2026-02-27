using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager I { get; private set; }

    public enum TurnPhase
    {
        Draw,
        Main,
        Battle,
        End
    }

    public bool isPlayerTurn { get; private set; } = true;
    public bool hasPlayedManaThisTurn { get; private set; } = false;
    public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Draw;

    // ✅ 追加ターン（Joker魔法など）
    // 「次のターンも同じ側」にしたいときに、現在ターン側のOwnerを入れる
    // スタック不可（1回分のみ）
    OwnerType? extraTurnOwner = null;

    [Header("UI")]
    public TMP_Text turnText; // CanvasのTxt_Turn

    [Header("Draw")]
    public DeckFromSprites deck; // シーンの DeckFromSprites

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(this);
            return;
        }

        I = this;

        if (!deck)
        {
            deck = FindFirstObjectByType<DeckFromSprites>();
            Debug.Log($"[TurnManager] auto deck = {(deck ? deck.name : "NULL")}");
        }

        RefreshUI();
    }

    void Start()
    {
        BeginTurn();
    }

    // =========================
    // Public rule checks
    // =========================

    public bool CanPlayMana()
        => isPlayerTurn && CurrentPhase == TurnPhase.Main && !hasPlayedManaThisTurn;

    public bool CanPlaySpell()
        => isPlayerTurn && CurrentPhase == TurnPhase.Main;

    public bool CanSummon()
        => isPlayerTurn && CurrentPhase == TurnPhase.Main;

    public bool CanAttack()
        => isPlayerTurn && CurrentPhase == TurnPhase.Battle;

    public void OnPlayedMana()
    {
        hasPlayedManaThisTurn = true;
        RefreshUI();
        ManaCountUI.RefreshOwner(OwnerType.Player);
    }

    // 旧互換
    public void OnManaPlayed() => OnPlayedMana();

    // =========================
    // Extra Turn (Public API)
    // =========================

    /// <summary>
    /// ✅ 追加ターンを付与する（このターンの後、同じOwnerがもう一度ターンを行う）
    /// デフォはスタック不可：すでに予約済みなら何もしない
    /// </summary>
    public void GrantExtraTurn(OwnerType owner)
    {
        if (extraTurnOwner.HasValue) return;
        extraTurnOwner = owner;
        Debug.Log($"[TurnManager] ExtraTurn granted to {owner}");
    }

    // =========================
    // Turn / Phase flow
    // =========================

    public void EndTurnButton()
    {
        if (!isPlayerTurn) return;   // ✅ ボタンはプレイヤー専用
        SwitchTurn();
    }

    // 既存の外部呼び出し互換（今まで EndTurn() 呼んでたなら残す）
    public void EndTurn()
    {
        ForceToEndPhase();
    }

    void BeginTurn()
    {
        hasPlayedManaThisTurn = false;

        var owner = isPlayerTurn ? OwnerType.Player : OwnerType.Enemy;

        // ターン開始時処理（アンタップ＆召喚酔い解除＆攻撃回数リセット等）
        RefreshManaAtTurnStart(owner);

        if (BattleManager.I != null)
            BattleManager.I.OnTurnStart(owner);

        // Draw から開始
        SetPhase(TurnPhase.Draw);

        // Drawフェイズ処理は即時に実行してMainへ
        DoDrawPhase(owner);
    }

    void DoDrawPhase(OwnerType owner)
    {
        Debug.Log($"[DoDrawPhase] owner={owner} frame={Time.frameCount}\n{System.Environment.StackTrace}");

        if (deck == null) return;

        if (owner == OwnerType.Player)
            deck.Draw(1);
        else
            deck.DrawTo(OwnerType.Enemy, 1);

        SetPhase(TurnPhase.Main);

        // Mana表示更新（ドロー後の表示揺れ対策）
        ManaCountUI.RefreshOwner(isPlayerTurn ? OwnerType.Player : OwnerType.Enemy);

        if (!isPlayerTurn)
        {
            EnemyAI.I?.TakeTurn();
            RefreshUI();
            return;
        }

        Debug.Log($"Phase -> {CurrentPhase} ({(isPlayerTurn ? "Player" : "Enemy")})");
    }

    // プレイヤー用：Main→Battle→End を進めたい時に呼ぶ（ボタンでも自動でも）
    public void AdvancePhase()
    {
        if (!isPlayerTurn) return; // 敵は今は自動

        switch (CurrentPhase)
        {
            case TurnPhase.Main:
                SetPhase(TurnPhase.Battle);
                break;
            case TurnPhase.Battle:
                SetPhase(TurnPhase.End);
                CompleteTurn();
                break;
            case TurnPhase.Draw:
                SetPhase(TurnPhase.Main);
                break;
            case TurnPhase.End:
                break;
        }

        RefreshUI();
        Debug.Log($"Phase -> {CurrentPhase} (Player)");
    }

    void ForceToEndPhase()
    {
        if (!isPlayerTurn)
            return;

        SetPhase(TurnPhase.End);
        CompleteTurn();
        RefreshUI();
    }

    // ✅ ここで「次ターンに進む」を一本化
    void TransitionToNextTurn()
    {
        var currentOwner = isPlayerTurn ? OwnerType.Player : OwnerType.Enemy;

        // ✅ 追加ターンが現在のownerに予約されているなら、反転せずもう一度同じ側
        if (extraTurnOwner.HasValue && extraTurnOwner.Value == currentOwner)
        {
            extraTurnOwner = null;
            Debug.Log($"[TurnManager] ExtraTurn consumed by {currentOwner}");
            BeginTurn();
            return;
        }

        // ✅ 通常
        isPlayerTurn = !isPlayerTurn;
        BeginTurn();
    }

    void CompleteTurn()
    {
        TransitionToNextTurn();
    }

    void SetPhase(TurnPhase phase)
    {
        CurrentPhase = phase;
    }

    // =========================
    // UI
    // =========================

    void RefreshUI()
    {
        if (turnText == null) return;

        string who = isPlayerTurn ? "プレイヤー" : "相手";
        string mana = hasPlayedManaThisTurn ? "（マナ済）" : "（マナ未）";
        string phase = CurrentPhase.ToString();

        turnText.text = $"{who} のターン  [{phase}] {mana}";
    }

    // =========================
    // Mana
    // =========================

    public int GetAvailableMana(OwnerType owner)
    {
        var manaCards = ZoneManager.I.GetCards(owner, ZoneType.Mana);
        int count = 0;
        foreach (var c in manaCards)
        {
            if (c == null) continue;
            if (!c.IsTapped) count++;
        }
        return count;
    }

    public bool TrySpendMana(OwnerType owner, int amount)
    {
        if (amount <= 0) return true;

        var manaCards = ZoneManager.I.GetCards(owner, ZoneType.Mana);

        var used = new List<CardController>();
        foreach (var c in manaCards)
        {
            if (c == null) continue;
            if (c.IsTapped) continue;

            used.Add(c);
            if (used.Count >= amount) break;
        }

        if (used.Count < amount) return false;

        foreach (var c in used)
            c.SetTapped(true);

        // ✅ 表示更新（未タップ数が減る）
        ManaCountUI.RefreshOwner(owner);

        return true;
    }

    public void RefreshManaAtTurnStart(OwnerType owner)
    {
        var manaCards = ZoneManager.I.GetCards(owner, ZoneType.Mana);
        foreach (var c in manaCards)
        {
            if (c == null) continue;
            c.SetTapped(false);
        }

        // ✅ 表示更新（アンタップで未タップ数が戻る）
        ManaCountUI.RefreshOwner(owner);
    }

    public void EndTurnFrom(OwnerType caller)
    {
        if (caller == OwnerType.Player && !isPlayerTurn) return;
        if (caller == OwnerType.Enemy && isPlayerTurn) return;

        TransitionToNextTurn();
    }

    public void EndTurnFromEnemy()
    {
        if (isPlayerTurn) return;    // ✅ 敵専用
        SwitchTurn();
    }

    void SwitchTurn()
    {
        TransitionToNextTurn();
    }
}