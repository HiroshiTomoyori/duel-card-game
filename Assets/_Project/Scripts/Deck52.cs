using System.Collections.Generic;
using UnityEngine;

public class Deck52 : MonoBehaviour
{
    [Header("Refs")]
    public GameObject cardPrefab;
    public Transform handArea;

    [Header("Config")]
    public int startingHand = 5;

    Dictionary<string, Sprite> spriteDict;
    List<CardId> deck = new List<CardId>();

    void Start()
    {
        // 1) スプライト読込
        spriteDict = CardSpriteDB.LoadAllFromResources("Cards");

        // 2) 52枚構築
        Build52();

        // 3) シャッフル
        Shuffle(deck);

        // 4) 初期手札
        Draw(startingHand);
    }

    void Build52()
    {
        deck.Clear();
        foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
        {
            for (int r = 1; r <= 13; r++)
                deck.Add(new CardId(s, r));
        }
    }

    public void Draw(int amount = 1)
    {
        Debug.Log($"[Deck52.Draw] amount={amount} frame={Time.frameCount}");
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0) return;

            var id = deck[0];
            deck.RemoveAt(0);

            var go = Instantiate(cardPrefab, handArea);
            var view = go.GetComponent<CardView>();

            string key = CardSpriteDB.Key(id);

            if (spriteDict.TryGetValue(key, out var sp))
                view.SetSprite(sp);
            else
                Debug.LogWarning($"Sprite not found: {key} (Resources/Cards)");
        }

        HandFanLayout.I?.Layout();
    }

    void Shuffle(List<CardId> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
