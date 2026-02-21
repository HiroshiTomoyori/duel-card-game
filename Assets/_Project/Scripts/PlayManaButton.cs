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
                // マナに置く時は傾きをリセット
                sel.transform.localRotation = Quaternion.identity;

                SelectionManager.I.Clear();
                TurnManager.I.OnPlayedMana();
                Debug.Log("Played mana.");
                // 選択解除
                SelectionManager.I.Clear();

                // このターンはマナ置き済みにする
                TurnManager.I.OnPlayedMana();

                Debug.Log("Played mana.");

                    // マナは数字表示にするので、カードは非表示（ロジックは残る）
                sel.gameObject.SetActive(false);

                SelectionManager.I.Clear();
                TurnManager.I.OnPlayedMana();

                ManaCountUI.I?.Refresh();

                Debug.Log("Played mana.");
            }
        }
    }
}
