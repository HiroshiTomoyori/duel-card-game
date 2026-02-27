using UnityEngine;

[ExecuteAlways]
public class RectFollow : MonoBehaviour
{
    public RectTransform target; // ShieldsRoot
    RectTransform rt;

    void Awake() => rt = GetComponent<RectTransform>();

    void LateUpdate()
    {
        if (!rt || !target) return;
        rt.anchorMin = target.anchorMin;
        rt.anchorMax = target.anchorMax;
        rt.pivot     = target.pivot;
        rt.anchoredPosition = target.anchoredPosition;
        rt.sizeDelta = target.sizeDelta;
        rt.localScale = Vector3.one;
    }
}