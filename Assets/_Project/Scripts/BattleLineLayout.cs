using UnityEngine;
using System.Collections.Generic;

public class BattleLineLayout : MonoBehaviour
{
    public static BattleLineLayout I;

    [Header("Horizontal")]
    public float maxSpreadX = 520f;
    public float maxStepX = 170f;

    [Header("Vertical")]
    public float playerY = 20f;
    public float enemyY = -120f;

    void Awake()
    {
        I = this;
    }

    public void Layout(Transform battleAnchor)
    {
        if (!battleAnchor) return;

        List<RectTransform> cards = new List<RectTransform>();

        foreach (Transform child in battleAnchor)
        {
            CardController c = child.GetComponent<CardController>();
            if (c == null) continue;

            if (child is RectTransform rt)
                cards.Add(rt);
        }

        int n = cards.Count;
        if (n == 0) return;

        float total = Mathf.Min(maxSpreadX, maxStepX * (n - 1));
        float step = (n <= 1) ? 0 : total / (n - 1);
        float startX = -total * 0.5f;

        // ⭐ 敵かプレイヤーかをOwnerで判定
        CardController cc = cards[0].GetComponent<CardController>();
        bool isEnemy = cc.owner == OwnerType.Enemy;

        float y = isEnemy ? enemyY : playerY;

        for (int i = 0; i < n; i++)
        {
            RectTransform rt = cards[i];

            rt.anchoredPosition = new Vector2(
                startX + step * i,
                y
            );

            rt.localRotation = Quaternion.identity;

            // ⭐ 描画順安定
            rt.SetSiblingIndex(i);
        }
    }
}