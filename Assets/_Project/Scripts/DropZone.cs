using UnityEngine;
using UnityEngine.UI;

public class DropZone : MonoBehaviour
{
    [Header("Zone")]
    public OwnerType owner;
    public ZoneType zoneType;

    [Header("Highlight (optional)")]
    [Tooltip("光らせるためのImage。未設定なら自分のImageを使う。")]
    public Image highlightImage;

    [Range(0f, 1f)]
    public float highlightAlpha = 0.35f;

    Image selfImage;
    Color baseColor;
    bool initialized;

    void Awake()
    {
        InitIfNeeded();
        Highlight(false);
    }

    void InitIfNeeded()
    {
        if (initialized) return;

        selfImage = GetComponent<Image>();

        // highlightImage が未設定なら自分の Image を使う
        if (highlightImage == null)
            highlightImage = selfImage;

        if (highlightImage != null)
            baseColor = highlightImage.color;

        initialized = true;
    }

    public void Highlight(bool on)
    {
        InitIfNeeded();
        if (highlightImage == null) return;

        // 「色はそのまま、Alphaだけ変える」方式（背景を壊しにくい）
        var c = highlightImage.color;

        if (on)
        {
            // 緑寄せ（必要なければこの3行消してOK）
            c.r = 0.2f;
            c.g = 1.0f;
            c.b = 0.4f;
            c.a = highlightAlpha;
        }
        else
        {
            // 元の色に戻す
            c = baseColor;

            // もし「元が不透明で背景画像が消える」なら、透明だけ戻す方式にする：
            // c.a = 0f;
        }

        highlightImage.color = c;
    }
}