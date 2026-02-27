using UnityEngine;

public class ShieldGlowSystem : MonoBehaviour
{
    public static ShieldGlowSystem I { get; private set; }

    public ZoneGlow playerShieldGlow;
    public ZoneGlow enemyShieldGlow;

    void Awake() => I = this;

    public void SetSelecting(OwnerType owner, bool on)
    {
            Debug.Log($"[GlowSystem] SetSelecting owner={owner} on={on} " +
              $"p={(playerShieldGlow ? playerShieldGlow.name : "NULL")} " +
              $"e={(enemyShieldGlow ? enemyShieldGlow.name : "NULL")}");
        if (owner == OwnerType.Player) playerShieldGlow?.SetGlow(on);
        else enemyShieldGlow?.SetGlow(on);
    }

    public void ClearAll()
    {
        playerShieldGlow?.SetGlow(false);
        enemyShieldGlow?.SetGlow(false);
    }
}