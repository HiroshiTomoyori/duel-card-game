using UnityEngine;
using TMPro;

public class EnemyHandCountUI : MonoBehaviour
{
    public static EnemyHandCountUI I { get; private set; }

    [Header("Assign")]
    public RectTransform enemyHandArea; // E_HandArea
    public TMP_Text enemyHandText;      // "Hand: 0"

    void Awake() => I = this;

    void Start() => Refresh();

    public void Refresh()
    {
        if (!enemyHandArea || !enemyHandText) return;

        int count = 0;
        foreach (Transform child in enemyHandArea)
            if (child.GetComponent<CardController>() != null) count++;

        enemyHandText.text = $"Hand: {count}";
    }
}