using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager I { get; private set; }

    public Sprite enemyHandBackSprite; // Inspectorで back1 を入れる

    class Zone
    {
        public Transform anchor;
        public readonly List<CardController> cards = new List<CardController>();
    }

    private readonly Dictionary<(OwnerType, ZoneType), Zone> zones = new();

    void Awake()
    {
        I = this;

        foreach (var a in FindObjectsOfType<ZoneAnchor>(true))
        {
            var key = (a.owner, a.zoneType);
            if (zones.ContainsKey(key))
            {
                Debug.LogError($"[ZoneManager] Duplicate ZoneAnchor key: {key}  existing={zones[key].anchor.name}  new={a.name}");
                continue; // 上書きしない
            }
            zones[key] = new Zone { anchor = a.transform };
        }
    }

    public Transform GetAnchor(OwnerType owner, ZoneType zone)
    {
        if (zones.TryGetValue((owner, zone), out var z)) return z.anchor;
        Debug.LogError($"Zone anchor not found: {owner}/{zone}");
        return null;
    }

    public void AddToZone(CardController card, OwnerType owner, ZoneType zone)
    {
        var anchor = GetAnchor(owner, zone);
        if (anchor == null) return;

        card.owner = owner;
        card.currentZone = zone;

        card.transform.SetParent(anchor, false);

        // ✅ ゾーン見た目（スケール等）を一括適用
        card.ApplyZoneVisual();

        // ✅ 敵手札は常に裏
        if (owner == OwnerType.Enemy && zone == ZoneType.Hand)
        {
            if (enemyHandBackSprite != null)
                card.ShowBack(enemyHandBackSprite);
        }

        // ✅ マナはカードを消す（データとしては残す）
        if (zone == ZoneType.Mana)
            card.gameObject.SetActive(false);
        else
            card.gameObject.SetActive(true);

        zones[(owner, zone)].cards.Add(card);
        RefreshLayout(owner, zone);

// ✅ マナ表示更新（Player/Enemy両対応）
if (zone == ZoneType.Mana)
    ManaCountUI.RefreshOwner(owner);
    }

public bool Move(CardController card, ZoneType toZone)
{
    if (card == null) return false;

    // ★移動元を先に保存（後で召喚判定に使う）
    ZoneType fromZone = card.currentZone;

    var fromKey = (card.owner, card.currentZone);
    if (!zones.TryGetValue(fromKey, out var from)) return false;

    var toKey = (card.owner, toZone);
    if (!zones.TryGetValue(toKey, out var to))
    {
        Debug.LogError($"Move failed, toZone not registered: {toKey}");
        return false;
    }

    if (!from.cards.Remove(card)) return false;

    // =========================
    // 実移動
    // =========================
    card.currentZone = toZone;
    card.transform.SetParent(to.anchor, false);

    // ✅ マナに入ったら非表示、それ以外は表示
    if (toZone == ZoneType.Mana)
        card.gameObject.SetActive(false);
    else
        card.gameObject.SetActive(true);

    // ✅ 移動後に見た目（スケール等）を一括適用
    card.ApplyZoneVisual();

    // ✅ 敵手札へ移動したら常に裏
    if (card.owner == OwnerType.Enemy && toZone == ZoneType.Hand)
    {
        if (enemyHandBackSprite != null)
            card.ShowBack(enemyHandBackSprite);
    }

    to.cards.Add(card);

    RefreshLayout(card.owner, fromZone);
    RefreshLayout(card.owner, toZone);

    // ✅ マナ表示更新（Player/Enemy両対応）
    if (fromZone == ZoneType.Mana || toZone == ZoneType.Mana)
    {
        ManaCountUI.RefreshOwner(card.owner);
    }

    // =========================
    // ✅ 召喚フック（ドラッグでもボタンでも統一）
    // Hand -> Battle に入った瞬間を「召喚成功」とみなす
    // =========================
    if (fromZone == ZoneType.Hand && toZone == ZoneType.Battle)
    {
        // 召喚酔いなど、召喚共通処理をここで入れたいならここに集約してOK
        SummonEffectSystem.I?.OnSummoned(card);
    }

    return true;
}

