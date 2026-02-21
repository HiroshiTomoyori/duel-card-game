using System;
using UnityEngine;
using UnityEngine.UI;

public class JokerTriggerChoiceUI : MonoBehaviour
{
    public Button btnSummon;
    public Button btnMagic;
    public Button btnKeep;

    Action _onSummon;
    Action _onMagic;
    Action _onKeep;

    void Awake()
    {
        gameObject.SetActive(false);

        if (btnSummon) btnSummon.onClick.AddListener(() => { Hide(); _onSummon?.Invoke(); });
        if (btnMagic)  btnMagic.onClick.AddListener(() => { Hide(); _onMagic?.Invoke(); });
        if (btnKeep)   btnKeep.onClick.AddListener(() => { Hide(); _onKeep?.Invoke(); });
    }

    public void Show(Action onSummon, Action onMagic, Action onKeep)
    {
        _onSummon = onSummon;
        _onMagic  = onMagic;
        _onKeep   = onKeep;

        gameObject.SetActive(true);
    }

    void Hide()
    {
        gameObject.SetActive(false);
        _onSummon = _onMagic = _onKeep = null;
    }
}
