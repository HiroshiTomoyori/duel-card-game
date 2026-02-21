using UnityEngine;

public class ShieldRowLayout : MonoBehaviour
{
    [Header("Base card size (same as hand)")]
    public Vector2 cardSize = new Vector2(100, 150);

    [Header("Spacing between cards (negative = overlap)")]
    public float spacing = -10f;   // ← 少し重ねる

    [Header("Mirror for enemy side")]
    public bool mirror = false;

    [Header("Shield scale")]
    [Range(0.3f, 1f)]
    public float scale = 0.78f;    // ← 少し大きめ

    public void Apply()
    {
        int n = transform.childCount;
        if (n == 0) return;

        // スケールを考慮した横幅
        float step = (cardSize.x * scale) + spacing;
        float total = step * (n - 1);
        float startX = -total * 0.5f;

        for (int i = 0; i < n; i++)
        {
            int index = mirror ? (n - 1 - i) : i;

            var rt = transform.GetChild(index) as RectTransform;
            if (!rt) continue;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = cardSize;
            rt.anchoredPosition = new Vector2(startX + step * i, 0f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * scale;
        }
    }

    void OnEnable() => Apply();

#if UNITY_EDITOR
    void OnValidate() => Apply();
#endif
}