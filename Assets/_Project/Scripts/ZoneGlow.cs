using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ZoneGlow : MonoBehaviour
{
    [SerializeField] private Image img;
    [SerializeField, Range(0f, 1f)] private float onAlpha = 0.35f;
    [SerializeField] private bool debugLogs = false;

    void Reset() => img = GetComponent<Image>();

    void Awake()
    {
        if (!img) img = GetComponent<Image>();
        img.raycastTarget = false; // クリックを邪魔しない

        // 初期状態はOFF
        img.enabled = false;

        if (debugLogs) Debug.Log($"[ZoneGlow] Awake name={name} img={(img ? "OK" : "NULL")} enabled={img.enabled}");
    }

    public void SetGlow(bool on)
    {
        if (!img)
        {
            if (debugLogs) Debug.LogWarning($"[ZoneGlow] SetGlow({on}) but img is NULL name={name}");
            return;
        }

        // ✅ まず表示/非表示を確実化
        img.enabled = on;

        // ✅ ONのときは必ず見えるAlphaに
        var c = img.color;
        c.a = on ? onAlpha : 0f;
        img.color = c;

        if (debugLogs)
            Debug.Log($"[ZoneGlow] SetGlow({on}) name={name} enabled={img.enabled} alpha={img.color.a}");
    }
}