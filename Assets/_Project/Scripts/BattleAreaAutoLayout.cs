using UnityEngine;

public class BattleAreaAutoLayout : MonoBehaviour
{
    public RectTransform enemyBattleArea;
    public RectTransform playerBattleArea;

    void Awake()
    {
        Apply(enemyBattleArea, 0.60f, 0.90f);
        Apply(playerBattleArea, 0.10f, 0.40f);
    }

    void Apply(RectTransform rt, float minY, float maxY)
    {
        if (!rt) return;

        rt.anchorMin = new Vector2(0f, minY);
        rt.anchorMax = new Vector2(1f, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
