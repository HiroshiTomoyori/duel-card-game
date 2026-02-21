using UnityEngine;

public class BattleLineLayout : MonoBehaviour
{
    public static BattleLineLayout I;

    [Header("Tuning (pixels)")]
    public float maxSpreadX = 520f;   // 横方向の最大広がり
    public float maxStepX = 170f;     // 少数枚のときの間隔
    public float y = 0f;             // 高さ固定（必要なら調整）

    void Awake()
    {
        I = this;
    }

    public void Layout(Transform battleAnchor)
    {
        if (!battleAnchor) return;

        // CardControllerを持つ子だけを対象にする（UIや背景が混ざってもOK）
        var cards = new System.Collections.Generic.List<RectTransform>();
        foreach (Transform child in battleAnchor)
        {
            if (child.GetComponent<CardController>() == null) continue;
            if (child is RectTransform rt) cards.Add(rt);
        }

        int n = cards.Count;
        if (n == 0) return;

        // 総幅に上限をつけて自然な間隔に
        float total = Mathf.Min(maxSpreadX, maxStepX * (n - 1));
        float step = (n <= 1) ? 0f : total / (n - 1);
        float startX = -total * 0.5f;

        for (int i = 0; i < n; i++)
        {
            var rt = cards[i];
            rt.anchoredPosition = new Vector2(startX + step * i, y);
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling();
        }
    }
}