void RefreshLayout(OwnerType owner, ZoneType zone)
{
    // =========================
    // Hand
    // =========================

    // プレイヤー手札（表・扇）
    if (owner == OwnerType.Player && zone == ZoneType.Hand)
        HandFanLayout.I?.Layout();

    // 敵手札（裏・逆扇）
    if (owner == OwnerType.Enemy && zone == ZoneType.Hand)
    {
        EnemyHandFanLayout.I?.Layout();
        EnemyHandCountUI.I?.Refresh();
    }

    // =========================
    // Mana
    // =========================

    // プレイヤーマナ（重ねレイアウト + 分数表示更新）
    if (owner == OwnerType.Player && zone == ZoneType.Mana)
    {
        ManaStackLayout.I?.Layout();
        ManaCountUI.RefreshOwner(OwnerType.Player);
    }

    // 敵マナ（表示更新だけでもOK。レイアウトがあるならここで呼ぶ）
    if (owner == OwnerType.Enemy && zone == ZoneType.Mana)
    {
        // EnemyManaStackLayout.I?.Layout(); // ←もし作るなら
        ManaCountUI.RefreshOwner(OwnerType.Enemy);
    }

    // =========================
    // Battle (両方横並び)
    // =========================
    if (zone == ZoneType.Battle)
    {
        var anchor = GetAnchor(owner, zone);
        BattleLineLayout.I?.Layout(anchor);
    }

    // =========================
    // Shield (両方横並び)
    // =========================
    if (zone == ZoneType.Shield)
        RefreshShieldRow(owner);

    // =========================
    // Grave (もしレイアウトがあるなら)
    // =========================
    // if (zone == ZoneType.Grave)
    // {
    //     var a = GetAnchor(owner, ZoneType.Grave);
    //     a?.GetComponent<GraveLayout>()?.Layout();
    // }
}

    public IReadOnlyList<CardController> GetCards(OwnerType owner, ZoneType zone)
    {
        if (zones.TryGetValue((owner, zone), out var z)) return z.cards;
        return System.Array.Empty<CardController>();
    }

    public bool Remove(CardController card)
    {
        if (card == null) return false;

        var key = (card.owner, card.currentZone);
        if (!zones.TryGetValue(key, out var z)) return false;

        bool removed = z.cards.Remove(card);
        if (removed) RefreshLayout(card.owner, card.currentZone);

    // ✅ マナ表示更新（Player/Enemy両対応）
    if (card.currentZone == ZoneType.Mana)
        ManaCountUI.RefreshOwner(card.owner);

        return removed;
    }

    public Transform playerGrave;
    public Transform enemyGrave;

    public void SendToGrave(CardController card)
    {
        if (card == null) return;

        // どのゾーンに居てもまず外す（Battle/Hand/Mana など全部共通で処理できる）
        Remove(card);

        Transform grave = card.owner == OwnerType.Player ? playerGrave : enemyGrave;

        card.transform.SetParent(grave, false);
        card.transform.localPosition = Vector3.zero;

        card.currentZone = ZoneType.Grave;

        // ✅ 墓地へ行った見た目も一応統一（等倍に戻す等）
        card.ApplyZoneVisual();

        // 墓地は基本クリック無効（閲覧UIは後で）
        var cg = card.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        // 状態リセット（墓地での見た目事故防止）
        card.SetTapped(false);
        card.SetSummoningSick(false);

        // ✅ 墓地は表示する
        card.gameObject.SetActive(true);

        RefreshLayout(card.owner, ZoneType.Grave);

        // ✅ マナ表示更新（Player/Enemy両対応）
        // 墓地送りで「マナから減った」可能性があるので両方更新してOK
        ManaCountUI.RefreshOwner(card.owner);
    }

    void RefreshShieldRow(OwnerType owner)
    {
        var a = GetAnchor(owner, ZoneType.Shield);
        if (a == null) return;

        var row = a.GetComponent<ShieldRowLayout>();
        if (row != null) row.Apply();
    }
}