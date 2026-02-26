using UnityEngine;

public class PlayManaButton : MonoBehaviour
{
    public void PlaySelectedToMana()
    {
        // 選択カード取得
        var sel = SelectionManager.I?.selected;
        if (sel == null)
        {
            Debug.Log("No card selected.");
            return;
        }

        // ターン制限チェック
        if (TurnManager.I == null || !TurnManager.I.CanPlayMana())
        {
            Debug.Log("Cannot play mana this turn.");
            return;
        }

        // 手札 → マナ に移動
        if (sel.owner == OwnerType.Player && sel.currentZone == ZoneType.Hand)
        {
            bool ok = ZoneManager.I.Move(sel, ZoneType.Mana);

        if (ok)
        {
            // マナに置く時は傾きをリセット（任意）
            sel.transform.localRotation = Quaternion.identity;

            // 選択解除
            SelectionManager.I?.Clear();

            // このターンはマナ置き済みにする（1回だけ！）
            TurnManager.I?.OnPlayedMana();

            // マナは数字表示にするのでカードは非表示（ロジックは残る）
            sel.gameObject.SetActive(false);

            // マナ表示更新
            ManaCountUI.RefreshOwner(OwnerType.Player);

            Debug.Log("Played mana.");
        }
        }
    }
}
