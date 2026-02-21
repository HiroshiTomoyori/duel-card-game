using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private Image image;

    void Awake()
    {
        // Resetに頼らず、実行時に必ず拾う
        if (image == null)
        {
            image = GetComponent<Image>();
            if (image == null)
                image = GetComponentInChildren<Image>(true);
        }

        if (image == null)
            Debug.LogError($"[CardView] Image reference missing on {name}", this);
    }

    public void SetSprite(Sprite sp)
    {
        if (image == null)
        {
            Debug.LogError($"[CardView] SetSprite called but image is NULL on {name}", this);
            return;
        }

        image.sprite = sp;
        image.preserveAspect = true;
        image.color = Color.white;
    }
}