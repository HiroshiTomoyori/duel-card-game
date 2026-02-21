using UnityEngine;
using UnityEngine.UI;

public class EndTurnButton : MonoBehaviour
{
    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            Debug.Log("[UI] EndTurn clicked");
            if (TurnManager.I != null) TurnManager.I.EndTurn(); // ←あなたの関数名に合わせる
            else Debug.LogError("[UI] TurnManager.I is null");
        });
    }
}