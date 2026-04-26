using UnityEngine;
using UnityEngine.UI;

public class AttackButtonUI : MonoBehaviour
{
    public static AttackButtonUI I { get; private set; }

    [SerializeField] Button attackButton;
    CardController attacker;

    void Awake()
    {
        I = this;

        if (!attackButton) attackButton = GetComponentInChildren<Button>(true);

        Debug.Log($"[AttackButtonUI] awake button={(attackButton ? attackButton.name : "NULL")}");

        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(OnClick);
        }

        gameObject.SetActive(false);
    }

    public void Show(CardController attackerCard)
    {
        Debug.Log("[AttackButtonUI] Show called");
        attacker = attackerCard;

        if (attacker == null)
        {
            Hide();
            return;
        }

        // 敵やバトルゾーン外のカードは出さない
        if (attacker.owner != OwnerType.Player || attacker.currentZone != ZoneType.Battle)
        {
            Hide();
            return;
        }

        // BattleManager側の判定（1ターン1回/召喚酔い/タップ）で弾く
        if (BattleManager.I == null || !BattleManager.I.CanAttackFromUI(attacker))
        {
            Debug.Log($"[AttackButtonUI] Show denied attacker={attacker.name} sick={attacker.SummoningSick} tapped={attacker.IsTapped}");
            Hide();
            return;
        }

        gameObject.SetActive(true);

        if (attackButton != null)
            attackButton.interactable = true;

        Debug.Log($"[AttackButtonUI] show attacker={attacker.name}");
    }



    public void Hide()
    {
        attacker = null;
        gameObject.SetActive(false);
    }

    void OnClick()
    {
        Debug.Log("[AttackButtonUI] clicked");

        if (attacker == null) { Hide(); return; }

        // ✅ 保険：押した瞬間に状態が変わってる可能性もある
        if (attacker.SummoningSick || attacker.IsTapped)
        {
            Debug.Log($"[AttackButtonUI] Click denied sick={attacker.SummoningSick} tapped={attacker.IsTapped}");
            Hide();
            return;
        }

        BattleManager.I?.DeclareAttack(attacker);
        Hide();
    }
}
