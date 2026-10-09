using Game.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wheel.Rules;

namespace Game.Runtime.Views
{
    /// <summary>One number on the zone strip.</summary>
    public sealed class ZoneCellView : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text number;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color safeColor = new(0.45f, 0.85f, 0.2f);
        [SerializeField] Color superColor = new(1f, 0.6f, 0.1f);
        [SerializeField] Color currentColor = Color.black;

        public void Show(int zone, WheelTier tier, bool isCurrent)
        {
            number.enabled = true;
            number.text = zone.ToString();
            background.enabled = isCurrent;
            number.color = isCurrent ? currentColor : tier switch
            {
                WheelTier.Silver => safeColor,
                WheelTier.Golden => superColor,
                _ => normalColor,
            };
        }

        public void Hide()
        {
            number.enabled = false;
            background.enabled = false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            background = UiRefs.Find<Image>(this, "ui_image_zone_cell_bg_value");
            number = UiRefs.Find<TMP_Text>(this, "ui_text_zone_number_value");
        }
#endif
    }
}
