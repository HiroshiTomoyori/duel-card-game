using UnityEngine;

public class ShieldBreakInput : MonoBehaviour
{
    public static ShieldBreakInput I { get; private set; }

    public GameObject enemyShieldHitPanel;   // E_ShieldArea/ShieldsRoot/HitPanel
    public GameObject playerShieldHitPanel;  // P_ShieldArea/ShieldsRoot/HitPanel

    public bool IsSelecting { get; private set; }
    public OwnerType TargetOwner { get; private set; }

    int remainingBreaks = 0;

    void Awake()
    {
        I = this;
        SetHitPanels(false, false);
        Debug.Log("[ShieldBreakInput] Awake");
    }

    public void BeginSelect(OwnerType targetOwner, int breakCount)
    {
        TargetOwner = targetOwner;
        remainingBreaks = Mathf.Max(1, breakCount);
        IsSelecting = true;

        SetHitPanels(targetOwner == OwnerType.Enemy, targetOwner == OwnerType.Player);

        Debug.Log($"[ShieldSelect] Begin target={TargetOwner} count={remainingBreaks}");
    }

    public void OnShieldClicked(CardController shieldCard)
    {
        Debug.Log($"[ShieldSelect] OnShieldClicked called IsSelecting={IsSelecting} Target={TargetOwner} cardOwner={(shieldCard? shieldCard.owner.ToString() : "null")}");
        if (!IsSelecting) return;
        if (shieldCard == null) return;
        if (shieldCard.owner != TargetOwner) return;

        Debug.Log("[ShieldSelect] Clicked: " + shieldCard.name);

        BreakSpecificShield(shieldCard);

        remainingBreaks--;
        if (remainingBreaks <= 0) EndSelect();
        else Debug.Log($"[ShieldSelect] Remaining={remainingBreaks}");
    }

    void EndSelect()
    {
        IsSelecting = false;
        remainingBreaks = 0;

        SetHitPanels(false, false);

        Debug.Log("[ShieldSelect] End");
    }

    void SetHitPanels(bool enemyOn, bool playerOn)
    {
        if (enemyShieldHitPanel) enemyShieldHitPanel.SetActive(enemyOn);
        if (playerShieldHitPanel) playerShieldHitPanel.SetActive(playerOn);
    }

    void BreakSpecificShield(CardController target)
    {
        if (BattleManager.I != null)
        {
            BattleManager.I.BreakSpecificShield(target);
            return;
        }

        target.ShowFront();
        var root = target.transform.parent;
        Destroy(target.gameObject);
        if (root) root.GetComponent<ShieldRowLayout>()?.Apply();
        ShieldCountUI.I?.Refresh();
    }
}