using System.Collections.Generic;
using UnityEngine;

public class EnemyHandFanLayout : MonoBehaviour
{
    public static EnemyHandFanLayout I;

    [Header("Fit inside area")]
    [Range(0.1f, 1f)] public float widthRatio = 0.85f;
    [Range(0.0f, 1f)] public float heightRatio = 0.20f;
    public float curve = 12f;
    public float yOffset = 20f;

    [Header("Rotation")]
    public float maxRotate = 6f;
    public bool mirror = true;

    [Header("Overlap (source-side)")]
    [Tooltip("カードの横方向の見えてほしい幅（px）。小さいほど重なる")]
    [Min(0f)] public float revealPixels = 28f;

    [Tooltip("最低でもこの割合は見える（カード幅×割合）。0.05=5%")]
    [Range(0f, 0.5f)] public float revealMinRatio = 0.08f;

    [Tooltip("中央カードを最前面にする（おすすめ）")]
    public bool bringCenterToFront = true;

    RectTransform areaRect;
    bool dirty = true;

    void Awake()
    {
        I = this;
        areaRect = transform as RectTransform;
    }

    void OnTransformChildrenChanged() => dirty = true;

    void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        Layout();
    }

    public void Layout()
    {
        if (!areaRect) return;
        if (areaRect.rect.width <= 1f || areaRect.rect.height <= 1f) return;

        var cards = new List<RectTransform>();
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeInHierarchy) continue;
            if (child.GetComponent<CardController>() == null) continue;
            if (child is RectTransform rt) cards.Add(rt);
        }

        int n = cards.Count;
        if (n == 0) return;

        float areaW = areaRect.rect.width * widthRatio;
        float areaH = areaRect.rect.height * heightRatio;

        // --- “ほぼ重ね”のキモ：カード幅から刻み幅(step)を決める ---
        float cardW = GetCardWidth(cards);
        float stepWanted = Mathf.Max(revealPixels, cardW * revealMinRatio);

        // ただし、エリア幅を超えるなら自動でさらに詰める
        // 総横幅 = (n-1)*step + cardW が areaW に収まるように step を抑える
        float stepMaxToFit = (n <= 1) ? 0f : Mathf.Max(0f, (areaW - cardW) / (n - 1));
        float step = (n <= 1) ? 0f : Mathf.Min(stepWanted, stepMaxToFit);

        // 中心寄せ：左端x
        float totalW = (n <= 1) ? cardW : (cardW + step * (n - 1));
        float leftX = -totalW * 0.5f + cardW * 0.5f;

        for (int i = 0; i < n; i++)
        {
            // 見た目の左→右順のインデックス（mirrorなら逆順のカードを割り当てる）
            int index = mirror ? (n - 1 - i) : i;
            var rt = cards[index];

            // xはstep刻みでほぼ重ねる
            float x = leftX + step * i;

            // 扇カーブ：中心ほど少し下へ（上側手札なので yはマイナス方向へ）
            float t = (n == 1) ? 0.5f : (float)i / (n - 1);
            float u = (t - 0.5f) * 2f; // -1..1

            float y = Mathf.Abs(u) * areaH;
            float centerDip = 1f - Mathf.Abs(u);
            y += centerDip * curve;

            // 敵は上側：下方向へ扇
            y = -y + yOffset;

            // 回転も軽く付ける（逆扇）
            float rot = Mathf.Lerp(-maxRotate, maxRotate, t);
            rot = -rot;

            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, rot);
        }

        if (bringCenterToFront && n > 0)
        {
            // 中央を手前に（自然）
            cards[n / 2].SetAsLastSibling();
        }
    }

    float GetCardWidth(List<RectTransform> cards)
    {
        // 1枚目から幅を推定（Scaleも考慮）
        var rt = cards[0];
        float w = rt.rect.width;
        if (w <= 1f) w = rt.sizeDelta.x;
        float s = rt.lossyScale.x;
        if (s <= 0.0001f) s = 1f;
        return Mathf.Max(1f, w * s);
    }

    public void MarkDirty() => dirty = true;
}