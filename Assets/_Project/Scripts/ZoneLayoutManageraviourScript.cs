using UnityEngine;

public class ZoneLayoutManager : MonoBehaviour
{
    [Header("Assign these (RectTransform)")]
    public RectTransform P_HandArea;
    public RectTransform P_ManaArea;
    public RectTransform P_BattleArea;
    public RectTransform P_ShieldArea;

    public RectTransform E_HandArea;
    public RectTransform E_ManaArea;
    public RectTransform E_BattleArea;
    public RectTransform E_ShieldArea;

    [Header("Vertical layout tuning (0-1 normalized)")]
    [Range(0.05f, 0.40f)] public float handHeight = 0.22f;
    [Range(0.05f, 0.25f)] public float manaHeight = 0.12f;
    [Range(0.10f, 0.50f)] public float battleHeight = 0.26f;
    [Range(0.05f, 0.20f)] public float shieldHeight = 0.10f;

    [Range(0.00f, 0.05f)] public float gap = 0.01f; // vertical spacing between zones

    [Header("Side columns (0-1 normalized)")]
    [Range(0.10f, 0.30f)] public float shieldColumnWidth = 0.18f; // left column
    [Range(0.10f, 0.35f)] public float manaColumnWidth = 0.22f;   // right column
    [Range(0.00f, 0.05f)] public float sidePadding = 0.02f;       // padding from left/right edge
    [Range(0.00f, 0.08f)] public float centerPadding = 0.01f;     // gap between center and side columns

    void Start()
    {
        Apply();
    }

    [ContextMenu("Apply Layout")]
    public void Apply()
    {
        // -----------------------------
        // Vertical slices
        // -----------------------------
        // Player (bottom half): Hand -> Mana -> Battle
        float pHandYMin = 0f;
        float pHandYMax = pHandYMin + handHeight;

        float pManaYMin = pHandYMax + gap;
        float pManaYMax = pManaYMin + manaHeight;

        float pBattleYMin = pManaYMax + gap;
        float pBattleYMax = pBattleYMin + battleHeight;

        // Enemy (top half): Hand -> Mana -> Battle (top-down)
        float eHandYMax = 1f;
        float eHandYMin = eHandYMax - handHeight;

        float eManaYMax = eHandYMin - gap;
        float eManaYMin = eManaYMax - manaHeight;

        float eBattleYMax = eManaYMin - gap;
        float eBattleYMin = eBattleYMax - battleHeight;

        // -----------------------------
        // Horizontal regions (DM-style)
        // Left  : Shields (column)
        // Right : Mana (column)
        // Center: Battle (wide center)
        // Hands : full width (optional: you can make it center too if you prefer)
        // -----------------------------

        // Hands (full width)
        ApplyFullWidth(P_HandArea, pHandYMin, pHandYMax);
        ApplyFullWidth(E_HandArea, eHandYMin, eHandYMax);

        // Mana (right column)
        ApplyRightColumn(P_ManaArea, pManaYMin, pManaYMax);
        ApplyRightColumn(E_ManaArea, eManaYMin, eManaYMax);

        // Battle (center area)
        ApplyCenterArea(P_BattleArea, pBattleYMin, pBattleYMax);
        ApplyCenterArea(E_BattleArea, eBattleYMin, eBattleYMax);

        // Shields (left column) - near bottom-left / top-left
        //ApplyLeftColumnFixedHeight(P_ShieldArea, isTop: false);
        //ApplyLeftColumnFixedHeight(E_ShieldArea, isTop: true);
    }

    // ====== Layout helpers ======

    void ApplyFullWidth(RectTransform rt, float yMin, float yMax)
    {
        if (!rt) return;

        rt.anchorMin = new Vector2(0f, yMin);
        rt.anchorMax = new Vector2(1f, yMax);

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void ApplyLeftColumnFixedHeight(RectTransform rt, bool isTop)
    {
        if (!rt) return;

        float xMin = sidePadding;
        float xMax = xMin + shieldColumnWidth;

        float yMin, yMax;
        if (isTop)
        {
            yMax = 1f - sidePadding;
            yMin = yMax - shieldHeight;
        }
        else
        {
            yMin = sidePadding;
            yMax = yMin + shieldHeight;
        }

        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void ApplyRightColumn(RectTransform rt, float yMin, float yMax)
    {
        if (!rt) return;

        float xMax = 1f - sidePadding;
        float xMin = xMax - manaColumnWidth;

        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void ApplyCenterArea(RectTransform rt, float yMin, float yMax)
    {
        if (!rt) return;

        // Center = screen width minus left shields column minus right mana column (plus padding gaps)
        float xMin = sidePadding + shieldColumnWidth + centerPadding;
        float xMax = 1f - sidePadding - manaColumnWidth - centerPadding;

        // 念のため（幅が逆転したら安全に戻す）
        if (xMax <= xMin)
        {
            xMin = 0.20f;
            xMax = 0.80f;
        }

        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
