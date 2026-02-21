using System.Collections.Generic;
using UnityEngine;

public static class CardSpriteDB
{
    // 例: s01, h13, d10, k07
    // suitPrefix: Spade=s, Heart=h, Diamond=d, Club=k
    public static Dictionary<string, Sprite> LoadAllFromResources(string resourcesFolder = "Cards")
    {
        var dict = new Dictionary<string, Sprite>();
        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);

        foreach (var sp in sprites)
        {
            // key = ファイル名（拡張子なし）
            // 例: "s01"
            if (!dict.ContainsKey(sp.name))
                dict.Add(sp.name, sp);
        }

        return dict;
    }

    public static string Key(CardId id)
    {
        string p = id.suit switch
        {
            Suit.Spade => "s",
            Suit.Heart => "h",
            Suit.Diamond => "d",
            Suit.Club => "k",
            _ => "s"
        };

        return $"{p}{id.rank:00}";
    }
}
