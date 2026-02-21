using System.Collections.Generic;
using UnityEngine;

public class DeckFromSprites : MonoBehaviour
{
    public GameObject cardPrefab;

    public string resourcesFolder = "CardHouseCards"; // Resources/CardHouseCards
    public int startingHand = 5;
    public int deckSize = 54;
    public int startingShields = 5;

    [Header("Shield UI Roots (assign ShieldsRoot here)")]
    public RectTransform playerShieldsRoot; // P_ShieldArea/ShieldsRoot
    public RectTransform enemyShieldsRoot;  // E_ShieldArea/ShieldsRoot

    private readonly List<CardInstance> deck = new List<CardInstance>();
    private int nextId = 1;

    private Sprite backSprite; // 裏面

    void Start()
    {
        BuildDeck();

        // シールド
        DealShields(OwnerType.Player, startingShields);
        DealShields(OwnerType.Enemy, startingShields);

        // 初期手札（プレイヤー）
        Draw(startingHand);

        // ★初期手札（敵）
        DrawTo(OwnerType.Enemy, startingHand);
        EnemyHandCountUI.I?.Refresh();
    }

    void BuildDeck()
    {
        deck.Clear();
        nextId = 1;
        backSprite = null;

        var all = Resources.LoadAll<Sprite>(resourcesFolder);

        foreach (var sp in all)
        {
            var n = sp.name.ToLower();
            if (n.Contains("joker") || n.StartsWith("j"))
                Debug.Log("[DeckFromSprites] found joker-like: " + sp.name);
        }

        // 裏面スプライトを探す（名前は環境で違う可能性があるのでゆるく）
        foreach (var sp in all)
        {
            var n = sp.name.ToLower();
            if (n == "back" || n.Contains("back"))
            {
                backSprite = sp;
                break;
            }
        }

        // カードスプライトをデッキに入れる
        foreach (var sp in all)
        {
            if (sp == null) continue;

            var n = sp.name.ToLower();

            // 裏は除外
            if (n == "back" || n.Contains("back")) continue;

            // ✅ 旧ルール（英字+数字）に加えて、joker も許可
            bool letterDigit = (n.Length >= 2 && char.IsLetter(n[0]) && char.IsDigit(n[1]));
            bool isJoker = n.StartsWith("joker"); // joker1, joker2

            if (letterDigit || isJoker)
            {
                deck.Add(new CardInstance(nextId++, sp));
            }
        }

        Shuffle(deck);

        Debug.Log($"Deck cards: {deck.Count} (back={(backSprite ? backSprite.name : "NULL")})");

        int jokerCount = 0;
        foreach (var c in deck)
        {
            var n = c.sprite ? c.sprite.name.ToLower() : "";
            if (n.StartsWith("joker")) jokerCount++;
        }
        Debug.Log("Joker count in deck(list): " + jokerCount);
    }

    public void Draw(int n = 1)
    {
        Debug.Log($"[Draw] n={n} deckCount={deck.Count} ZoneManager.I={(ZoneManager.I ? "OK" : "NULL")}");

        for (int i = 0; i < n; i++)
        {
            if (deck.Count == 0) return;

            var inst = deck[0];
            deck.RemoveAt(0);

            var go = Instantiate(cardPrefab);
            var cc = go.GetComponent<CardController>();
            cc.Bind(inst);

            ZoneManager.I.AddToZone(cc, OwnerType.Player, ZoneType.Hand);
        }

        HandFanLayout.I?.Layout();
    }

    public void DrawTo(OwnerType owner, int n = 1)
    {
    Debug.Log($"[DrawTo] owner={owner} n={n} frame={Time.frameCount}\n{System.Environment.StackTrace}");
        for (int i = 0; i < n; i++)
        {
            if (deck.Count == 0) return;

            var inst = deck[0];
            deck.RemoveAt(0);

            var go = Instantiate(cardPrefab);
            var cc = go.GetComponent<CardController>();
            cc.Bind(inst);

            ZoneManager.I.AddToZone(cc, owner, ZoneType.Hand);

            // ✅ ここが肝：敵手札は裏
            if (owner == OwnerType.Enemy)
                cc.ShowBack(backSprite);
        }

        if (owner == OwnerType.Player) HandFanLayout.I?.Layout();
        else EnemyHandFanLayout.I?.MarkDirty();
    }

    public CardController DrawTopToZone(OwnerType owner, ZoneType zone)
    {
        if (deck.Count == 0) return null;

        var inst = deck[0];
        deck.RemoveAt(0);

        var go = Instantiate(cardPrefab);
        var cc = go.GetComponent<CardController>();
        cc.Bind(inst);

        // 敵のカードは「手札/シールド」だけ裏にする（マナは非表示運用なら裏不要）
        if (owner == OwnerType.Enemy && (zone == ZoneType.Hand || zone == ZoneType.Shield))
            cc.ShowBack(backSprite);

        ZoneManager.I.AddToZone(cc, owner, zone);

        // マナは非表示運用なら隠す（シールドは表示する）
        if (zone == ZoneType.Mana)
            cc.gameObject.SetActive(false);
        else
            cc.gameObject.SetActive(true);

        // UI更新（必要最低限）
        if (owner == OwnerType.Player && zone == ZoneType.Hand)
            HandFanLayout.I?.Layout();

        // シールドに引いた場合は ShiedsRoot 側に寄せる（見た目だけ）
        /*if (zone == ZoneType.Shield)
        {
            RectTransform root = (owner == OwnerType.Player) ? playerShieldsRoot : enemyShieldsRoot;
            if (root != null)
            {
                cc.transform.SetParent(root, false);
                cc.ShowBack(backSprite);
                root.GetComponent<ShieldRowLayout>()?.Apply();
            }
        }*/

        return cc;
    }

    public void DealShields(OwnerType owner, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (deck.Count == 0) return;

            var inst = deck[0];
            deck.RemoveAt(0);

            var go = Instantiate(cardPrefab);
            var cc = go.GetComponent<CardController>();
            cc.Bind(inst);

            // シールドゾーンへ（ゲームロジック上の所属）
            ZoneManager.I.AddToZone(cc, owner, ZoneType.Shield);

            // ▼ 見た目の親だけ ShieldsRoot に付け替える
            /*RectTransform root = (owner == OwnerType.Player) ? playerShieldsRoot : enemyShieldsRoot;
            if (root != null)
                cc.transform.SetParent(root, false);*/

            // 裏表示（シールドは常に裏）
            cc.ShowBack(backSprite);

            // 表示する（数だけ表示の時の名残を撤去）
            cc.gameObject.SetActive(true);
        }

        // 並べる（最狭にしたいなら ShieldRowLayout の spacing=0）
        playerShieldsRoot?.GetComponent<ShieldRowLayout>()?.Apply();
        enemyShieldsRoot?.GetComponent<ShieldRowLayout>()?.Apply();

        // 数表示も更新
        ShieldCountUI.I?.Refresh();
    }

    void Shuffle(List<CardInstance> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}