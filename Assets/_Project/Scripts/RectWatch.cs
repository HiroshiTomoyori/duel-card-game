using UnityEngine;

public class RectWatch : MonoBehaviour
{
    RectTransform rt;
    Vector2 lastPos, lastSize;
    Vector2 lastMin, lastMax;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        lastPos = rt.anchoredPosition;
        lastSize = rt.sizeDelta;
        lastMin = rt.anchorMin;
        lastMax = rt.anchorMax;
    }

    void LateUpdate()
    {
        if (rt.anchoredPosition != lastPos || rt.sizeDelta != lastSize ||
            rt.anchorMin != lastMin || rt.anchorMax != lastMax)
        {
            Debug.Log($"[RectWatch] {name} changed: pos {lastPos}->{rt.anchoredPosition} size {lastSize}->{rt.sizeDelta} anchor {lastMin},{lastMax}->{rt.anchorMin},{rt.anchorMax}");
            lastPos = rt.anchoredPosition;
            lastSize = rt.sizeDelta;
            lastMin = rt.anchorMin;
            lastMax = rt.anchorMax;
        }
    }
}