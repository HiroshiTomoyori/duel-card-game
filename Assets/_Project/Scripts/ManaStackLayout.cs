using UnityEngine;

public class ManaStackLayout : MonoBehaviour
{
    public static ManaStackLayout I;

    [Header("Stack layout (pixels)")]
    public float maxSpreadX = 140f;   // ★束全体の最大横幅（ここがキモ）
    public float maxStepX = 45f;      // 1枚あたりの最大ズレ（少数枚の時）
    public float stepY = -4f;         // 少し下げると束っぽい（0でもOK）
    public float rotateZ = 0f;        // マナは基本0推奨

    [Header("Options")]
    public bool centerStack = true;   // ★束を中央寄せにする
    public bool bringLastToFront = true;

    void Awake()
    {
        I = this;
    }

    public void Layout()
    {
        int count = transform.childCount;
        if (count == 0) return;

        // 枚数が増えても“束の総幅”が maxSpreadX を超えないようにする
        float total = Mathf.Min(maxSpreadX, maxStepX * (count - 1));
        float stepX = (count <= 1) ? 0f : total / (count - 1);

        float startX = centerStack ? -total * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt == null) continue;

            rt.anchoredPosition = new Vector2(startX + i * stepX, i * stepY);

            // マナは傾き持ち越し事故が多いので基本まっすぐ
            rt.localRotation = Quaternion.Euler(0, 0, rotateZ);

            if (bringLastToFront) rt.SetAsLastSibling();
        }
    }
}
