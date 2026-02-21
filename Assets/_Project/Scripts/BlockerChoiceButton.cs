using System;
using UnityEngine;
using UnityEngine.UI;

public class BlockerChoiceButton : MonoBehaviour
{
    [SerializeField] Image icon;

    CardController card;
    Action<CardController> onClick;

    public void Setup(CardController c, Action<CardController> onClick)
    {
        card = c;
        this.onClick = onClick;

        if (icon != null && c != null && c.instance != null)
            icon.sprite = c.instance.sprite;
    }

    public void OnClick()
    {
        Debug.Log("[BlockUI] Button clicked: " + (card ? card.name : "null"));


        onClick?.Invoke(card);
    }
}
