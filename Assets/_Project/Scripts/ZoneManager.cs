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

        zones[(owner, zone)].cards.Add(card);
        RefreshLayout(owner, zone);
    }

    public bool Move(CardController card, ZoneType toZone)
    {
        var fromKey = (card.owner, card.currentZone);
        if (!zones.TryGetValue(fromKey, out var from)) return false;

        var toKey = (card.owner, toZone);
        if (!zones.TryGetValue(toKey, out var to))
        {
            Debug.LogError($"Move failed, toZone not registered: {toKey}");
            return false;
        }

        if (!from.cards.Remove(card)) return false;

        card.currentZone = toZone;
        card.transform.SetParent(to.anchor, false);    
        // ✅ Handに入ったら必ず表示
        if (toZone == ZoneType.Hand)
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

        RefreshLayout(card.owner, fromKey.Item2);
        RefreshLayout(card.owner, toZone);
        return true;
    }

    void RefreshLayout(OwnerType owner, ZoneType zone)
    {
        // プレイヤー手札
        if (owner == OwnerType.Player && zone == ZoneType.Hand)
            HandFanLayout.I?.Layout();

        // ★敵手札（裏面扇形）
        if (owner == OwnerType.Enemy && zone == ZoneType.Hand)
        {
            EnemyHandFanLayout.I?.Layout();
            EnemyHandCountUI.I?.Refresh();
        }

        // プレイヤーマナ（必要なら）
        if (owner == OwnerType.Player && zone == ZoneType.Mana)
            ManaStackLayout.I?.Layout();

        // バトル（両方横並び）
        if (zone == ZoneType.Battle)
        {
            var anchor = GetAnchor(owner, zone);
            BattleLineLayout.I?.Layout(anchor);
        }

        // シールドの並び（もしレイアウトスクリプトがあるなら）
        // zone == ZoneType.Shield の時に ShieldRowLayout をかけたい場合はここで
        // var a = GetAnchor(owner, ZoneType.Shield);
        // a?.GetComponent<ShieldRowLayout>()?.Apply();

        // シールドの並び（両方）
        if (zone == ZoneType.Shield)
            RefreshShieldRow(owner);
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

        // 墓地のレイアウト更新（zones辞書にGraveを持つならこれが効く）
        RefreshLayout(card.owner, ZoneType.Grave);
    }

    void RefreshShieldRow(OwnerType owner)
    {
        var a = GetAnchor(owner, ZoneType.Shield);
        if (a == null) return;

        var row = a.GetComponent<ShieldRowLayout>();
        if (row != null) row.Apply(); // ← あなたのメソッド名が Layout/Apply/Refresh なら合わせて
    }
}