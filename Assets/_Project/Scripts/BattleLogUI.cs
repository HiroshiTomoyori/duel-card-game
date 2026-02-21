using TMPro;
using UnityEngine;

public class BattleLogUI : MonoBehaviour
{
    public static BattleLogUI I { get; private set; }

    [SerializeField] TMP_Text text;

    void Awake()
    {
        I = this;
        if (!text) text = GetComponent<TMP_Text>();

        // Clear();  ← これをやめる
        Set("BattleLog Ready"); // とりあえず起動確認用
    }


    public void Set(string msg)
    {
        if (!text) return;
        text.text = msg;
    }

    public void Clear()
    {
        Set("");
    }

    public static string CardLabel(CardController c)
    {
        if (c == null) return "(null)";
        return $"{c.owner} Power:{c.Power}";
    }
}
