using UnityEngine;
using TMPro;

public class ManaCountUI : MonoBehaviour
{
    public static ManaCountUI I { get; private set; }

    [Header("Player")]
    public RectTransform playerManaArea;
    public TMP_Text playerManaText;

    [Header("Enemy")]
    public RectTransform enemyManaArea;
    public TMP_Text enemyManaText;

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
        if (playerManaArea && playerManaText)
            playerManaText.text = $"Mana: {CountCards(playerManaArea)}";

        if (enemyManaArea && enemyManaText)
            enemyManaText.text = $"Mana: {CountCards(enemyManaArea)}";
    }

    int CountCards(RectTransform area)
    {
        int c = 0;
        foreach (Transform child in area)
            if (child.GetComponent<CardController>() != null)
                c++;
        return c;
    }
}
