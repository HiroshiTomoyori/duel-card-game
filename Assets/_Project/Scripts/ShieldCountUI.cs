using UnityEngine;
using TMPro;

public class ShieldCountUI : MonoBehaviour
{
    public static ShieldCountUI I { get; private set; }

    [Header("Player")]
    public RectTransform playerShieldArea;
    public TMP_Text playerShieldText;

    [Header("Enemy")]
    public RectTransform enemyShieldArea;
    public TMP_Text enemyShieldText;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        int p = CountZone(OwnerType.Player, ZoneType.Shield);
        int e = CountZone(OwnerType.Enemy,  ZoneType.Shield);

        if (playerShieldText) playerShieldText.text = $"Shield: {p}";
        if (enemyShieldText)  enemyShieldText.text  = $"Shield: {e}";

        Debug.Log($"[ShieldUI] P={p} E={e}");
    }

    int CountZone(OwnerType owner, ZoneType zone)
    {
        var list = ZoneManager.I != null ? ZoneManager.I.GetCards(owner, zone) : null;
        if (list == null) return 0;

        int n = 0;
        foreach (var c in list)
            if (c != null) n++;
        return n;
    }


 /*   public void Add(int delta)
    {
        count += delta;
        if (count < 0) count = 0;
        Refresh();
    }
*/
}
