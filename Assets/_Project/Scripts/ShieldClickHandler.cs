using UnityEngine;
using UnityEngine.EventSystems;

public class ShieldClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CardController cc;

    void Awake()
    {
        cc = GetComponent<CardController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked: " + name);

        if (!ShieldBreakInput.I) return;
        ShieldBreakInput.I.OnShieldClicked(cc);
    }
}