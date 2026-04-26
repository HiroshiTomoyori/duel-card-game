using UnityEngine;
using System.Collections.Generic;

public class BattleLineLayout : MonoBehaviour
{
    public static BattleLineLayout I;

    [Header("Tuning (pixels)")]
    public float maxSpreadX = 520f;
    public float maxStepX = 170f;

    public float playerY = 0f;      // プレイヤー高さ
    public float enemyY = -80f;     // 敵だけ下げる

    // ⭐ 追加：このカードを最前面にする
    public CardController forceFrontCard;

    void Awake()
    {
        I = this;
    }

    public void Layout(Transform battleAnchor)
    {
        if (!battleAnchor) return;

        // 対象カード収集
        var cards = new List<RectTransform>();
        foreach (Transform child in battleAnchor)
        {
            if (child.GetComponent<CardController>() == null) continue;
            if (child is RectTransform rt) cards.Add(rt);
        }

        int n = cards.Count;
        if (n == 0) return;

        // 横配置計算
        float total = Mathf.Min(maxSpreadX, maxStepX * (n - 1));
        float step = (n <= 1) ? 0f : total / (n - 1);
        float startX = -total * 0.5f;

        // ⭐ 敵かプレイヤーか判定
        float y = battleAnchor.name.Contains("Enemy")
            ? enemyY
            : playerY;

        // 配置
        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];

            rt.anchoredPosition = new Vector2(startX + step * i, y);
            rt.localRotation = Quaternion.identity;

            // 通常順序
            rt.SetSiblingIndex(i);
        }

        // ⭐ 最重要：特定カードを最前面へ
        if (forceFrontCard != null)
        {
            var rt = forceFrontCard.GetComponent<RectTransform>();

            // 同じバトルゾーンにいるときだけ前に出す
            if (rt != null && rt.parent == battleAnchor)
            {
                rt.SetAsLastSibling();
            }
        }
    }
}