using UnityEngine;

public class ShieldBreakInput : MonoBehaviour
{
    public static ShieldBreakInput I { get; private set; }

    [Header("Assign (HitPanels)")]
    public GameObject enemyShieldHitPanel;   // E_ShieldArea/ShieldsRoot/HitPanel
    public GameObject playerShieldHitPanel;  // P_ShieldArea/ShieldsRoot/HitPanel

    [Header("Debug")]
    public bool debugLogs = true;

    // ✅ HitPanelが発光を隠してるか切り分けたい時にON（テスト用）
    // ONにすると BeginSelect の直後に HitPanel をOFFにして、発光が見えるか確認できる
    public bool debugDisableHitPanelDuringSelect = false;

    public bool IsSelecting { get; private set; }
    public OwnerType TargetOwner { get; private set; }

    int remainingBreaks = 0;

    void Awake()
    {
        I = this;

        SetHitPanels(false, false);

        // ✅ 起動時は発光を全部OFFしたいが、Awake順でGlowSystemがまだ居ない可能性があるのでログだけ
        if (debugLogs)
            Debug.Log($"[ShieldBreakInput] Awake  GlowSystem={(ShieldGlowSystem.I ? "OK" : "NULL")}");

        // ※ ここでClearAllしたい気持ちは分かるけど、初期化順問題があるのでStartに寄せるのが安全
        // ShieldGlowSystem.I?.ClearAll();
    }

    void Start()
    {
        // ✅ StartならGlowSystemのAwakeが終わってる可能性が高い
        var glow = GetGlow();
        if (glow != null)
        {
            glow.ClearAll();
            if (debugLogs) Debug.Log("[ShieldBreakInput] Start -> Glow ClearAll()");
        }
        else
        {
            if (debugLogs) Debug.LogWarning("[ShieldBreakInput] Start -> GlowSystem NOT FOUND (ClearAll skipped)");
        }
    }

    ShieldGlowSystem GetGlow()
    {
        if (ShieldGlowSystem.I != null) return ShieldGlowSystem.I;

        // ✅ 非アクティブ含めて探す（初期化順/置き忘れ対策）
        var found = FindFirstObjectByType<ShieldGlowSystem>(FindObjectsInactive.Include);
        if (debugLogs)
            Debug.Log($"[ShieldBreakInput] GetGlow() I is NULL -> Find => {(found ? "FOUND" : "NOT FOUND")}");
        return found;
    }

    public void BeginSelect(OwnerType targetOwner, int breakCount)
    {
        TargetOwner = targetOwner;
        remainingBreaks = Mathf.Max(1, breakCount);
        IsSelecting = true;

        // HitPanel ON
        bool enemyOn = targetOwner == OwnerType.Enemy;
        bool playerOn = targetOwner == OwnerType.Player;
        SetHitPanels(enemyOn, playerOn);

        var glow = GetGlow();

        if (debugLogs)
        {
            Debug.Log(
                $"[ShieldSelect] Begin " +
                $"target={TargetOwner} count={remainingBreaks} " +
                $"GlowSystem={(glow ? "OK" : "NULL")} " +
                $"HitPanel(enemy={enemyOn}, player={playerOn}) " +
                $"enemyPanelActive={(enemyShieldHitPanel ? enemyShieldHitPanel.activeSelf : false)} " +
                $"playerPanelActive={(playerShieldHitPanel ? playerShieldHitPanel.activeSelf : false)}"
            );
        }

        // ✅ 選択開始：シールドエリア発光ON
        if (glow != null)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Glow ON call");
            glow.SetSelecting(TargetOwner, true);
        }
        else
        {
            if (debugLogs) Debug.LogWarning("[ShieldSelect] GlowSystem is NULL -> cannot Glow ON");
        }

        // ✅ テスト：HitPanelが発光を隠していないか切り分け
        if (debugDisableHitPanelDuringSelect)
        {
            if (debugLogs) Debug.LogWarning("[ShieldSelect][TEST] debugDisableHitPanelDuringSelect ON -> turning HitPanels OFF (visual test)");
            SetHitPanels(false, false);
        }
    }

    public void OnShieldClicked(CardController shieldCard)
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[ShieldSelect] OnShieldClicked " +
                $"IsSelecting={IsSelecting} Target={TargetOwner} " +
                $"card={(shieldCard ? shieldCard.name : "null")} " +
                $"cardOwner={(shieldCard ? shieldCard.owner.ToString() : "null")} " +
                $"cardZone={(shieldCard ? shieldCard.currentZone.ToString() : "null")}"
            );
        }

        if (!IsSelecting)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Click ignored: IsSelecting=false");
            return;
        }

        if (shieldCard == null)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Click ignored: shieldCard=null");
            return;
        }

        if (shieldCard.owner != TargetOwner)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Click ignored: different owner");
            return;
        }

        // （保険）シールド以外を押してる事故防止
        if (shieldCard.currentZone != ZoneType.Shield)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Click ignored: card is not in Shield zone");
            return;
        }

        if (debugLogs) Debug.Log("[ShieldSelect] Click accepted -> BreakSpecificShield");

        BreakSpecificShield(shieldCard);

        remainingBreaks--;
        if (debugLogs) Debug.Log($"[ShieldSelect] After break -> remaining={remainingBreaks}");

        if (remainingBreaks <= 0) EndSelect();
    }

    void EndSelect()
    {
        if (debugLogs)
            Debug.Log($"[ShieldSelect] End called  Target={TargetOwner} remaining={remainingBreaks}  GlowSystem={(GetGlow() ? "OK" : "NULL")}");

        // ✅ 選択終了：シールドエリア発光OFF
        var glow = GetGlow();
        if (glow != null)
        {
            if (debugLogs) Debug.Log("[ShieldSelect] Glow OFF call");
            glow.SetSelecting(TargetOwner, false);
        }

        IsSelecting = false;
        remainingBreaks = 0;

        SetHitPanels(false, false);

        if (debugLogs) Debug.Log("[ShieldSelect] End finished (IsSelecting=false, HitPanels OFF)");
    }

    void SetHitPanels(bool enemyOn, bool playerOn)
    {
        if (enemyShieldHitPanel) enemyShieldHitPanel.SetActive(enemyOn);
        if (playerShieldHitPanel) playerShieldHitPanel.SetActive(playerOn);

        if (debugLogs)
        {
            Debug.Log(
                $"[ShieldBreakInput] SetHitPanels enemyOn={enemyOn} playerOn={playerOn} " +
                $"enemyPanel={(enemyShieldHitPanel ? enemyShieldHitPanel.activeSelf : false)} " +
                $"playerPanel={(playerShieldHitPanel ? playerShieldHitPanel.activeSelf : false)}"
            );
        }
    }

    void BreakSpecificShield(CardController target)
    {
        if (debugLogs)
            Debug.Log($"[ShieldSelect] BreakSpecificShield target={(target ? target.name : "null")}  BattleManager={(BattleManager.I ? "OK" : "NULL")}");

        if (BattleManager.I != null)
        {
            BattleManager.I.BreakSpecificShield(target);
            return;
        }

        // フォールバック（単体テスト用）
        target.ShowFront();
        var root = target.transform.parent;
        Destroy(target.gameObject);
        if (root) root.GetComponent<ShieldRowLayout>()?.Apply();
        ShieldCountUI.I?.Refresh();

        if (debugLogs) Debug.Log("[ShieldSelect] BreakSpecificShield fallback finished");
    }
}