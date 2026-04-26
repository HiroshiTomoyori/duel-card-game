using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CardView))]
public class CardController : MonoBehaviour, IPointerClickHandler
{
    public OwnerType owner;
    public ZoneType currentZone;

    public CardInstance instance { get; private set; }

    [SerializeField] Sprite backSprite;

    public int Cost => instance != null ? instance.cost : 0;
    public int Power => Cost;

    public bool IsBlocker => instance != null && instance.canBlock;
    public bool CanAttackBase => instance != null && instance.canAttack;
    public bool IgnoreSummonSickness => instance != null && instance.ignoreSummonSickness;
    public bool IsSpell => instance != null && instance.type == CardType.Spell;

    private CardView view;
    private RectTransform rt;
    private Image img;

    public bool IsTapped { get; private set; }
    public bool SummoningSick { get; private set; } = true;

    void Awake()
    {
        view = GetComponent<CardView>();
        rt = transform as RectTransform;
        img = GetComponentInChildren<Image>();

        if (backSprite == null)
            backSprite = Resources.Load<Sprite>("Art/back");
    }

    // ================================
    // Bind（データセット）
    // ================================
    public void Bind(CardInstance inst)
    {
        instance = inst;

        if (inst != null && inst.sprite != null)
            view.SetSprite(inst.sprite);

        // ★重要：Bind後は必ずゾーンルールを再適用
        ApplyZoneVisual();
    }

    // ================================
    // クリック処理
    // ================================
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("カードクリックされた");
        // シールド選択中
        if (ShieldBreakInput.I != null && ShieldBreakInput.I.IsSelecting)
        {
            if (currentZone == ZoneType.Shield &&
                owner == ShieldBreakInput.I.TargetOwner)
            {
                ShieldBreakInput.I.OnShieldClicked(this);
            }
            return;
        }

        // 敵手札はクリック無効
        if (owner == OwnerType.Enemy && currentZone == ZoneType.Hand)
            return;

        // 特殊召喚
        if (SpecialSummonTargetSystem.I != null &&
            SpecialSummonTargetSystem.I.IsSelecting)
        {
            SpecialSummonTargetSystem.I.TrySelectBase(this);
            return;
        }

        // スペル対象選択
        if (SpellTargetSystem.I != null &&
            SpellTargetSystem.I.IsSelecting)
        {
            SpellTargetSystem.I.TrySelectTarget(this);
            return;
        }

        // プレイヤー手札
        if (owner == OwnerType.Player &&
            currentZone == ZoneType.Hand)
        {
            SelectionManager.I?.Select(this);
            return;
        }

        // プレイヤーバトル
        if (owner == OwnerType.Player &&
            currentZone == ZoneType.Battle)
        {
            if (CanAttackBase && !SummoningSick && !IsTapped)
                AttackButtonUI.I?.Show(this);
            else
                AttackButtonUI.I?.Hide();
        }
    }

    // ================================
    // 見た目制御
    // ================================
    public void ApplyZoneVisual()
    {
        // 基本サイズ
        transform.localScale = Vector3.one;

        // ① シールド（両者共通）
        if (currentZone == ZoneType.Shield)
        {
            transform.localScale = Vector3.one * 0.8f;
            ShowBack();
            return;
        }

        // ② 手札
        if (currentZone == ZoneType.Hand)
        {
            if (owner == OwnerType.Enemy)
            {
                transform.localScale = Vector3.one * 0.8f; // ★敵手札だけ小さく
                ShowBack();                                // 敵は裏
            }
            else
            {
                ShowFront();                               // プレイヤーは通常サイズ
            }
            return;
        }

        // ③ その他ゾーン
        ShowFront();
    }

    public void ShowBack()
    {
        if (backSprite == null)
        {
            Debug.LogWarning($"[ShowBack] backSprite null on {name}");
            return;
        }

        view.SetSprite(backSprite);
    }

    public void ShowFront()
    {
        if (instance == null) return;
        view.SetSprite(instance.sprite);
    }

    void SetRaycast(bool enabled)
    {
        if (img != null)
            img.raycastTarget = enabled;
    }

    // ================================
    // 状態管理
    // ================================
    public void SetTapped(bool v)
    {
        IsTapped = v;
        if (rt != null)
            rt.localRotation = v
                ? Quaternion.Euler(0, 0, 90f)
                : Quaternion.identity;
    }

    public void SetSummoningSick(bool v)
    {
        SummoningSick = v;
    }

    public void SetSelectedVisual(bool selected)
    {
        if (rt == null) return;

        var p = rt.anchoredPosition;
        p.y = selected ? 30f : 0f;
        rt.anchoredPosition = p;
    }

    void OnDisable()
    {
        Debug.LogWarning($"[Card Disabled] {name} owner={owner} zone={currentZone}");
    }

    public void ShowBack(Sprite sprite)
    {
        if (sprite != null) backSprite = sprite;
        ShowBack();
    }
}