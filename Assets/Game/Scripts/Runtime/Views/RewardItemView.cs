using DG.Tweening;
using Game.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    /// <summary>Icon plus stacked amount for one reward type.</summary>
    public sealed class RewardItemView : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text amount;

        public void Bind(Sprite sprite, int value)
        {
            icon.sprite = sprite;
            amount.text = Formatting.Amount(value);
        }

        public void Punch()
        {
            content.DOKill(true);
            content.DOPunchScale(Vector3.one * 0.2f, 0.35f, 8, 0.7f);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            content = UiRefs.Find<RectTransform>(this, "ui_pivot_reward_item");
            icon = UiRefs.Find<Image>(this, "ui_image_reward_icon_value");
            amount = UiRefs.Find<TMP_Text>(this, "ui_text_reward_amount_value");
        }
#endif
    }
}
