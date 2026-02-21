using UnityEngine;

public class HandFanLayout : MonoBehaviour
{
    public static HandFanLayout I;

public float maxWidth = 200f;   // 端〜端の中心距離
public float maxSpacing = 50f;  // 上限（中心間隔）
public float curve = 14f;       // 深さ
public float maxAngle = 6f;     // 回転
    void Awake()
    {
        I = this;
    }

    public void Layout()
    {Debug.Log($"[HandFanLayout] Layout called. count={transform.childCount}");
        int count = transform.childCount;
        if (count <= 0) return;

        // 1枚なら中央に置いて回転なし（0割回避）
        if (count == 1)
        {
            var card = transform.GetChild(0) as RectTransform;
            if (card != null)
            {
                card.anchoredPosition = Vector2.zero;
                card.localRotation = Quaternion.identity;
            }
            return;
        }

float spacing = Mathf.Min(maxWidth / (count - 1), maxSpacing);        float center = (count - 1) / 2f; // count>=2 なので 0 にならない

        for (int i = 0; i < count; i++)
        {
            RectTransform card = transform.GetChild(i) as RectTransform;
            if (card == null) continue;

            float offset = i - center;

            card.anchoredPosition = new Vector2(offset * spacing, -Mathf.Abs(offset) * curve);

            // -1〜+1 に正規化して角度を付ける
            float t = offset / center;          // center>0
            float angle = t * maxAngle;

            card.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}
