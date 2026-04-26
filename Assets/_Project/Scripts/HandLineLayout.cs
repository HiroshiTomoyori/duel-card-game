using UnityEngine;
using System.Collections.Generic;

public class HandLineLayout : MonoBehaviour
{
    public static HandLineLayout I;

    [Header("Tuning")]
    public float spacing = 140f;   // カード間隔（カード幅より少し広め）
    public float y = 0f;           // 高さ

    void Awake()
    {
        I = this;
    }

    public void Layout(Transform handAnchor)
    {
        if (!handAnchor) return;

        var cards = new List<RectTransform>();

        foreach (Transform child in handAnchor)
        {
            if (child.GetComponent<CardController>() == null) continue;
            if (child is RectTransform rt) cards.Add(rt);
        }

        int n = cards.Count;
        if (n == 0) return;

        float totalWidth = spacing * (n - 1);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];

            rt.anchoredPosition = new Vector2(startX + spacing * i, y);
            rt.localRotation = Quaternion.identity; // ←回転ゼロ（重要）
            rt.SetSiblingIndex(i);
        }
    }
}