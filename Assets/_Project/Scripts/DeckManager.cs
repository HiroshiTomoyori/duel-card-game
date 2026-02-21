using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform handArea;

    public int startingHand = 5;

    List<int> deck = new List<int>();

    void Start()
    {
        BuildDeck();
        Draw(startingHand);
    }

    void BuildDeck()
    {
        deck.Clear();

        // 仮デッキ：20枚
        for (int i = 0; i < 20; i++)
            deck.Add(i);

        Shuffle(deck);
    }

    public void Draw(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (deck.Count == 0) return;

            deck.RemoveAt(0);

            Instantiate(cardPrefab, handArea);
        }

        HandFanLayout.I.Layout();
    }

    void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
