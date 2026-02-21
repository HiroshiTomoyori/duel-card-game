using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager I { get; private set; }

    public CardController selected { get; private set; }

    void Awake()
    {
        I = this;
    }

    public void Select(CardController card)
    {
        if (selected == card)
        {
            selected.SetSelectedVisual(false);
            selected = null;
            return;
        }

        if (selected != null) selected.SetSelectedVisual(false);

        selected = card;
        selected.SetSelectedVisual(true);
    }

    public void Clear()
    {
        if (selected != null) selected.SetSelectedVisual(false);
        selected = null;
    }
}
