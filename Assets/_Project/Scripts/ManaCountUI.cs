using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ManaCountUI : MonoBehaviour
{
    // OwnerごとにUIを保持（Player/Enemyで共存できる）
    public static readonly Dictionary<OwnerType, ManaCountUI> ByOwner = new();

    [Header("Assign")]
    public OwnerType owner = OwnerType.Player;
    public RectTransform manaArea;   // P_ManaArea / E_ManaArea
    public TMP_Text manaText;        // Txt_ManaCount / Txt_EnemyManaCount

    void Awake()
    {
        ByOwner[owner] = this;
        // Debug.Log($"[ManaCountUI] Awake owner={owner} name={name}");
    }

    void OnDestroy()
    {
        if (ByOwner.TryGetValue(owner, out var v) && v == this)
            ByOwner.Remove(owner);
    }

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!manaArea || !manaText) return;

        int total = 0;
        foreach (Transform child in manaArea)
            if (child.GetComponent<CardController>() != null) total++;

        int available = 0;
        var manaCards = ZoneManager.I ? ZoneManager.I.GetCards(owner, ZoneType.Mana) : null;
        if (manaCards != null)
        {
            foreach (var c in manaCards)
            {
                if (c == null) continue;
                if (!c.IsTapped) available++;
            }
        }

        manaText.text = $"Mana: {available}/{total}";
    }

    public static void RefreshOwner(OwnerType o)
    {
        if (ByOwner.TryGetValue(o, out var ui) && ui != null)
            ui.Refresh();
    }
}