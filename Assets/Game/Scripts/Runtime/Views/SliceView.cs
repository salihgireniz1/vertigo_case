using DG.Tweening;
using Game.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    /// <summary>One chamber of the wheel. The root is placed by the wheel; only the content pivot animates.</summary>
    public sealed class SliceView : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text amount;

        public void Bind(Sprite sprite, string label)
        {
            icon.sprite = sprite;
            amount.text = label;
            amount.enabled = !string.IsNullOrEmpty(label);
        }

        /// <summary>Counter-rotates the content so icons stay upright while the wheel turns.</summary>
        public void KeepUpright(float wheelAngle) => content.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);

        public void PunchHighlight()
        {
            content.DOKill(true);
            content.DOPunchScale(Vector3.one * 0.35f, 0.6f, 6, 0.6f).SetLink(gameObject);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            content = UiRefs.Find<RectTransform>(this, "ui_pivot_slice_content");
            icon = UiRefs.Find<Image>(this, "ui_image_slice_icon_value");
            amount = UiRefs.Find<TMP_Text>(this, "ui_text_slice_amount_value");
        }
#endif
    }
}
