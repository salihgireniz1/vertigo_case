using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Core;
using Game.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Views
{
    /// <summary>Horizontal zone numbers with the current zone centred. Slides one cell per zone step.</summary>
    public sealed class ZoneStripView : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] HorizontalLayoutGroup layout;
        [SerializeField] ZoneCellView[] cells;
        [SerializeField, Min(0.05f)] float slideDuration = 0.35f;

        int shownZone = -1;

        public async UniTask ShowZoneAsync(int zone, Func<int, WheelTier> tierOf, CancellationToken cancellationToken)
        {
            var slide = shownZone >= 0 && zone - shownZone == 1; // first draw, restart or revive jump: no slide
            Fill(zone, tierOf);
            if (!slide) return;

            // New labels are already in place; start one cell to the right and slide back to centre.
            var cellWidth = ((RectTransform)cells[0].transform).rect.width + layout.spacing;
            content.DOKill();
            content.anchoredPosition = new Vector2(cellWidth, 0f);
            await content.DOAnchorPosX(0f, slideDuration).SetEase(Ease.OutCubic).ToUniTask(cancellationToken: cancellationToken);
        }

        void Fill(int zone, Func<int, WheelTier> tierOf)
        {
            var half = cells.Length / 2;
            for (var i = 0; i < cells.Length; i++)
            {
                var cellZone = zone - half + i;
                if (cellZone < 1) cells[i].Hide();
                else cells[i].Show(cellZone, tierOf(cellZone), cellZone == zone);
            }
            shownZone = zone;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            content = UiRefs.Find<RectTransform>(this, "ui_content_zone_cells");
            layout = content != null ? content.GetComponent<HorizontalLayoutGroup>() : null;
            cells = content != null ? content.GetComponentsInChildren<ZoneCellView>(true) : new ZoneCellView[0];
        }
#endif
    }
}
